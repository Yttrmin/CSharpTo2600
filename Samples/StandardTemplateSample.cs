using VCSFramework;
using VCSFramework.Templates.Standard;
using static VCSFramework.Registers;

namespace Samples
{
    [TemplatedProgram(typeof(StandardTemplate))]
    public static class StandardTemplateSample
    {
        private static byte BackgroundColor;

        [VBlank]
        public static void ResetBackgroundColor() => BackgroundColor = 0;

        [Kernel(KernelType.EveryScanline)]
        [KernelScanlineRange(192, 96)]
        public static void KernelAscend()
        {
            ColuBk = BackgroundColor;
            BackgroundColor++;
        }

        [Kernel(KernelType.EveryScanline)]
        [KernelScanlineRange(96, 0)]
        public static void KernelDescend()
        {
            ColuBk = BackgroundColor;
            BackgroundColor--;
        }
    }
}
