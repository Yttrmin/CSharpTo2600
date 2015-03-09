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
            private LocalVariableManager VariableManager;

            private MethodType MethodType
            {
                get { return MethodInfo.GetCustomAttribute<SpecialMethodAttribute>()?.GameMethod ?? MethodType.UserDefined; }
            }

            public MethodCompiler(MethodDeclarationSyntax MethodDeclaration, MethodInfo MethodInfo, GameCompiler Compiler)
            {
                this.MethodInfo = MethodInfo;
                this.Compiler = Compiler;
                Name = MethodDeclaration.Identifier.Text;
                this.MethodDeclaration = MethodDeclaration;
                MethodBody = new List<AssemblyLine>();
                TypeStack = new Stack<Type>();
                VariableManager = new LocalVariableManager(Compiler.ROMBuilder.VariableManager);
            }

            public Subroutine Compile()
            {
                VariableManager = new CompilerPrePassLocals(Compiler, MethodDeclaration).Process();
                AllocateLocals();
                Visit(MethodDeclaration);
                return new Subroutine(Name, MethodInfo, MethodBody.ToImmutableArray(), MethodType);
            }

            private void AllocateLocals()
            {
                foreach (var Variable in VariableManager.GetLocalScopeVariables().Cast<LocalVariable>().OrderBy(v => v.Address.Start))
                {
                    MethodBody.AddRange(Fragments.AllocateLocal(Variable));
                }
            }

            public override void Visit(SyntaxNode node)
            {
                DebugPrintNode(node);
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
                base.VisitAssignmentExpression(node);
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

            private VariableInfo GetVariable(ISymbol Symbol)
            {
                if (Symbol.ContainingAssembly.ToString() == Compiler.FrameworkAssembly.ToString())
                {
                    var ContainingType = Compiler.FrameworkAssembly.GetType(Symbol.ContainingType.ToString(), true);
                    var Member = ContainingType.GetMember(Symbol.Name, MemberTypes.Property,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Single();
                    var GlobalName = Member.GetCustomAttribute<CompilerIntrinsicGlobalAttribute>().GlobalSymbol.Name;
                    return VariableManager.GetVariable(GlobalName);
                }
                else
                {
                    return VariableManager.GetVariable(Symbol.Name);
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

            private sealed class CompilerPrePassLocals : CSharpSyntaxWalker
            {
                private LocalVariableManager VariableManager;
                private readonly MethodDeclarationSyntax Method;
                private readonly GameCompiler Compiler;

                public CompilerPrePassLocals(GameCompiler Compiler, MethodDeclarationSyntax Method)
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
                    foreach (var Identifier in Identifiers)
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
                    if (!VariableManager.GetLocalScopeVariables().Any())
                    {
                        return 0;
                    }
                    return VariableManager.GetLocalScopeVariables().Max(v => v.Address.End) + 1;
                }
            }
        }
    }
}
