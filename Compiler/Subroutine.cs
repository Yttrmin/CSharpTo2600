using System.Linq;
using System.Collections.Immutable;
using System.Reflection;
using CSharpTo2600.Framework;
using CSharpTo2600.Framework.Assembly;
using Microsoft.CodeAnalysis;

namespace CSharpTo2600.Compiler
{
    public class Subroutine
    {
        public readonly string Name;
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
                    throw new FatalCompilationException($"Attempted to access the body of a non-compiled subroutine: {Name}");
                }
            }
        }
        // Could just make Body nullable, but meaning might be unclear.
        public readonly bool IsCompiled;
        public readonly MethodType Type;
        private readonly IMethodSymbol Symbol;
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
