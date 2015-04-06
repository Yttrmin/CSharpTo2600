using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TypeInfo = System.Reflection.TypeInfo;

namespace CSharpTo2600.Compiler
{
    public sealed partial class GameCompiler
    {
        private sealed class TypeCompiler
        {
            private readonly Type CLRType;
            private TypeInfo TypeInfo { get; }
            private readonly GameCompiler Compiler;
            private readonly ClassDeclarationSyntax ClassNode;

            private TypeCompiler(Type CLRType, GameCompiler Compiler)
            {
                this.CLRType = CLRType;
                TypeInfo = CLRType.GetTypeInfo();
                this.Compiler = Compiler;
                //@TODO - Classes of same name but in differing namespaces. Also, messy.
                ClassNode = (from tree in Compiler.Compilation.SyntaxTrees
                             let root = tree.GetRoot()
                             from node in root.DescendantNodes()
                             let classNode = node as ClassDeclarationSyntax
                             where classNode != null
                             let className = classNode.Identifier.Text
                             where className == CLRType.Name
                             select classNode).Single();
            }

            public static CompiledType CompileType(Type CLRType, GameCompiler GCompiler)
            {
                var Compiler = new TypeCompiler(CLRType, GCompiler);
                var Globals = Compiler.ParseFields();
                Compiler.ParseMethods();
                throw new NotImplementedException();
            }

            private ImmutableDictionary<SyntaxToken, GlobalVariable> ParseFields()
            {
                var AllFields = ClassNode.ChildNodes().OfType<FieldDeclarationSyntax>();
                if (AllFields.Count() != CLRType.GetTypeInfo().DeclaredFields.Count())
                {
                    throw new FatalCompilationException($"SyntaxTree and reflection field count don't match. Tree: {AllFields.Count()}   Reflection: {CLRType.GetTypeInfo().DeclaredFields.Count()}");
                }
                var Result = new Dictionary<SyntaxToken, GlobalVariable>();
                foreach (var Field in AllFields)
                {
                    var Tuples = ParseFieldDeclaration(Field);
                    foreach (var Tuple in Tuples)
                    {
                        Result.Add(Tuple.Item1, Tuple.Item2);
                    }
                }
                return Result.ToImmutableDictionary();
            }

            private ImmutableArray<Subroutine> ParseMethods()
            {
                var AllMethods = ClassNode.ChildNodes().OfType<MethodDeclarationSyntax>();
                if (AllMethods.Count() != CLRType.GetTypeInfo().DeclaredMethods.Count())
                {
                    throw new FatalCompilationException($"SyntaxTree and reflection method count don't match. Tree: {AllMethods.Count()}   Reflection: {CLRType.GetTypeInfo().DeclaredMethods.Count()}");
                }
                var Result = new List<Subroutine>();
                foreach (var Method in AllMethods)
                {
                    Result.Add(ParseMethodDeclaration(Method));
                }
                return Result.ToImmutableArray();
            }

            private IEnumerable<Tuple<SyntaxToken, GlobalVariable>> ParseFieldDeclaration(FieldDeclarationSyntax FieldNode)
            {
                if (FieldNode.Declaration.Variables.Any(v => v.Initializer != null))
                {
                    throw new FatalCompilationException("Fields can't have initializers yet.", FieldNode);
                }
                var Results = new List<Tuple<ISymbol, GlobalVariable>>();
                //var RealType = Compiler.GetType(FieldNode.Declaration.Type);
                foreach (var Declarator in FieldNode.Declaration.Variables)
                {
                    var Identifier = Declarator.Identifier;
                    var VariableName = Identifier.ToString();
                    var FieldInfo = TypeInfo.GetDeclaredField(VariableName);
                    if (!FieldInfo.IsStatic)
                    {
                        throw new FatalCompilationException($"Instance fields not yet supported: {FieldInfo.Name}");
                    }

                    var Global = new GlobalVariable(VariableName, FieldInfo.FieldType, new Range(), false);
                    yield return new Tuple<SyntaxToken, GlobalVariable>(Identifier, Global);
                }
            }

            private Subroutine ParseMethodDeclaration(MethodDeclarationSyntax MethodNode)
            {
                throw new NotImplementedException();
            }
        }
    }
}
