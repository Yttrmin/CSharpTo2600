using System.Linq;
using System.Collections.Immutable;
using CSharpTo2600.Framework;

namespace CSharpTo2600.Compiler
{
    internal class Subroutine
    {
        public readonly string Name;
        [System.Obsolete("Replace InstructionInfo")]
        public readonly ImmutableArray<InstructionInfo> Instructions;
        public readonly MethodType Type;
        //@TODO - Handle comments.
        public int InstructionCount { get { return Instructions.Length; } }
        public int CycleCount { get { return Instructions.Sum(i => i.Cycles); } }

        public Subroutine(string Name, ImmutableArray<InstructionInfo> Instructions, MethodType Type)
        {
            this.Name = Name;
            this.Instructions = Instructions;
            this.Type = Type;
        }

        public Subroutine ReplaceInstructions(ImmutableArray<InstructionInfo> NewInstructions)
        {
            return new Subroutine(Name, NewInstructions, Type);
        }
    }
}
