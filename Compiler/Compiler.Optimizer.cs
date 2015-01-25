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
                Console.WriteLine($"Optimization results: {Subroutine.InstructionCount} instructions to {Optimized.InstructionCount}. "
                    + $"{Subroutine.CycleCount} cycles to {Optimized.CycleCount}.");
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
                    //@TODO - i check shouldn't be neccessary once locals work.
                    if (i != Instructions.Length-1 && Instruction.Text == "PHA" && Instructions[i + 1].Text == "PLA")
                    {
                        // Skip this and the next instruction so they're not added.
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
