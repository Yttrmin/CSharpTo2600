using CSharpTo2600.Framework.Assembly;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CSharpTo2600.Compiler
{
    internal static class SubroutineBodyExtensions
    {
        public static IEnumerable<AssemblyLine> StripForInlining(this IEnumerable<AssemblyLine> SubroutineBody)
        {
            // Assume void return, 0 params for now. So just strip RTS.
            var LastInstruction = (Instruction)SubroutineBody.Last();
            Debug.Assert(LastInstruction.OpCode == "RTS");
            return SubroutineBody.Take(SubroutineBody.Count() - 1);
        }

        public static IEnumerable<AssemblyLine> StripTrivia(this IEnumerable<AssemblyLine> SubroutineBody)
        {
            return SubroutineBody.Where(a => !(a is Trivia));
        }
    }
}
