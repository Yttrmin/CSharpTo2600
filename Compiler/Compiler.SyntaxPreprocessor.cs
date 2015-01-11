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
                var Children = base.Visit(node.Expression);
				// If the child expression has the Modified annotation, we know its really
				// a BlockSyntax of a simplified assignment.
                if (Children.HasAnnotations("Modified"))
                {
                    var Annotation = Children.GetAnnotations("Modified").Single();
                    var Block = (BlockSyntax)Children;
                    var Expression = SyntaxFactory.ParseExpression(Annotation.Data);
                    Block = Block.AddStatements(SyntaxFactory.ExpressionStatement(
                        Expression));
                    return Block;
                }
                else
                {
                    return Children;
                }
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
					// This is largely terrible because of using annotations.
					//@TODO - Don't use annotations.
					// Basically, we're just finishing up the assignment expression. Our left side will stay the same,
					// but the right side needs to be put together.
                    var Annotations = ChangedResult.GetAnnotations("FragmentL");
                    var Annotation = Annotations.Count() == 0 ? ChangedResult.GetAnnotations("FragmentR").Single() : Annotations.Single();
                    var LastLocal = ChangedResult.Statements.OfType<LocalDeclarationStatementSyntax>().Last();
                    var LastVarIdentifier = SyntaxFactory.IdentifierName(LastLocal.Declaration.Variables.Single().Identifier);
                    var BinaryKind = ((BinaryExpressionSyntax)node.Right).CSharpKind();
                    if (Annotation.Kind == "FragmentL")
                    {
                        var Assignment = SyntaxFactory.AssignmentExpression(node.CSharpKind(),
                            node.Left,
                            SyntaxFactory.BinaryExpression(BinaryKind,
                                SyntaxFactory.ParseExpression(Annotation.Data),
                                LastVarIdentifier));
                        var NewAnnotation = new SyntaxAnnotation("Modified", Assignment.ToString());
                        ChangedResult = ChangedResult.WithAdditionalAnnotations(NewAnnotation);
						// Everything is put back together, return the Block and all our locals and whatnot
						// are added to the tree.
                        return ChangedResult;
                    }
                    else if (Annotation.Kind == "FragmentR")
                    {
                        var Assignment = SyntaxFactory.AssignmentExpression(node.CSharpKind(),
                            node.Left,
                            SyntaxFactory.BinaryExpression(BinaryKind,
                                LastVarIdentifier,
                                SyntaxFactory.ParseExpression(Annotation.Data)));
                        var NewAnnotation = new SyntaxAnnotation("Modified", Assignment.ToString());
                        ChangedResult = ChangedResult.WithAdditionalAnnotations(NewAnnotation);
                        return ChangedResult;
                    }
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

                    // Unfortunately we can't add our BinaryExpression to the Block, because it's not a statement.
					//@TODO - Could we just use ExpressionStatement factory method instead of annotation garbage?
                    if (Nested == node.Left)
                    {
						// This is terrible.
                        Block = Block.WithAdditionalAnnotations(new SyntaxAnnotation("FragmentR", node.Right.ToString()));
                    }
                    else if (Nested == node.Right)
                    {
                        Block = Block.WithAdditionalAnnotations(new SyntaxAnnotation("FragmentL", node.Left.ToString()));
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }

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
