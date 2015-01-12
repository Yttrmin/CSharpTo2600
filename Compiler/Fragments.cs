using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CSharpTo2600.Framework;
using CSharpTo2600.Framework.Instructions;

namespace CSharpTo2600.Compiler
{
    internal static partial class Fragments
    {
        static Fragments()
        {
            if(!BitConverter.IsLittleEndian)
            {
                throw new FatalCompilationException("This architecture is big-endian and not supported.");
            }
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
            // I know this is basically LoadIntoVariable. Bear with me.
            for(var i = 0; i < Bytes.Length; i++)
            {
                yield return LDA(Bytes[i]);
                yield return PHA();
            }
        }

        private static IEnumerable<InstructionInfo> StackAllocate(int Bytes)
        {
            //@TODO
            if (true/*Bytes <= 3*/)
            {
                return Enumerable.Repeat(PHA(), Bytes);
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

        /// <summary>
        /// Loads a C# object into a 6502 variable.
        /// </summary>
        /// <param name="Destination">Symbol of destination variable.</param>
        /// <param name="Source">Object to store into Destination.</param>
        /// <param name="Type">Type of the destination variable.</param>
        /// <returns></returns>
        public static IEnumerable<InstructionInfo> LoadIntoVariable(string Destination, object Source, Type Type)
        {
            VerifyType(Type);
            if (Type == typeof(byte))
            {
                // Special case for single byte variables since this is hopefully most cases.
                yield return LDA((byte)Convert.ChangeType(Source, typeof(byte)));
                yield return STA("\{Destination}");
                yield break;
            }
            var Size = Marshal.SizeOf(Type);
            // Types may not be the same such as the case of Roslyn giving us an int literal
            // being assigned to a long variable. Convert it to the proper type or else
            // marshaling will break.
            var ConvertedStruct = Convert.ChangeType(Source, Type);
            var StructBytes = StructToByteArray(ConvertedStruct, Size);
            byte AValue = 0;
            for(var i = 0; i < Size; i++)
            {
                // The 6502 is little-endian, so follow convention.
                // Thankfully, x64 is also little-endian, so the marshaling will already
                // be in the rihg torder.
                var NextByte = StructBytes[i];
                // Don't bother with an LDA if A already has the value.
                if(NextByte != AValue || i == 0)
                {
                    yield return LDA(StructBytes[i]);
                }
                AValue = NextByte;
                yield return STA("\{Destination}+\{i}");
            }
        }

        private static void VerifyType(Type Type)
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
            //@REMOVEME
            if(Type != typeof(byte))
            {
                throw new ArgumentException("Only bytes supported.");
            }
        }

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
