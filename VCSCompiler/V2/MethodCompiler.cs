﻿#nullable enable
using Mono.Cecil;
using Mono.Cecil.Cil;
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
        // @TODO - Probably just use an enum.
        private readonly bool Inline;
        private readonly bool Entrypoint;
        private readonly AssemblyDefinition UserAssembly;
        private readonly CilInstructionCompiler.Options? CilOptions;

        public static Function Compile(MethodDefinition method, AssemblyDefinition userAssembly, bool inline, bool entrypoint = false, CilInstructionCompiler.Options? cilOptions = null)
            => new MethodCompiler(method, userAssembly, inline, entrypoint, cilOptions).Compile();

        private MethodCompiler(MethodDefinition method, AssemblyDefinition userAssembly, bool inline, bool entrypoint, CilInstructionCompiler.Options? cilOptions)
        {
            Method = method;
            UserAssembly = userAssembly;
            Inline = inline;
            Entrypoint = entrypoint;
            CilOptions = cilOptions != null ? cilOptions with { LiftLocals = !method.IsRecursive() } : null;
        }

        private Function Compile()
        {
            var cilCompiler = new CilInstructionCompiler(Method, UserAssembly, CilOptions);
            var body = cilCompiler.Compile()
                .ToImmutableArray();
            if (Inline)
            {
                var endLabel = new BranchTargetLabel("INLINE_RET_TARGET");
                body = body.Prepend(new BeginBlock()).Append(endLabel).Append(new EndBlock()).Select(entry =>
                {
                    if (entry is ReturnVoid returnFromCall)
                    {
                        // Probably will always have exactly 1 instruction, right?
                        return new Branch(returnFromCall.SourceInstruction, endLabel);
                    }
                    return entry;
                }).ToImmutableArray();
            }
            else if (Entrypoint)
            {
                // @TODO - Should invoke all .cctors.
                // Prepend the .cctor if there is one.
                var cctor = Method.DeclaringType.Methods.SingleOrDefault(m => m.Name == ".cctor");
                if (cctor != null)
                {
                    var inlineCctor = MethodCompiler.Compile(cctor, UserAssembly, true);
                    body = inlineCctor.Body.Concat(body).ToImmutableArray();
                }
                body = body.Prepend(new EntryPoint()).ToImmutableArray();
            }
            if (!Compiler.Options.DisableOptimizations)
            {
                body = Optimize(body);
            }
            var inlineString = Inline ? " inline call of " : " ";
            body = GenerateStackOps(body);
            if (!Inline) // @TODO - Extension PrependIf() ?
                body = body.Prepend(new MethodLabel(Method)).ToImmutableArray();
            body = body
                .Prepend(new Comment($"Begin{inlineString}{Method.FullName}"))
                .Append(new Comment($"End{inlineString}{Method.FullName}"))
                .Append(new EndFunction())
                .ToImmutableArray();
            return new Function(Method, body);
        }

        private ImmutableArray<IAssemblyEntry> Optimize(ImmutableArray<IAssemblyEntry> entries)
        {
            ImmutableArray<IAssemblyEntry> preOptimize;
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
                messageBuilder.AppendLine(string.Join(Environment.NewLine, invalidEntries));
                throw new InvalidOperationException(messageBuilder.ToString());
            }
            return postOptimize;

            /// <summary>
            /// Attempts to perform a specific optimization upon <paramref name="entries"/>.
            /// If there are no entries that can be optimized by this optimizer, no changes will be made.
            /// </summary>
            /// <param name="entries">The <see cref="AssemblyEntry"/>s to optimize.</param>
            /// <returns>A new <see cref="ImmutableArray{AssemblyEntry}"/> that may or may not differ from what was passed in.</returns>
            static ImmutableArray<IAssemblyEntry> Optimize(ImmutableArray<IAssemblyEntry> entries, Optimizer optimizer)
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

                static IEnumerable<IAssemblyEntry> LinkedToEntries(LinkedEntry firstNode)
                {
                    for (var node = firstNode; node != null; node = node.Next)
                    {
                        yield return node.Value;
                    }
                }
            }

            static LinkedEntry Replace(LinkedEntry root, LinkedEntry toReplace, LinkedEntry newEntry)
            {
                if (root.Next == toReplace)
                    return new LinkedEntry(root.Value, newEntry);
                else if (root.Next != null)
                    return new LinkedEntry(root.Value, Replace(root.Next, toReplace, newEntry));
                else
                    return root;
            }
        }

        /// <summary>
        /// Generates stack-related let psuedoops that let us attempt to track the size/type of elements on the stack.
        /// This must be called AFTER optimizations. Most optimizations eliminate stack operations anyways. But the
        /// presence of the psuedoops will likely interfere with most optimizer's pattern matching too.
        /// </summary>
        private ImmutableArray<IAssemblyEntry> GenerateStackOps(ImmutableArray<IAssemblyEntry> entries)
        {
            var stackTracker = new StackTracker(entries);

            var initialization = stackTracker.GenerateInitializationEntries();
            var entriesWithStackLets = entries
                .Select(entry =>
                {
                    if (entry is IMacroCall macroCall)
                    {
                        macroCall.PerformStackOperation(stackTracker);
                        if (stackTracker.TryGenerateStackOperation(out var stackOperation))
                        {
                            return new StackMutatingMacroCall(macroCall, stackOperation);
                        }
                    }
                    return entry;
                });

            return initialization.Concat(entriesWithStackLets).ToImmutableArray();
        }
    }
}
