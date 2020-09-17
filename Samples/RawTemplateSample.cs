using VCSFramework.V2;
using VCSFramework.V2.Templates;
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
