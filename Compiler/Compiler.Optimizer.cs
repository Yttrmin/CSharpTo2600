using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using CSharpTo2600.Framework.Assembly;

namespace CSharpTo2600.Compiler
{
    partial class Compiler
    {
        private class Optimizer
        {
            public Subroutine PefromAllOptimizations(Subroutine Subroutine)
            {
                var Instructions = Subroutine.Body;
                Instructions = RedundantStackPushPull(Instructions);
                var Optimized = Subroutine.ReplaceBody(Instructions);
                Console.WriteLine($"Optimization results: {Subroutine.InstructionCount} instructions to {Optimized.InstructionCount}. "
                    + $"{Subroutine.CycleCount} cycles to {Optimized.CycleCount}.");
                return Optimized;
            }

            /// <summary>
            /// Finds PHAs immediately followed by PLAs and deletes both instructions.
            /// </summary>
            public ImmutableArray<AssemblyLine> RedundantStackPushPull(ImmutableArray<AssemblyLine> Body)
            {
                //@TODO - Could do this with PHP/PLP when we support it.
                var Optimized = new List<AssemblyLine>();
                Instruction PreviousInstruction = null;
                foreach(var Instruction in FilterLineOptimizing<Instruction>(Body, Optimized))
                {
                    if(PreviousInstruction != null && PreviousInstruction.OpCode == "PHA" && Instruction.OpCode == "PLA")
                    {
                        Optimized.Remove(PreviousInstruction);
                        PreviousInstruction = null;
                    }
                    else
                    {
                        Optimized.Add(Instruction);
                        PreviousInstruction = Instruction;
                    }
                }
                return Optimized.ToImmutableArray();
            }

            private IEnumerable<T> FilterLineOptimizing<T>(IEnumerable<AssemblyLine> Lines, IList<AssemblyLine> NewLines)
                where T : AssemblyLine
            {
                foreach(var Line in Lines)
                {
                    if(Line is T)
                    {
                        yield return (T)Line;
                    }
                    else
                    {
                        NewLines.Add(Line);
                    }
                }
            }
        }
    }
}
