using CSharpTo2600.Framework;
using static CSharpTo2600.Framework.TIARegisters;

namespace CSharpTo2600.FunctionalitySamples
{
    /// <summary>
    /// Compiles to an NTSC Atari 2600 ROM that cycles through all possible background colors.
    /// </summary>
    [Atari2600Game]
    static class MainLoopChangeBkColor
    {
        private static byte Color;

        // Tells the compiler that this code is to be executed every frame
        // during the vertical blank period.
        [SpecialMethod(MethodType.MainLoop)]
        static void Tick()
        {
            // COLUBK doesn't actually use the 0 bit (LSB), so the background
            // color changes every other frame.
            Color++;
            // BackgroundColor maps directly to the COLUBK TIA register.
            BackgroundColor = Color;
        }
    }
}
