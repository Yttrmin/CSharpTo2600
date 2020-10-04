using System;

namespace VCSFramework.V2.Templates.Standard
{
    // Purposefully nesting these despite being public to make it obvious if you're using the wrong attributes with [ProgramTemplate].
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class VBlankAttribute : Attribute { }

    /// <summary>
    /// Marks a method to be executed to handle a visible scanline.
    /// If there is only a single kernel method, it implicitly runs for every scanline.
    /// If there is more than one kernel method, each must be marked with a <see cref="KernelScanlineRangeAttribute"/>.
    /// Different kernel types can be mixed.
    /// Kernel methods can't have overlapping ranges, except for <see cref="KernelType.EveryEvenNumberScanline"/> and <see cref="KernelType.EveryOddNumberScanline"/> where overlapping pairs are required.
    /// 
    /// Method must be void returning.
    /// If the method uses a kernel type besides <see cref="KernelType.Manual"/>, it can accept a single byte parameter representing the current scanline index.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class KernelAttribute : Attribute
    {
        public KernelType KernelType { get; }
        public bool UnrollLoop { get; }

        public KernelAttribute(KernelType kernelType, bool unrollLoop = false)
        {
            KernelType = kernelType;
            UnrollLoop = unrollLoop;
        }
    }

    /// <summary>
    /// Specifies the inclusive start and exclusive end of a range of scanlines that this method handles.
    /// When all range attributes are taken together, they must be exhaustive of the entire range of scanlines.
    /// The scanline index counts down to 0 over the frame. Keep this in mind when laying out data you intend to index into.
    /// The first scanline is 192 for NTSC and 228 for PAL/SECAM. The last scanline is 0.
    /// The parameters should be treated as if they used in a loop like: <code>for (var i = Start; i > End; i--) { }</code>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class KernelScanlineRangeAttribute : Attribute
    {
        public Range Ntsc { get; }
        public Range PalSecam { get; }

        /// <summary>
        /// Specifies a range of NTSC scanlines that this method handles.
        /// Note scanlines count down, so <paramref name="NtscStart"/> should be greater than <paramref name="NtscEnd"/>.
        /// </summary>
        /// <param name="NtscStart">Inclusive. Maximum value is 192.</param>
        /// <param name="NtscEnd">Exclusive. Must be less than <paramref name="NtscStart"/>.</param>
        public KernelScanlineRangeAttribute(int NtscStart, int NtscEnd) : this(NtscStart, NtscEnd, -1, -1) { }

        /// <summary>
        /// Specifies a range of scanlines that this method handles.
        /// Note scanlines count down, so the start value should be greater than the end value.
        /// </summary>
        /// <param name="NtscStart">Inclusive. Maximum value is 192.</param>
        /// <param name="NtscEnd">Exclusive. Must be less than <paramref name="NtscStart"/>. Minimum value is 0.</param>
        /// <param name="PalSecamStart">Inclusive. Maximum value is 228.</param>
        /// <param name="PalSecamEnd">Exclusive. Must be less than <paramref name="PalSecamStart"/>. Minimum value is 0.</param>
        public KernelScanlineRangeAttribute(int NtscStart = -1, int NtscEnd = -1, int PalSecamStart = -1, int PalSecamEnd = -1)
        {
            Ntsc = NtscStart..NtscEnd;
            PalSecam = PalSecamStart..PalSecamEnd;
        }
    }

    /// <summary>
    /// Marks a method to be executed during the overscan phase.
    /// Multiple methods may be marked with this. They will be executed in the order they are declared.
    /// Marked methods must return void and take no parameters.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class OverscanAttribute : Attribute { }
}
