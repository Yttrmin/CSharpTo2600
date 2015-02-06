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
    public sealed partial class Compiler
    {
        private readonly CSharpCompilation Compilation;
        private CSharpSyntaxTree Tree { get { return (CSharpSyntaxTree)Compilation.SyntaxTrees.Single(); } }
        private CSharpSyntaxNode Root { get { return Tree.GetRoot(); } }
        private readonly Assembly CompiledAssembly;
        private readonly Assembly FrameworkAssembly;
        private readonly SemanticModel Model;
        private readonly ROMBuilder ROMBuilder;

        static void Main(string[] args)
        {
            //@TODO - Handle more than 1 source file, workspaces.
            var FileName = args[0];
            var DASMPath = args[1];
            var Tree = (CSharpSyntaxTree)CSharpSyntaxTree.ParseText(File.ReadAllText(FileName));
            new Compiler(CreateCompilation(Tree)).Compile(DASMPath);
            Console.ReadLine();
        }

        public Compiler(CSharpCompilation Compilation)
        {
            FrameworkAssembly = typeof(Atari2600Game).Assembly;
            ROMBuilder = new ROMBuilder();

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

        public void Compile(string DASMPath)
        {
            var GameClass = GetGameClass();
            var Walker = new GameClassCompiler(this, GameClass);
            Walker.Compile();
            var OutputPath = ROMBuilder.WriteToFile("out.asm");
            var DASMSuccess = CompileOutput(DASMPath, OutputPath);
            if(DASMSuccess)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Compilation successful.");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Compilation failed.");
            }
            Console.ForegroundColor = ConsoleColor.Black;
        }

        private bool CompileOutput(string DASMPath, string FullPath)
        {
            var DASM = new Process();
            var FullDASMPath = Path.Combine(DASMPath, "dasm.exe");
            DASM.StartInfo.FileName = FullDASMPath;
            if(!File.Exists(FullDASMPath))
            {
                throw new FileNotFoundException($"DASM executable not found at: {FullDASMPath}");
            }
            DASM.StartInfo.UseShellExecute = false;
            DASM.StartInfo.RedirectStandardOutput = true;
            DASM.StartInfo.WorkingDirectory = DASMPath;
            DASM.StartInfo.Arguments = $"\"{FullPath}\" -f3 -ooutput.bin -soutput.sym -loutput.lst";
            DASM.StartInfo.CreateNoWindow = true;

            DASM.Start();
            DASM.WaitForExit();
            var Output = DASM.StandardOutput.ReadToEnd();
            var Success = DASM.ExitCode == 0;
            Console.WriteLine(Output);
            return Success;
        }

        private Type GetGameClass()
        {
            return CompiledAssembly.DefinedTypes.Single(t => t.GetCustomAttribute<Atari2600Game>() != null);
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
    }

    internal class FatalCompilationException : Exception
    {
        public FatalCompilationException(string Message) : base(Message)
        {

        }

        public FatalCompilationException(string Message, SyntaxNode Node)
            : this($"(Line #{Node.SyntaxTree.GetLineSpan(Node.Span).StartLinePosition.Line}): {Message}")
        {

        }
    }
}
