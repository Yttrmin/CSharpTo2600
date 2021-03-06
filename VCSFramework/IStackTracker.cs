﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace VCSFramework
{
    public interface IStackTracker
    {
        IEnumerable<ArrayLetOp> GenerateInitializationEntries();
        bool TryGenerateStackOperation([NotNullWhen(true)] out StackOperation? stackOperation);
        void Pop(int amount);
        void Push(IExpression typeExpression, IExpression sizeExpression);
        void Push(int stackIndex);
        /*void Push(IFunctionCall typeFunction, IFunctionCall sizeFunction);
        // @TODO - Delete one of these??
        void Push(TypeLabel type, TypeSizeLabel size);
        void Push(TypeLabel type, ISizeLabel size);
        void Push(PointerTypeLabel type, PointerSizeLabel size);
        void Push(PointerTypeLabel type, StackSizeArrayLabel size);
        void Push(StackTypeArrayLabel type, StackSizeArrayLabel size);*/
    }

    /// <summary>Type used to indicate a stack element is empty.</summary>
    public struct Nothing { }
}