using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CSharpTo2600.Framework;
using System.Diagnostics;
using CSharpTo2600.Framework.Assembly;
using System.Reflection;

namespace CSharpTo2600.Compiler
{
    partial class GameCompiler
    {
        private sealed class MethodCompiler : CSharpSyntaxWalker
        {
            private readonly GameCompiler Compiler;
            private readonly MethodDeclarationSyntax MethodDeclaration;
            private readonly string Name;
            private readonly List<AssemblyLine> MethodBody;
            private readonly Stack<Type> TypeStack;
            private readonly MethodInfo MethodInfo;
            private readonly CompilationInfo CompilationInfo;

            private MethodType MethodType
            {
                get { return MethodInfo.GetCustomAttribute<SpecialMethodAttribute>()?.GameMethod ?? MethodType.UserDefined; }
            }

            private MethodCompiler(MethodDeclarationSyntax MethodDeclaration, MethodInfo MethodInfo, 
                INamedTypeSymbol ContainingType, CompilationInfo CompilationInfo, GameCompiler Compiler)
            {
                this.MethodInfo = MethodInfo;
                this.CompilationInfo = CompilationInfo;
                this.Compiler = Compiler;
                Name = MethodDeclaration.Identifier.Text;
                this.MethodDeclaration = MethodDeclaration;
                MethodBody = new List<AssemblyLine>();
                TypeStack = new Stack<Type>();
            }

            public static Subroutine CompileMethod(MethodDeclarationSyntax MethodDeclaration, 
                MethodInfo MethodInfo, IMethodSymbol Symbol, CompilationInfo CompilationInfo, 
                GameCompiler GCompiler)
            {
                var Compiler = new MethodCompiler(MethodDeclaration, MethodInfo, Symbol.ContainingType, CompilationInfo, GCompiler);
                Compiler.Visit(MethodDeclaration);
                return new Subroutine(Compiler.Name, MethodInfo, Symbol, Compiler.MethodBody.ToImmutableArray(), 
                    Compiler.MethodType);
            }

            public override void Visit(SyntaxNode node)
            {
                DebugPrintNode(node);
                if (node.Parent is BlockSyntax && !(node is BlockSyntax))
                {
                    MethodBody.Add(AssemblyFactory.Comment(node.GetText().ToString().Trim(), 0));
                }
                base.Visit(node);
            }

            public override void VisitAttributeList(AttributeListSyntax node)
            {
                // Don't care about attributes. Not calling base since it'll eventually
                // hit an identifier, and this will attempt to treat it as a variable and
                // push it which we obviously don't want.
                // If you want attributes use reflection.
                return;
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
                if (node.Kind() != SyntaxKind.SimpleAssignmentExpression)
                {
                    throw new FatalCompilationException("Only simple assignment currently permitted.");
                }
                // Don't visit left so we don't end up pushing it on the stack.
                base.Visit(node.Right);
                var Type = TypeStack.Pop();
                // At the point of assignment there should only be one thing on the TypeStack, the
                // type of the result of the right-side expression.
                Debug.Assert(TypeStack.Count == 0);
                var Symbol = Compiler.Model.GetSymbolInfo(node.Left).Symbol;
                var Variable = GetVariable(Symbol);
                MethodBody.AddRange(Fragments.StoreVariable(Variable, Type));
            }

            public override void VisitCastExpression(CastExpressionSyntax node)
            {
                base.VisitCastExpression(node);
                var FromType = TypeStack.Pop();
                var ToType = Compiler.GetType(node.Type);
                MethodBody.AddRange(Fragments.Cast(FromType, ToType));
                TypeStack.Push(ToType);
            }

            public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
            {
                base.VisitPostfixUnaryExpression(node);
                var Type = TypeStack.Pop();
                var Symbol = Compiler.Model.GetSymbolInfo(node.Operand).Symbol;
                var Variable = GetVariable(Symbol);
                switch (node.Kind())
                {
                    case SyntaxKind.PostIncrementExpression:
                        MethodBody.AddRange(Fragments.Add(Variable, 1));
                        // Reuse type from TypeStack since it'll be the same.
                        MethodBody.AddRange(Fragments.StoreVariable(Variable, Type));
                        break;
                    case SyntaxKind.PostDecrementExpression:
                        throw new NotImplementedException();
                    default:
                        throw new FatalCompilationException("Unknown postfix unary kind.");
                }
            }

            public override void VisitBinaryExpression(BinaryExpressionSyntax node)
            {
                var Kind = node.Kind();
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
                if (TypeA != TypeB)
                {
                    throw new InvalidOperationException("Types don't match");
                }
                switch (node.Kind())
                {
                    case SyntaxKind.AddExpression:
                        MethodBody.AddRange(Fragments.Add(TypeA));
                        TypeStack.Push(TypeA);
                        break;
                    case SyntaxKind.SubtractExpression:
                        MethodBody.AddRange(Fragments.Subtract(TypeA));
                        TypeStack.Push(TypeA);
                        break;
                    default:
                        throw new NotImplementedException(node.Kind().ToString());
                }

            }

            public override void VisitLiteralExpression(LiteralExpressionSyntax node)
            {
                var LiteralTypeSymbol = Compiler.Model.GetTypeInfo(node).ConvertedType;
                var LiteralType = Compiler.GetType(LiteralTypeSymbol);
                var ParseMethod = LiteralType.GetMethod("Parse", new[] { typeof(string) });
                var Value = ParseMethod.Invoke(null, new[] { node.Token.ValueText });
                TypeStack.Push(LiteralType);
                MethodBody.AddRange(Fragments.PushLiteral(Value));
                base.VisitLiteralExpression(node);
            }

            public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            {
                var Variable = GetVariable(node);
                TypeStack.Push(Variable.Type);
                MethodBody.AddRange(Fragments.PushVariable(Variable));
                // Return so we don't trigger IdentifierName handling.
                return;
            }

            public override void VisitIdentifierName(IdentifierNameSyntax node)
            {
                var Assignment = node.Parent as AssignmentExpressionSyntax;
                // If this identifier is from the left side of an assignment,
                // don't push it onto the stack.
                if (Assignment?.Left == node)
                {
                    base.VisitIdentifierName(node);
                    return;
                }
                var Symbol = Compiler.Model.GetSymbolInfo(node).Symbol;
                var Variable = GetVariable(Symbol);
                TypeStack.Push(Variable.Type);
                MethodBody.AddRange(Fragments.PushVariable(Variable));
                base.VisitIdentifierName(node);
            }

            private VariableInfo GetVariable(MemberAccessExpressionSyntax Node)
            {
                var FieldSymbol = (IFieldSymbol)Compiler.Model.GetSymbolInfo(Node.Name).Symbol;
                return GetVariable(FieldSymbol);
            }

            private VariableInfo GetVariable(ISymbol Symbol)
            {
                var FieldSymbol = Symbol as IFieldSymbol;
                if (FieldSymbol != null)
                {
                    return CompilationInfo.GetVariableFromField(FieldSymbol);
                }
                else
                {
                    throw new FatalCompilationException($"Attempted to access something other than a static field: {Symbol.Name}");
                }
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
        }
    }
}
