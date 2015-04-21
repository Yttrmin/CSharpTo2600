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
        private readonly SemanticModel Model;
        private CompileOptions Options = CompileOptions.Default;
        public const string DASMPath = "./Dependencies/DASM/";

        static void Main(string[] args)
        {
            //@TODO - Handle more than 1 source file, workspaces.
            var FileName = args[0];
            var Tree = (CSharpSyntaxTree)CSharpSyntaxTree.ParseText(File.ReadAllText(FileName));
            new GameCompiler(CreateCompilation(Tree)).Compile();
            Console.ReadLine();
        }

        public GameCompiler(string SourceText, CompileOptions Options)
            : this(CreateCompilation(CSharpSyntaxTree.ParseText(SourceText)))
        {
            this.Options = Options;
        }

        public GameCompiler(string SourceText)
            : this(CreateCompilation(CSharpSyntaxTree.ParseText(SourceText)))
        {
        }

        public GameCompiler(CSharpCompilation Compilation)
        {
            if (!EndianHelper.EndiannessIsSet)
            {
                EndianHelper.Endianness = Options.Endianness;
            }

            FrameworkAssembly = typeof(Atari2600Game).Assembly;

            this.Compilation = Compilation;

            using (var Stream = new MemoryStream())
            {
                this.Compilation.Emit(Stream);
                CompiledAssembly = Assembly.Load(Stream.ToArray());
            }
            Model = this.Compilation.GetSemanticModel(this.Compilation.SyntaxTrees.Single());

            Console.WriteLine(this.Tree.GetRoot());
        }

        private static CSharpCompilation CreateCompilation(SyntaxTree Tree)
        {
            var MSCorLib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
            var RetroLib = MetadataReference.CreateFromFile(typeof(Atari2600Game).Assembly.Location);
            var Compilation = CSharpCompilation.Create("TestAssembly", new[] { Tree },
                new[] { MSCorLib, RetroLib }, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var CompilerErrors = Compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error);
            if (CompilerErrors.Any())
            {
                Console.WriteLine("!Roslyn compilation failed! Messages:");
                foreach (var Error in CompilerErrors)
                {
                    Console.WriteLine(Error.ToString());
                }
                throw new FatalCompilationException("File must compile with Roslyn to be compiled to 6502.");
            }
            return Compilation;
        }

        public void Compile()
        {
            var CompilationInfo = new CompilationInfo(Model);
            // First stage is to parse the types without compiling any methods. This gets us the
            // type's fields and subroutine signatures.
            foreach (var Type in CompiledAssembly.DefinedTypes)
            {
                CompilationInfo = CompilationInfo.WithType(TypeParser.ParseType(Type, Compilation));
            }
            // All fields have been explored, so we have enough information to layout globals
            // in memory.
            CompilationInfo = MemoryManager.Analyze(CompilationInfo);
            // Now we can compile methods, knowing any field accesses or method calls should work
            // since we explored them in the parsing stage.
            foreach (var Type in CompilationInfo.AllTypes)
            {
                CompilationInfo = CompilationInfo.WithReplacedType(TypeCompiler.CompileType(Type, CompilationInfo, this));
            }
            var ASMPath = ROMCreator.CreateASMFile(CompilationInfo);
            var DASMSuccess = AssembleOutput(ASMPath);
            if (DASMSuccess)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Compilation successful.");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Compilation failed.");
            }
        }

        internal static bool AssembleOutput(string AssemblyFilePath)
        {
            var DASM = new Process();
            var FullDASMPath = Path.Combine(DASMPath, "dasm.exe");
            DASM.StartInfo.FileName = FullDASMPath;
            if (!File.Exists(FullDASMPath))
            {
                throw new FileNotFoundException($"DASM executable not found at: {FullDASMPath}");
            }
            var OutputDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            DASM.StartInfo.UseShellExecute = false;
            DASM.StartInfo.RedirectStandardOutput = true;
            DASM.StartInfo.WorkingDirectory = DASMPath;
            DASM.StartInfo.Arguments = $"\"{AssemblyFilePath}\" -f3 -o{Path.Combine(OutputDirectory, "output.bin")} -s{Path.Combine(OutputDirectory, "output.sym")} -l{Path.Combine(OutputDirectory, "output.lst")}";
            DASM.StartInfo.CreateNoWindow = true;

            DASM.Start();
            DASM.WaitForExit();
            var Output = DASM.StandardOutput.ReadToEnd();
            // DASM documentation says this returns 0 on success and 1 otherwise. This is not
            // true since it returned 0 when the ASM was missing the 'processor' op, causing a
            // lot of errors and spit out a 0 byte BIN. Hopefully nothing else returns 0 on failure.
            var Success = DASM.ExitCode == 0;
            Console.WriteLine(Output);
            return Success;
        }

        private Type GetGameClass()
        {
            var GameClass = CompiledAssembly.DefinedTypes
                .SingleOrDefault(t => t.GetCustomAttribute<Atari2600Game>() != null);
            if (GameClass == null)
            {
                throw new GameClassNotFoundException();
            }
            return GameClass;
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
