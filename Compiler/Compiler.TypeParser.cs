using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using CSharpTo2600.Framework.Assembly;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CSharpTo2600.Compiler
{
    partial class GameCompiler
    {
        private sealed class TypeParser
        {
            private readonly INamedTypeSymbol Symbol;



            private TypeParser(Type CLRType, CSharpCompilation Compilation)
            {
                Symbol = Compilation.GetSymbolsWithName(s => s == CLRType.Name).Cast<INamedTypeSymbol>().Single();
            }

            public static ProcessedType ParseType(Type CLRType, CSharpCompilation Compilation, CompilationState State)
            {
                var Symbol = Compilation.GetSymbolsWithName(s => s == CLRType.Name).Cast<INamedTypeSymbol>().Single();
                ParseFields(Symbol, State);
                // We've determined the type's fields and methods (although not the method bodies).
                // That's enough for any other class to deal with us (other types' know our fields, don't
                // need to know our method bodies).
                // So method compilation should go fine so long as all types involved have been
                // at least parsed.
                return new ProcessedType(Symbol);
            }

            public static ImmutableDictionary<IMethodSymbol, Subroutine> ParseMethods(CompilationState State,
                ProcessedType Type)
            {
                return ParseMethods(Type.Symbol, State);
            }

            private static void ParseFields(INamedTypeSymbol Symbol, CompilationState State)
            {
                foreach (var FieldSymbol in Symbol.GetMembers().OfType<IFieldSymbol>())
                {
                    // Check for name conflicts with reserved symbols.
                    if (typeof(ReservedSymbols).GetTypeInfo().DeclaredFields.Any(f => f.Name == FieldSymbol.Name))
                    {
                        throw new VariableNameReservedException(FieldSymbol);
                    }
                }
            }

            private static ImmutableDictionary<IMethodSymbol, Subroutine> ParseMethods(INamedTypeSymbol Symbol, 
                CompilationState State)
            {
                var Result = new Dictionary<IMethodSymbol, Subroutine>();
                foreach (var MethodSymbol in Symbol.GetMembers().OfType<IMethodSymbol>())
                {
                    //@TODO - Handle overloaded methods
                    // If we're a partial method, make sure we're a symbol for the implementation.
                    // Otherwise we'll end up compiling an empty method.
                    var TrueMethodSymbol = MethodSymbol.PartialImplementationPart ?? MethodSymbol;

                    // Only supporting void/byte return and 0 parameters for now.
                    var ReturnType = State.GetTypeFromSymbol((INamedTypeSymbol)TrueMethodSymbol.ReturnType);
                    if (ReturnType != State.BuiltIn.Void && ReturnType != State.BuiltIn.Byte)
                    {
                        throw new FatalCompilationException($"Method must have void or byte return type: {TrueMethodSymbol.Name}");
                    }
                    if(TrueMethodSymbol.Parameters.Length != 0)
                    {
                        throw new FatalCompilationException($"Method must have 0 parameters: {TrueMethodSymbol.Name}");
                    }
                    // e.g. default constructor (.ctor)
                    if(TrueMethodSymbol.IsImplicitlyDeclared)
                    {
                        continue;
                    }
                    
                    var Subroutine = new Subroutine(TrueMethodSymbol.Name, ReturnType, MethodSymbol);
                    Result.Add(MethodSymbol, Subroutine);
                }
                return Result.ToImmutableDictionary();
            }
        }
    }
}
