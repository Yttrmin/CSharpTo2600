using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CSharpTo2600.Framework;
using CSharpTo2600.Framework.Instructions;

namespace CSharpTo2600.Compiler
{
    //@TODO - Unit test all these. Probably by running the output of the assembler into an emulator
    // and examining the registers and memory after execution.
    internal static partial class Fragments
    {
        static Fragments()
        {
            if(!BitConverter.IsLittleEndian)
            {
                throw new FatalCompilationException("This architecture is big-endian and not supported.");
            }
        }

        public static IEnumerable<InstructionInfo> Fit(Type From, Type To)
        {
            VerifyType(From);
            VerifyType(To);
            if(From == To)
            {
                throw new ArgumentException("Attempted to fit to same type: \{From}");
            }
            //@TODO - Should there be all those type checks we did in MethodCompiler in here?
            var FromSize = Marshal.SizeOf(From);
            var ToSize = Marshal.SizeOf(To);
            if(ToSize > FromSize)
            {
                return Pad(From, To);
            }
            else if(ToSize < FromSize)
            {
                return Truncate(From, To);
            }
            else
            {
                throw new ArgumentException("Attempted to fit types of same size: \{From} to \{To}");
            }
        }

        public static IEnumerable<InstructionInfo> Truncate(Type From, Type To)
        {
            VerifyType(From);
            VerifyType(To);
            if(From == To)
            {
                throw new ArgumentException("Attempted to truncate to the same type: \{From}");
            }
            var FromSize = Marshal.SizeOf(From);
            var ToSize = Marshal.SizeOf(To);
            if (ToSize > FromSize)
            {
                throw new ArgumentException("Attempted to truncate to a larger type: from \{From} to \{To}");
            }
            if(ToSize == FromSize)
            {
                throw new ArgumentException("Attempt to truncate to a same-size type. Something wrong is probably happening. From \{From} to \{To}");
            }
            var ToDrop = FromSize - ToSize;
            //@TODO - This probably isn't right endian-wise.
            return StackDeallocate(ToDrop);
        }

        public static IEnumerable<InstructionInfo> Pad(Type From, Type To)
        {
            VerifyType(From);
            VerifyType(To);
            if (From == To)
            {
                throw new ArgumentException("Attempted to truncate to the same type: \{From}");
            }
            var FromSize = Marshal.SizeOf(From);
            var ToSize = Marshal.SizeOf(To);
            if (ToSize < FromSize)
            {
                throw new ArgumentException("Attempted to pad to a smaller type: from \{From} to \{To}");
            }
            if (ToSize == FromSize)
            {
                throw new ArgumentException("Attempt to pad to a same-size type. Something wrong is probably happening. From \{From} to \{To}");
            }
            var ToPad = ToSize - FromSize;
            //@TODO - Check endian
            return StackAllocate(ToPad, 0);
        }

        public static IEnumerable<InstructionInfo> AllocateLocal(Type Type, out int Size)
        {
            VerifyType(Type);
            Size = Marshal.SizeOf(Type);
            return StackAllocate(Size);
        }

        // Really make sure you're calling this on the most recently allocated local.
        public static IEnumerable<InstructionInfo> DeallocateLocal(Type Type)
        {
            VerifyType(Type);
            var Size = Marshal.SizeOf(Type);
            return StackDeallocate(Size);
        }

        public static IEnumerable<InstructionInfo> PushLiteral(object Value)
        {
            VerifyType(Value.GetType());
            var Size = Marshal.SizeOf(Value.GetType());
            var Bytes = StructToByteArray(Value, Size);
            // Compiler-generated stack operations are stored big-endian for ease. Padding
            // is done by pushing more most-significant bytes. Truncating is done by
            // popping the most-significant bytes. This is easy when the most-significant
            // byte is at the top of the stack.
            for(var i = 0; i < Bytes.Length; i++)
            {
                yield return LDA(Bytes[i]);
                yield return PHA();
            }
        }

        [Obsolete("Use VariableInfo")]
        public static IEnumerable<InstructionInfo> PushVariable(string Name, Type Type)
        {
            VerifyType(Type);
            var Size = Marshal.SizeOf(Type);
            for (var i = 0; i < Size; i++)
            {
                yield return LDA(Name, i);
                yield return PHA();
            }
        }

        // Precondition: Data stored on stack in big-endian.
        [Obsolete("Use VariableInfo", true)]
        public static IEnumerable<InstructionInfo> StoreVariable(string Name, Type Type)
        {
            VerifyType(Type);
            var Size = Marshal.SizeOf(Type);
            // Data is big-endian, but we want to store it little-endian, so iterate like this.
            for (var i = Size - 1; i >= 0; i--)
            {
                yield return PLA();
                yield return STA(Name, i);
            }
        }

        /// <summary>
        /// Takes a value off the stack and stores it in a variable.
        /// </summary>
        /// <param name="StackType">The type of the value on the stack.</param>
        /// Precondition: Big-endian value is on the 6502 stack.
        /// Postcondition: Value is removed from 6502 stack. Variable holds value.
        public static IEnumerable<InstructionInfo> StoreVariable(VariableInfo Variable, Type StackType)
        {
            VerifyType(StackType);
            var Result = Enumerable.Empty<InstructionInfo>();
            if (!IsCastable(StackType, Variable.Type))
            {
                throw new FatalCompilationException("Types don't match for assignment: \{StackType} to \{Variable.Type}");
            }
            else if (StackType != Variable.Type)
            {
                Result = Result.Concat(Fit(StackType, Variable.Type));
            }

            if (Variable.AddressIsAbsolute)
            {
                for (var i = Variable.Size - 1; i >= 0; i--)
                {
                    yield return PLA();
                    yield return STA(Variable.Name, i);
                }
            }
            else if(Variable.AddressIsFrameRelative)
            {
                throw new NotImplementedException();
            }
        }

        private static IEnumerable<InstructionInfo> StackAllocate(int Bytes, byte? InitializeTo=null)
        {
            //@TODO
            if (true/*Bytes <= 3*/ || InitializeTo.HasValue)
            {
                if(InitializeTo.HasValue)
                {
                    yield return LDA(InitializeTo.Value);
                }
                for(var i = 0; i < Bytes; i++)
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

        private static IEnumerable<InstructionInfo> StackDeallocate(int Bytes)
        {
            //@TODO
            if(true/*Bytes <= 2*/)
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
            if(Type == typeof(float) || Type == typeof(double))
            {
                throw new ArgumentException("Type can not be a floating-point type.");
            }
            if(Type == typeof(decimal))
            {
                throw new ArgumentException("Type can not be a decimal.");
            }
            if(Type == typeof(char))
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

        /// <returns>The struct's bytes in little-endian. That is, a[0] is the
        /// least-significant byte, a[length-1] is the most-significant byte.</returns>
        private static byte[] StructToByteArray(object Struct, int Size)
        {
            var Ptr = Marshal.AllocHGlobal(Size);
            var Array = new byte[Size];
            Marshal.StructureToPtr(Struct, Ptr, false);
            Marshal.Copy(Ptr, Array, 0, Array.Length);
            Marshal.FreeHGlobal(Ptr);
            return Array;
        }
    }
}
