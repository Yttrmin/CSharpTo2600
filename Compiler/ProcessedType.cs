using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Linq;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace CSharpTo2600.Compiler
{
    internal sealed class ProcessedType
    {
        public readonly Type CLRType;
        public readonly INamedTypeSymbol Symbol;
        public readonly ImmutableDictionary<IMethodSymbol, Subroutine> Subroutines;
        public readonly ImmutableDictionary<IFieldSymbol, GlobalVariable> Globals;
        public bool IsStatic { get { return CLRType.IsAbstract && CLRType.IsSealed; } }
        public bool IsValueType { get { return CLRType.IsValueType; } }
        public bool IsCompiled { get { return Subroutines.Values.Any(s => !s.IsCompiled); } }

        public ProcessedType(Type CLRType, INamedTypeSymbol Symbol, 
            ImmutableDictionary<IMethodSymbol, Subroutine> Subroutines, 
            ImmutableDictionary<IFieldSymbol, GlobalVariable> Globals)
        {
            this.CLRType = CLRType;
            this.Symbol = Symbol;
            this.Subroutines = Subroutines;
            this.Globals = Globals;
        }
    }
}
