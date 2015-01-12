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
            //@TODO - Use command line arguments instead.
            //@TODO - Handle more than 1 source file, workspaces.
            var FileName = @"..\..\..\ScreenColors\Simple.cs";
            var Tree = (CSharpSyntaxTree)CSharpSyntaxTree.ParseText(File.ReadAllText(FileName));
            new Compiler(CreateCompilation(Tree)).Compile();
            Console.WriteLine("Compilation finished.");
            Console.ReadLine();
        }

        public Compiler(CSharpCompilation Compilation)
        {
            FrameworkAssembly = typeof(Atari2600Game).Assembly;
            ROMBuilder = new ROMBuilder();

            this.Compilation = Preprocess(Compilation);
            
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

        private CSharpCompilation Preprocess(CSharpCompilation Compilation)
        {
            var Tree = Compilation.SyntaxTrees.Single();
            var Model = Compilation.GetSemanticModel(Tree);
            var Preprocessor = new SyntaxPreprocessor(Model);
            var NewRoot = Preprocessor.Visit(Tree.GetRoot()).NormalizeWhitespace();
            var NewTree = CSharpSyntaxTree.Create((CSharpSyntaxNode)NewRoot);
            return CreateCompilation(NewTree);
        }

        public void Compile()
        {
            var GameClass = GetGameClass();
            var Walker = new GameClassCompiler(this);
            Walker.Visit(GameClass);
            //@TODO - Use command line arguments instead.
            ROMBuilder.WriteToFile("out.asm");
            return;
        }

        private IEnumerable<MethodDeclarationSyntax> GetSpecialMethods(ClassDeclarationSyntax Class)
        {
            var Methods = Class.Members.OfType<MethodDeclarationSyntax>();
            foreach(var Method in Methods)
            {
                var Attribute = GetSpecialMethodAttribute(Method);
                if(Attribute == null)
                {
                    continue;
                }
                else
                {
                    yield return Method;
                }
            }
        }

        /// <returns>The SpecialMethodAttribute if one exists, otherwise null.</returns>
        private SpecialMethodAttribute GetSpecialMethodAttribute(MethodDeclarationSyntax Method)
        {
            var ClassDeclaration = (ClassDeclarationSyntax)Method.Parent;
            var TypeMaybe = Model.GetDeclaredSymbol(ClassDeclaration);
            var FullTypeName = Model.GetDeclaredSymbol(ClassDeclaration).ToString();
            var ContainingType = CompiledAssembly.GetType(FullTypeName);
            //@TODO - This won't handle overloaded methods.
            var MethodInfo = ContainingType.GetMethod(Method.Identifier.Text, 
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            return MethodInfo.GetCustomAttribute<SpecialMethodAttribute>();
        }

        private ClassDeclarationSyntax GetGameClass()
        {
            var GameClass = Root.DescendantNodes().OfType<ClassDeclarationSyntax>().SingleOrDefault();
            if (GameClass == null)
            {
                throw new FatalCompilationException("No game class found", Root);
            }
            //@TODO - Move to static analyzer.
            if (!GameClass.Modifiers.Any(t => t.Text == "static"))
            {
                throw new FatalCompilationException("Game class must be a static class.", GameClass);
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
    }

    internal class FatalCompilationException : Exception
    {
        public FatalCompilationException(string Message) : base(Message)
        {

        }

        public FatalCompilationException(string Message, SyntaxNode Node)
            : this("(Line #\{Node.SyntaxTree.GetLineSpan(Node.Span).StartLinePosition.Line}): \{Message}")
        {

        }
    }
}
