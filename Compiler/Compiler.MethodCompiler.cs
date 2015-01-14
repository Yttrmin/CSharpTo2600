using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CSharpTo2600.Framework;
using System.Linq;
using System.Diagnostics;

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
            private readonly Stack<Type> TypeStack;

            public MethodCompiler(MethodDeclarationSyntax MethodDeclaration, Compiler Compiler)
            {
                this.Compiler = Compiler;
                Name = MethodDeclaration.Identifier.Text;
                this.MethodDeclaration = MethodDeclaration;
                MethodInstructions = new List<InstructionInfo>();
                TypeStack = new Stack<Type>();
            }

            public Subroutine Compile()
            {
                Visit(MethodDeclaration);
                return new Subroutine(Name, MethodInstructions.ToImmutableArray(), MethodType.Initialize);
            }

            public override void Visit(SyntaxNode node)
            {
                DebugPrintNode(node);
                base.Visit(node);
            }

            public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
            {
                base.VisitAssignmentExpression(node);
                var Type = TypeStack.Pop();
                // At the point of assignment there should only be one thing on the TypeStack, the
                // type of the result of the right-side expression.
                Debug.Assert(TypeStack.Count == 0);
                var Global = Compiler.ROMBuilder.GetGlobal(((IdentifierNameSyntax)node.Left).Identifier.Text);
                if(!IsCastable(Type, Global.Type))
                {
                    throw new FatalCompilationException("Types don't match for assignment: \{Type} to \{Global.Type}");
                }
                else if(Type != Global.Type)
                {
                    MethodInstructions.AddRange(Fragments.Fit(Type, Global.Type));
                }
                MethodInstructions.AddRange(Fragments.StoreVariable(Global.Name, Global.Type));
            }

            public override void VisitBinaryExpression(BinaryExpressionSyntax node)
            {
                var Kind = node.CSharpKind();
                // Handles commutativity?
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

            public override void VisitIdentifierName(IdentifierNameSyntax node)
            {
                var Assignment = node.Parent as AssignmentExpressionSyntax;
                // If this identifier is from the left side of an assignment,
                // don't push it onto the stack.
                if(Assignment?.Left == node)
                {
                    base.VisitIdentifierName(node);
                    return;
                }
                //@TODO - Support locals. Part of ROMBuilder?
                var Global = Compiler.ROMBuilder.GetGlobal(node.Identifier.Text);
                TypeStack.Push(Global.Type);
                MethodInstructions.AddRange(Fragments.PushVariable(Global.Name, Global.Type));
                base.VisitIdentifierName(node);
            }

            private bool IsCastable(Type From, Type To)
            {
                //@TODO - Not complete.
                if(From == To || From.IsAssignableFrom(To))
                {
                    return true;
                }
                if(From.IsPrimitive && To.IsPrimitive)
                {
                    return true;
                }
                return false;
            }

            private object ToSmallestNumeric(object Value)
            {
                //@TODO - May have to try ulong as well.
                // Roundabout since you can only unbox to its actual type.
                var NumericValue = long.Parse(Value.ToString());
                if(byte.MinValue <= NumericValue && NumericValue <= byte.MaxValue)
                {
                    return (byte)NumericValue;
                }
                if (int.MinValue <= NumericValue && NumericValue <= int.MaxValue)
                {
                    return (int)NumericValue;
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
