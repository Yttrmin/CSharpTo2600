﻿using VCSFramework.V2;
using VCSFramework.V2.Templates.Standard;
using static VCSFramework.Registers;

namespace Samples
{
    [TemplatedProgram(typeof(StandardTemplate))]
    public static class StandardTemplateSample
    {
        private static byte BackgroundColor;

        [VBlank]
        public static void IncrementBackgroundColor() => ColuBk = BackgroundColor++;

        /*[Overscan]
        public static void ResetBackgroundColor() => BackgroundColor = 0;

        [Kernel(KernelType.EveryScanline)]
        public static void Kernel()
        {
            ColuBk = BackgroundColor;
            BackgroundColor++;
        }*/
    }
}
