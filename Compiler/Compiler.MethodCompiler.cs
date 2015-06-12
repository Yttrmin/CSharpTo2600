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
using System.Linq;

namespace CSharpTo2600.Compiler
{
    partial class GameCompiler
    {
        /// <summary>
        /// Responsible for turning a C# method into a 6502-compatibile Subroutine.
        /// </summary>
        private sealed class MethodCompiler : CSharpSyntaxWalker
        {
            private static readonly IVariableInfo ReturnValue;

            //@TODO - Test compiler Optimize setting affecting this.
            private readonly IOptimizer Optimizer;
            private readonly SemanticModel Model;
            private readonly MethodDeclarationSyntax MethodDeclaration;
            private readonly string Name;
            private readonly List<AssemblyLine> MethodBody;
            //@TODO - Use ProcessedType, not Type
            private readonly Stack<Type> TypeStack;
            private readonly CompilationState CompilationState;
            private readonly MethodType MethodType;
            private bool IsPerformanceCritical { get { return MethodType == MethodType.Kernel; } }

            static MethodCompiler()
            {
                ReturnValue = VariableInfo.CreateDirectlyAddressableCustomVariable("_ReturnValue", typeof(byte), 0x80);
            }

            private MethodCompiler(MethodDeclarationSyntax MethodDeclaration, MethodInfo MethodInfo, 
                INamedTypeSymbol ContainingType, CompilationState CompilationState, SemanticModel Model,
                bool Optimize)
            {
                MethodType = MethodInfo.GetCustomAttribute<SpecialMethodAttribute>()?.GameMethod ?? MethodType.UserDefined;
                this.CompilationState = CompilationState;
                this.Model = Model;
                Name = MethodDeclaration.Identifier.Text;
                this.MethodDeclaration = MethodDeclaration;
                if (Optimize)
                {
                    Optimizer = new Optimizer();
                }
                else
                {
                    Optimizer = new NullOptimizer();
                }
                MethodBody = new List<AssemblyLine>();
                TypeStack = new Stack<Type>();
            }

            /// <summary>
            /// Compiles a C# method into a Subroutine.
            /// Any types, fields, methods, etc that this method relies must have
            /// already been parsed (not compiled) previously.
            /// </summary>
            public static Subroutine CompileMethod(MethodInfo MethodInfo, IMethodSymbol Symbol, 
                CompilationState CompilationState, SemanticModel Model, bool Optimize)
            {
                var MethodDeclaration = (MethodDeclarationSyntax)Symbol.DeclaringSyntaxReferences.Single().GetSyntax();
                var Compiler = new MethodCompiler(MethodDeclaration, MethodInfo, 
                    Symbol.ContainingType, CompilationState, Model, Optimize);
                Compiler.Visit(MethodDeclaration);
                // Assume a void return.
                Compiler.MethodBody.Add(AssemblyFactory.RTS());
                var Subroutine = new Subroutine(Compiler.Name, MethodInfo, Symbol, 
                    Compiler.MethodBody.ToImmutableArray(), Compiler.MethodType);
                Subroutine = Compiler.Optimizer.PerformAllOptimizations(Subroutine);
                return Subroutine;
            }

            public override void Visit(SyntaxNode node)
            {
                DebugPrintNode(node);
                // Emit the C# code as a comment so there's a frame of reference
                // among all the assembly code.
                var ReturnNode = node as ReturnStatementSyntax;
                if (node.Parent is BlockSyntax && !(node is BlockSyntax)) // Don't emit whole blocks of code
                {
                    // If we don't trim trivia then source comments will get included.
                    // This can mess up the assembly comment due to newlines, and is
                    // also just useless clutter.
                    var Text = node.WithoutTrivia().GetText().ToString().Trim();
                    MethodBody.Add(AssemblyFactory.Comment(Text, 0));
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
                var Symbol = Model.GetSymbolInfo(node.Left).Symbol;
                var Variable = GetVariable(Symbol);
                MethodBody.AddRange(Fragments.StoreVariable(Variable, Type));
            }

            public override void VisitCastExpression(CastExpressionSyntax node)
            {
                base.VisitCastExpression(node);
                var FromType = TypeStack.Pop();
                var ToType = GetType(Model.GetTypeInfo(node).Type);
                MethodBody.AddRange(Fragments.Cast(FromType, ToType));
                TypeStack.Push(ToType);
            }

            public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
            {
                base.VisitPostfixUnaryExpression(node);
                var Type = TypeStack.Pop();
                var Symbol = Model.GetSymbolInfo(node.Operand).Symbol;
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
                var LiteralTypeSymbol = Model.GetTypeInfo(node).ConvertedType;
                var LiteralType = GetType(LiteralTypeSymbol);
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

            public override void VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                // We can assume void return, 0 parameters, since it is checked during parsing.
                var MethodSymbol = (IMethodSymbol)Model.GetSymbolInfo(node).Symbol;
                var Subroutine = CompilationState.GetSubroutineFromSymbol(MethodSymbol);
                if (Subroutine.Type != MethodType.UserDefined)
                {
                    throw new AttemptedToInvokeSpecialMethodException(Subroutine, Name);
                }
                MethodBody.AddRange(Fragments.Invoke(Subroutine));

                if (!MethodSymbol.ReturnsVoid)
                {
                    MethodBody.AddRange(Fragments.PushVariable(ReturnValue));
                    // Can only be a byte if we're assigning it to a variable.
                    TypeStack.Push(typeof(byte));
                }
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
                var Symbol = Model.GetSymbolInfo(node).Symbol;
                var Variable = GetVariable(Symbol);
                TypeStack.Push(Variable.Type);
                MethodBody.AddRange(Fragments.PushVariable(Variable));
                base.VisitIdentifierName(node);
            }

            /// How do return values work?
            /// There's a special byte variable called _ReturnValue.
            /// "return [expression];" is logically just "_ReturnValue = [expression]; return;"
            /// In terms of performance it's actually superior to messing with the stack like in
            /// a conventional call stack. The downside is that (when we support more types), 
            /// _ReturnValue will have to be sized to the largest type returned by a method,
            /// instead of just allocating stack space as needed.
            public override void VisitReturnStatement(ReturnStatementSyntax node)
            {
                // If it's a void return, nothing to do.
                if(node.Expression == null)
                {
                    return;
                }

                base.VisitReturnStatement(node);
                // Only non-void type we return is a byte.
                MethodBody.AddRange(Fragments.StoreVariable(ReturnValue, typeof(byte)));
                // Don't emit RTS since that's always appended to the end of a subroutine in CompileMethod().
            }

            private IVariableInfo GetVariable(MemberAccessExpressionSyntax Node)
            {
                var FieldSymbol = (IFieldSymbol)Model.GetSymbolInfo(Node.Name).Symbol;
                return GetVariable(FieldSymbol);
            }

            private IVariableInfo GetVariable(ISymbol Symbol)
            {
                var FieldSymbol = Symbol as IFieldSymbol;
                if (FieldSymbol != null)
                {
                    return CompilationState.GetVariableFromField(FieldSymbol);
                }
                //@TODO @DELETEME - Support properties in general, no special case.
                else if (Symbol is IPropertySymbol && Symbol.ContainingType.Name == nameof(TIARegisters))
                {
                    var Type = typeof(TIARegisters);
                    var Property = Type.GetTypeInfo().GetDeclaredProperty(Symbol.Name);
                    var IntrinsicAttribte = Property.GetCustomAttribute<CompilerIntrinsicGlobalAttribute>();
                    return VariableInfo.CreateRegisterVariable(IntrinsicAttribte.GlobalSymbol);
                }
                else
                {
                    throw new FatalCompilationException($"Attempted to access something other than a static field: {Symbol.Name}");
                }
            }

            [Obsolete("Use only Symbols.")]
            private Type GetType(ITypeSymbol TypeSymbol)
            {
                var FullyQualifiedNameFormat = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
                var FullyQualifiedName = TypeSymbol.ToDisplayString(FullyQualifiedNameFormat);
                //@TODO - Won't find types outside of mscorlib.
                var TrueType = Type.GetType(FullyQualifiedName);
                if (TrueType == null)
                {
                    throw new ArgumentException("TypeSyntaxes must correspond to an mscorlib type for now.", nameof(TypeSymbol));
                }
                return TrueType;
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
