using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CSharpTo2600.Compiler
{
    internal static class RoslynExtensions
    {
        public static void DebugPrintKinds(this SyntaxNode @this)
        {
            foreach (var e in Enum.GetValues(typeof(SyntaxKind)))
            {
                if (@this.IsKind((SyntaxKind)e))
                {
                    Console.WriteLine("Expression \"\{@this}\" is \{e}");
                }
            }
        }

        public static IEnumerable<AttributeSyntax> AllAttributes(this MethodDeclarationSyntax @this)
        {
            var Attributes = from list in @this.AttributeLists
                             from attributes in list.Attributes
                             select attributes;
            return Attributes;
        }

        public static string GetAttributeName(this AttributeSyntax @this, SemanticModel Model)
        {
            return Model.GetSymbolInfo(@this).Symbol.ContainingType.Name;
        }

        public static Type ToType(this SyntaxNode @this, SemanticModel Model)
        {
            var FullyQualifiedName = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
            var TypeName = Model.GetTypeInfo(@this).Type.ToDisplayString(FullyQualifiedName);
            return Type.GetType(TypeName, true, false);
        }
    }
}
