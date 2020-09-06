using System.Runtime.InteropServices;
using static VCSFramework.Registers;

namespace Samples.CSharpFeatures
{
    // Not a proper VCS program.
    static class StructSample
    {
        private static MultiByteStruct _MultiByteStruct;
        private static CompositeStruct _CompositeStruct;
        private static SingleByteStruct _SingleByteStruct;

        public static void Main()
        {
            _CompositeStruct = new CompositeStruct
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
                _MultiByteStruct.ValueB = (byte)(_SingleByteStruct.Value + _CompositeStruct.StructA.ValueA);
                _CompositeStruct.StructA.ValueC = _MultiByteStruct.ValueB;
                ColuBk = _CompositeStruct.StructA.ValueC;
                _SingleByteStruct.Value = _MultiByteStruct.ValueB;
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
}
