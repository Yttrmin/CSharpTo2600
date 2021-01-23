using static VCSFramework.Registers;

namespace Samples.CSharpFeatures
{
    static class MethodSample
    {
        private static byte BackgroundColor;

        public static void Main()
        {
            while (true)
            {
                ref var color = ref RefReturningMethod();
                color += ReturnByte();
                ColuBk = GetColorContainingStruct().ValueB;
            }
        }

        private static void VoidMethod(byte parameter)
        {
            BackgroundColor = parameter;
        }

        private static ref byte RefReturningMethod()
        {
            return ref BackgroundColor;
        }

        private static ReturnStruct GetColorContainingStruct()
        {
            return new ReturnStruct
            {
                ValueA = default,
                ValueB = BackgroundColor,
                ValueC = 0x0E
            };
        }

        private static byte ReturnByte()
        {
            return 0x1;
        }

        private struct ReturnStruct
        {
            public byte ValueA;
            public byte ValueB;
            public byte ValueC;
        }
    }
}
