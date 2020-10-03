﻿#nullable enable
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using VCSFramework.V2;

namespace VCSCompiler.V2
{
    // @TODO - Since every Optimization (at least so far) is just a single overridden method, we could probably reduce them
    // to English comments and delegate instances and cut down on some of the boilerplate.

    /// <summary>
    /// Base class of all optimizations.
    /// Optimizations take 1..x number of contiguous Macros and reduce them to 1 instruction.
    /// </summary>
    internal abstract class Optimization
    {
        /// <summary>
        /// Attempts to perform a specific optimization upon <paramref name="entries"/>.
        /// If there are no entries that can be optimized by this optimizer, no changes will be made.
        /// </summary>
        /// <param name="entries">The <see cref="AssemblyEntry"/>s to optimize.</param>
        /// <returns>A new <see cref="ImmutableArray{AssemblyEntry}"/> that may or may not differ from what was passed in.</returns>
        public ImmutableArray<AssemblyEntry> Optimize(ImmutableArray<AssemblyEntry> entries)
        {
            // Convert the array of elements into a linked list of LinkedEntry.
            var firstNode = new LinkedEntry(entries[0], null);
            Enumerable.Range(0, entries.Length)
                .Skip(1)
                .Aggregate(firstNode, (pair, i) =>
                {
                    pair.Next = new LinkedEntry(entries[i], null);
                    return pair.Next;
                });

            // Root is only used so that the firstElement is optimizable, we discard it later.
            var root = new LinkedEntry(new Comment("I SHOULD NOT BE EMITTED!!! OPTIMIZATION BUG!!!"), firstNode);
            for (var node = root; node != null; node = node.Next)
            {
                var pendingNext = node.Next;
                if (pendingNext == null)
                {
                    break;
                }
                node.Next = DetermineNextEntry(pendingNext);
            }

            return LinkedToEntries(firstNode).ToImmutableArray();

            static IEnumerable<AssemblyEntry> LinkedToEntries(LinkedEntry firstNode)
            {
                for (var node = firstNode; node != null; node = node.Next)
                {
                    yield return node.Value;
                }
            }
        }

        /// <summary>
        /// Based on the provided node and X number of subsequent nodes, either return a more optimized
        /// node, or return <paramref name="next"/> if no optimization is made.
        /// </summary>
        /// <param name="next">The next node in the linked list of entries.</param>
        /// <returns>A more optimized node, or <paramref name="next"/> if no optimization was made.</returns>
        protected abstract LinkedEntry DetermineNextEntry(LinkedEntry next);

        /// <summary>
        /// A singly linked list node containing an <see cref="AssemblyEntry"/> and a link to the next node.
        /// </summary>
        protected sealed class LinkedEntry
        {
            public LinkedEntry? Next { get; set; }
            public AssemblyEntry Value { get; }

            public LinkedEntry(AssemblyEntry value, LinkedEntry? next)
            {
                Value = value;
                Next = next;
            }

            public void Deconstruct(out AssemblyEntry Value, out LinkedEntry? Next)
            {
                Value = this.Value;
                Next = this.Next;
            }
        }
    }

    /// <summary>
    /// <see cref="PushConstant"/> + <see cref="PopToGlobal"/> = <see cref="AssignConstantToGlobal"/>
    /// </summary>
    internal sealed class PushConstantPopToGlobalOptimization : Optimization
    {
        protected override LinkedEntry DetermineNextEntry(LinkedEntry next)
        {
            // Pushing an integer and popping to a boolean is valid CIL, so requiring identical types would be incorrect.
            return next switch
            {
                (PushConstant(var instA, var constant, _, var size), 
                (PopToGlobal(var instB, var global, _, _, _, _), var trueNext))
                    => new LinkedEntry(new AssignConstantToGlobal(instA.Concat(instB), constant, global, size), trueNext),
                _ => next
            };
        }
    }

    internal sealed class PushGlobalPopToGlobal_To_CopyGlobalToGlobal : Optimization
    {
        protected override LinkedEntry DetermineNextEntry(LinkedEntry next)
        {
            return next switch
            {
                (PushGlobal(var instA, var global, _, var size),
                (PopToGlobal(var instB, var targetGlobal, _, var targetSize, _, _), var trueNext))
                    => new LinkedEntry(new CopyGlobalToGlobal(instA.Concat(instB), global, size, targetGlobal, targetSize), trueNext),
                _ => next
            };
        }
    }

    internal sealed class PushGlobalPushConstantAddFromStack_To_AddFromGlobalAndConstant : Optimization
    {
        protected override LinkedEntry DetermineNextEntry(LinkedEntry next)
        {
            return next switch
            {
                // PushGlobal+PushConstant or PushConstant+PushGlobal are both fine.
                (PushGlobal(var instA, var global, var globalType, var globalSize),
                (PushConstant(var instB, var constant, var constantType, var constantSize),
                (AddFromStack(var instC, _, _, _, _), var trueNext)))
                    => new LinkedEntry(new AddFromGlobalAndConstant(instA.Concat(instB.Concat(instC)), global, globalType, globalSize, constant, constantType, constantSize), trueNext),
                (PushConstant(var instA, var constant, var constantType, var constantSize),
                (PushGlobal(var instB, var global, var globalType, var globalSize),
                (AddFromStack(var instC, _, _, _, _), var trueNext)))
                    => new LinkedEntry(new AddFromGlobalAndConstant(instA.Concat(instB.Concat(instC)), global, globalType, globalSize, constant, constantType, constantSize), trueNext),
                _ => next
            };
        }
    }

    internal sealed class AddFromGlobalAndConstantPopToGlobal_To_AddFromGlobalAndConstantToGlobal : Optimization
    {
        protected override LinkedEntry DetermineNextEntry(LinkedEntry next)
        {
            return next switch
            {
                (AddFromGlobalAndConstant(var instA, var global, var globalType, var globalSize, var constant, var constantType, var constantSize),
                (PopToGlobal(var instB, var targetGlobal, var targetType, var targetSize, _, _), var trueNext))
                    => new LinkedEntry(new AddFromGlobalAndConstantToGlobal(instA.Concat(instB), global, globalType, globalSize, constant, constantType, constantSize, targetGlobal, targetType, targetSize), trueNext),
                _ => next
            };
        }
    }

    internal sealed class AddFromGlobalAndConstantToGlobal_To_IncrementGlobal : Optimization
    {
        protected override LinkedEntry DetermineNextEntry(LinkedEntry next)
        {
            return next switch
            {
                (AddFromGlobalAndConstantToGlobal(var inst, var sourceGlobal, var globalType, var globalSize, var constant, _, _, var targetGlobal, _, _), var trueNext)
                    when sourceGlobal == targetGlobal && constant.Value is byte b && b == 1
                    => new LinkedEntry(new IncrementGlobal(inst, targetGlobal, globalType, globalSize), trueNext),
                _ => next
            };
        }
    }

    internal sealed class EliminateUnconditionalBranchToNextInstruction : Optimization
    {
        protected override LinkedEntry DetermineNextEntry(LinkedEntry next)
        {
            // This primarily happens when inlining methods that have a single exit point. The 'ret' gets replaced with an
            // unconditional jump to the end of the method, which the 'ret' is already at, so it's completely useless.
            // This removes that unconditonal jump but leaves the label (in case there's other instructions in the
            // method that branch to it).
            return next switch
            {
                (Branch(_, var targetLabel), (InstructionLabel instructionLabel, var trueNext)) when targetLabel == instructionLabel 
                    => new LinkedEntry(instructionLabel, trueNext),
                _ => next
            };
        }
    }

    internal sealed class InlineAssemblyInvocationToInlineAssemblyEntry : Optimization
    {
        protected override LinkedEntry DetermineNextEntry(LinkedEntry next)
        {
            return next switch
            {
                (LoadString(var ldStrInstruction), (InlineAssembly(_), var trueNext)) => new(new InlineAssemblyEntry((string)ldStrInstruction.Single().Operand), trueNext),
                _ => next
            };
        }
    }
}
