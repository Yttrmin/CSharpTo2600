using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CSharpTo2600.Compiler
{
    public enum Endianness
    {
        None,
        // Least-significant byte is stored at the lowest memory location.
        // Variables point to LSB.
        Little,
        // Most-significant byte is stored at the lowest memory location.
        // Variables point to MSB.
        Big
    }

    internal static class EndianHelper
    {
        private static Endianness _Endianness;
        public static Endianness Endianness
        {
            // Never returns Endianness.None
            get
            {
                if (_Endianness == Endianness.None)
                {
                    throw new InvalidOperationException("Endianness was not initialized before being read.");
                }
                return _Endianness;
            }
            set
            {
                if (_Endianness != Endianness.None)
                {
                    throw new InvalidOperationException("Endianness can't be changed after its initial set");
                }
                if (value == Endianness.None)
                {
                    throw new ArgumentException("Can't set Endianness to None");
                }
                _Endianness = value;
            }
        }

        public static bool EndiannessIsSet
        {
            get { return _Endianness != Endianness.None; }
        }

        static EndianHelper()
        {
            if (!BitConverter.IsLittleEndian)
            {
                throw new InvalidOperationException("This architecture is big-endian and not supported.");
            }
        }

        public static IEnumerable<byte> GetBytesForStack<T>(T Value) where T : struct
        {
            if (Endianness == Endianness.Big)
            {
                // Big endian has MSB at lowest address.
                // Return LSBs so we end with the MSB on top of stack (lowest address).
                return LeastSignificantBytes(Value);
            }
            else // Endianness == Endianness.Little
            {
                // Little endian has LSB at lowest address.
                // Return MSBs so we end with the LSB on top of stack.
                return MostSignificantBytes(Value);
            }
        }

        /// <summary>
        /// Returns all the bytes of the Value, starting from the LSB to the MSB.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        internal static IEnumerable<byte> LeastSignificantBytes<T>(T Value) where T : struct
        {
            //@TODO - Handle structs with >1 field. Otherwise it'll flip the order of fields as well.
            var Bytes = StructToByteArray(Value, Marshal.SizeOf(typeof(T)));
            // Our architecture is assumed to be little-endian, so Bytes is already LE.
            return Bytes;
        }

        internal static IEnumerable<byte> MostSignificantBytes<T>(T Value) where T : struct
        {
            //@TODO - Handle structs with >1 field. Otherwise it'll flip the order of fields as well.
            var Bytes = StructToByteArray(Value, Marshal.SizeOf(typeof(T)));
            Array.Reverse(Bytes);
            return Bytes;
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
