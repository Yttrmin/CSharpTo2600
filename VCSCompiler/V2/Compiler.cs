﻿#nullable enable
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Mono.Cecil;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using VCSFramework.V2;

namespace VCSCompiler.V2
{
    public sealed class Compiler
    {
        private readonly AssemblyDefinition FrameworkAssembly;
        private readonly AssemblyDefinition UserAssembly;
        private readonly CompilerOptions Options;

        private Compiler(AssemblyDefinition frameworkAssembly, AssemblyDefinition userAssemblyDefinition, CompilerOptions options)
        {
            FrameworkAssembly = frameworkAssembly;
            UserAssembly = userAssemblyDefinition;
            Options = options;
        }

        public static RomInfo CompileFromFile(string sourcePath, CompilerOptions options)
        {
            /**
             * Assumptions:
             * 1) This program is single threaded, there will never be a justification to multi-thread it.
             * 2) CIL will be processed instead of C#, since it's much easier to translate to 6502 ASM than a syntax tree.
             * 
             * Problems:
             * 1) To push a local, we need to know its size, because that's gonna change the number of LDA/PHA performed.
             * 2) Popping a local has a similar problem.
             * 3) Knowing when to emit a label. Normally only know when an instruction (often later than the label is declared) branches to it.
             * 
             * Approach Option #1:
             * 1) All we really is for the entry point to get compiled, so let's cut to the chase. No processing a list of types,
             *  or building up call trees. Just start compiling the entry point.
             * 2) Any unknown methods encountered have a label generated and a JSR emitted to that label.
             *  a) Inlined methods - TODO
             * 3) Any unknown locals encountered have macros emitted to push/pop X number of bytes. X is a label representing
             *  the byte size of the type.
             * 4) We now have a CIL-compiled (though probably macro-filled) entry point, and a bunch of labels that need values.
             *  a) For every method label, compile the corresponding method just as the entry point was. This may result in more labels
             *     being generated, which is fine.
             *  b) Every method (excluding those never called) is now compiled and has an associated label. For every variable 
             *     size label, "compile" the associated type (determine size, base type, field/method members). Update size labels
             *     with value for actual size. Base type and member data is more for organization and information than code generation.
             *  c) Now we probably have an opportunity to optimize label values. E.g., see if memory locations can be reused depending on
             *     callers. Probably fine to save that for later though.
             * 5) Write the .asm file in a reasonable order. Entry point first, group types together, etc.
             * 6) In the end, the generated .asm may look like an intermediate representation, with lots of parameterized macro
             *  calls defining common tasks, rather than forcing the compiler to implement them and introduce more ASM-rewriting.
             */
            var frameworkPath = options.FrameworkPath;
            var compilation = CompilationCreator.CreateFromFilePaths(new[] { sourcePath });
            var userAssemblyDefinition = GetAssemblyDefinition(compilation, out var assemblyStream);
            var frameworkAssembly = Assembly.Load(new AssemblyName(Path.GetFileNameWithoutExtension(frameworkPath)));
            var frameworkAssemblyDefinition = AssemblyDefinition.ReadAssembly(frameworkPath, new ReaderParameters { ReadSymbols = true });
            
            var compiler = new Compiler(frameworkAssemblyDefinition, userAssemblyDefinition, options);
            var entryPointBody = compiler.CompileEntryPoint();
            entryPointBody = compiler.Optimize(entryPointBody);
            entryPointBody = compiler.GenerateStackOps(entryPointBody);
            var labelMap = new LabelMap(new[] { entryPointBody }, new[] { frameworkAssemblyDefinition, userAssemblyDefinition }.ToImmutableArray());

            var assemblyWriter = new AssemblyWriter(new()
            {
                { userAssemblyDefinition.MainModule.EntryPoint, entryPointBody }
            }, labelMap);

            RomInfo romInfo;
            
            if (options.OutputPath != null)
            {
                romInfo = assemblyWriter.WriteToFile(options.OutputPath);
            }
            else
            {
                romInfo = assemblyWriter.WriteToConsole();
            }

            if (options.TextEditorPath != null && romInfo.AssemblyPath != null)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = options.TextEditorPath,
                        Arguments = romInfo.AssemblyPath
                    });
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to open text editor at {options.TextEditorPath} with ASM file {romInfo.AssemblyPath} because: {e.Message}");
                }
            }

            if (options.EmulatorPath != null && romInfo.RomPath != null)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = options.EmulatorPath,
                        Arguments = romInfo.RomPath
                    });
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to open emulator at {options.EmulatorPath} with BIN file {romInfo.RomPath} because: {e.Message}");
                }
            }

            var final = romInfo.IsSuccessful ? "Compilation succeeded." : "Compilation failed";
            Console.WriteLine(final);
            return romInfo;
        }

        private static AssemblyDefinition GetAssemblyDefinition(
            CSharpCompilation compilation, 
            out MemoryStream assemblyStream)
        {
            assemblyStream = new MemoryStream();
            var emitOptions = new EmitOptions(debugInformationFormat: DebugInformationFormat.Embedded);
            var result = compilation.Emit(assemblyStream, options: emitOptions);
            if (!result.Success)
            {
                foreach (var diagnostic in result.Diagnostics)
                {
                    Console.WriteLine(diagnostic.ToString());
                }
                throw new FatalCompilationException("Failed to emit compiled assembly.");
            }
            assemblyStream.Position = 0;

            var parameters = new ReaderParameters { ReadSymbols = true };
            return AssemblyDefinition.ReadAssembly(assemblyStream, parameters);
        }

        public ImmutableArray<AssemblyEntry> CompileEntryPoint()
        {
            var entryPoint = UserAssembly.EntryPoint;
            var cilCompiler = new CilInstructionCompiler(entryPoint, FrameworkAssembly);
            var roughCompilation = cilCompiler.Compile();

            // Mark as entry point so we can find it later.
            return roughCompilation.Prepend(new EntryPoint()).ToImmutableArray();
        }

        private ImmutableArray<AssemblyEntry> Optimize(ImmutableArray<AssemblyEntry> entries)
        {
            var optimizers = new[]
            {
                new PushConstantPopToGlobalOptimization()
            };

            ImmutableArray<AssemblyEntry> preOptimize;
            var postOptimize = entries;

            // Optimizers may rely on the output of other optimizers. So loop until there's 
            // no more changes being made.
            do
            {
                preOptimize = postOptimize;
                foreach (var optimizer in optimizers)
                {
                    postOptimize = optimizer.Optimize(postOptimize);
                }
            } while (!preOptimize.SequenceEqual(postOptimize));
            return postOptimize;
        }

        /// <summary>
        /// Generates stack-related let psuedoops that let us attempt to track the size/type of elements on the stack.
        /// This must be called AFTER optimizations. Most optimizations eliminate stack operations anyways. But the
        /// presence of the psuedoops will likely interfere with most optimizer's pattern matching too.
        /// </summary>
        private ImmutableArray<AssemblyEntry> GenerateStackOps(ImmutableArray<AssemblyEntry> entries)
        {
            int maxPush = 0;
            int maxPop = 0;
            foreach (var entry in entries.OfType<Macro>())
            {
                if (entry.Effects.OfType<PushStackAttribute>().SingleOrDefault() is PushStackAttribute pushAttr)
                {
                    maxPush = Math.Max(maxPush, pushAttr.Count);
                }
                if (entry.Effects.OfType<PopStackAttribute>().SingleOrDefault() is PopStackAttribute popAttr)
                {
                    maxPop = Math.Max(maxPop, popAttr.Count);
                }
            }
            var maxStack = Math.Max(maxPush, maxPop);

            return entries
                .Select(entry =>
                {
                    if (entry is Macro macro)
                    {
                        return macro.WithStackLets(maxStack);
                    }
                    return entry;
                }).ToImmutableArray();
            throw new NotImplementedException();
        }
    }
}
