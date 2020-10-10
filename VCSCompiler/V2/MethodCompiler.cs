#nullable enable
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VCSFramework.V2;

namespace VCSCompiler.V2
{
    internal partial class MethodCompiler
    {
        private readonly MethodDefinition Method;
        private readonly bool Inline;
        private readonly AssemblyDefinition UserAssembly;
        private readonly CilInstructionCompiler.Options? CilOptions;

        public static ImmutableArray<AssemblyEntry> Compile(MethodDefinition method, AssemblyDefinition userAssembly, bool inline, CilInstructionCompiler.Options? cilOptions = null)
            => new MethodCompiler(method, userAssembly, inline, cilOptions).Compile();

        public MethodCompiler(MethodDefinition method, AssemblyDefinition userAssembly, bool inline, CilInstructionCompiler.Options? cilOptions)
        {
            Method = method;
            UserAssembly = userAssembly;
            Inline = inline;
            CilOptions = cilOptions;
        }

        public ImmutableArray<AssemblyEntry> Compile()
        {
            var cilCompiler = new CilInstructionCompiler(Method, UserAssembly, CilOptions);
            var body = cilCompiler.Compile().ToImmutableArray();
            if (Inline)
            {
                var endLabel = new InstructionLabel("INLINE_RET_TARGET");
                body = body.Prepend(new BeginBlock()).Append(endLabel).Append(new EndBlock()).Select(entry =>
                {
                    if (entry is ReturnVoid returnFromCall)
                    {
                        // Probably will always have exactly 1 instruction, right?
                        return new Branch(returnFromCall.Instructions.Single(), endLabel);
                    }
                    return entry;
                }).ToImmutableArray();
            }
            if (!Compiler.Options.DisableOptimizations)
            {
                body = Optimize(body);
            }
            body = GenerateStackOps(body);
            return body;
        }

        private ImmutableArray<AssemblyEntry> Optimize(ImmutableArray<AssemblyEntry> entries)
        {
            ImmutableArray<AssemblyEntry> preOptimize;
            var postOptimize = entries;

            // Optimizers may rely on the output of other optimizers. So loop until there's 
            // no more changes being made.
            do
            {
                preOptimize = postOptimize;
                if (!Compiler.Options.DisableOptimizations)
                {
                    postOptimize = OptionalOptimizations.Aggregate(preOptimize, Optimize);
                }
                postOptimize = MandatoryOptimizations.Aggregate(postOptimize, Optimize);
            } while (!preOptimize.SequenceEqual(postOptimize));

            var invalidEntries = postOptimize.Where(e => e.GetType() == typeof(LoadString) || e.GetType() == typeof(InlineAssembly)).ToImmutableArray();
            if (invalidEntries.Any())
            {
                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine("Invalid entries found in post-optimized code. This is likely the result of using special features (e.g. InlineAssembly()) that were compiled into a form that wasn't detected by its Optimizer. Make sure you're only invoking them exactly how they're documented.");
                messageBuilder.AppendLine("Invalid entries:");
                messageBuilder.AppendLine(string.Join(Environment.NewLine, invalidEntries.Select(e => e.Output)));
                throw new InvalidOperationException(messageBuilder.ToString());
            }
            return postOptimize;

            /// <summary>
            /// Attempts to perform a specific optimization upon <paramref name="entries"/>.
            /// If there are no entries that can be optimized by this optimizer, no changes will be made.
            /// </summary>
            /// <param name="entries">The <see cref="AssemblyEntry"/>s to optimize.</param>
            /// <returns>A new <see cref="ImmutableArray{AssemblyEntry}"/> that may or may not differ from what was passed in.</returns>
            static ImmutableArray<AssemblyEntry> Optimize(ImmutableArray<AssemblyEntry> entries, Optimizer optimizer)
            {
                // Convert the array of elements into a linked list of LinkedEntry.
                var lastNode = new LinkedEntry(entries.Last(), null);
                var firstNode = Enumerable.Range(0, entries.Length)
                    .Reverse()
                    .Skip(1)
                    .Aggregate(lastNode, (nextEntry, i) =>
                    {
                        return new LinkedEntry(entries[i], nextEntry);
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
                    node.Next = optimizer(pendingNext);
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
        }

        /// <summary>
        /// Generates stack-related let psuedoops that let us attempt to track the size/type of elements on the stack.
        /// This must be called AFTER optimizations. Most optimizations eliminate stack operations anyways. But the
        /// presence of the psuedoops will likely interfere with most optimizer's pattern matching too.
        /// </summary>
        private ImmutableArray<AssemblyEntry> GenerateStackOps(ImmutableArray<AssemblyEntry> entries)
        {
            var stackTracker = new StackTracker(entries);

            var initialization = stackTracker.GenerateInitializationEntries();
            var entriesWithStackLets = entries
                .Select(entry =>
                {
                    if (entry is Macro macro)
                    {
                        return macro.WithStackLets(stackTracker, LabelGenerator.NothingType, LabelGenerator.NothingSize);
                    }
                    return entry;
                });

            return initialization.Concat(entriesWithStackLets).ToImmutableArray();
        }
    }
}
