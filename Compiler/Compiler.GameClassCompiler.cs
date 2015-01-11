using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpTo2600.Compiler
{
    public sealed partial class Compiler
    {
        private sealed class GameClassCompiler : CSharpSyntaxWalker
        {
            private readonly Compiler Compiler;
            private SemanticModel Model { get { return Compiler.Model; } }
            private ROMBuilder ROMBuilder { get { return Compiler.ROMBuilder; } }

            public GameClassCompiler(Compiler Compiler)
            {
                this.Compiler = Compiler;
            }

            public override void VisitFieldDeclaration(FieldDeclarationSyntax FieldNode)
            {
                var RealType = GetType(FieldNode.Declaration.Type);
                foreach (var Declarator in FieldNode.Declaration.Variables)
                {
                    var VariableName = Declarator.Identifier.ToString();
                    ROMBuilder.AddGlobalVariable(RealType, VariableName);
                }
                base.VisitFieldDeclaration(FieldNode);
            }

            public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                var MethodCompiler = new MethodCompiler(node);
                var Subroutine = MethodCompiler.Compile();
                ROMBuilder.AddSubroutine(Subroutine);
                base.VisitMethodDeclaration(node);
            }

            private Type GetType(TypeSyntax TypeSyntax)
            {
                var Info = Model.GetTypeInfo(TypeSyntax);
                var FullyQualifiedNameFormat = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
                var FullyQualifiedName = Info.Type.ToDisplayString(FullyQualifiedNameFormat);
                //@TODO - Won't find types outside of mscorlib.
                var TrueType = Type.GetType(FullyQualifiedName);
                if (TrueType == null)
                {
                    throw new ArgumentException("TypeSyntaxes must correspond to an mscorlib type for now.", nameof(TypeSyntax));
                }
                return TrueType;
            }
        }
    }
}
