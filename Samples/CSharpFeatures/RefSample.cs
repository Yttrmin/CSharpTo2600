using static VCSFramework.Registers;

namespace Samples.CSharpFeatures
{
    static class RefSample
    {
        private static byte BackgroundColor;

        public static void Main()
        {
            ref var color = ref BackgroundColor;
            while(true)
            {
                color++;
                ColuBk = BackgroundColor;
            }
        }
    }
}
