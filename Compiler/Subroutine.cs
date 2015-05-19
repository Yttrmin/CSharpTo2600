using System.Linq;
using System.Collections.Immutable;
using System.Reflection;
using CSharpTo2600.Framework;
using CSharpTo2600.Framework.Assembly;
using Microsoft.CodeAnalysis;

namespace CSharpTo2600.Compiler
{
    /// <summary>
    /// Immutable representation of a 6502-compatible subroutine.
    /// </summary>
    public class Subroutine
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
        public MethodType Type { get; }
        public Symbol Label { get; }
        private IMethodSymbol Symbol { get; }
        public MethodInfo OriginalMethod { get; }
        public int InstructionCount { get { return Body.OfType<Instruction>().Count(); } }
        public int CycleCount { get { return Body.OfType<Instruction>().Sum(i => i.Cycles); } }
        //@TODO - IsInstance/IsStatic

        internal Subroutine(string Name, MethodInfo OriginalMethod, IMethodSymbol Symbol, 
            ImmutableArray<AssemblyLine>? Body, MethodType Type)
        {
            this.Name = Name;
            this.OriginalMethod = OriginalMethod;
            this.Type = Type;
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

        internal Subroutine(string Name, MethodInfo OriginalMethod, IMethodSymbol Symbol, MethodType Type)
            : this(Name, OriginalMethod, Symbol, ImmutableArray<AssemblyLine>.Empty, Type)
        {
        }

        internal Subroutine ReplaceBody(ImmutableArray<AssemblyLine> NewInstructions)
        {
            return new Subroutine(Name, OriginalMethod, Symbol, NewInstructions, Type);
        }

        public override string ToString()
        {
            return OriginalMethod.ToString();
        }
    }
}
