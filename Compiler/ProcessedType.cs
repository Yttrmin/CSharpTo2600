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
        public ImmutableDictionary<IMethodSymbol, Subroutine> Subroutines { get; }
        public ImmutableDictionary<IFieldSymbol, IVariableInfo> StaticFields { get; }
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
            ImmutableDictionary<IFieldSymbol, IVariableInfo> Globals,
            int? InstanceSize=null)
        {
            this.Symbol = Symbol;
            this.Subroutines = Subroutines;
            this.StaticFields = Globals;
            //@TODO - Instance fields, calculate size.
            this.InstanceSize = InstanceSize ?? 0;
        }

        /// <summary>
        /// Constructs a new ProcessedType based on an existing one.
        /// Any parameters not provided will be copied from the existing ProcessedType instead.
        /// </summary>
        internal ProcessedType(ProcessedType Base, INamedTypeSymbol Symbol=null,
            ImmutableDictionary<IMethodSymbol, Subroutine> Subroutines=null,
            ImmutableDictionary<IFieldSymbol, IVariableInfo> Globals=null)
            : this(Symbol ?? Base.Symbol,
                  Subroutines ?? Base.Subroutines,
                  Globals ?? Base.StaticFields)
        {

        }

        internal static ProcessedType FromBuiltInType(INamedTypeSymbol Symbol, int InstanceSize)
        {
            return new ProcessedType(Symbol,
                ImmutableDictionary<IMethodSymbol, Subroutine>.Empty,
                ImmutableDictionary<IFieldSymbol, IVariableInfo>.Empty,
                InstanceSize);
        }

        public override string ToString()
        {
            return Symbol.ToString();
        }
    }
}
