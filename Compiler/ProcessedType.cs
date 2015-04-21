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
        public readonly ImmutableDictionary<IFieldSymbol, IVariableInfo> Globals;
        public bool IsStatic { get { return CLRType.IsAbstract && CLRType.IsSealed; } }
        public bool IsValueType { get { return CLRType.IsValueType; } }
        public bool IsCompiled { get { return Subroutines.Values.Any(s => !s.IsCompiled); } }

        public ProcessedType(Type CLRType, INamedTypeSymbol Symbol, 
            ImmutableDictionary<IMethodSymbol, Subroutine> Subroutines,
            ImmutableDictionary<IFieldSymbol, IVariableInfo> Globals)
        {
            this.CLRType = CLRType;
            this.Symbol = Symbol;
            this.Subroutines = Subroutines;
            this.Globals = Globals;
        }

        public ProcessedType(ProcessedType Base, Type CLRType=null, INamedTypeSymbol Symbol=null,
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
