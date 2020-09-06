using System.Runtime.InteropServices;
using static VCSFramework.Registers;

namespace Samples.CSharpFeatures
{
    // Not a proper VCS program.
    static class StructSample
    {
        private static MultiByteStruct MultiByteStruct;
        private static SingleByteStruct SingleByteStruct;

        public static void Main()
        {
            MultiByteStruct.ValueB = 0;
            while (true)
            {
                SingleByteStruct.Value++;
                ColuBk = SingleByteStruct.Value;
            }
        }
    }

    // Doesn't matter if this is declared inside or outside of StructSample.
    [StructLayout(LayoutKind.Explicit)]
    struct SingleByteStruct
    {
        // Note static values already work out of the box.
        [FieldOffset(0)]
        public byte Value;
    }

    [StructLayout(LayoutKind.Explicit)]
    struct MultiByteStruct
    {
        [FieldOffset(0)]
        public byte ValueA;
        [FieldOffset(1)]
        public byte ValueB;
        [FieldOffset(2)]
        public byte ValueC;
    }
}
