#nullable enable
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCSFramework.V2;

namespace VCSCompiler.V2
{
    internal class MethodCompiler
    {
        private readonly MethodDefinition Method;
        private readonly bool Inline;
        private readonly AssemblyDefinition UserAssembly;

        public static ImmutableArray<AssemblyEntry> Compile(MethodDefinition method, AssemblyDefinition userAssembly, bool inline)
            => new MethodCompiler(method, userAssembly, inline).Compile();

        public MethodCompiler(MethodDefinition method, AssemblyDefinition userAssembly, bool inline)
        {
            Method = method;
            UserAssembly = userAssembly;
            Inline = inline;
        }

        public ImmutableArray<AssemblyEntry> Compile()
        {
            // @TODO - Options
            var cilCompiler = new CilInstructionCompiler(Method, UserAssembly);
            // @TODO - Special treatment for inlining
            var body = cilCompiler.Compile().ToImmutableArray();
            body = Optimize(body);
            body = GenerateStackOps(body);
            if (Inline)
            {
                var endLabel = new InstructionLabel("INLINE_RET_TARGET");
                body = body.Prepend(new BeginBlock()).Append(endLabel).Append(new EndBlock()).Select(entry =>
                {
                    if (entry is ReturnFromCall returnFromCall)
                    {
                        // Probably will always have exactly 1 instruction, right?
                        return new Branch(returnFromCall.Instructions.Single(), endLabel);
                    }
                    return entry;
                }).ToImmutableArray();
            }
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
            return postOptimize;
        }

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
