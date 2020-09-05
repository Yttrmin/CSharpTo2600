using VCSFramework;
using static VCSFramework.Registers;

namespace Samples.CSharpFeatures
{
    static class BoolSample
    {
        private static bool ShouldLoop = true;

        static void Main()
        {
            //SetShouldLoopToTrue();
            Loop:
            while (ShouldLoop)
            {
                var alwaysTrue = InTim == 0;
                ShouldLoop |= alwaysTrue;
                ColuBk = 0x0E;
            }
            goto Loop;
        }
        
        static void SetShouldLoopToTrue()
        {
            ShouldLoop = true;
        }
    }
}
