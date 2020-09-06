using System.Runtime.InteropServices;
using static VCSFramework.Registers;

namespace Samples.CSharpFeatures
{
    // Not a proper VCS program.
    static class StructSample
    {
        private static MultiByteStruct MultiByteStruct;
        private static CompositeStruct CompositeStruct;
        private static SingleByteStruct SingleByteStruct;

        public static void Main()
        {
            CompositeStruct = new CompositeStruct
            {
                Value = 0x12,
                StructA = new MultiByteStruct
                {
                    ValueA = 0x1,
                    ValueB = 0x56,
                    ValueC = 0x78
                },
                StructB = new SingleByteStruct
                {
                    Value = 0x9A
                }
            };
            while (true)
            {
                MultiByteStruct.ValueB = (byte)(SingleByteStruct.Value + CompositeStruct.StructA.ValueA);
                CompositeStruct.StructA.ValueC = MultiByteStruct.ValueB;
                ColuBk = CompositeStruct.StructA.ValueC;
                SingleByteStruct.Value = MultiByteStruct.ValueB;
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

    [StructLayout(LayoutKind.Explicit)]
    struct CompositeStruct
    {
        [FieldOffset(0)]
        public byte Value;
        [FieldOffset(1)]
        public MultiByteStruct StructA;
        [FieldOffset(4)]
        public SingleByteStruct StructB;
    }
}
