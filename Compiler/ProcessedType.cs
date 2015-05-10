using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace CSharpTo2600.Compiler
{
    public sealed class ProcessedType
    {
        //@TODO - Use only Symbol.
        public readonly Type CLRType;
        public readonly INamedTypeSymbol Symbol;
        public readonly ImmutableDictionary<IMethodSymbol, Subroutine> Subroutines;
        public readonly ImmutableDictionary<IFieldSymbol, IVariableInfo> Globals;
        public bool IsStatic { get { return CLRType.IsAbstract && CLRType.IsSealed; } }
        public bool IsValueType { get { return CLRType.IsValueType; } }
        public bool IsCompiled { get { return Subroutines.Values.Any(s => !s.IsCompiled); } }
        public string Name { get { return Symbol.Name; } }

        internal ProcessedType(Type CLRType, INamedTypeSymbol Symbol, 
            ImmutableDictionary<IMethodSymbol, Subroutine> Subroutines,
            ImmutableDictionary<IFieldSymbol, IVariableInfo> Globals)
        {
            this.CLRType = CLRType;
            this.Symbol = Symbol;
            this.Subroutines = Subroutines;
            this.Globals = Globals;
        }

        internal ProcessedType(ProcessedType Base, Type CLRType=null, INamedTypeSymbol Symbol=null,
            ImmutableDictionary<IMethodSymbol, Subroutine> Subroutines=null,
            ImmutableDictionary<IFieldSymbol, IVariableInfo> Globals=null)
        {
            this.CLRType = CLRType ?? Base.CLRType;
            this.Symbol = Symbol ?? Base.Symbol;
            this.Subroutines = Subroutines ?? Base.Subroutines;
            this.Globals = Globals ?? Base.Globals;
        }
    }
}
