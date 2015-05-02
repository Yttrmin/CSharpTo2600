using CSharpTo2600.Framework;

namespace CSharpTo2600.FunctionalitySamples
{
    [Atari2600Game]
    static partial class KernelChangeBkColor_MultiType_Logic
    {
        [SpecialMethod(MethodType.MainLoop)]
        static partial void Tick()
        {
            KernelChangeBkColor_MultiType_Data.Color = 0;
        }
    }
}
