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
                ColuBk = GetColorContainingStruct().ValueB;
            }
        }

        private static void VoidMethod()
        {
            byte local = BackgroundColor;
            byte increment = 1;
            byte finalValue = (byte)(local + increment);
            BackgroundColor = finalValue;
        }

        private static ReturnStruct GetColorContainingStruct()
        {
            var newColor = (byte)(BackgroundColor + ReturnByte());
            BackgroundColor = newColor;
            return new ReturnStruct
            {
                ValueA = default,
                ValueB = newColor,
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
