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
                // @TODO ColuBk = BackgroundColor++; causes a dup instruction which messes with
                // optimizations. Is it something we can account for?
                BackgroundColor++;
                ColuBk = BackgroundColor;
            }
        }
    }
}
