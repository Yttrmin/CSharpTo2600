using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpTo2600.Compiler
{
    public sealed partial class Compiler
    {
        private sealed class GameClassCompiler : CSharpSyntaxWalker
        {
            private readonly Compiler Compiler;
            private SemanticModel Model { get { return Compiler.Model; } }
            private ROMBuilder ROMBuilder { get { return Compiler.ROMBuilder; } }

            public GameClassCompiler(Compiler Compiler)
            {
                this.Compiler = Compiler;
            }

            public override void VisitFieldDeclaration(FieldDeclarationSyntax FieldNode)
            {
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
                if(node.Initializer != null)
                {
                    throw new FatalCompilationException("Properties can't have initializers yet.", node);
                }
                var Type = Compiler.GetType(node.Type);
                var Accessors = node.AccessorList.Accessors;
                var AutoImplemented = Accessors.Any(a => a.Body == null);
                if(AutoImplemented)
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
                var MethodCompiler = new MethodCompiler(node, Compiler);
                var Subroutine = MethodCompiler.Compile();
                ROMBuilder.AddSubroutine(Subroutine);
                base.VisitMethodDeclaration(node);
            }
        }
    }
}
