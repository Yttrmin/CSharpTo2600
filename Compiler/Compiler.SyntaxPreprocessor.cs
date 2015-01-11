using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpTo2600.Compiler
{
    partial class Compiler
    {
        internal class SyntaxPreprocessor : CSharpSyntaxRewriter
        {
            private readonly SemanticModel Model;
            //@TODO - Better name for temps.
            private char Next = 'A';

            public SyntaxPreprocessor(SemanticModel Model)
            {
                this.Model = Model;
            }

            public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax node)
            {
                return base.Visit(node.Expression);
            }

            public override SyntaxNode VisitAssignmentExpression(AssignmentExpressionSyntax node)
            {
                // Returns AssignmentExpression when normal, BlockStatement when modified.
                var Result = base.Visit(node.Right);
                var NoChangeResult = Result as AssignmentExpressionSyntax;
                if (NoChangeResult != null)
                {
                    return NoChangeResult;
                }
                var ChangedResult = Result as BlockSyntax;
                if (ChangedResult != null)
                {
                    // Basically, we're just finishing up the assignment expression. Our left side will stay the same,
                    // but the right side needs to be put together.
                    // Extract the statement from the Block and remove it from the Block.
                    var Fragment = (ExpressionStatementSyntax)ChangedResult.Statements.Last();
                    var FragmentExpression = Fragment.Expression;
                    ChangedResult = ChangedResult.RemoveNode(Fragment, SyntaxRemoveOptions.KeepNoTrivia);
                    // Find out what side of the binary expression this expression belongs to. Matters for
                    // operations that aren't commutative.
                    var Side = Fragment.GetAnnotations("Fragment").Single().Data;
                    // Get the identifier of the local that will replace the other side of the binary expression.
                    var LastLocal = ChangedResult.Statements.OfType<LocalDeclarationStatementSyntax>().Last();
                    var LastVarIdentifier = SyntaxFactory.IdentifierName(LastLocal.Declaration.Variables.Single().Identifier);
                    var BinaryKind = node.Right.CSharpKind();
                    // Determine the left and right side of the new binary expression based on which side the fragment
                    // belongs to.
                    var LeftSide = Side == "Right" ? LastVarIdentifier : FragmentExpression;
                    var RightSide = Side == "Right" ? FragmentExpression : LastVarIdentifier;
                    // Construct the new AssignmentExpression...
                    var Assignment = SyntaxFactory.AssignmentExpression(node.CSharpKind(),
                        node.Left,
                        SyntaxFactory.BinaryExpression(
                            BinaryKind,
                            LeftSide,
                            RightSide));
                    // And wrap it in an ExpressionStatement and add it to the Block. All done.
                    ChangedResult = ChangedResult.AddStatements(SyntaxFactory.ExpressionStatement(Assignment));
                    ChangedResult = ChangedResult.WithAdditionalAnnotations(new SyntaxAnnotation("Modified"));
                    return ChangedResult;
                }
                throw new InvalidOperationException();
            }

            public override SyntaxNode VisitBinaryExpression(BinaryExpressionSyntax node)
            {
                if (IsTrivialBinaryExpression(node))
                {
					// If it's already simple we don't have to do anything.
                    return node;
                }
                else
                {
                    // Time to simplify.
                    // Shove everything in a Block because we can only return one SyntaxNode.
					// There is probably a better way. Maybe I'll figure it out.
                    var Block = SyntaxFactory.Block();
                    Block = Block.WithLeadingTrivia(SyntaxFactory.Comment("// Start of local var extraction."));
                    var Nested = GetNestedExpression(node);
					// We are the root BinaryExpression, this block holds the result of all nested ones.
                    Block = BinaryExpressionToLocal(Nested, Block);
                    // Still need this annotation to later on determine which side of the binary expression
                    // this goes on?
                    var FragmentString = Nested == node.Left ? "Right" : "Left";
                    var Fragment = FragmentString == "Right" ? node.Right : node.Left;
                    // Just put the fragment in an ExpressionStatement so we can put in the block, removing
                    // and modifying it later.
                    var FragmentStatement = SyntaxFactory.ExpressionStatement(Fragment).WithAdditionalAnnotations(
                        new SyntaxAnnotation("Fragment", FragmentString));
                    Block = Block.AddStatements(FragmentStatement);
                    Block = Block.WithTrailingTrivia(SyntaxFactory.Comment("// End of local var extraction."));
                    return Block;
                }
            }

			/// <summary>
            /// Transforms a BinaryExpressionSyntax into a simple assignment to a local.
            /// </summary>
            /// <param name="node">Binary expression to transform.</param>
            /// <param name="Block">Block to add the LocalDeclarationStatementSyntaxTo</param>
            /// <returns>A modified Block containing the new LocalDeclarationStatementSyntax.</returns>
            private BlockSyntax BinaryExpressionToLocal(BinaryExpressionSyntax node, BlockSyntax Block)
            {
                // Doing this up here since we modify node and thus can't use the SemanticModel.
                // We could just not modify node and make another variable (probably should).
                var TypeString = Model.GetTypeInfo(node).Type.ToString();
                BlockSyntax Child;
                if (!IsTrivialBinaryExpression(node))
                {
                    // If there's more nesting, recursively resolve it.
                    Child = BinaryExpressionToLocal(GetNestedExpression(node), Block);
                    var NewestLocal = Child.Statements.OfType<LocalDeclarationStatementSyntax>().Last();
                    var Identifier = SyntaxFactory.IdentifierName(NewestLocal.Declaration.Variables.Single().Identifier);
                    Block = Child;
					// The nested BinaryExpression was moved out of this BinaryExpression and into some local variable.
					// So replace the nested BinaryExpression node with the Identifier representing the local.
                    node = node.ReplaceNode((SyntaxNode)GetNestedExpression(node), Identifier);
                }

				// Convert our BinaryExpression into a LocalDeclarationStatementSyntax.

				// Our initializer is simply our whole BinaryExpression slapped onto the right side of an equals sign.
                var Initializer = SyntaxFactory.EqualsValueClause(node);
				// Declare our local variable of some identifier and give it our Initializer.
                var VariableDeclarator = SyntaxFactory.VariableDeclarator(
                                    identifier: SyntaxFactory.Identifier(Next.ToString()),
                                    argumentList: null,
                                    initializer: Initializer);
				// Increment name for future local variables.
                Next++;
				// Add our local variable to a list, since that's how the factory method wants it.
                var Variables = SyntaxFactory.SeparatedList<VariableDeclaratorSyntax>().Add(VariableDeclarator);
				// Figure out the type of local based on the type of the expression.
                var Type = SyntaxFactory.ParseTypeName(TypeString);
				// Put it all together in a LocalDeclarationStatementSyntax.
                var NewNode =
                    SyntaxFactory.LocalDeclarationStatement(
                        declaration: SyntaxFactory.VariableDeclaration(
                            type: Type,
                            variables: Variables));
				// Add it to the block and return it.
                Block = Block.AddStatements(NewNode);
                return Block;
            }

            private BinaryExpressionSyntax GetNestedExpression(BinaryExpressionSyntax node)
            {
				//@TODO - See IsTrivialBinaryExpression
                if (node.Left.ChildNodes().Any())
                {
                    return (BinaryExpressionSyntax)node.Left;
                }
                else if (node.Right.ChildNodes().Any())
                {
                    return (BinaryExpressionSyntax)node.Right;
                }
                else
                {
                    throw new InvalidOperationException("Something went wrong");
                }
            }

            private bool IsTrivialBinaryExpression(BinaryExpressionSyntax node)
            {
                var LeftDepth = node.Left.ChildNodes().Count();
                var RightDepth = node.Right.ChildNodes().Count();
                //@TODO
                Debug.Assert((LeftDepth == 0 && RightDepth == 0) || (LeftDepth == 0 ^ RightDepth == 0));
                return LeftDepth == 0 && RightDepth == 0;
            }
        }
    }
}
