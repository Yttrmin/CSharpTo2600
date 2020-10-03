﻿#nullable enable
using Mono.Cecil;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VCSFramework.V2;

namespace VCSCompiler.V2
{
    internal class MethodCompiler
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
            var optimizers = new Optimization[]
            {
                new PushConstantPopToGlobalOptimization(),
                new PushGlobalPopToGlobal_To_CopyGlobalToGlobal(),
                new PushGlobalPushConstantAddFromStack_To_AddFromGlobalAndConstant(),
                new AddFromGlobalAndConstantPopToGlobal_To_AddFromGlobalAndConstantToGlobal(),
                new AddFromGlobalAndConstantToGlobal_To_IncrementGlobal(),
                new EliminateUnconditionalBranchToNextInstruction(),
                new InlineAssemblyInvocationToInlineAssemblyEntry(),
            };

            ImmutableArray<AssemblyEntry> preOptimize;
            var postOptimize = entries;

            // Optimizers may rely on the output of other optimizers. So loop until there's 
            // no more changes being made.
            do
            {
                preOptimize = postOptimize;
                foreach (var optimizer in optimizers)
                {
                    postOptimize = optimizer.Optimize(postOptimize);
                }
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
