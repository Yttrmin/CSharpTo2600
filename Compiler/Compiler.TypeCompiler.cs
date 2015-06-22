using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpTo2600.Compiler
{
    public sealed partial class GameCompiler
    {
        private sealed class TypeCompiler
        {
            private readonly ProcessedType ParsedType;
            private readonly CompileOptions CompileOptions;

            private TypeCompiler(ProcessedType ParsedType, CompileOptions CompileOptions)
            {
                this.ParsedType = ParsedType;
                this.CompileOptions = CompileOptions;
            }

            public static ProcessedType CompileType(ProcessedType ParsedType, CompilationState State, GameCompiler GCompiler)
            {
                var Compiler = new TypeCompiler(ParsedType, GCompiler.Options);
                var CompiledMethods = Compiler.CompileMethods(State, GCompiler);
                return new ProcessedType(ParsedType.Symbol, CompiledMethods);
            }

            private ImmutableDictionary<IMethodSymbol, Subroutine> CompileMethods(CompilationState CompilationState, GameCompiler GCompiler)
            {
                var Result = new Dictionary<IMethodSymbol, Subroutine>();
                foreach (var SymbolSubPair in ParsedType.Subroutines)
                {
                    // Pretty sure methods can only have one declaration. Even implemented partial methods
                    // only have one declaration.
                    var MethodNode = (MethodDeclarationSyntax)SymbolSubPair.Key.DeclaringSyntaxReferences.Single().GetSyntax();
                    var Model = GCompiler.Compilation.GetSemanticModel(MethodNode.SyntaxTree);
                    var CompiledSubroutine = MethodCompiler.CompileMethod(SymbolSubPair.Key, CompilationState, Model, 
                        CompileOptions.Optimize);
                    Result.Add(SymbolSubPair.Key, CompiledSubroutine);
                }
                return Result.ToImmutableDictionary();
            }
        }
    }
}
