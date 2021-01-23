using System.Runtime.InteropServices;
using static VCSFramework.Registers;

namespace Samples.CSharpFeatures
{
    static class RefSample
    {
        private static byte BackgroundColor;
        private static CompositeStruct _CompositeStruct;

        public static void Main()
        {
            ref var color = ref _CompositeStruct.StructA.ValueB;
            while (true)
            {
                color++;
                ColuBk = _CompositeStruct.StructA.ValueB;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        struct SingleByteStruct
        {
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
