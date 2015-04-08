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
            private readonly INamedTypeSymbol Symbol;

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
                Symbol = Compiler.Compilation.Assembly.GetTypeByMetadataName(CLRType.FullName);
                if (Symbol == null)
                {
                    throw new FatalCompilationException($"Could not find type symbol in compilation: {CLRType.FullName}");
                }
            }

            public static ProcessedType CompileType(Type CLRType, GameCompiler GCompiler)
            {
                var Compiler = new TypeCompiler(CLRType, GCompiler);
                var Globals = Compiler.ParseFields();
                var ParsedSubroutines = Compiler.ParseMethods();
                // We've determined the type's fields and methods (although not the method bodies).
                // That's enough for any other class to deal with us (other types' know our fields, don't
                // need to know our method bodies).
                // So method compilation should go fine even if we have to compile another type (e.g.
                // we try to access another un-processed type's fields).
                var FirstStageType = new ProcessedType(CLRType, Compiler.Symbol, ParsedSubroutines, Globals);
                throw new NotImplementedException();
            }

            private ImmutableDictionary<IFieldSymbol, GlobalVariable> ParseFields()
            {
                var AllFields = ClassNode.ChildNodes().OfType<FieldDeclarationSyntax>();
                if (AllFields.Count() != CLRType.GetTypeInfo().DeclaredFields.Count())
                {
                    throw new FatalCompilationException($"SyntaxTree and reflection field count don't match. Tree: {AllFields.Count()}   Reflection: {CLRType.GetTypeInfo().DeclaredFields.Count()}");
                }
                var Result = new Dictionary<IFieldSymbol, GlobalVariable>();
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

            private ImmutableDictionary<IMethodSymbol, Subroutine> ParseMethods()
            {
                var AllMethods = ClassNode.ChildNodes().OfType<MethodDeclarationSyntax>();
                if (AllMethods.Count() != CLRType.GetTypeInfo().DeclaredMethods.Count())
                {
                    throw new FatalCompilationException($"SyntaxTree and reflection method count don't match. Tree: {AllMethods.Count()}   Reflection: {CLRType.GetTypeInfo().DeclaredMethods.Count()}");
                }
                var Result = new Dictionary<IMethodSymbol, Subroutine>();
                foreach (var Method in AllMethods)
                {
                    var ParsedMethod = ParseMethodDeclaration(Method);
                    Result.Add(ParsedMethod.Item1, ParsedMethod.Item2);
                }
                return Result.ToImmutableDictionary();
            }

            private ImmutableDictionary<IMethodSymbol, Subroutine> CompileMethods(ImmutableDictionary<IMethodSymbol, Subroutine> NoncompiledMethods)
            {
                throw new NotImplementedException();
            }

            private IEnumerable<Tuple<IFieldSymbol, GlobalVariable>> ParseFieldDeclaration(FieldDeclarationSyntax FieldNode)
            {
                if (FieldNode.Declaration.Variables.Any(v => v.Initializer != null))
                {
                    throw new FatalCompilationException("Fields can't have initializers yet.", FieldNode);
                }
                foreach (var Declarator in FieldNode.Declaration.Variables)
                {
                    var VariableName = Declarator.Identifier.ToString();
                    var FieldInfo = TypeInfo.GetDeclaredField(VariableName);
                    if (!FieldInfo.IsStatic)
                    {
                        throw new FatalCompilationException($"Instance fields not yet supported: {FieldInfo.Name}");
                    }
                    var FieldSymbol = Symbol.GetMembers(FieldInfo.Name).Cast<IFieldSymbol>().Single();
                    var Global = new GlobalVariable(VariableName, FieldInfo.FieldType, new Range(), false);
                    yield return new Tuple<IFieldSymbol, GlobalVariable>(FieldSymbol, Global);
                }
            }

            private Tuple<IMethodSymbol, Subroutine> ParseMethodDeclaration(MethodDeclarationSyntax MethodNode)
            {
                var MethodName = MethodNode.Identifier.Text;
                var MethodInfo = TypeInfo.GetDeclaredMethod(MethodName);

                var Parameters = MethodInfo.GetParameters();
                var PossibleMethods = Symbol.GetMembers(MethodInfo.Name).Cast<IMethodSymbol>();
                IMethodSymbol MethodSymbol = null;
                foreach (var Method in PossibleMethods)
                {
                    for (var i = 0; i < Method.Parameters.Count(); i++)
                    {
                        //@TODO - This seems like a really shaky way to check type equality!
                        if (Method.Parameters[i].Type.ToString() != Parameters[i].ToString())
                        {
                            continue;
                        }
                    }
                    MethodSymbol = Method;
                    break;
                }
                if (Symbol == null)
                {
                    throw new FatalCompilationException($"Could not find method symbol for: {MethodInfo.Name}");
                }
                
                //@TODO - Get actual MethodType
                var Subroutine = new Subroutine(MethodName, MethodInfo, MethodSymbol, Framework.MethodType.None);
                return new Tuple<IMethodSymbol, Subroutine>(MethodSymbol, Subroutine);
            }
        }
    }
}
