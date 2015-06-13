using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpTo2600.Compiler
{
    public sealed partial class GameCompiler
    {
        // The hierarchy only tells you if a method may call another method ever.
        // It may call a method 0 times (e.g. if(SomeConditionFalseAtRuntime){Call();} )
        // It may call a method 1 time.
        // It may call a method some arbitrary number of times (loops, gotos, etc).
        // That specific number (including 0 or 1) is not provided.
        // The absense of a method from a Calls list indicates that the method will never
        // be called from there, specifically by no invocation for that method existing
        // anywhere in the body of the method.
        /// <summary>
        /// Immutable representation of a method call hierarchy.
        /// </summary>
        internal sealed class MethodCallHierarchy
        {
            private readonly ImmutableDictionary<IMethodSymbol, HierarchyNode> SymbolToNode;
            public static readonly MethodCallHierarchy Empty
                = new MethodCallHierarchy(ImmutableDictionary<IMethodSymbol, HierarchyNode>.Empty);

            private MethodCallHierarchy(ImmutableDictionary<IMethodSymbol, HierarchyNode> SymbolToNode)
            {
                this.SymbolToNode = SymbolToNode;
            }

            internal MethodCallHierarchy Replace(HierarchyNode Node)
            {
                if (!Contains(Node.Method))
                {
                    throw new ArgumentException("Attempted to replace a node that isn't in the hierarchy.");
                }
                return new MethodCallHierarchy(SymbolToNode.SetItem(Node.Method, Node));
            }

            internal MethodCallHierarchy Add(HierarchyNode Node)
            {
                return new MethodCallHierarchy(SymbolToNode.Add(Node.Method, Node));
            }

            public HierarchyNode LookupMethod(IMethodSymbol Symbol)
            {
                return SymbolToNode[Symbol];
            }

            public bool Contains(IMethodSymbol Symbol)
            {
                return SymbolToNode.ContainsKey(Symbol);
            }

            /// <summary>
            /// Creates a string representing the hierarchies for each root method (methods with no callers).
            /// </summary>
            public string PrintHierarchyForRoots()
            {
                var Builder = new StringBuilder();
                var Roots = SymbolToNode.Values.Where(n => n.Callers.Count() == 0);
                foreach (var Root in Roots)
                {
                    PrintHierarchy(Root, Builder, 0);
                }
                return Builder.ToString();
            }

            private void PrintHierarchy(HierarchyNode Root, StringBuilder Builder, int IndentationCount)
            {
                Builder.AppendLine($"{new string(' ', IndentationCount)}{Root.Method.Name.ToString()}");
                foreach (var Call in Root.Calls)
                {
                    PrintHierarchy(Call, Builder, IndentationCount + 1);
                }
            }
        }

        /// <summary>
        /// Immutable representation of a method and both: what possibly calls it, and what it possibly calls.
        /// </summary>
        internal sealed class HierarchyNode
        {
            public IMethodSymbol Method { get; }
            /// <summary>
            /// All methods that this method may call 0 or more times.
            /// Order is not significant.
            /// </summary>
            public ImmutableArray<HierarchyNode> Calls;
            /// <summary>
            /// All methods that may call this method 0 or more times.
            /// Order is not significant.
            /// </summary>
            public ImmutableArray<HierarchyNode> Callers;

            private HierarchyNode(IMethodSymbol Symbol)
            {
                Method = Symbol;
                Calls = ImmutableArray<HierarchyNode>.Empty;
                Callers = ImmutableArray<HierarchyNode>.Empty;
            }

            private HierarchyNode(HierarchyNode Base, ImmutableArray<HierarchyNode>? Calls = null,
                ImmutableArray<HierarchyNode>? Callers = null)
            {
                Method = Base.Method;
                this.Calls = Calls ?? Base.Calls;
                this.Callers = Callers ?? Base.Callers;
            }

            internal static HierarchyNode CreateEmptyNode(IMethodSymbol Symbol)
            {
                return new HierarchyNode(Symbol);
            }

            internal HierarchyNode WithCaller(HierarchyNode Caller)
            {
                return new HierarchyNode(this, Callers: Callers.Add(Caller));
            }

            internal HierarchyNode WithCall(HierarchyNode Call)
            {
                return new HierarchyNode(this, Calls: Calls.Add(Call));
            }
        }

        //@TODO - This is probably broken with recursive C# methods.
        internal sealed class HierarchyBuilder : CSharpSyntaxWalker
        {
            private readonly SemanticModel Model;
            private MethodCallHierarchy Hierarchy;
            private HierarchyNode Node;

            private HierarchyBuilder(HierarchyNode Node, MethodCallHierarchy Hierarchy, SemanticModel Model)
            {
                this.Node = Node;
                this.Hierarchy = Hierarchy;
                this.Model = Model;
            }

            /// <summary>
            /// Builds a new MethodCallHierarchy by recursively exploring every method invocation encountered.
            /// </summary>
            public static MethodCallHierarchy RecursiveBuilder(IMethodSymbol Origin, MethodCallHierarchy Hierarchy,
                SemanticModel Model)
            {
                if (Hierarchy.Contains(Origin))
                {
                    throw new InvalidOperationException("Attempted to begin traversal with an existing node.");
                }
                var Node = HierarchyNode.CreateEmptyNode(Origin);
                Hierarchy = Hierarchy.Add(Node);
                var Builder = new HierarchyBuilder(Node, Hierarchy, Model);
                Builder.Visit(Origin.DeclaringSyntaxReferences.Single().GetSyntax());
                return Builder.Hierarchy;
            }

            public override void VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                var InvokedMethodSymbol = (IMethodSymbol)Model.GetSymbolInfo(node).Symbol;
                if (!Hierarchy.Contains(InvokedMethodSymbol))
                {
                    Hierarchy = HierarchyBuilder.RecursiveBuilder(InvokedMethodSymbol, Hierarchy, Model);
                }
                var OtherNode = Hierarchy.LookupMethod(InvokedMethodSymbol);
                Node = Node.WithCall(OtherNode);
                OtherNode = OtherNode.WithCaller(Node);
                Hierarchy = Hierarchy.Replace(OtherNode).Replace(Node);
            }
        }
    }
}
