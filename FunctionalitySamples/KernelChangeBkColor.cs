using CSharpTo2600.Framework;
using static CSharpTo2600.Framework.TIARegisters;

namespace CSharpTo2600.FunctionalitySamples
{
    [Atari2600Game]
    static class KernelChangeBkColor
    {
        private static byte Color;

        [SpecialMethod(MethodType.MainLoop)]
        static void MainLoop()
        {
            // Reset the color to 0 each frame so we don't get flicker.
            Color = 0;
        }

        [SpecialMethod(MethodType.Kernel)]
        [Kernel(KernelTechnique.CallEveryScanline)]
        static void Kernel()
        {
            BackgroundColor = Color;
            Color++;
        }
    }
}
