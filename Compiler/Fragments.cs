using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CSharpTo2600.Framework;
using CSharpTo2600.Framework.Instructions;

namespace CSharpTo2600.Compiler
{
    internal static class Fragments
    {
        static Fragments()
        {
            if(!BitConverter.IsLittleEndian)
            {
                throw new FatalCompilationException("This architecture is big-endian and not supported.");
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
            if (Type == typeof(byte))
            {
                // Special case for single byte variables since this is hopefully most cases.
                yield return LDA((byte)Convert.ChangeType(Source, typeof(byte)));
                yield return STA("\{Destination}");
                yield break;
            }
            if (!Type.IsValueType)
            {
                throw new ArgumentException("Type must be a value type.", nameof(Type));
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
