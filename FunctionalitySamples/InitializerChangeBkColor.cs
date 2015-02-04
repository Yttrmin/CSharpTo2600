using CSharpTo2600.Framework;
using static CSharpTo2600.Framework.TIARegisters;

namespace CSharpTo2600.FunctionalitySamples
{
    [Atari2600Game]
    static class SingleChangeBkColor
    {
        [SpecialMethod(MethodType.Initialize)]
        [System.Obsolete(CSharpTo2600.Framework.Assembly.Symbols.AUDC0, true)]
        static void Initialize()
        {
            BackgroundColor = 0x5E;
        }
    }
}
