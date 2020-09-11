using VCSFramework.V2;
using static VCSFramework.Registers;

namespace Samples
{
    [TemplatedProgram(typeof(RawTemplate))]
    public static class RawTemplateSample
    {
        public static void Main()
        {
            while (true)
            {
                ColuBk = 0x0E;
            }
        }
    }
}
