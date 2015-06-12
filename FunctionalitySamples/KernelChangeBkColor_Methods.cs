using CSharpTo2600.Framework;
using static CSharpTo2600.Framework.TIARegisters;

namespace CSharpTo2600.FunctionalitySamples
{
    [Atari2600Game]
    static class KernelChangeBkColor_Methods
    {
        private static byte Color;

        [SpecialMethod(MethodType.MainLoop)]
        static void MainLoop()
        {
            // Reset the color to 0 each frame so we don't get flicker.
            ResetColor();
        }

        [SpecialMethod(MethodType.Kernel)]
        [Kernel(KernelTechnique.CallEveryScanline)]
        static void Kernel()
        {
            BackgroundColor = NextBackgroundColor();
        }

        static void ResetColor()
        {
            Color = 0;
            return;
        }

        static byte NextBackgroundColor()
        {
            IncrementColor();
            return Color;
        }

        static void IncrementColor()
        {
            Color++;
            return;
        }
    }
}
