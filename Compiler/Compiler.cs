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
        private CompileOptions Options = CompileOptions.Default;

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
            var BuiltInTypes = ConstructBuiltInTypes(Compiler).ToImmutableArray();
            foreach (var BuiltInType in BuiltInTypes)
            {
                CompilationState = CompilationState.WithType(BuiltInType);
            }
            // First stage is to parse the types without compiling any methods. This gets us the
            // type's fields and subroutine signatures.
            foreach (var Type in Compiler.CompiledAssembly.DefinedTypes)
            {
                CompilationState = CompilationState.WithType(TypeParser.ParseType(Type, Workspace.Compilation, CompilationState));
            }
            // All methods have been detected, so now we can build a call hierarchy.
            var Hierarchy = ConstructMethodCallHierarchy(Compiler, CompilationState);
            // All fields have been explored, so we have enough information to layout globals
            // in memory.
            CompilationState = MemoryManager.Analyze(CompilationState);
            // Now we can compile methods, knowing any field accesses or method calls should work
            // since we explored them in the parsing stage.
            foreach (var Type in CompilationState.AllTypes)
            {
                if(BuiltInTypes.Contains(Type))
                {
                    continue;
                }
                CompilationState = CompilationState.WithReplacedType(TypeCompiler.CompileType(Type, CompilationState, Compiler));
            }
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

        private static IEnumerable<ProcessedType> ConstructBuiltInTypes(GameCompiler Compiler)
        {
            var AssemblySymbol = (IAssemblySymbol)Compiler.Compilation.GetAssemblyOrModuleSymbol(CompilerWorkspace.MSCorLibReference);

            // Byte
            var ByteSymbol = AssemblySymbol.GetTypeByMetadataName("System.Byte");
            var ProcessedByte = ProcessedType.FromBuiltInType(ByteSymbol, 1);
            yield return ProcessedByte;

            // Void
            var VoidSymbol = AssemblySymbol.GetTypeByMetadataName("System.Void");
            var ProcessedVoid = ProcessedType.FromBuiltInType(VoidSymbol, 0);
            yield return ProcessedVoid;
        }

        private static MethodCallHierarchy ConstructMethodCallHierarchy(GameCompiler Compiler, CompilationState State)
        {
            var Roots = State.AllSubroutines.Where(s => s.Type != MethodType.UserDefined);
            var Hierarchy = MethodCallHierarchy.Empty;
            foreach(var Subroutine in Roots)
            {
                var MethodDeclaration = (MethodDeclarationSyntax)Subroutine.Symbol.DeclaringSyntaxReferences.Single().GetSyntax();
                var Model = Compiler.Compilation.GetSemanticModel(MethodDeclaration.SyntaxTree);
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
