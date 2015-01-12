using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CSharpTo2600.Framework;
using System.Linq;

namespace CSharpTo2600.Compiler
{
    partial class Compiler
    {
        private sealed class MethodCompiler : CSharpSyntaxWalker
        {
            private readonly Compiler Compiler;
            private readonly MethodDeclarationSyntax MethodDeclaration;
            private readonly string Name;
            private readonly List<InstructionInfo> MethodInstructions;
            private readonly Dictionary<VariableDeclarationSyntax, int> Temps;

            public MethodCompiler(MethodDeclarationSyntax MethodDeclaration, Compiler Compiler)
            {
                this.Compiler = Compiler;
                Name = MethodDeclaration.Identifier.Text;
                this.MethodDeclaration = MethodDeclaration;
                MethodInstructions = new List<InstructionInfo>();
                Temps = new Dictionary<VariableDeclarationSyntax, int>();
            }

            public Subroutine Compile()
            {
                Visit(MethodDeclaration);
                if(Temps.Count > 0)
                {
                    foreach(var Key in Temps.Keys)
                    {
                        MethodInstructions.AddRange(Fragments.DeallocateLocal(Compiler.GetType(Key.Type)));
                    }
                    Temps.Clear();
                }
                //@TODO
                return new Subroutine(Name, MethodInstructions.ToImmutableArray(), MethodType.Initialize);
            }

            public override void Visit(SyntaxNode node)
            {
                DebugPrintNode(node);
                base.Visit(node);
            }

            public override void VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
            {
                var Declarator = node.Declaration.Variables.Single();
                var Identifier = Declarator.Identifier;
                var TypeSyntax = node.Declaration.Type;
                int Size;
                MethodInstructions.AddRange(Fragments.AllocateLocal(Compiler.GetType(TypeSyntax), out Size));
                foreach(var Key in Temps.Keys.ToArray())
                {
                    Temps[Key] += Size;
                }
                Temps[node.Declaration] = 0;
                base.VisitLocalDeclarationStatement(node);
            }

            public override void VisitEqualsValueClause(EqualsValueClauseSyntax node)
            {
                base.VisitEqualsValueClause(node);
            }

            public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
            {
                base.VisitAssignmentExpression(node);
            }

            public override void VisitBinaryExpression(BinaryExpressionSyntax node)
            {
                base.VisitBinaryExpression(node);
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
