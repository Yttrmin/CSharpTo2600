using System;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpTo2600.Compiler
{
    public sealed partial class GameCompiler
    {
        private sealed class GameClassCompiler : CSharpSyntaxWalker
        {
            private readonly GameCompiler Compiler;
            private SemanticModel Model { get { return Compiler.Model; } }
            private ROMBuilder ROMBuilder { get { return Compiler.ROMBuilder; } }
            private readonly IOptimizer Optimizer;
            private readonly Type ClassType;
            private readonly ClassDeclarationSyntax Root;

            public GameClassCompiler(GameCompiler Compiler, Type ClassType)
            {
                this.Compiler = Compiler;
                this.ClassType = ClassType;

                if (!(ClassType.IsAbstract && ClassType.IsSealed))
                {
                    throw new GameClassNotStaticException(ClassType);
                }

                Root = this.Compiler.Root.DescendantNodes().Where(n => (n as ClassDeclarationSyntax)?.Identifier.Text == ClassType.Name)
                    .Cast<ClassDeclarationSyntax>().Single();
                if (Compiler.Options.Optimize)
                {
                    Optimizer = new Optimizer();
                }
                else
                {
                    Optimizer = new NullOptimizer();
                }
            }

            public void Compile()
            {
                Visit(Root);
            }

            public override void VisitFieldDeclaration(FieldDeclarationSyntax FieldNode)
            {
                if (FieldNode.Declaration.Variables.Any(v => v.Initializer != null))
                {
                    throw new FatalCompilationException("Fields can't have initializers yet.", FieldNode);
                }
                var RealType = Compiler.GetType(FieldNode.Declaration.Type);
                foreach (var Declarator in FieldNode.Declaration.Variables)
                {
                    var VariableName = Declarator.Identifier.ToString();
                    ROMBuilder.AddGlobalVariable(RealType, VariableName);
                }
                base.VisitFieldDeclaration(FieldNode);
            }

            public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
            {
                if (node.Initializer != null)
                {
                    throw new FatalCompilationException("Properties can't have initializers yet.", node);
                }
                var Type = Compiler.GetType(node.Type);
                var Accessors = node.AccessorList.Accessors;
                var AutoImplemented = Accessors.Any(a => a.Body == null);
                if (AutoImplemented)
                {
                    // Auto-implemented properties are just thin wrappers over private backing fields.
                    // There's no point compiling get_/set_ methods, just treat it as a regular global.
                    var VariableName = node.Identifier.Text;
                    ROMBuilder.AddGlobalVariable(Type, VariableName);
                }
                else
                {
                    throw new FatalCompilationException("Only auto-implemented properties are supported yet.", node);
                }
                base.VisitPropertyDeclaration(node);
            }

            public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                var MethodInfo = GetMethodInfo(node);
                var MethodCompiler = new MethodCompiler(node, MethodInfo, Compiler);
                var Subroutine = MethodCompiler.Compile();
                Subroutine = Optimizer.PerformAllOptimizations(Subroutine);
                ROMBuilder.AddSubroutine(Subroutine);
                base.VisitMethodDeclaration(node);
            }

            private MethodInfo GetMethodInfo(MethodDeclarationSyntax node)
            {
                //@TODO - Probably won't work properly with overloaded methods.
                var Method = ClassType.GetMethod(node.Identifier.Text, BindingFlags.Public | BindingFlags.NonPublic
                    | BindingFlags.Static);
                if (Method == null)
                {
                    throw new FatalCompilationException($"Reflection failed to find method: {node.Identifier.Text}", node);
                }
                return Method;
            }
        }
    }
}
