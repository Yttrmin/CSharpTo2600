using CSharpTo2600.Framework;
using static CSharpTo2600.Framework.TIARegisters;

namespace CSharpTo2600.FunctionalitySamples
{
    [Atari2600Game]
    static class MainLoopChangeBkColor_MultiType
    {
        [SpecialMethod(MethodType.MainLoop)]
        static void Tick()
        {
            //OtherStaticClass.Color++;
            BackgroundColor = OtherStaticClass.Color;
        }
    }

    static class OtherStaticClass
    {
        public static byte Color;
    }
}
