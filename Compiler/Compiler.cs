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
    public partial class Compiler
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
            var FileName = @"..\..\..\ScreenColors\Program.cs";
            var Tree = (CSharpSyntaxTree)CSharpSyntaxTree.ParseText(File.ReadAllText(FileName));
            var MSCorLib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
            var RetroLib = MetadataReference.CreateFromFile(typeof(Atari2600Game).Assembly.Location);
            var Compilation = CSharpCompilation.Create("TestAssembly", new[] { Tree },
                new[] { MSCorLib, RetroLib }, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            new Compiler(Compilation).Compile();
            Console.WriteLine("Compilation finished.");
            Console.ReadLine();
        }

        public Compiler(CSharpCompilation Compilation)
        {
            FrameworkAssembly = typeof(Atari2600Game).Assembly;
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
            using (var Stream = new MemoryStream())
            {
                Compilation.Emit(Stream);
                CompiledAssembly = Assembly.Load(Stream.ToArray());
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
            SetupGlobals(Class.Members.OfType<FieldDeclarationSyntax>());
            var SpecialMethods = GetSpecialMethods(Class);
            foreach(var Method in SpecialMethods)
            {
                //@TODO - Catch invocation of user methods.
                var CompiledMethod = CompileMethod(Method);
                ROMBuilder.AddSubroutine(CompiledMethod);
            }
            
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

        private Subroutine CompileMethod(MethodDeclarationSyntax Method)
        {
            var Attribute = GetSpecialMethodAttribute(Method);
            var MethodType = Attribute?.GameMethod ?? Framework.MethodType.UserDefined;
            var Builder = new SubroutineBuilder(this, Method.Identifier.Text, MethodType);
            var Expressions = Method.Body.ChildNodes().OfType<ExpressionStatementSyntax>();
            foreach(var ExpressionStatement in Expressions)
            {
                var Expression = ExpressionStatement.ChildNodes().Single();
                // Emit source line as a comment for debugging.
                var CommentPieces = Expression.ToString().Split(new[] { Environment.NewLine, " " }, StringSplitOptions.RemoveEmptyEntries);
                var Comment = string.Join(" ", CommentPieces);
                Builder.Append(Instructions.Comment(Comment));
                // Lets us give each expression its own convenient method at the cost of invoking the DLR.
                // I have absolutely zero concerns about performance.
                Builder.Append(Expression as dynamic);
            }
            return Builder.ToSubroutine();
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

        private void SetupGlobals(IEnumerable<FieldDeclarationSyntax> Fields)
        {
            foreach (var Field in Fields)
            {
                var RealType = GetType(Field.Declaration.Type);
                var VariableDeclator = Field.Declaration.DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
                var VariableName = VariableDeclator.Identifier.ToString();
                ROMBuilder.AddGlobalVariable(RealType, VariableName);
            }
        }

        private Type GetType(TypeSyntax TypeSyntax)
        {
            var Info = Model.GetTypeInfo(TypeSyntax);
            var FullyQualifiedNameFormat = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
            var FullyQualifiedName = Info.Type.ToDisplayString(FullyQualifiedNameFormat);
            //@TODO - Won't find types outside of mscorlib.
            var TrueType = Type.GetType(FullyQualifiedName);
            if(TrueType == null)
            {
                throw new ArgumentException("TypeSyntaxes must correspond to an mscorlib type for now.", nameof(TypeSyntax));
            }
            return TrueType;
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
