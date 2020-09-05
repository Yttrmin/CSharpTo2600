using VCSFramework;
using static VCSFramework.Registers;

namespace Samples.CSharpFeatures
{
    static class BoolAndMethodSample
    {
        private static bool ShouldLoopA;
        private static bool ShouldLoopB;
        private static bool ShouldLoopC = true;

        static void Main()
        {
            SetShouldLoopAToTrue();
            SetShouldLoopBToTrue();
        Loop:
            while (ShouldLoopA && ShouldLoopB && ShouldLoopC)
            {
                var alwaysTrue = InTim == 0;
                ShouldLoopC |= alwaysTrue;
                ColuBk = 0x0E;
            }
            goto Loop;
        }
        
        static void SetShouldLoopAToTrue()
        {
            ShouldLoopA = true;
        }

        [AlwaysInline]
        static void SetShouldLoopBToTrue()
        {
            ShouldLoopB = true;
        }
    }
}
