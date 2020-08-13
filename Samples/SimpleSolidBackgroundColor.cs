using static VCSFramework.Registers;

namespace Samples
{
    // Not a proper VCS program.
    // Displays a constant white background (on emulator, not tested on TVs).
    static class SimpleSolidBackgroundColor
    {
        public static void Main()
        {
            ColuBk = 0x0E;
            while (true) ;
        }
    }
}
