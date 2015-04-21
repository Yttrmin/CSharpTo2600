using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static CSharpTo2600.Framework.Assembly.AssemblyFactory;
using CSharpTo2600.Framework.Assembly;

namespace CSharpTo2600.Compiler
{
    //@TODO - Unit test all these. Probably by running the output of the assembler into an emulator
    // and examining the registers and memory after execution.
    internal static partial class Fragments
    {
        public static IEnumerable<AssemblyLine> Cast(Type From, Type To)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Pads or truncates a value on the stack of type From to match the
        /// size of type To.
        /// </summary>
        /// <param name="From">Type of the value on the stack to fit.</param>
        /// <param name="To">Type to pad/truncate the stack value to.</param>
        private static IEnumerable<AssemblyLine> Fit(Type From, Type To)
        {
            VerifyType(From);
            VerifyType(To);
            if (From == To)
            {
                throw new ArgumentException($"Attempted to fit to same type: {From}");
            }
            //@TODO - Should there be all those type checks we did in MethodCompiler in here?
            var FromSize = Marshal.SizeOf(From);
            var ToSize = Marshal.SizeOf(To);
            if (ToSize > FromSize)
            {
                return Pad(From, To);
            }
            else if (ToSize < FromSize)
            {
                return Truncate(From, To);
            }
            else
            {
                throw new ArgumentException($"Attempted to fit types of same size: {From} to {To}");
            }
        }

        private static IEnumerable<AssemblyLine> Truncate(Type From, Type To)
        {
            VerifyType(From);
            VerifyType(To);
            if (From == To)
            {
                throw new ArgumentException($"Attempted to truncate to the same type: {From}");
            }
            var FromSize = Marshal.SizeOf(From);
            var ToSize = Marshal.SizeOf(To);
            if (ToSize > FromSize)
            {
                throw new ArgumentException($"Attempted to truncate to a larger type: from {From} to {To}");
            }
            if (ToSize == FromSize)
            {
                throw new ArgumentException($"Attempt to truncate to a same-size type. Something wrong is probably happening. From {From} to {To}");
            }
            var ToDrop = FromSize - ToSize;
            //@TODO - This probably isn't right endian-wise.
            return StackDeallocate(ToDrop);
        }

        private static IEnumerable<AssemblyLine> Pad(Type From, Type To)
        {
            VerifyType(From);
            VerifyType(To);
            if (From == To)
            {
                throw new ArgumentException($"Attempted to truncate to the same type: {From}");
            }
            var FromSize = Marshal.SizeOf(From);
            var ToSize = Marshal.SizeOf(To);
            if (ToSize < FromSize)
            {
                throw new ArgumentException($"Attempted to pad to a smaller type: from {From} to {To}");
            }
            if (ToSize == FromSize)
            {
                throw new ArgumentException($"Attempt to pad to a same-size type. Something wrong is probably happening. From {From} to {To}");
            }
            var ToPad = ToSize - FromSize;
            return StackAllocate(ToPad, 0);
        }

        // Really make sure you're calling this on the most recently allocated local.
        public static IEnumerable<AssemblyLine> DeallocateLocal(Type Type)
        {
            VerifyType(Type);
            var Size = Marshal.SizeOf(Type);
            return StackDeallocate(Size);
        }

        public static IEnumerable<AssemblyLine> PushLiteral(object Value)
        {
            VerifyType(Value.GetType());
            var Bytes = EndianHelper.GetBytesForStack((dynamic)Value);
            foreach (var Byte in Bytes)
            {
                yield return LDA(Byte);
                yield return PHA();
            }
        }

        /// <summary>
        /// Pushes the value of a variable onto the stack.
        /// </summary>
        /// Postcondition: Value of Variable is pushed onto 6502 stack in big-endian.
        public static IEnumerable<AssemblyLine> PushVariable(IVariableInfo Variable)
        {
            if (Variable.IsDirectlyAddressable)
            {
                // This is not dependent on endianness. Just pushing the bytes at the highest
                // address first so that its laid out the same way on the stack as it is
                // in the variable.
                for (var i = Variable.Size - 1; i >= 0; i--)
                {
                    yield return LDA(Variable.AssemblySymbol, i);
                    yield return PHA();
                }
            }
            else if (Variable.IsStackRelative)
            {
                throw new NotImplementedException();
            }
            else
            {
                throw new FatalCompilationException($"Attempted push of non-addressable variable: {Variable.Name}");
            }
        }

        /// <summary>
        /// Takes a value off the stack and stores it in a variable.
        /// </summary>
        /// <param name="StackType">The type of the value on the stack.</param>
        /// Precondition: Big-endian value is on the 6502 stack.
        /// Postcondition: Value is removed from 6502 stack. Variable holds value.
        public static IEnumerable<AssemblyLine> StoreVariable(IVariableInfo Variable, Type StackType)
        {
            // We should never have to perform casts here.
            if (StackType != Variable.Type)
            {
                throw new FatalCompilationException($"Stack/Target Type mismtach. [{StackType}] on stack, but [{Variable.Type}] is target.");
            }

            if (Variable.IsDirectlyAddressable)
            {
                // This is not dependent on endianness. Just a straight copy.
                for (var i = 0; i < Variable.Size; i++)
                {
                    yield return PLA();
                    yield return STA(Variable.AssemblySymbol, i);
                }
            }
            else if (Variable.IsStackRelative)
            {
                throw new NotImplementedException();
            }
            else
            {
                throw new FatalCompilationException($"Attempted store to non-addressable variable: {Variable.Name}");
            }
        }

        public static IEnumerable<AssemblyLine> ClearSystem()
        {
            yield return SEI();
            yield return CLD();
            yield return LDX(0xFF);
            yield return TXS();
            yield return LDA(0);
            var ClearLabel = Label(".ClearMem");
            yield return ClearLabel;
            yield return STA((byte)0, Index.X);
            yield return DEX();
            yield return BNE(ClearLabel);
            // Clear address 00 to 0. 00 is VSYNC, which is set almost immediately
            // afterwards, and only uses a single bit, but might as well be thorough. 
            yield return STA((byte)0, Index.X);
        }

        // Postcondition: Stack pointer decremented by # of bytes requested.
        //                Warning: Could cause stack overflow (decrement past 0x00 or overwrite globals).
        // Postcondition: Value of new stack values are either what was passed in, or garbage otherwise.
        private static IEnumerable<AssemblyLine> StackAllocate(int Bytes, byte? InitializeTo = null)
        {
            //@TODO
            if (true/*Bytes <= 3*/ || InitializeTo.HasValue)
            {
                if (InitializeTo.HasValue)
                {
                    yield return LDA(InitializeTo.Value);
                }
                for (var i = 0; i < Bytes; i++)
                {
                    yield return PHA();
                }
            }
            else
            {
                // Manually subtract stack pointer. Less cycles than >3 PHAs
                throw new NotImplementedException();
            }
        }

        private static IEnumerable<AssemblyLine> StackDeallocate(int Bytes)
        {
            //@TODO
            if (true/*Bytes <= 2*/)
            {
                return Enumerable.Repeat(PLA(), Bytes);
            }
            else
            {
                // Manually add stack pointer. Less cycles than >2 PHA
                throw new NotImplementedException();
            }
        }

        public static void VerifyType(Type Type)
        {
            if (!Type.IsValueType)
            {
                throw new ArgumentException("Type must be a value type.");
            }
            if (Type == typeof(float) || Type == typeof(double))
            {
                throw new ArgumentException("Type can not be a floating-point type.");
            }
            if (Type == typeof(decimal))
            {
                throw new ArgumentException("Type can not be a decimal.");
            }
            if (Type == typeof(char))
            {
                throw new ArgumentException("Type can not be a char.");
            }
        }

        private static bool IsCastable(Type From, Type To)
        {
            //@TODO - Not complete.
            if (From == To || From.IsAssignableFrom(To))
            {
                return true;
            }
            if (From.IsPrimitive && To.IsPrimitive)
            {
                return true;
            }
            return false;
        }
    }
}
