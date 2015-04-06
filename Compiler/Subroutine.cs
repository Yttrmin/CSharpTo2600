using System.Linq;
using System.Collections.Immutable;
using System.Reflection;
using CSharpTo2600.Framework;
using CSharpTo2600.Framework.Assembly;

namespace CSharpTo2600.Compiler
{
    internal class Subroutine
    {
        public readonly string Name;
        public readonly ImmutableArray<AssemblyLine> Body;
        public readonly MethodType Type;
        public MethodInfo OriginalMethod { get; }
        public int InstructionCount { get { return Body.OfType<Instruction>().Count(); } }
        public int CycleCount { get { return Body.OfType<Instruction>().Sum(i => i.Cycles); } }
        //@TODO - IsInstance/IsStatic

        public Subroutine(string Name, MethodInfo OriginalMethod, ImmutableArray<AssemblyLine> Body, MethodType Type)
        {
            this.Name = Name;
            this.OriginalMethod = OriginalMethod;
            this.Body = Body;
            this.Type = Type;
        }

        public Subroutine ReplaceBody(ImmutableArray<AssemblyLine> NewInstructions)
        {
            return new Subroutine(Name, OriginalMethod, NewInstructions, Type);
        }
    }
}
