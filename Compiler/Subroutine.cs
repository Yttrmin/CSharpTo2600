using System.Linq;
using System.Collections.Immutable;
using CSharpTo2600.Framework;
using CSharpTo2600.Framework.Assembly;
using Microsoft.CodeAnalysis;

namespace CSharpTo2600.Compiler
{
    /// <summary>
    /// Immutable representation of a 6502-compatible subroutine.
    /// </summary>
    public sealed class Subroutine
    {
        public string Name { get; }
        // Is there any case where no body is an intended final result?
        private readonly ImmutableArray<AssemblyLine> _Body;
        public ImmutableArray<AssemblyLine> Body
        {
            get
            {
                if (IsCompiled)
                {
                    return _Body;
                }
                else
                {
                    // You normally shouldn't throw in a getter, but something is terribly
                    // wrong if this is reached.
                    throw new FatalCompilationException($"Attempted to access the body of a non-compiled subroutine: {Name}");
                }
            }
        }
        // Could just make Body nullable, but meaning might be unclear.
        public bool IsCompiled { get; }
        public MethodType Type { get { return GetMethodType(); } }
        public KernelTechnique KernelTechnique { get { return GetKernelTechnique(); } }
        public Symbol Label { get; }
        internal IMethodSymbol Symbol { get; }
        public int InstructionCount { get { return Body.OfType<Instruction>().Count(); } }
        public int CycleCount { get { return Body.OfType<Instruction>().Sum(i => i.Cycles); } }
        public ProcessedType ReturnType { get; }

        internal Subroutine(string Name, ProcessedType ReturnType, IMethodSymbol Symbol, 
            ImmutableArray<AssemblyLine>? Body)
        {
            this.Name = Name;
            this.ReturnType = ReturnType;
            this.Symbol = Symbol;
            //@TODO - Make unique even for overloaded methods
            Label = AssemblyFactory.Label($"{Symbol.ContainingType.Name}_{Symbol.Name}");
            if (Body.HasValue)
            {
                IsCompiled = true;
                _Body = Body.Value;
            }
            else
            {
                IsCompiled = false;
                _Body = ImmutableArray<AssemblyLine>.Empty;
            }
        }

        internal Subroutine(string Name, ProcessedType ReturnType, IMethodSymbol Symbol, 
            MethodType Type)
            : this(Name, ReturnType, Symbol, ImmutableArray<AssemblyLine>.Empty)
        {
        }

        private MethodType GetMethodType()
        {
            var MethodTypeAttribute = Symbol.GetAttributes().SingleOrDefault(a => a.AttributeClass.Name == nameof(SpecialMethodAttribute));
            if(MethodTypeAttribute == null)
            {
                return MethodType.UserDefined;
            }
            return (MethodType)MethodTypeAttribute.ConstructorArguments.Single().Value;
        }

        private KernelTechnique GetKernelTechnique()
        {
            if(Type != MethodType.Kernel)
            {
                throw new FatalCompilationException($"Attempted to determine the kernel technique for {Symbol}, but it's a {Type} method instead!");
            }
            var Attribute = Symbol.GetAttributes().SingleOrDefault(a => a.AttributeClass.Name == nameof(KernelAttribute));
            if(Attribute == null)
            {
                throw new FatalCompilationException($"Method {Symbol} is missing KernelAttribute.");
            }
            return (KernelTechnique)Attribute.ConstructorArguments.Single().Value;
        }

        internal Subroutine ReplaceBody(ImmutableArray<AssemblyLine> NewInstructions)
        {
            return new Subroutine(Name, ReturnType, Symbol, NewInstructions);
        }

        public override string ToString()
        {
            return Symbol.Name;
        }
    }
}
