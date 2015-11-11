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
        public INamedTypeSymbol Symbol { get; }
        public ImmutableArray<IMethodSymbol> Subroutines { get { return Symbol.GetMembers().OfType<IMethodSymbol>().ToImmutableArray(); } }
        public ImmutableArray<IFieldSymbol> StaticFields { get { return Symbol.GetMembers().OfType<IFieldSymbol>().ToImmutableArray(); } }
        public bool IsStatic { get { return Symbol.IsStatic; } }
        public bool IsValueType { get { return Symbol.IsValueType; } }
        public string Name { get { return Symbol.Name; } }
        public int InstanceSize { get; }

        /// <summary>
        /// Construct a new ProcessedType from scratch.
        /// </summary>
        internal ProcessedType(INamedTypeSymbol Symbol, int? InstanceSize=null)
        {
            this.Symbol = Symbol;
            //@TODO - Instance fields, calculate size.
            this.InstanceSize = InstanceSize ?? 0;
        }

        /// <summary>
        /// Constructs a new ProcessedType based on an existing one.
        /// Any parameters not provided will be copied from the existing ProcessedType instead.
        /// </summary>
        internal ProcessedType(ProcessedType Base, INamedTypeSymbol Symbol=null)
            : this(Symbol ?? Base.Symbol)
        {

        }

        internal static ProcessedType FromBuiltInType(INamedTypeSymbol Symbol, int InstanceSize)
        {
            return new ProcessedType(Symbol, InstanceSize);
        }

        public override string ToString()
        {
            return Symbol.ToString();
        }
    }
}
