#nullable enable
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using VCSFramework.V2;

namespace VCSCompiler.V2
{
    public sealed class Compiler
    {
        private readonly AssemblyDefinition UserAssembly;
        private static CompilerOptions? _Options;
        internal static CompilerOptions Options
        {
            get
            {
                if (_Options == null)
                    throw new InvalidOperationException($"Attempted to fetch {nameof(Options)} but it's null, this should never happen.");
                return _Options;
            }
        }

        private Compiler(AssemblyDefinition userAssemblyDefinition, CompilerOptions options)
        {
            UserAssembly = userAssemblyDefinition;
            _Options = options;
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
            var userAssemblyDefinition = CreateAssemblyDefinition(new[] { sourcePath }.ToImmutableArray());

            var compiler = new Compiler(userAssemblyDefinition, options);
            var entryPointBody = compiler.CompileEntryPoint();
            var allFunctions = compiler.GetAllFunctions(entryPointBody).Prepend(entryPointBody);
            var labelMap = new LabelMap(allFunctions, userAssemblyDefinition);

            var assemblyWriter = new AssemblyWriter(labelMap.FunctionToBody.Add(userAssemblyDefinition.MainModule.EntryPoint, entryPointBody), labelMap, options.SourceAnnotations);

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
            _Options = null;
            return romInfo;
        }

        private static AssemblyDefinition CreateAssemblyDefinition(ImmutableArray<string> sourcePaths)
        {
            // First we compile without the generated template so we can find what type to use.
            // Then we compile with the generated template and return the AssemblyDefinition containing that and the user types.
            var firstCompilation = CompilationCreator.CreateFromFilePaths(sourcePaths, null);
            GetAssemblyDefinition(firstCompilation, out var firstAssemblyStream);
            var loadContext = new AssemblyLoadContext(null, true);
            var firstAssembly = loadContext.LoadFromStream(firstAssemblyStream!);
            firstAssemblyStream.Dispose();

            ProgramTemplate template;
            var userProgramType = firstAssembly.GetTypes().SingleOrDefault(t => t.CustomAttributes.Any(a => a.AttributeType.FullName == typeof(TemplatedProgramAttribute).FullName));
            if (userProgramType == null)
            {
                // If no types are marked with [TemplatedProgram], assume they want a RawTemplate.
                userProgramType = firstAssembly.EntryPoint?.DeclaringType ?? throw new InvalidOperationException($"Could not find a type marked with [TemplatedProgram] nor an entry point.");
                template = new RawTemplate(userProgramType);
            }
            else
            {
                var templateAttribute = userProgramType.CustomAttributes.Single(a => a.AttributeType.FullName == typeof(TemplatedProgramAttribute).FullName);
                var templateType = (Type)templateAttribute.ConstructorArguments[0].Value!;
                var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                template = (ProgramTemplate?)Activator.CreateInstance(templateType, flags, null, new[] { userProgramType }, null) 
                    ?? throw new InvalidOperationException($"Failed to create instance of type '{templateType}'");
            }

            // Don't dispose MemoryStream or it breaks Cecil.
            loadContext.Unload();

            var generatedSourceText = template.GenerateSourceText();
            var generatedSourcePath = Path.Combine(Path.GetTempPath(), $"{template.GeneratedTypeName}.generated.cs");
            File.WriteAllText(generatedSourcePath, generatedSourceText);

            var finalCompilation = CompilationCreator.CreateFromFilePaths(sourcePaths.Append(generatedSourcePath), template.GeneratedTypeName);
            return GetAssemblyDefinition(finalCompilation, out var _);
        }

        private static AssemblyDefinition GetAssemblyDefinition(CSharpCompilation compilation, out MemoryStream assemblyStream)
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
            var definition = AssemblyDefinition.ReadAssembly(assemblyStream, parameters);
            assemblyStream.Position = 0;
            return definition;
        }

        public ImmutableArray<AssemblyEntry> CompileEntryPoint()
        {
            var entryPoint = UserAssembly.EntryPoint;
            var compiledBody = MethodCompiler.Compile(entryPoint, UserAssembly, false, new CilInstructionCompiler.Options
            {
                InlineAllCalls = true
            });
            // @TODO - Control should never return from the entry point. For RawTemplate, this means ensuring the _user_'s entry point
            // never returns. For StandardTemplate, _its_ entry point should never return.

            // Prepend the .cctor if there is one.
            var cctor = entryPoint.DeclaringType.Methods.SingleOrDefault(m => m.Name == ".cctor");
            if (cctor != null)
            {
                var inlineCctorCall = new InlineMethod(Instruction.Create(OpCodes.Call, cctor), cctor, MethodCompiler.Compile(cctor, UserAssembly, true));
                compiledBody = compiledBody.Prepend(inlineCctorCall).ToImmutableArray();
            }

            // Prepend EntryPoint(), which performs CPU/memory initialization.
            return compiledBody
                .Prepend(new EntryPoint())
                .ToImmutableArray();
        }

        private IEnumerable<ImmutableArray<AssemblyEntry>> GetAllFunctions(ImmutableArray<AssemblyEntry> body)
        {
            foreach (var entry in body)
            {
                if (entry is InlineMethod inlineMethod)
                {
                    yield return inlineMethod.Entries;
                    foreach (var function in GetAllFunctions(inlineMethod.Entries))
                    {
                        yield return function;
                    }
                }
                else if (entry is ICallMacro call)
                {
                    // Non-inline methods are compiled twice. Here it's compiled just so we can extract
                    // labels for LabelMap. LabelMap will re-compile it (hopefully identically), and
                    // that's what will actually be written out.
                    var compiledBody = MethodCompiler.Compile(call.Method, UserAssembly, false);
                    yield return compiledBody;
                    foreach (var function in GetAllFunctions(compiledBody))
                    {
                        yield return function;
                    }
                }
            }
        }
    }
}
