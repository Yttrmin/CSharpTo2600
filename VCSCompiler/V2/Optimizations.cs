﻿#nullable enable
using System;
using System.Collections.Immutable;
using System.Linq;
using VCSFramework.V2;

namespace VCSCompiler.V2
{
    internal partial class MethodCompiler
    {
        // Can't use a record since we need to change Next, which is a pain when immutable.
        private sealed class LinkedEntry
        {
            public IAssemblyEntry Value { get; }
            public LinkedEntry? Next { get; set; }

            public LinkedEntry(IAssemblyEntry value, LinkedEntry? next)
            {
                Value = value;
                Next = next;
            }

            public void Deconstruct(out IAssemblyEntry value, out LinkedEntry? next)
            {
                value = Value;
                next = Next;
            }
        }

        private delegate LinkedEntry Optimizer(LinkedEntry next);

        /// <summary>Optimizations that can't be disabled because the program literally won't assemble.</summary>
        private static ImmutableArray<Optimizer> MandatoryOptimizations = new Optimizer[]
        {
            // Turns an AssemblyUtilities.InlineAssembly() call into an entry that emits the assembly string.
            next => next switch
            {
                (LoadString(var ldStrInstruction), (InlineAssemblyCall, var trueNext)) => 
                    new(new InlineAssembly(((string)ldStrInstruction.Operand).Split(Environment.NewLine).Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).Prepend("// Begin inline assembly").Append("// End inline assembly").ToImmutableArray()), trueNext),
                _ => next
            },
        }.ToImmutableArray();

        /// <summary>Optimizations that can be disabled, and will just make the program less efficient (perhaps fatally so).</summary>
        private static ImmutableArray<Optimizer> OptionalOptimizations = new Optimizer[]
        {
            // PushConstant + PopToGlobal = AssignConstantToGlobal
            next => next switch
            {
                // Pushing an integer and popping to a boolean is valid CIL, so requiring identical types would be incorrect.
                (PushConstant(var instA, var constant, _, var size),
                (PopToGlobal(var instB, var global, _, _, _, _), var trueNext))
                    => new(new AssignConstantToGlobal(instA.Concat(instB), constant, global, size), trueNext),
                _ => next
            },

            // PushGlobal + PopToGlobal = CopyGlobalToGlobal
            next => next switch
            {
                (PushGlobal(var instA, var global, _, var size),
                (PopToGlobal(var instB, var targetGlobal, _, var targetSize, _, _), var trueNext))
                    => new(new CopyGlobalToGlobal(instA.Concat(instB), global, size, targetGlobal, targetSize), trueNext),
                _ => next
            },

            // Adding a global and constant via the stack can be done in one macro, avoiding putting the constant on the stack.
            next => next switch
            {
                // PushGlobal+PushConstant or PushConstant+PushGlobal are both fine.
                (PushGlobal(var instA, var global, var globalType, var globalSize),
                (PushConstant(var instB, var constant, var constantType, var constantSize),
                (AddFromStack(var instC, _, _, _, _), var trueNext)))
                    => new(new AddFromGlobalAndConstant(instA.Concat(instB.Concat(instC)), global, globalType, globalSize, constant, constantType, constantSize), trueNext),
                (PushConstant(var instA, var constant, var constantType, var constantSize),
                (PushGlobal(var instB, var global, var globalType, var globalSize),
                (AddFromStack(var instC, _, _, _, _), var trueNext)))
                    => new(new AddFromGlobalAndConstant(instA.Concat(instB.Concat(instC)), global, globalType, globalSize, constant, constantType, constantSize), trueNext),
                _ => next
            },

            // Adding a global and constant and storing to a global, can all be done in one macro off the stack.
            next => next switch
            {
                (AddFromGlobalAndConstant(var instA, var global, var globalType, var globalSize, var constant, var constantType, var constantSize),
                (PopToGlobal(var instB, var targetGlobal, var targetType, var targetSize, _, _), var trueNext))
                    => new(new AddFromGlobalAndConstantToGlobal(instA.Concat(instB), global, globalType, globalSize, constant, constantType, constantSize, targetGlobal, targetType, targetSize), trueNext),
                _ => next
            },

            // Adding 1 to a global, and storing it in the same global, can be done as a single increment macro.
            next => next switch
            {
                (AddFromGlobalAndConstantToGlobal(var inst, var sourceGlobal, var globalType, var globalSize, var constant, _, _, var targetGlobal, _, _), var trueNext)
                    when sourceGlobal == targetGlobal && constant.Value is byte b && b == 1
                    => new(new IncrementGlobal(inst, targetGlobal, globalType, globalSize), trueNext),
                _ => next
            },

            // Remove unconditional jumps to the very next instruction.
            next => next switch
            {
                // This primarily happens when inlining methods that have a single exit point. The 'ret' gets replaced with an
                // unconditional jump to the end of the method, which the 'ret' is already at, so it's completely useless.
                // This removes that unconditonal jump but leaves the label (in case there's other instructions in the
                // method that branch to it).
                (Branch(_, var targetLabel), (InstructionLabel instructionLabel, var trueNext)) when targetLabel == instructionLabel
                    => new(instructionLabel, trueNext),
                _ => next
            },
        }.ToImmutableArray();
    }
}
