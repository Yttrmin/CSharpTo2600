using static VCSFramework.Registers;

namespace Samples.CSharpFeatures
{
    // Not a proper VCS program.
    static unsafe class PointerSample
    {
        private static byte BackgroundColor;
        private static byte* Value = (byte*)0x90;

        public static void Main()
        {
            while (true)
            {
                *Value = 1;
                fixed (byte* ptr = &BackgroundColor)
                {
                    *ptr += *Value;
                    ColuBk = *ptr;
                }
            }
        }
    }
}
