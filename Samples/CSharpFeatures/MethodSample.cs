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
                ColuBk = ReturnIncrementByte();
            }
        }

        private static void VoidMethod()
        {
            byte local = BackgroundColor;
            byte increment = 1;
            byte finalValue = (byte)(local + increment);
            BackgroundColor = finalValue;
        }

        private static byte ReturnIncrementByte()
        {
            return BackgroundColor++;
        }
    }
}
