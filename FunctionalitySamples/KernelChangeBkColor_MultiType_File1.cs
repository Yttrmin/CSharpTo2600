using CSharpTo2600.Framework;
using static CSharpTo2600.Framework.TIARegisters;

namespace CSharpTo2600.FunctionalitySamples
{
    // Make sure the compiler can handle partial types/methods.
    static partial class KernelChangeBkColor_MultiType_Logic
    {
        static partial void Tick();

        [SpecialMethod(MethodType.Kernel)]
        [Kernel(KernelTechnique.CallEveryScanline)]
        static void Kernel()
        {
            BackgroundColor = KernelChangeBkColor_MultiType_Data.Color;
            KernelChangeBkColor_MultiType_Data.Color++;
        }
    }

    static class KernelChangeBkColor_MultiType_Data
    {
        public static byte Color;
    }
}
