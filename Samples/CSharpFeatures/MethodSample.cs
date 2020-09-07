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
                // @TODO - This is broken until we support ldfld against instances.
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
            var newColor = BackgroundColor++;
            return new ReturnStruct
            {
                ValueA = default,
                ValueB = newColor,
                ValueC = 0x0E
            };
        }

        private struct ReturnStruct
        {
            public byte ValueA;
            public byte ValueB;
            public byte ValueC;
        }
    }
}
