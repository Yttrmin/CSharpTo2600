using VCSFramework;
using VCSFramework.Templates;
using static VCSFramework.Registers;

namespace Samples
{
    // @TODO - Should copy old NtscBackgroundColorsSample.
    [TemplatedProgram(typeof(RawTemplate))] // Attribute is optional, RawTemplate is implied if it's absent.
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
