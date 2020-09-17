using VCSFramework.V2;
using static VCSFramework.Registers;

namespace Samples
{
    [TemplatedProgram(typeof(StandardTemplate))]
    public static class StandardTemplateSample
    {
        private static byte BackgroundColor;

        [StandardTemplate.Overscan]
        public static void ResetBackgroundColor() => BackgroundColor = 0;

        [StandardTemplate.Kernel(StandardTemplate.KernelType.EveryScanline)]
        public static void Kernel()
        {
            ColuBk = BackgroundColor;
            BackgroundColor++;
        }
    }
}
