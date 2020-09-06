﻿#nullable enable
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

        private sealed record TypedStackElement(BaseTypeLabel Type, BaseSizeLabel Size)
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

            public override string ToString() => $"Stack[{Index}]";
        }

        private static readonly LetLabel StackTypeLabel = new("STACK_TYPEOF");
        private static readonly LetLabel StackSizeLabel = new("STACK_SIZEOF");
        private readonly BaseStackElement[] StackState;
        private int MaxDepth => StackState.Length;

        public StackTracker(ImmutableArray<AssemblyEntry> entries)
        {
            var maxDepth = 0;
            var depth = 0;
            foreach (var entry in entries.OfType<Macro>())
            {
                if (entry.Effects.OfType<PushStackAttribute>().SingleOrDefault() is PushStackAttribute pushAttr)
                {
                    depth += pushAttr.Count;
                }
                if (entry.Effects.OfType<PopStackAttribute>().SingleOrDefault() is PopStackAttribute popAttr)
                {
                    depth -= popAttr.Count;
                }
                maxDepth = Math.Max(maxDepth, depth);
            }

            StackState = new BaseStackElement[maxDepth];
            // The compiler should emit the [Nothing,...] initializer at the
            // start of the function, so we're safe to use indexes from the start.
            ReplaceStackWithIndexes();
        }

        public void Push(TypeLabel type, SizeLabel size)
        {
            CheckDepth();
            PercolateUp();

            StackState[0] = new TypedStackElement(type, size);
        }

        public void Push(TypeLabel type, BaseSizeLabel size)
        {
            CheckDepth();
            PercolateUp();

            StackState[0] = new TypedStackElement(type, size);
        }

        public void Push(PointerTypeLabel type, PointerSizeLabel size)
        {
            CheckDepth();
            PercolateUp();

            StackState[0] = new TypedStackElement(type, size);
        }

        public void Push(PointerTypeLabel type, StackSizeArrayLabel size)
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

        public void Push(StackTypeArrayLabel type, StackSizeArrayLabel size)
        {
            CheckDepth();
            PercolateUp();

            if (type.Index != size.Index)
                throw new ArgumentException($"Expected same index for type/size. Instead got type[{type.Index}] and size[{size.Index}]");
            StackState[0] = new IndexedStackElement(type.Index);
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
            if (MaxDepth == 0)
                yield break;
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

            ReplaceStackWithIndexes();

            return result;
        }

        private void ReplaceStackWithIndexes()
        {
            for (var i = 0; i < StackState.Length; i++)
            {
                StackState[i] = new IndexedStackElement(i);
            }
        }

        private void PercolateUp()
        {
            // [0, 1, 2, 3, 4]
            // ->
            // [0, 0, 1, 2, 3]
            for (var i = StackState.Length - 1; i > 0; i--)
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
