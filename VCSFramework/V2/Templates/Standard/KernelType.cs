using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCSFramework.V2.Templates.Standard
{
    public enum KernelType
    {
        Invalid,
        /// <summary>
        /// Method is invoked once for every visible scanline for this program's region.
        /// After the method returns, a WSync() call is performed before calling the method for the next scanline.
        /// </summary>
        EveryScanline,
        // @TODO - Is this even safe to support? Maybe if we could show the user their cycle count. Though in that case, couldn't
        // we just decide for the user whether to emit a WSync() or to eat up the rest of the time for them?
        /// <summary>
        /// Method is invoked once for every visible scanline for this program's region, without adding a WSync() call at the end.
        /// This should be used instead of <see cref="EveryScanline"/> if the user needs the extra time, and is capable of either counting cycles to ensure
        /// that they don't runover to the next scanline.
        /// Cycle counting is inherently fragile when using C# since changes to optimization may produce different timings with the same source code.
        /// </summary>
        EveryScanlineWithoutWSync,
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
