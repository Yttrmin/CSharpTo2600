using CSharpTo2600.Framework;
using static CSharpTo2600.Framework.TIARegisters;

namespace CSharpTo2600.FunctionalitySamples
{
    [Atari2600Game]
    static class MainLoopChangeBkColor_MultiType_Logic
    {
        [SpecialMethod(MethodType.MainLoop)]
        static void Tick()
        {
            MainLoopChangeBkColor_MultiType_Data.Color++;
            BackgroundColor = MainLoopChangeBkColor_MultiType_Data.Color;
        }
    }
}
