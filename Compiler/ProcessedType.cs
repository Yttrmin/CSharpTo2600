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
        //@TODO - This will result in a circular reference if Subroutine's return type is 
        // its containing type. Resolve this similar to how StaticFields was.
        public ImmutableDictionary<IMethodSymbol, Subroutine> Subroutines { get; }
        public ImmutableArray<IFieldSymbol> StaticFields { get { return Symbol.GetMembers().OfType<IFieldSymbol>().ToImmutableArray(); } }
        public bool IsStatic { get { return Symbol.IsStatic; } }
        public bool IsValueType { get { return Symbol.IsValueType; } }
        public bool IsCompiled { get { return Subroutines.Values.Any(s => !s.IsCompiled); } }
        public string Name { get { return Symbol.Name; } }
        public int InstanceSize { get; }

        /// <summary>
        /// Construct a new ProcessedType from scratch.
        /// </summary>
        internal ProcessedType(INamedTypeSymbol Symbol, 
            ImmutableDictionary<IMethodSymbol, Subroutine> Subroutines,
            int? InstanceSize=null)
        {
            this.Symbol = Symbol;
            this.Subroutines = Subroutines;
            //@TODO - Instance fields, calculate size.
            this.InstanceSize = InstanceSize ?? 0;
        }

        /// <summary>
        /// Constructs a new ProcessedType based on an existing one.
        /// Any parameters not provided will be copied from the existing ProcessedType instead.
        /// </summary>
        internal ProcessedType(ProcessedType Base, INamedTypeSymbol Symbol=null,
            ImmutableDictionary<IMethodSymbol, Subroutine> Subroutines=null)
            : this(Symbol ?? Base.Symbol,
                  Subroutines ?? Base.Subroutines)
        {

        }

        internal static ProcessedType FromBuiltInType(INamedTypeSymbol Symbol, int InstanceSize)
        {
            return new ProcessedType(Symbol,
                ImmutableDictionary<IMethodSymbol, Subroutine>.Empty,
                InstanceSize);
        }

        public override string ToString()
        {
            return Symbol.ToString();
        }
    }
}
