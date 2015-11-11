using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace CSharpTo2600.Compiler
{
    public sealed partial class GameCompiler
    {
        [Obsolete]
        private sealed class TypeCompiler
        {
            private readonly ProcessedType ParsedType;
            private readonly CompileOptions CompileOptions;

            private TypeCompiler(ProcessedType ParsedType, CompileOptions CompileOptions)
            {
                this.ParsedType = ParsedType;
                this.CompileOptions = CompileOptions;
            }

            public static ImmutableDictionary<IMethodSymbol, Subroutine> CompileType(ProcessedType ParsedType, CompilationState State, GameCompiler GCompiler)
            {
                var Compiler = new TypeCompiler(ParsedType, GCompiler.Options);
                var CompiledMethods = Compiler.CompileMethods(State, GCompiler);
                return CompiledMethods;
            }

            private ImmutableDictionary<IMethodSymbol, Subroutine> CompileMethods(CompilationState CompilationState, GameCompiler GCompiler)
            {
                var Result = new Dictionary<IMethodSymbol, Subroutine>();
                foreach(var Symbol in ParsedType.Subroutines)
                {
                    var MethodNode = (MethodDeclarationSyntax)Symbol.DeclaringSyntaxReferences.Single().GetSyntax();
                    var Model = GCompiler.Compilation.GetSemanticModel(MethodNode.SyntaxTree);
                    var CompiledSubroutine = MethodCompiler.CompileMethod(Symbol, CompilationState, Model, CompileOptions.Optimize);
                    Result.Add(Symbol, CompiledSubroutine);
                }
                return Result.ToImmutableDictionary();
            }
        }
    }
}
