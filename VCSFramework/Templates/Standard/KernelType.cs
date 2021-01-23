namespace VCSFramework.Templates.Standard
{
    public enum KernelType
    {
        Invalid,
        /// <summary>
        /// Method is invoked once for every visible scanline for this program's region.
        /// After the method returns, a WSync() call is performed before calling the method for the next scanline.
        /// </summary>
        EveryScanline,
        /// <summary>
        /// Requires another method to have <see cref="EveryOddNumberScanline"/>. Method is invoked for every even-numbered scanline.
        /// </summary>
        EveryEvenNumberScanline,
        EveryOddNumberScanline,
        /// <summary>
        /// Method is invoked once after VBlank. 
        /// User is responsible for manually counting all scanlines and returning control after the appropriate number have been drawn.
        /// </summary>
        Manual,
    }
}
