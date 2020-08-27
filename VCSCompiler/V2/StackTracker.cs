#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using VCSFramework.V2;

namespace VCSCompiler.V2
{
    public sealed class StackTracker : IStackTracker
    {
        // @TODO - May need an "Unknown" type for when we cross basic block boundaries.
        private abstract record BaseStackElement
        {
            public abstract string TypeString { get; }
            public abstract string SizeString { get; }
        }

        private sealed record TypedStackElement(TypeLabel Type, SizeLabel Size)
            : BaseStackElement
        {
            public static TypedStackElement Nothing
                => new(LabelGenerator.NothingType, LabelGenerator.NothingSize);

            public override string TypeString => Type;

            public override string SizeString => Size;
        }

        private sealed record FunctionStackElement(Function TypeFunction, Function SizeFunction)
            : BaseStackElement
        {
            public override string TypeString => TypeFunction;

            public override string SizeString => SizeFunction;
        }

        private sealed record IndexedStackElement(int Index) : BaseStackElement
        {
            public override string TypeString => new StackTypeArrayLabel(Index);

            public override string SizeString => new StackSizeArrayLabel(Index);
        }

        private static readonly LetLabel StackTypeLabel = new("STACK_TYPEOF");
        private static readonly LetLabel StackSizeLabel = new("STACK_SIZEOF");
        private readonly BaseStackElement[] StackState;
        private int MaxDepth => StackState.Length;

        public StackTracker(ImmutableArray<AssemblyEntry> entries)
        {
            var maxPush = 0;
            var maxPop = 0;
            foreach (var entry in entries.OfType<Macro>())
            {
                if (entry.Effects.OfType<PushStackAttribute>().SingleOrDefault() is PushStackAttribute pushAttr)
                {
                    maxPush = Math.Max(maxPush, pushAttr.Count);
                }
                if (entry.Effects.OfType<PopStackAttribute>().SingleOrDefault() is PopStackAttribute popAttr)
                {
                    maxPop = Math.Max(maxPop, popAttr.Count);
                }
            }
            var maxDepth = Math.Max(maxPush, maxPop);

            StackState = Enumerable.Repeat(TypedStackElement.Nothing, maxDepth)
                .Cast<BaseStackElement>().ToArray();
        }

        public void Push(TypeLabel type, SizeLabel size)
        {
            CheckDepth();
            PercolateUp();

            StackState[0] = new TypedStackElement(type, size);
        }

        public void Push(Function typeFunction, Function sizeFunction)
        {
            CheckDepth();
            PercolateUp();

            StackState[0] = new FunctionStackElement(typeFunction, sizeFunction);
        }

        public void Pop(int amount = 1)
        {
            CheckDepth();
            for (var i = 0; i < amount; i++)
            {
                PopInternal();
            }

            void PopInternal()
            {
                for (var i = 0; i < StackState.Length - 1; i++)
                {
                    StackState[i] = StackState[i + 1];
                }
                StackState[StackState.Length - 1] = TypedStackElement.Nothing;
            }
        }

        public IEnumerable<ArrayLetOp> GenerateInitializationEntries()
        {
            yield return new(StackTypeLabel, Enumerable.Repeat(TypedStackElement.Nothing.Type, MaxDepth).Select(e => e.ToString()));
            yield return new(StackSizeLabel, Enumerable.Repeat(TypedStackElement.Nothing.Size, MaxDepth).Select(e => e.ToString()));
        }

        public ImmutableArray<ArrayLetOp> GenerateStackSetters()
        {
            /* Example of how addition with storage of result should work (2 pushes, 2 pops, 1 push, 1 pop)
             * n = Nothing, ?x = Unknown with some positional identifier/determination.
             * 
             * [n, n] -> Push byte
             * [Byte, n] -> Generate -> [TYPE_System_Byte, TYPE_Nothing]
             * [?a, ?b] -> Push byte
             * [Byte, ?a] -> Generate -> [TYPE_System_Byte, STACK_TYPEOF[0]]
             * [?c, ?a] -> Pop
             * [?a, n] -> Pop
             * [n, n] -> Push Byte
             * [Byte, n] -> Generate -> [TYPE_System_Byte, TYPE_Nothing]
             * [?d, ?e] -> Pop byte
             * [?e, n] -> Generate -> [STACK_TYPEOF[1], TYPE_Nothing]
             */

            CheckDepth();
            var typeValues = StackState.Select(e => e.TypeString);
            var sizeValues = StackState.Select(e => e.SizeString);
            var result = new ArrayLetOp[]
            {
                new(StackTypeLabel, typeValues),
                new(StackSizeLabel, sizeValues)
            }.ToImmutableArray();

            for (var i = 0; i < StackState.Length; i++)
            {
                StackState[i] = new IndexedStackElement(i);
            }

            return result;
        }

        private void PercolateUp()
        {
            for (var i = 1; i < StackState.Length; i++)
            {
                StackState[i] = StackState[i - 1];
            }
        }

        private void CheckDepth()
        {
            if (MaxDepth == 0)
            {
                throw new InvalidOperationException("Stack operations on a 0-depth stack should not be happening, something went wrong.");
            }
        }
    }
}
