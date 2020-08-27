﻿using System.Collections.Generic;
using System.Collections.Immutable;

namespace VCSFramework.V2
{
    public interface IStackTracker
    {
        IEnumerable<ArrayLetOp> GenerateInitializationEntries();
        ImmutableArray<ArrayLetOp> GenerateStackSetters();
        void Pop(int amount = 1);
        void Push(Function typeFunction, Function sizeFunction);
        void Push(TypeLabel type, SizeLabel size);
    }
}