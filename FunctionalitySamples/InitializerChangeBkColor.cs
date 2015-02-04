using CSharpTo2600.Framework;
using static CSharpTo2600.Framework.TIARegisters;

namespace CSharpTo2600.FunctionalitySamples
{
    [Atari2600Game]
    static class SingleChangeBkColor
    {
        [SpecialMethod(MethodType.Initialize)]
        static void Initialize()
        {
            BackgroundColor = 0x5E;
        }
    }
}
