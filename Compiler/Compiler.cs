using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using System.IO;
using CSharpTo2600.Framework;
using System.Reflection;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace CSharpTo2600.Compiler
{
    public sealed partial class GameCompiler
    {
        private readonly CSharpCompilation Compilation;
        private readonly Assembly CompiledAssembly;
        private readonly Assembly FrameworkAssembly;
        private readonly CompileOptions Options = CompileOptions.Default;

        static void Main(string[] args)
        {
            GameCompiler.CompileFromFilePaths(args, CompileOptions.Default);
            Console.ReadLine();
        }
        
        private GameCompiler(CSharpCompilation Compilation, CompileOptions Options)
        {
            if (!EndianHelper.EndiannessIsSet)
            {
                EndianHelper.Endianness = Options.Endianness;
            }

            this.Options = Options;
            FrameworkAssembly = typeof(Atari2600Game).Assembly;

            this.Compilation = Compilation;

            using (var Stream = new MemoryStream())
            {
                this.Compilation.Emit(Stream);
                CompiledAssembly = Assembly.Load(Stream.ToArray());
            }
        }

        public static CompilationResult CompileFromTexts(params string[] SourceTexts)
        {
            return CompileFromTexts(CompileOptions.Default, SourceTexts);
        }

        public static CompilationResult CompileFromTexts(CompileOptions CompileOptions, params string[] SourceTexts)
        {
            return Compile(CompilerWorkspace.FromSourceTexts(SourceTexts), CompileOptions);
        }

        public static CompilationResult CompileFromFilePaths(IEnumerable<string> FilePaths, CompileOptions Options)
        {
            //@TODO - What if no paths were passed?
            //@TODO - What if the same file is passed multiple times?
            return Compile(CompilerWorkspace.FromFilePaths(FilePaths), Options);
        }

        private static CompilationResult Compile(CompilerWorkspace Workspace, CompileOptions Options)
        {
            var Compiler = new GameCompiler(Workspace.Compilation, Options);
            var CompilationState = new CompilationState();
            // Immediately put this in an array so it isn't enumerated multiple times and make duplicates.
            var BuiltInTypes = Compiler.ConstructBuiltInTypes().ToImmutableArray();
            CompilationState = CompilationState.WithBuiltInTypes(BuiltInTypes);
            // First stage is to parse the types without compiling any methods.
            // This gets us the type's fields and method metadata.
            var Subroutines = new Dictionary<IMethodSymbol, Subroutine>();
            foreach (var Type in Compiler.CompiledAssembly.DefinedTypes)
            {
                var ProcessedType = TypeParser.ParseType(Type, Workspace.Compilation, CompilationState);
                CompilationState = CompilationState.WithType(ProcessedType);
                var ParsedInfos = TypeParser.ParseMethods(ProcessedType, CompilationState);
                Subroutines = Subroutines.Concat(ParsedInfos).ToDictionary(s => s.Key, s => s.Value);
            }
            CompilationState = CompilationState.WithSubroutines(Subroutines.ToImmutableDictionary());
            // All methods have been detected, so now we can build a call hierarchy.
            var Hierarchy = Compiler.ConstructMethodCallHierarchy(CompilationState);
            // All fields have been explored, so we have enough information to layout globals
            // in memory.
            CompilationState = CompilationState.WithMemoryMap(MemoryManager.Analyze(CompilationState));
            // Now we can compile methods, knowing any field accesses or method calls should work
            // since we explored them in the parsing stage.
            var CompiledSubroutines = new Dictionary<IMethodSymbol, SubroutineInfo>();
            foreach (var Subroutine in CompilationState.AllSubroutines)
            {
                var SubroutineInfo = Compiler.CompileMethod(Subroutine, CompilationState);
                CompiledSubroutines[SubroutineInfo.Symbol] = SubroutineInfo;
            }
            CompilationState = CompilationState.WithSubroutineInfos(CompiledSubroutines.ToImmutableDictionary());
            var ROMInfo = ROMCreator.CreateROM(CompilationState);
            if (ROMInfo.DASMSuccess)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Compilation successful.");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Compilation failed.");
            }
            return ROMInfo;
        }

        private IEnumerable<Tuple<Type, ProcessedType>> ConstructBuiltInTypes()
        {
            var AssemblySymbol = (IAssemblySymbol)Compilation.GetAssemblyOrModuleSymbol(CompilerWorkspace.MSCorLibReference);

            // Byte
            var ByteSymbol = AssemblySymbol.GetTypeByMetadataName("System.Byte");
            var ProcessedByte = ProcessedType.FromBuiltInType(ByteSymbol, sizeof(byte));
            yield return Tuple.Create(typeof(byte), ProcessedByte);

            // Void
            var VoidSymbol = AssemblySymbol.GetTypeByMetadataName("System.Void");
            var ProcessedVoid = ProcessedType.FromBuiltInType(VoidSymbol, 0);
            yield return Tuple.Create(typeof(void), ProcessedVoid);
        }

        private SubroutineInfo CompileMethod(Subroutine Subroutine, CompilationState CompilationState)
        {
            var Symbol = Subroutine.Symbol;
            var MethodNode = (MethodDeclarationSyntax)Symbol.DeclaringSyntaxReferences.Single().GetSyntax();
            var Model = Compilation.GetSemanticModel(MethodNode.SyntaxTree);
            var CompiledSubroutine = MethodCompiler.CompileMethod(Symbol, CompilationState, Model, Options.Optimize);
            return CompiledSubroutine;
        }

        private ImmutableDictionary<IMethodSymbol, SubroutineInfo> CompileMethods(ProcessedType ParsedType, CompilationState CompilationState)
        {
            var Result = new Dictionary<IMethodSymbol, SubroutineInfo>();
            foreach (var Subroutine in CompilationState.GetSubroutinesFromType(ParsedType))
            {
                var Symbol = Subroutine.Symbol;
                var MethodNode = (MethodDeclarationSyntax)Symbol.DeclaringSyntaxReferences.Single().GetSyntax();
                var Model = Compilation.GetSemanticModel(MethodNode.SyntaxTree);
                var CompiledSubroutine = MethodCompiler.CompileMethod(Symbol, CompilationState, Model, Options.Optimize);
                Result.Add(Symbol, CompiledSubroutine);
            }
            return Result.ToImmutableDictionary();
        }

        private MethodCallHierarchy ConstructMethodCallHierarchy(CompilationState State)
        {
            var Roots = State.AllSubroutines.Where(s => s.Type != MethodType.UserDefined);
            var Hierarchy = MethodCallHierarchy.Empty;
            foreach(var Subroutine in Roots)
            {
                if(Hierarchy.Contains(Subroutine.Symbol))
                {
                    // The root must've already been explored if it's in the hierarchy.
                    // If it's already been explored, there must be a path from another root leading to it.
                    // Roots can't call roots.
                    throw new AttemptedToInvokeSpecialMethodException(Subroutine, "UNKNOWN");
                }
                var MethodDeclaration = (MethodDeclarationSyntax)Subroutine.Symbol.DeclaringSyntaxReferences.Single().GetSyntax();
                var Model = Compilation.GetSemanticModel(MethodDeclaration.SyntaxTree);
                Hierarchy = HierarchyBuilder.RecursiveBuilder(Subroutine.Symbol, Hierarchy, Model);
            }
            return Hierarchy;
        }
    }

    public struct CompileOptions
    {
        public readonly bool Optimize;
        public readonly Endianness Endianness;
        public static readonly CompileOptions Default =
            new CompileOptions(Optimize: true, Endianness: Endianness.Big);

        public CompileOptions(bool Optimize, Endianness Endianness)
        {
            this.Optimize = Optimize;
            this.Endianness = Endianness;
        }
    }
}
