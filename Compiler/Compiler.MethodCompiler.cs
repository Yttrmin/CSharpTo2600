using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CSharpTo2600.Framework;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;

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
            private LocalVariableManager VariableManager;

            public MethodCompiler(MethodDeclarationSyntax MethodDeclaration, Compiler Compiler)
            {
                this.Compiler = Compiler;
                Name = MethodDeclaration.Identifier.Text;
                this.MethodDeclaration = MethodDeclaration;
                MethodInstructions = new List<InstructionInfo>();
                TypeStack = new Stack<Type>();
                VariableManager = new LocalVariableManager(Compiler.ROMBuilder.VariableManager);
            }

            public Subroutine Compile()
            {
                VariableManager = new CompilerPrePassLocals(Compiler, MethodDeclaration).Process();
                AllocateLocals();
                Visit(MethodDeclaration);
                return new Subroutine(Name, MethodInstructions.ToImmutableArray(), MethodType.Initialize);
            }

            private void AllocateLocals()
            {
                foreach(var Variable in VariableManager.GetLocalScopeVariables().OrderBy(v => v.Address.Start))
                {
                    //@TODO - Symbol
                    var size = 0;
                    MethodInstructions.AddRange(Fragments.AllocateLocal(Variable.Type, out size));
                }
            }

            public override void Visit(SyntaxNode node)
            {
                DebugPrintNode(node);
                base.Visit(node);
            }

            public override void VisitEqualsValueClause(EqualsValueClauseSyntax node)
            {
                base.VisitEqualsValueClause(node);
                var Declarator = (VariableDeclaratorSyntax)node.Parent;
                var Identifier = Declarator.Identifier.ToString();
                throw new NotImplementedException();
            }

            public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
            {
                base.VisitAssignmentExpression(node);
                var Type = TypeStack.Pop();
                // At the point of assignment there should only be one thing on the TypeStack, the
                // type of the result of the right-side expression.
                Debug.Assert(TypeStack.Count == 0);
                var LeftSideIdentifier = ((IdentifierNameSyntax)node.Left).Identifier.Text;
                var Variable = VariableManager.GetVariable(LeftSideIdentifier);
                MethodInstructions.AddRange(Fragments.StoreVariable(Variable, Type));
            }

            public override void VisitCastExpression(CastExpressionSyntax node)
            {
                base.VisitCastExpression(node);
                var From = TypeStack.Pop();
                var ToType = Compiler.GetType(node.Type);
                if(!IsCastable(From, ToType))
                {
                    throw new FatalCompilationException($"Cannot perform typecast from: {From} to {ToType}");
                }
                MethodInstructions.AddRange(Fragments.Fit(From, ToType));
                TypeStack.Push(ToType);
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
                var Variable = VariableManager.GetVariable(node.Identifier.Text);
                TypeStack.Push(Variable.Type);
                MethodInstructions.AddRange(Fragments.PushVariable(Variable.Name, Variable.Type));
                base.VisitIdentifierName(node);
            }

            [Obsolete("Fragments")]
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
                // Make sure to add these from smallest to largest!
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
                Console.WriteLine($"{node.GetType().Name}");
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

            private sealed class CompilerPrePassLocals : CSharpSyntaxWalker
            {
                private LocalVariableManager VariableManager;
                private readonly MethodDeclarationSyntax Method;
                private readonly Compiler Compiler;

                public CompilerPrePassLocals(Compiler Compiler, MethodDeclarationSyntax Method)
                {
                    this.Compiler = Compiler;
                    VariableManager = new LocalVariableManager(Compiler.ROMBuilder.VariableManager);
                    this.Method = Method;
                }

                public LocalVariableManager Process()
                {
                    Visit(Method);
                    return VariableManager;
                }

                public override void VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
                {
                    // Child scopes (blocks) shouldn't be an issue since that only affects accessibility,
                    // and Roslyn will already ensure there's no errors. It shouldn't have an effect
                    // on measuring lifetime either.
                    var Identifiers = node.Declaration.Variables.Select(v => v.Identifier.ToString());//Single().Identifier.ToString();
                    var TypeSyntax = node.Declaration.Type;
                    var LocalType = Compiler.GetType(TypeSyntax);

                    // It could be possible to just use unused globals space and save the hassle of
                    // fiddling with the stack frame, but then we'd have to somehow statically track
                    // when and what addresses are used. Could give each local a global address if
                    // there's enough? Let's just stick with stack for now for simplicity.
                    foreach(var Identifier in Identifiers)
                    {
                        var AddressStart = NextOffset();
                        var AddressEnd = NextOffset() + Marshal.SizeOf(LocalType) - 1;
                        var Address = new Range(AddressStart, AddressEnd);
                        VariableManager = VariableManager.AddVariable(Identifier, LocalType, Address);
                    }
                    base.VisitLocalDeclarationStatement(node);
                }

                //@TODO - Anticipate return address when we get to method calls.
                private int NextOffset()
                {
                    if(!VariableManager.GetLocalScopeVariables().Any())
                    {
                        return 0;
                    }
                    return VariableManager.GetLocalScopeVariables().Max(v => v.Address.End) + 1;
                }
            }
        }
    }
}
