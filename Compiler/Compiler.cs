using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO;
using CSharpTo2600.Framework;

namespace CSharpTo2600.Compiler
{
    public partial class Compiler
    {
        private readonly CSharpCompilation Compilation;
        private CSharpSyntaxTree Tree { get { return (CSharpSyntaxTree)Compilation.SyntaxTrees.Single(); } }
        private CSharpSyntaxNode Root { get { return Tree.GetRoot(); } }
        private readonly SemanticModel Model;
        private readonly ROMBuilder ROMBuilder;

        static void Main(string[] args)
        {
            //@TODO - Use command line arguments instead.
            //@TODO - Handle more than 1 source file, workspaces.
            var FileName = @"..\..\..\ScreenColors\Program.cs";
            var Tree = (CSharpSyntaxTree)CSharpSyntaxTree.ParseText(File.ReadAllText(FileName));
            var MSCorLib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
            var RetroLib = MetadataReference.CreateFromFile(typeof(Atari2600Game).Assembly.Location);
            var Compilation = CSharpCompilation.Create("TestAssembly", new[] { Tree },
                new[] { MSCorLib, RetroLib });
            new Compiler(Compilation).Compile();
            Console.WriteLine("Compilation finished.");
            Console.ReadLine();
        }

        public Compiler(CSharpCompilation Compilation)
        {
            ROMBuilder = new ROMBuilder();
            this.Compilation = Compilation;
            var CompilerErrors = Compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error);
            if (CompilerErrors.Any())
            {
                Console.WriteLine("!Roslyn compilation failed! Messages:");
                foreach(var Error in CompilerErrors)
                {
                    Console.WriteLine(Error.ToString());
                }
                throw new FatalCompilationException("File must compile with Roslyn to be compiled to 6502.");
            }
            Model = Compilation.GetSemanticModel(Tree);
        }

        public void Compile()
        {
            var GameClass = GetGameClass();
            CompileGameClass(GameClass);
            //@TODO - Use command line arguments instead.
            ROMBuilder.WriteToFile("out.asm");
            return;
        }

        private void CompileGameClass(ClassDeclarationSyntax Class)
        {
            SetupGlobals(Class.DescendantNodes().OfType<FieldDeclarationSyntax>());
            var EntryPoint = Class.Members.OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.Text == "Main").Single();
            ROMBuilder.AddSubroutine(CompileMethod(EntryPoint));
        }

        private Subroutine CompileMethod(MethodDeclarationSyntax Method)
        {
            var Attribute = FromAttributeSyntax(Method.AllAttributes().Single());
            var Builder = new SubroutineBuilder(this, Method.Identifier.Text, Attribute.GameMethod);
            var Expressions = Method.Body.ChildNodes().OfType<ExpressionStatementSyntax>();
            foreach(var ExpressionStatement in Expressions)
            {
                var Expression = ExpressionStatement.ChildNodes().Single();
                // Lets us give each expression its own convenient method at the cost of invoking the DLR.
                // I have absolutely zero concerns about performance.
                Builder.Append(Expression as dynamic);
            }
            return Builder.ToSubroutine();
        }

        // Not putting this in the library since it'll add a dependency. Also it is terrible.
        [Obsolete("Just use reflection on the compiled assembly.")]
        private SpecialMethodAttribute FromAttributeSyntax(AttributeSyntax Attribute)
        {
            if(Attribute.GetAttributeName(Model) != nameof(SpecialMethodAttribute))
            {
                throw new ArgumentException("Attribute is not of \{nameof(SpecialMethodAttribute)}.");
            }
            var MethodTypeName = Attribute.ArgumentList.Arguments.Single().GetLastToken().Text;
            var MethodType = (MethodType)Enum.Parse(typeof(MethodType), MethodTypeName);
            return new SpecialMethodAttribute(MethodType);
        }

        private void SetupGlobals(IEnumerable<FieldDeclarationSyntax> Fields)
        {
            var FullyQualifiedName = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
            foreach (var Field in Fields)
            {
                var RealType = Type.GetType(Model.GetTypeInfo(Field.Declaration.Type).Type.ToDisplayString(FullyQualifiedName), true, false);
                var VariableDeclator = Field.Declaration.DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
                var VariableName = VariableDeclator.Identifier.ToString();
                ROMBuilder.AddGlobalVariable(RealType, VariableName);
            }
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
