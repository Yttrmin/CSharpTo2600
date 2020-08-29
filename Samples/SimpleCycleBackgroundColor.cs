using static VCSFramework.Registers;

namespace Samples
{
    // Not a proper VCS program.
    static class SimpleCycleBackgroundColor
    {
        private static byte BackgroundColor;

        public static void Main()
        {
            while (true)
            {
                // At the time of writing, these are the possible ways of getting the same
                // effect, in order of descending efficiency:
                // BackgroundColor++; ColuBk = BackgroundColor;
                // ColuBk = ++BackgroundColor; OR ColuBk = BackgroundColor += 1;
                // ColuBk = BackgroundColor++;
                ColuBk = BackgroundColor += 1;
            }
        }
    }
}
