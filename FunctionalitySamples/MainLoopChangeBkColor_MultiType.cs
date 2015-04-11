using CSharpTo2600.Framework;

namespace CSharpTo2600.FunctionalitySamples
{
    [Atari2600Game]
    static class MainLoopChangeBkColor_MultiType
    {
        [SpecialMethod(MethodType.MainLoop)]
        static void Tick()
        {
            OtherStaticClass.Color++;
            // Not supported yet.
            //BackgroundColor = OtherStaticClass.Color;
        }
    }

    static class OtherStaticClass
    {
        public static byte Color;
    }
}
