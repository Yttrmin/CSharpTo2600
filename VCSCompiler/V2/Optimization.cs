#nullable enable
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using VCSFramework.V2;

namespace VCSCompiler.V2
{
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
            return next switch
            {
                (PushConstant(var constant, var constType, var size, var instA), 
                (PopToGlobal(var global, var globalType, _, var instB), _))
                    when constType.Equals(globalType) 
                    => new LinkedEntry(new AssignConstantToGlobal(instA.Concat(instB), constant, global, size), next.Next?.Next),
                _ => next
            };
        }
    }
}
