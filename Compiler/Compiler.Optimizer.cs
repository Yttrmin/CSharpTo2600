using CSharpTo2600.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpTo2600.Compiler
{
    partial class Compiler
    {
        private class Optimizer
        {
            public Subroutine PefromAllOptimizations(Subroutine Subroutine)
            {
                var Instructions = Subroutine.Instructions;
                Instructions = RedundantStackPushPull(Instructions);
                var Optimized = Subroutine.ReplaceInstructions(Instructions);
                Console.WriteLine("Optimization results: \{Subroutine.InstructionCount} instructions to \{Optimized.InstructionCount}. "
                    + "\{Subroutine.CycleCount} cycles to \{Optimized.CycleCount}.");
                return Optimized;
            }

            /// <summary>
            /// Finds PHAs immediately followed by PLAs and deletes both instructions.
            /// </summary>
            public ImmutableArray<InstructionInfo> RedundantStackPushPull(ImmutableArray<InstructionInfo> Instructions)
            {
                //@TODO - Could do this with PHP/PLP when we support it.
                var Optimized = new List<InstructionInfo>();
                for(var i = 0; i < Instructions.Length; i++)
                {
                    var Instruction = Instructions[i];
                    if (Instruction.Text == "PHA" && Instructions[i + 1].Text == "PLA")
                    {
                        i++;
                        continue;
                    }
                    else
                    {
                        Optimized.Add(Instruction);
                    }
                }
                return Optimized.ToImmutableArray();
            }
        }
    }
}
