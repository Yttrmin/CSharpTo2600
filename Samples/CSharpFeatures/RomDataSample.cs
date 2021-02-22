using System.Collections.Generic;
using VCSFramework;
using static VCSFramework.Registers;

namespace Samples.CSharpFeatures
{
    public static class RomDataSample
    {
        [RomDataGenerator(nameof(GenerateByteData))]
        private static readonly RomData<byte> ByteRomData = default;
        [RomDataGenerator(nameof(GenerateUserByteData))]
        private static readonly RomData<UserByte> UserByteRomData;
        [RomDataGenerator(nameof(GenerateUserStructData))]
        private static readonly RomData<UserStruct> UserStructRomData;

        private static byte ByteDataIndex = 0;

        public unsafe static void Main()
        {
            while (true)
            {
                foreach (var value in ByteRomData)
                {
                    ColuBk = value;
                }
            }
        }

        // For a type used as a RomData<> arg to be accessible to the compiler, it MUST be public.
        // This is because the compiler uses instances with `dynamic` variables, which respects accessibility.
        public struct UserByte
        {
            public byte Value;
        }

        private struct UserStruct
        {
            public byte A;
            public byte B;
            public byte C;
        }

        private static IEnumerable<byte> GenerateByteData()
        {
            for (var i = 0; i <= 4; i += 2)
            {
                yield return (byte)i;
            }
        }

        private static IEnumerable<UserByte> GenerateUserByteData()
        {
            for (var i = 0; i <= byte.MaxValue; i += 2)
            {
                yield return new UserByte { Value = (byte)i };
            }
        }

        private static IEnumerable<UserStruct> GenerateUserStructData()
        {
            for (var i = 0; i <= byte.MaxValue; i += 2)
            {
                yield return new UserStruct
                {
                    B = (byte)i
                };
            }
        }

        public static RomData_GenerateByteData_Small_Iterator GetEnumerator(this RomData<byte> @this) => RomData_GenerateByteData_Small_Iterator.New();

        // If we're iterating over a chunk of ROM data <= 256 bytes in size, we can simply store the byte offset from the base of the data.
        // This would use Absolute,X addressing, which incurs a 1 cycle penalty when crossing a page boundary. If ROM space is available, it would
        // be best to make sure the data is all in the same page.
        public struct RomData_GenerateByteData_Small_Iterator
        {
            [RomDataGenerator(nameof(GenerateByteData))]
            private static RomData<byte> Data;
            private byte ByteIndex;

            public bool MoveNext()
            {
                ByteIndex += Data.Stride;
                return ByteIndex < Data.Length;
            }

            public unsafe ref readonly byte Current
            {
                [return: LongPointer]
                get => ref AssemblyUtilities.PointerToRef<byte>((byte*)Data.Pointer + ByteIndex);
            }

            public static RomData_GenerateByteData_Small_Iterator New()
            {
                return new RomData_GenerateByteData_Small_Iterator
                {
                    ByteIndex = (byte)-Data.Stride
                };
            }
        }

        // If we're iterating over a chunk of ROM data >256 bytes in size, we need to store the full pointer.
        // This would use Indirect,Y addressing (always with a 0 offset), with incurs a 1 cycle penalty when crossing a page boundary.
        // If ROM space is available, it would be best to make sure the data is aligned to the start of a page.
        public unsafe struct RomData_GenerateByteData_Large_Iterator
        {
            [RomDataGenerator(nameof(GenerateByteData))]
            private RomData<byte> Data;
            private byte* DataPtr;

            public bool MoveNext()
            {
                DataPtr++;
                return DataPtr < ((byte*)Data.Pointer + (Data.Stride * Data.Length));
            }

            public ref byte Current => ref *DataPtr;
        }
    }
}
