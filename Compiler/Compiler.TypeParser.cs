using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TypeInfo = System.Reflection.TypeInfo;

namespace CSharpTo2600.Compiler
{
    partial class GameCompiler
    {
        private sealed class TypeParser
        {
            private readonly INamedTypeSymbol Symbol;
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
                // So method compilation should go fine even if we have to compile another type (e.g.
                // we try to access another un-processed type's fields).
                return FirstStageType;
            }

            private ImmutableDictionary<IFieldSymbol, VariableInfo> ParseFields()
            {
                var Result = new Dictionary<IFieldSymbol, VariableInfo>();
                foreach (var Field in TypeInfo.DeclaredFields)
                {
                    var FieldSymbol = (IFieldSymbol)Symbol.GetMembers(Field.Name).Single();
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
                    var MethodType = Method.GetCustomAttribute<Framework.SpecialMethodAttribute>()?.GameMethod ?? Framework.MethodType.UserDefined;
                    var Subroutine = new Subroutine(Method.Name, Method, MethodSymbol, MethodType);
                    Result.Add(MethodSymbol, Subroutine);
                }
                return Result.ToImmutableDictionary();
            }
        }
    }
}
