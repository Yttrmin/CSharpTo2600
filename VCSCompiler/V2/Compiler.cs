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
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using VCSFramework.V2;
using VCSFramework.V2.Templates;

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
            var (userAssemblyDefinition, userAssembly) = CreateAssemblyDefinition(new[] { sourcePath }.ToImmutableArray());

            var compiler = new Compiler(userAssemblyDefinition, options);
            var entryPointBody = compiler.CompileEntryPoint();
            var allFunctions = compiler.RecursiveCompileAllFunctions(entryPointBody);
            var allLabelAssignments = CreateLabelAssignments(allFunctions.Prepend(entryPointBody).ToImmutableArray(), userAssemblyDefinition, userAssembly);
            var allRomData = allFunctions.Prepend(entryPointBody)
                .SelectMany(GetAllMacroParameters)
                .OfType<RomDataGlobalLabel>()
                .Select(label =>
                {
                    // @TODO - What do we do for 0 elements?
                    var romData = ImmutableArray.ToImmutableArray(InvokeRomDataGenerator(userAssembly, label.GeneratorMethod));
                    var elementSize = Marshal.SizeOf(Enumerable.First(romData));
                    var byteBuffer = new byte[romData.Length * elementSize];
                    var byteIndex = 0;
                    foreach (var element in romData)
                    {
                        var ptr = Marshal.AllocHGlobal(elementSize);
                        Marshal.StructureToPtr(element, ptr, false);
                        Marshal.Copy(ptr, byteBuffer, byteIndex, elementSize);
                        byteIndex += elementSize;
                        Marshal.FreeHGlobal(ptr);
                    }
                    return (label, byteBuffer.ToImmutableArray());
                })
                .ToImmutableArray();
            var fullProgram = AssemblyTemplate.GenerateProgram(entryPointBody, allFunctions, allLabelAssignments, allRomData);

            //var assemblyWriter = new AssemblyWriter(labelMap.FunctionToBody.Add(userAssemblyDefinition.MainModule.EntryPoint, entryPointBody), labelMap, options.SourceAnnotations);

            var qq = AssemblyTemplate.ProgramToString(fullProgram, SourceAnnotation.Both);
            var romInfo = Assemble(qq, options.OutputPath);

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

            static RomInfo Assemble(string assembly, string? outputPath)
            {
                // @TODO - Should probably delete these?? If there's 65K temp files it'll fail.
                var binPath = outputPath ?? Path.GetTempFileName();
                var asmPath = outputPath != null ? Path.ChangeExtension(outputPath, "asm") : Path.GetTempFileName();
                var listPath = outputPath != null ? Path.ChangeExtension(outputPath, "lst") : Path.GetTempFileName();

                File.WriteAllText(asmPath, assembly);

                var assemblerArgs = new[]
                {
                    //"-l",
                    //Path.Combine(Path.GetDirectoryName(asmPath)!, "labeltest.asm"),
                    asmPath,
                    "-o",
                    binPath,
                    "-L",
                    listPath,
                    "--format=flat"
                };

                using var stdoutStream = new MemoryStream();
                using var writer = new StreamWriter(stdoutStream) { AutoFlush = true };
                Console.SetOut(writer);
                Console.SetError(writer);

                Core6502DotNet.Core6502DotNet.Main(assemblerArgs);

                Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
                Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
                stdoutStream.Position = 0;
                using var reader = new StreamReader(stdoutStream);
                var stdoutText = reader.ReadToEnd();
                Console.WriteLine("Assembler output:");
                Console.WriteLine(stdoutText);

                if (!stdoutText.Contains("Assembly completed successfully."))
                {
                    Console.WriteLine("Assembly failed, there is probably an internal problem with the code that the compiler is generating.");
                    return new RomInfo
                    {
                        IsSuccessful = false,
                        AssemblyPath = asmPath
                    };
                }

                Console.WriteLine("Assembly was successful.");
                return new RomInfo
                {
                    IsSuccessful = true,
                    // @TODO - Don't emit if we used temp files?
                    AssemblyPath = asmPath,
                    RomPath = binPath,
                    ListPath = listPath
                };
            }
        }

        private static (AssemblyDefinition, Assembly) CreateAssemblyDefinition(ImmutableArray<string> sourcePaths)
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
                userProgramType = firstAssembly.GetEntryPoint()?.DeclaringType ?? throw new InvalidOperationException($"Could not find a type marked with [TemplatedProgram] nor an entry point.");
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
            var definition = GetAssemblyDefinition(finalCompilation, out var finalAssemblyStream);
            var finalAssembly = new AssemblyLoadContext(null, false).LoadFromStream(finalAssemblyStream);
            return (definition, finalAssembly);
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

        public Function CompileEntryPoint()
        {
            var entryPoint = UserAssembly.EntryPoint;
            return MethodCompiler.Compile(entryPoint, UserAssembly, false, true, new CilInstructionCompiler.Options
            {
                InlineAllCalls = true
            });
            // @TODO - Control should never return from the entry point. For RawTemplate, this means ensuring the _user_'s entry point
            // never returns. For StandardTemplate, _its_ entry point should never return.
        }

        private ImmutableArray<Function> RecursiveCompileAllFunctions(Function function)
        {
            var set = new HashSet<Function>();
            CompileAllFunctionsInternal(function, UserAssembly, set);
            return set.ToImmutableArray();

            static void CompileAllFunctionsInternal(Function function, AssemblyDefinition userAssembly, ISet<Function> set)
            {
                foreach (var entry in GetAllMacroParameters(function).OfType<FunctionLabel>())
                {
                    if (!set.Any(f => (MethodDef)f.Definition == entry.Method))
                    {
                        var compiledFunction = MethodCompiler.Compile(entry.Method, userAssembly, false);
                        set.Add(compiledFunction);
                        CompileAllFunctionsInternal(compiledFunction, userAssembly, set);
                    }
                }
            }
        }

        private static ImmutableArray<LabelAssign> CreateLabelAssignments(ImmutableArray<Function> functions, AssemblyDefinition userAssemblyDef, Assembly userAssembly)
        {
            // @TODO - Aliases
            var start = 0x80;
            var reserved = Enumerable.Range(0, GetReservedBytes(functions))
                .Select(i => new LabelAssign(new ReservedGlobalLabel(i), new Constant(new FormattedByte((byte)start++, ByteFormat.Hex))));
            var otherGlobals = functions.SelectMany(GetAllMacroParameters)
                .OfType<IGlobalLabel>()
                .Where(l => l is not PredefinedGlobalLabel && l is not RomDataGlobalLabel)
                .Distinct()
                .Select(l => new LabelAssign(l, new Constant(new FormattedByte((byte)start++, ByteFormat.Hex))));
            // @TODO - Check if we overflowed into stack.

            var typeId = 100;
            var allReferencedTypes = functions.SelectMany(GetAllMacroParameters).OfType<TypeLabel>().Select(l => l.Type)
                .Concat(functions.SelectMany(GetAllMacroParameters).OfType<PointerTypeLabel>().Select(l => l.ReferentType))
                .Prepend(BuiltInDefinitions.Nothing).Prepend(BuiltInDefinitions.Bool).Prepend(BuiltInDefinitions.Byte)
                .Distinct()
                .ToImmutableArray();
            var allPairedTypes = allReferencedTypes.Select(t => (new LabelAssign(new TypeLabel(t), new Constant((byte)typeId++)), new LabelAssign(new PointerTypeLabel(t), new Constant((byte)typeId++))));

            var allTypeSizes = allReferencedTypes.Select(t => new LabelAssign(new TypeSizeLabel(t), new Constant((byte)TypeData.Of(t, userAssemblyDef).Size)));

            var allLabelAssignments = new List<LabelAssign>();

            var aliasedFields = userAssemblyDef.CompilableTypes().SelectMany(t => t.Fields).Where(f => f.CustomAttributes.Any(a => a.AttributeType.FullName == typeof(InlineAssemblyAliasAttribute).FullName));
            foreach (var field in aliasedFields)
            {
                // @TODO - Support aliased RomData<>s.
                if (!field.IsStatic)
                    throw new InvalidOperationException($"[{nameof(InlineAssemblyAliasAttribute)}] can only be used on static fields. '{field.FullName}' is not static.");
                var aliases = field.CustomAttributes.Where(a => a.AttributeType.FullName == typeof(InlineAssemblyAliasAttribute).FullName).Select(a => (string)a.ConstructorArguments[0].Value);
                foreach (var alias in aliases)
                {
                    if (!alias.StartsWith(AssemblyUtilities.AliasPrefix))
                        throw new InvalidOperationException($"Alias '{alias}' must begin with '{AssemblyUtilities.AliasPrefix}'");
                    var existingAlias = allLabelAssignments.Where(a => a.Label is PredefinedGlobalLabel p && p.Name == alias).SingleOrDefault();
                    if (existingAlias != null)
                        throw new InvalidOperationException($"Alias '{alias}' is already being aliased to '{existingAlias.Value}', can't alias to '{field.FullName}' too.");
                    allLabelAssignments.Add(new(new PredefinedGlobalLabel(alias), new GlobalFieldLabel(field)));
                }
            }

            var romDataLengths = functions.SelectMany(GetAllMacroParameters)
                .OfType<Constant>()
                .Select(c => c.Value)
                .OfType<RomDataLength>()
                .Select(l => new LabelAssign(l, new Constant(Convert.ToByte(Enumerable.Count(InvokeRomDataGenerator(userAssembly, l.GeneratorMethod))))));

            allLabelAssignments.AddRange(reserved);
            allLabelAssignments.AddRange(otherGlobals);
            foreach (var pair in allPairedTypes)
            {
                allLabelAssignments.Add(pair.Item1);
                allLabelAssignments.Add(pair.Item2);
            }
            allLabelAssignments.AddRange(allTypeSizes);
            allLabelAssignments.AddRange(new LabelAssign[] { new(new PointerSizeLabel(true), new Constant(1)), new(new PointerSizeLabel(false), new Constant(2)) });
            allLabelAssignments.AddRange(romDataLengths);
            return allLabelAssignments.ToImmutableArray();
        }

        private static int GetReservedBytes(IEnumerable<Function> functions) 
            => functions.SelectMany(GetAllMacroCalls)
            .Select(m => m switch
            {
                StackMutatingMacroCall sm => sm.MacroCall,
                _ => m
            })
            .Select(m => m.GetType()).Max(t => t.GetCustomAttribute<ReservedBytesAttribute>()!.Count);

        private IEnumerable<FieldReference> GetAllFieldRefs(Function function)
            => GetAllMacroParameters(function).OfType<GlobalFieldLabel>().Select(l => l.Field).Distinct().Select(f => f.Field);

        private static IEnumerable<IExpression> GetAllMacroParameters(Function function)
            => function.Body.OfType<IMacroCall>().SelectMany(m => m.Parameters).Distinct();

        private static IEnumerable<IMacroCall> GetAllMacroCalls(Function function)
            => function.Body.OfType<IMacroCall>();

        private static dynamic InvokeRomDataGenerator(Assembly userAssembly, MethodDefinition generator)
        {
            var compiledMethod = userAssembly.Modules.Single().ResolveMethod(generator.MetadataToken.ToInt32()) ?? throw new InvalidOperationException($"Failed to lookup RomData generator '{generator.FullName}' in user assembly '{userAssembly}'.");
            return (dynamic)(compiledMethod.Invoke(null, null) ?? throw new InvalidOperationException($"Return value of RomData generator '{generator.FullName}' was NULL."));
        }
    }
}
