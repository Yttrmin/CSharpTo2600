using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO;
using CSharpTo2600.Framework;
using System.Reflection;
using System.Diagnostics;

namespace CSharpTo2600.Compiler
{
    public sealed partial class GameCompiler
    {
        private readonly CSharpCompilation Compilation;
        private CSharpSyntaxTree Tree { get { return (CSharpSyntaxTree)Compilation.SyntaxTrees.Single(); } }
        private CSharpSyntaxNode Root { get { return Tree.GetRoot(); } }
        private readonly Assembly CompiledAssembly;
        private readonly Assembly FrameworkAssembly;
        [Obsolete("We have multiple models. Pass approriate one to who needs it.", true)]
        private readonly SemanticModel Model;
        private readonly CompilerWorkspace Workspace;
        private CompileOptions Options = CompileOptions.Default;
        public const string DASMPath = "./Dependencies/DASM/";

        static void Main(string[] args)
        {
            GameCompiler.Compile(args, CompileOptions.Default);
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

            Console.WriteLine(this.Tree.GetRoot());
        }

        public static ROMInfo Compile(string SourceText)
        {
            return Compile(SourceText, CompileOptions.Default);
        }

        public static ROMInfo Compile(string SourceText, CompileOptions CompileOptions)
        {
            return Compile(new CompilerWorkspace(SourceText), CompileOptions);
        }

        public static ROMInfo Compile(IEnumerable<string> FilePaths, CompileOptions Options)
        {
            return Compile(new CompilerWorkspace(FilePaths), Options);
        }

        private static ROMInfo Compile(CompilerWorkspace Workspace, CompileOptions Options)
        {
            var Compiler = new GameCompiler(Workspace.Compilation, Options);
            var CompilationInfo = new CompilationInfo(Compiler.Model);
            // First stage is to parse the types without compiling any methods. This gets us the
            // type's fields and subroutine signatures.
            foreach (var Type in Compiler.CompiledAssembly.DefinedTypes)
            {
                CompilationInfo = CompilationInfo.WithType(TypeParser.ParseType(Type, Workspace.Compilation));
            }
            // All fields have been explored, so we have enough information to layout globals
            // in memory.
            CompilationInfo = MemoryManager.Analyze(CompilationInfo);
            // Now we can compile methods, knowing any field accesses or method calls should work
            // since we explored them in the parsing stage.
            foreach (var Type in CompilationInfo.AllTypes)
            {
                CompilationInfo = CompilationInfo.WithReplacedType(TypeCompiler.CompileType(Type, CompilationInfo, Compiler));
            }
            var ROMInfo = ROMCreator.CreateROM(CompilationInfo);
            if (ROMInfo.Success)
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

        private Type GetType(TypeSyntax TypeSyntax)
        {
            var Info = Model.GetTypeInfo(TypeSyntax);
            var FullyQualifiedNameFormat = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
            var FullyQualifiedName = Info.Type.ToDisplayString(FullyQualifiedNameFormat);
            //@TODO - Won't find types outside of mscorlib.
            var TrueType = Type.GetType(FullyQualifiedName);
            if (TrueType == null)
            {
                throw new ArgumentException("TypeSyntaxes must correspond to an mscorlib type for now.", nameof(TypeSyntax));
            }
            return TrueType;
        }

        private Type GetType(ITypeSymbol TypeSymbol)
        {
            var FullyQualifiedNameFormat = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
            var FullyQualifiedName = TypeSymbol.ToMinimalDisplayString(Model, 0, FullyQualifiedNameFormat);
            //@TODO - Won't find types outside of mscorlib.
            var TrueType = Type.GetType(FullyQualifiedName);
            if (TrueType == null)
            {
                throw new ArgumentException("TypeSyntaxes must correspond to an mscorlib type for now.", nameof(TypeSymbol));
            }
            return TrueType;
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
