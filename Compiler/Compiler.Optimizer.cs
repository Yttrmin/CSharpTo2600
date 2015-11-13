using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using CSharpTo2600.Framework.Assembly;

namespace CSharpTo2600.Compiler
{
    partial class GameCompiler
    {
        private interface IOptimizer
        {
            SubroutineInfo PerformAllOptimizations(SubroutineInfo Subroutine);
        }

        private class NullOptimizer : IOptimizer
        {
            public SubroutineInfo PerformAllOptimizations(SubroutineInfo Subroutine)
            {
                // Do nothing.
                return Subroutine;
            }
        }

        private class Optimizer : IOptimizer
        {
            private delegate ImmutableArray<AssemblyLine> OptimizingMethod(ImmutableArray<AssemblyLine> SubroutineBody);

            public SubroutineInfo PerformAllOptimizations(SubroutineInfo Subroutine)
            {
                //@TODO - We need to loop and alternate between these two. Removing PHA/PLAs creates
                // LDA/PLAs. Removing LDA/PLAs creates PHA/PLAs.
                // Making a method just for removing pairs of instructions would also be great.
                var Optimized = PerformOptimization(RedundantStackPushPull, Subroutine);
                Optimized = PerformOptimization(RedundantLoadStackPull, Optimized);
                return Optimized;
            }

            /// <summary>
            /// Finds PHAs immediately followed by PLAs and deletes both instructions.
            /// </summary>
            /// Rationale: There is a value x in A. PHA puts x on the stack.
            /// PLA removes x from the stack and puts it in A. The result is the
            /// value x in A. Since A will always have the value it started with,
            /// we can remove the stack operations.
            /// Example: Happens all the time. The compiler pushes the value of a
            /// variable or literal onto the stack, which is immediately popped
            /// and used in some operation.
            private ImmutableArray<AssemblyLine> RedundantStackPushPull(ImmutableArray<AssemblyLine> Body)
            {
                var Optimized = new List<AssemblyLine>();
                Instruction PreviousInstruction = null;
                foreach (var Instruction in FilterLineOptimizing<Instruction>(Body, Optimized))
                {
                    if (PreviousInstruction != null && PreviousInstruction.OpCode == "PHA" && Instruction.OpCode == "PLA")
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

            /// <summary>
            /// Finds LDAs immediately followed by PLAs and deletes the LDA.
            /// </summary>
            /// Rationale: LDA puts the value x in A. PLA then puts the value y in A.
            /// There is no opportunity to use x, so the LDA can be safely deleted.
            /// Example: Truncation. A 32-bit value may be pushed onto the stack, only to be
            /// immediately truncated to 8-bit. Results in LDA/PHAs followed by PLAs.
            /// Eliminating PHA/PLA pairs results in LDA/PLA pairs.
            private ImmutableArray<AssemblyLine> RedundantLoadStackPull(ImmutableArray<AssemblyLine> Body)
            {
                var Optimized = new List<AssemblyLine>();
                Instruction PreviousInstruction = null;
                foreach (var Instruction in FilterLineOptimizing<Instruction>(Body, Optimized))
                {
                    if (PreviousInstruction != null && PreviousInstruction.OpCode == "LDA" && Instruction.OpCode == "PLA")
                    {
                        Optimized.Remove(PreviousInstruction);
                        Optimized.Add(Instruction);
                        PreviousInstruction = Instruction;
                    }
                    else
                    {
                        Optimized.Add(Instruction);
                        PreviousInstruction = Instruction;
                    }
                }
                return Optimized.ToImmutableArray();
            }

            private SubroutineInfo PerformOptimization(OptimizingMethod Method, SubroutineInfo ToOptimize)
            {
                var Result = ToOptimize;
                SubroutineInfo PreviousSubroutine;
                var Iterations = 0;
                // Iterate as long as it keeps removing instructions.
                do
                {
                    PreviousSubroutine = Result;
                    var NewBody = Method(PreviousSubroutine.Body);
                    Result = PreviousSubroutine.ReplaceBody(NewBody);
                    Iterations++;
                }
                while (Result.InstructionCount < PreviousSubroutine.InstructionCount);
                Console.WriteLine($"{Method.Method.Name} ({Iterations} iterations) results: {ToOptimize.InstructionCount} instructions to {Result.InstructionCount}. {ToOptimize.CycleCount} cycles to {Result.CycleCount}.");
                return Result;
            }

            /// <summary>
            /// Returns AssemblyLines of type T for processing while adding the rest to NewLines.
            /// </summary>
            private IEnumerable<T> FilterLineOptimizing<T>(IEnumerable<AssemblyLine> Lines, IList<AssemblyLine> NewLines)
                where T : AssemblyLine
            {
                foreach (var Line in Lines)
                {
                    if (Line is T)
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
