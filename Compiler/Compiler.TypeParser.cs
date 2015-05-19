using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using CSharpTo2600.Framework.Assembly;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TypeInfo = System.Reflection.TypeInfo;

namespace CSharpTo2600.Compiler
{
    partial class GameCompiler
    {
        private sealed class TypeParser
        {
            private readonly INamedTypeSymbol Symbol;
            //@TODO - Use only Symbol.
            private readonly Type CLRType;
            private TypeInfo TypeInfo { get { return CLRType.GetTypeInfo(); } }

            private TypeParser(Type CLRType, CSharpCompilation Compilation)
            {
                this.CLRType = CLRType;
                Symbol = Compilation.GetSymbolsWithName(s => s == CLRType.Name).Cast<INamedTypeSymbol>().Single();
            }

            public static ProcessedType ParseType(Type CLRType, CSharpCompilation Compilation)
            {
                var Parser = new TypeParser(CLRType, Compilation);
                var Globals = Parser.ParseFields();
                var ParsedSubroutines = Parser.ParseMethods();
                var FirstStageType = new ProcessedType(CLRType, Parser.Symbol, ParsedSubroutines, Globals);
                // We've determined the type's fields and methods (although not the method bodies).
                // That's enough for any other class to deal with us (other types' know our fields, don't
                // need to know our method bodies).
                // So method compilation should go fine so long as all types involved have been
                // at least parsed.
                return FirstStageType;
            }

            private ImmutableDictionary<IFieldSymbol, IVariableInfo> ParseFields()
            {
                var Result = new Dictionary<IFieldSymbol, IVariableInfo>();
                foreach (var Field in TypeInfo.DeclaredFields)
                {
                    var FieldSymbol = (IFieldSymbol)Symbol.GetMembers(Field.Name).Single();
                    // Check for name conflicts with reserved symbols.
                    if (typeof(ReservedSymbols).GetTypeInfo().DeclaredFields.Any(f => f.Name == Field.Name))
                    {
                        throw new VariableNameReservedException(FieldSymbol);
                    }
                    Result.Add(FieldSymbol, VariableInfo.CreatePlaceholderVariable(FieldSymbol, Field.FieldType));
                }
                return Result.ToImmutableDictionary();
            }

            private ImmutableDictionary<IMethodSymbol, Subroutine> ParseMethods()
            {
                var Result = new Dictionary<IMethodSymbol, Subroutine>();
                foreach (var Method in TypeInfo.DeclaredMethods)
                {
                    //@TODO - Handle overloaded methods
                    var MethodSymbol = (IMethodSymbol)Symbol.GetMembers(Method.Name).Single();
                    // If we're a partial method, make sure we're a symbol for the implementation.
                    // Otherwise we'll end up compiling an empty method.
                    MethodSymbol = MethodSymbol.PartialImplementationPart ?? MethodSymbol;
                    var MethodType = Method.GetCustomAttribute<Framework.SpecialMethodAttribute>()?.GameMethod ?? Framework.MethodType.UserDefined;

                    // Only supporting void return and 0 parameters for now.
                    if (Method.ReturnType != typeof(void))
                    {
                        throw new FatalCompilationException($"Method must have void return: {Method.Name}");
                    }
                    if(Method.GetParameters().Length != 0)
                    {
                        throw new FatalCompilationException($"Method must have 0 parameters: {Method.Name}");
                    }

                    var Subroutine = new Subroutine(Method.Name, Method, MethodSymbol, MethodType);
                    Result.Add(MethodSymbol, Subroutine);
                }
                return Result.ToImmutableDictionary();
            }
        }
    }
}
