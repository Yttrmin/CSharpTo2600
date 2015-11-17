using System.Linq;
using System.Collections.Immutable;
using CSharpTo2600.Framework;
using CSharpTo2600.Framework.Assembly;
using Microsoft.CodeAnalysis;

namespace CSharpTo2600.Compiler
{
    /// <summary>
    /// Immutable representation of the metadata of a 6502 subroutine, excluding the body.
    /// </summary>
    public class Subroutine
    {
        public string Name { get; }
        public MethodType Type { get { return GetMethodType(); } }
        public KernelTechnique KernelTechnique { get { return GetKernelTechnique(); } }
        public Symbol Label { get; }
        public ProcessedType ReturnType { get; }
        internal IMethodSymbol Symbol { get; }

        public Subroutine(string Name, ProcessedType ReturnType, IMethodSymbol Symbol)
        {
            this.Name = Name;
            this.ReturnType = ReturnType;
            this.Symbol = Symbol;
            //@TODO - Make unique even for overloaded methods
            this.Label = AssemblyFactory.Label($"{Symbol.ContainingType.Name}_{Symbol.Name}");
        }

        private MethodType GetMethodType()
        {
            var MethodTypeAttribute = Symbol.GetAttributes().SingleOrDefault(a => a.AttributeClass.Name == nameof(SpecialMethodAttribute));
            if (MethodTypeAttribute == null)
            {
                return MethodType.UserDefined;
            }
            return (MethodType)MethodTypeAttribute.ConstructorArguments.Single().Value;
        }

        private KernelTechnique GetKernelTechnique()
        {
            if (Type != MethodType.Kernel)
            {
                throw new FatalCompilationException($"Attempted to determine the kernel technique for {Symbol}, but it's a {Type} method instead!");
            }
            var Attribute = Symbol.GetAttributes().SingleOrDefault(a => a.AttributeClass.Name == nameof(KernelAttribute));
            if (Attribute == null)
            {
                throw new FatalCompilationException($"Method {Symbol} is missing KernelAttribute.");
            }
            return (KernelTechnique)Attribute.ConstructorArguments.Single().Value;
        }

        public override string ToString()
        {
            return Symbol.Name;
        }
    }

    /// <summary>
    /// Compelte immutable representation of a 6502-compatible subroutine, including the body.
    /// </summary>
    public sealed class SubroutineInfo : Subroutine
    {
        /// <summary>
        /// The 6502 instructions that make up the subroutine.
        /// </summary>
        public ImmutableArray<AssemblyLine> Body { get; }
        /// <summary>
        /// Number of bytes the subroutine takes up in memory.
        /// </summary>
        public int Size { get { return Body.OfType<Instruction>().Sum(i => i.Size); } }
        public int InstructionCount { get { return Body.OfType<Instruction>().Count(); } }
        public int CycleCount { get { return Body.OfType<Instruction>().Sum(i => i.Cycles); } }

        internal SubroutineInfo(string Name, ProcessedType ReturnType, IMethodSymbol Symbol, 
            ImmutableArray<AssemblyLine> Body)
            : base(Name, ReturnType, Symbol)
        {
            this.Body = Body;
        }

        internal SubroutineInfo ReplaceBody(ImmutableArray<AssemblyLine> NewInstructions)
        {
            return new SubroutineInfo(Name, ReturnType, Symbol, NewInstructions);
        }
    }
}
