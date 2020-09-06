using static VCSFramework.Registers;

namespace Samples.CSharpFeatures
{
    // Not a proper VCS program.
    static unsafe class PointerSample
    {
        private static byte* Value = (byte*)0x90;

        public static void Main()
        {
            while (true)
            {
                (*Value)++;
                ColuBk = *Value;
            }
        }
    }
}
