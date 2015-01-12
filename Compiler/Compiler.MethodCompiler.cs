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
            private readonly Stack<Type> TypeStack;

            public MethodCompiler(MethodDeclarationSyntax MethodDeclaration, Compiler Compiler)
            {
                this.Compiler = Compiler;
                Name = MethodDeclaration.Identifier.Text;
                this.MethodDeclaration = MethodDeclaration;
                MethodInstructions = new List<InstructionInfo>();
                Temps = new Dictionary<VariableDeclarationSyntax, int>();
                TypeStack = new Stack<Type>();
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

            public override void VisitBinaryExpression(BinaryExpressionSyntax node)
            {
                var Kind = node.CSharpKind();
                if (Kind == SyntaxKind.SubtractExpression || Kind == SyntaxKind.DivideExpression)
                {
                    base.Visit(node.Right);
                    base.Visit(node.Left);
                }
                else
                {
                    base.Visit(node.Left);
                    base.Visit(node.Right);
                }
                
                var TypeA = TypeStack.Pop();
                var TypeB = TypeStack.Pop();
                if(TypeA != TypeB)
                {
                    throw new InvalidOperationException("Types don't match");
                }
                switch(node.CSharpKind())
                {
                    case SyntaxKind.AddExpression:
                        MethodInstructions.AddRange(Fragments.Add(TypeA));
                        TypeStack.Push(TypeA);
                        break;
                    case SyntaxKind.SubtractExpression:
                        MethodInstructions.AddRange(Fragments.Subtract(TypeA));
                        TypeStack.Push(TypeA);
                        break;
                    default:
                        throw new NotImplementedException(node.CSharpKind().ToString());
                }
                
            }

            public override void VisitLiteralExpression(LiteralExpressionSyntax node)
            {
                var Value = ToSmallestNumeric(node.Token.Value);
                TypeStack.Push(Value.GetType());
                MethodInstructions.AddRange(Fragments.PushLiteral(Value));
                base.VisitLiteralExpression(node);
            }

            private object ToSmallestNumeric(object Value)
            {
                // May have to try ulong as well.
                // Roundabout since you can only unbox to its actual type.
                var NumericValue = long.Parse(Value.ToString());
                if(byte.MinValue <= NumericValue && NumericValue <= byte.MaxValue)
                {
                    return (byte)NumericValue;
                }
                throw new ArgumentException("Value does not fit in a supported type.");
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
