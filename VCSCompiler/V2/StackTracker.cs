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
        public sealed record StackElement(IExpression Type, IExpression Size);

        private const string StackTypeLabel = "STACK_TYPEOF";
        private const string StackSizeLabel = "STACK_SIZEOF";
        private readonly StackElement[] StackState;
        private int MaxDepth => StackState.Length;

        public StackTracker(ImmutableArray<IAssemblyEntry> entries)
        {
            var maxDepth = 0;
            var depth = 0;
            foreach (var entry in entries.OfType<IMacroCall>())
            {
                if (entry.GetType().CustomAttributes.OfType<PushStackAttribute>().SingleOrDefault() is PushStackAttribute pushAttr)
                {
                    depth += pushAttr.Count;
                }
                if (entry.GetType().CustomAttributes.OfType<PopStackAttribute>().SingleOrDefault() is PopStackAttribute popAttr)
                {
                    depth -= popAttr.Count;
                }
                maxDepth = Math.Max(maxDepth, depth);
            }

            StackState = new StackElement[maxDepth];
            // The compiler should emit the [Nothing,...] initializer at the
            // start of the function, so we're safe to use indexes from the start.
            ReplaceStackWithIndexes();
        }

        public void Push(IExpression typeExpression, IExpression sizeExpression)
        {
            CheckDepth();
            PercolateUp();
            StackState[0] = new(typeExpression, sizeExpression);
        }

        public void Push(int stackIndex)
            => Push(new ArrayAccessOp(StackTypeLabel, stackIndex), new ArrayAccessOp(StackSizeLabel, stackIndex));

        public void Pop(int amount = 1)
        {
            if (amount <= 0)
                return;

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
                StackState[StackState.Length - 1] = new(LabelGenerator.NothingType, LabelGenerator.NothingSize);
            }
        }

        public IEnumerable<ArrayLetOp> GenerateInitializationEntries()
        {
            if (MaxDepth == 0)
                yield break;
            yield return new(StackTypeLabel, Enumerable.Repeat(LabelGenerator.NothingType, MaxDepth).Cast<IExpression>().ToImmutableArray());
            yield return new(StackSizeLabel, Enumerable.Repeat(LabelGenerator.NothingSize, MaxDepth).Cast<IExpression>().ToImmutableArray());
        }

        public bool TryGenerateStackOperation(out StackOperation stackOperation)
        {
            throw new NotImplementedException();
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
            var typeValues = StackState.Select(e => e.Type).ToImmutableArray();
            var sizeValues = StackState.Select(e => e.Size).ToImmutableArray();
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
                StackState[i] = new StackElement(new ArrayAccessOp(StackTypeLabel, i), new ArrayAccessOp(StackSizeLabel, i));
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
