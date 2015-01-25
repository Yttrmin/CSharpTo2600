﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CSharpTo2600.Framework;
using static CSharpTo2600.Framework.Instructions;
using Index = CSharpTo2600.Framework.Index;

namespace CSharpTo2600.Compiler
{
    partial class Fragments
    {
		public static IEnumerable<InstructionInfo> Add(Type Type)
        {
            VerifyType(Type);
            var Size = Marshal.SizeOf(Type);
            yield return PLA();
            yield return TSX();
            yield return CLC();
            yield return ADC(0x100, Index.X);
			// We can't just throw away the top of the stack, so reuse it to store the result.
			// Otherwise we'll leak stack space.
            yield return STA(0x100, Index.X);
        }

		public static IEnumerable<InstructionInfo> Subtract(Type Type)
        {
            VerifyType(Type);
            var Size = Marshal.SizeOf(Type);
            yield return PLA();
            yield return TSX();
            yield return SEC();
            yield return SBC(0x100, Index.X);
            yield return STA(0x100, Index.X);
        }

        public static IEnumerable<InstructionInfo> BitwiseOr(Type Type)
        {
            VerifyType(Type);
            var Size = Marshal.SizeOf(Type);
            yield return PLA();
            yield return TSX();
            throw new NotImplementedException();
        }
    }
}