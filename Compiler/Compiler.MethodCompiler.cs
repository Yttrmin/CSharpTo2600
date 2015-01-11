using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpTo2600.Compiler
{
    partial class Compiler
    {
        private sealed class MethodCompiler : CSharpSyntaxWalker
        {
            private readonly MethodDeclarationSyntax MethodDeclaration;
            private readonly string Name;

            public MethodCompiler(MethodDeclarationSyntax MethodDeclaration)
            {
                Name = MethodDeclaration.Identifier.Text;
                this.MethodDeclaration = MethodDeclaration;
            }

            public Subroutine Compile()
            {
                Visit(MethodDeclaration);
                throw new NotImplementedException();
            }

            public override void Visit(SyntaxNode node)
            {
                DebugPrintNode(node);
                base.Visit(node);
            }

            private void DebugPrintNode(SyntaxNode node)
            {
                for (var i = 0; i < DebugChildLevel(node); i++)
                {
                    Console.Write(" ");
                }
                Console.WriteLine("\{node.GetType().Name}");
            }

            private int DebugChildLevel(SyntaxNode n)
            {
                var i = 0;
                SyntaxNode IterNode = n;
                while (IterNode != MethodDeclaration)
                {
                    i++;
                    IterNode = IterNode.Parent;
                }
                return i;
            }
        }
    }
}
