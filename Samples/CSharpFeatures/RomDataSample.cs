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

        public static void Main()
        {
            while (true)
            {
                while (ByteDataIndex < UserByteRomData.Length)
                {
                    ColuBk = UserByteRomData[ByteDataIndex].Value;
                    ByteDataIndex++;
                }
                ByteDataIndex = 0;
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
            for (var i = 0; i <= byte.MaxValue; i += 2)
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
    }
}
