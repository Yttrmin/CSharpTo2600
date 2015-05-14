using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace CSharpTo2600.Compiler
{
    /// <summary>
    /// Immutable representation of a type that has been processed by the compiler.
    /// </summary>
    public sealed class ProcessedType
    {
        //@TODO - Use only Symbol.
        public Type CLRType { get; }
        public INamedTypeSymbol Symbol { get; }
        public ImmutableDictionary<IMethodSymbol, Subroutine> Subroutines { get; }
        public ImmutableDictionary<IFieldSymbol, IVariableInfo> Globals { get; }
        public bool IsStatic { get { return CLRType.IsAbstract && CLRType.IsSealed; } }
        public bool IsValueType { get { return CLRType.IsValueType; } }
        public bool IsCompiled { get { return Subroutines.Values.Any(s => !s.IsCompiled); } }
        public string Name { get { return Symbol.Name; } }

        /// <summary>
        /// Construct a new ProcessedType from scratch.
        /// </summary>
        internal ProcessedType(Type CLRType, INamedTypeSymbol Symbol, 
            ImmutableDictionary<IMethodSymbol, Subroutine> Subroutines,
            ImmutableDictionary<IFieldSymbol, IVariableInfo> Globals)
        {
            this.CLRType = CLRType;
            this.Symbol = Symbol;
            this.Subroutines = Subroutines;
            this.Globals = Globals;
        }

        /// <summary>
        /// Constructs a new ProcessedType based on an existing one.
        /// Any parameters not provided will be copied from the existing ProcessedType instead.
        /// </summary>
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
