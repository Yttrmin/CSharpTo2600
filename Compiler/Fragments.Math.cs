﻿using System;
using System.Collections.Generic;
using CSharpTo2600.Framework.Assembly;
using static CSharpTo2600.Framework.Assembly.AssemblyFactory;

namespace CSharpTo2600.Compiler
{
    //@TODO - We could just make our own implementations of op_Addition, etc instead
    // of always calling these Add etc methods.
    // It would help support users overloading operators in the future, and stays
    // more loyal to the language since right now every binary + goes here regardless
    // of type.
    // On the downside, the smallest defined op_Addition is for ints, so all our bytes
    // would have to be padded to ints, then truncated back to bytes. Maybe something
    // that can be optimized? Or add some special cases?
    partial class Fragments
    {
        public static IEnumerable<AssemblyLine> Add(ProcessedType Type)
        {
            if (Type.InstanceSize > 1)
            {
                throw new NotImplementedException(">8-bit math not supported yet.");
            }
            yield return PLA();
            yield return TSX();
            yield return CLC();
            yield return ADC(0, Index.X);
            // We can't just throw away the top of the stack, so reuse it to store the result.
            // Otherwise we'll leak stack space.
            yield return STA(0, Index.X);
        }

        public static IEnumerable<AssemblyLine> Add(IVariableInfo Variable, byte Constant)
        {
            //@TODO - Use INC when Constant=1
            if (Variable.Size > 1)
            {
                throw new NotImplementedException(">8-bit math not supported yet.");
            }
            yield return PLA();
            yield return CLC();
            yield return ADC(1);
            yield return PHA();
        }

        public static IEnumerable<AssemblyLine> Subtract(ProcessedType Type)
        {
            if (Type.InstanceSize > 1)
            {
                throw new NotImplementedException(">8-bit math not supported yet.");
            }
            yield return PLA();
            yield return TSX();
            yield return SEC();
            yield return SBC(0, Index.X);
            yield return STA(0, Index.X);
        }
    }
}
