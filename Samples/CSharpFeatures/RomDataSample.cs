using System.Collections.Generic;
using VCSFramework.V2;
using static VCSFramework.Registers;

namespace Samples.CSharpFeatures
{
    public static class RomDataSample
    {
        [RomDataGenerator(nameof(GenerateByteData))]
        private static readonly RomData<byte> ByteRomData = default;
        private static readonly RomData<UserStruct> CustomRomData;

        private static byte ByteDataIndex = 0;

        public static void Main()
        {
            while (true)
            {
                while (ByteDataIndex < ByteRomData.Length)
                {
                    ColuBk = ByteRomData[ByteDataIndex];
                    ByteDataIndex++;
                }
                ByteDataIndex = 0;
            }
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
    }
}
