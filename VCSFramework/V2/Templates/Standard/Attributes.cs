using System;

namespace VCSFramework.V2.Templates.Standard
{
    // Purposefully nesting these despite being public to make it obvious if you're using the wrong attributes with [ProgramTemplate].
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class VBlankAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class KernelAttribute : Attribute
    {
        public KernelType KernelType { get; }

        public KernelAttribute(KernelType kernelType)
        {
            KernelType = kernelType;
        }
    }

    /// <summary>
    /// Specifies the inclusive start and exclusive end of a range of scanlines that this method handles.
    /// When all range attributes are taken together, they must be exhaustive of the entire range of scanlines.
    /// The first scanline is 0. The last scanline is 192 for NTSC and 228 for PAL/SECAM.
    /// The parameters should be treated as if they used in a loop like: <code>for (int i = Start; i < End; i++) { }</code>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class KernelScanlineRangeAttribute : Attribute
    {
        public Range Ntsc { get; }
        public Range PalSecam { get; }

        /// <summary>
        /// Specifies a range of NTSC scanlines that this method handles.
        /// </summary>
        /// <param name="NtscStart">Inclusive. Minimum value is 0.</param>
        /// <param name="NtscEnd">Exclusive. Must be greater than <paramref name="NtscStart"/>. Maximum value is 192.</param>
        public KernelScanlineRangeAttribute(int NtscStart, int NtscEnd) : this(NtscStart, NtscEnd, -1, -1) { }

        /// <summary>
        /// Specifies a range of scanlines that this method handles.
        /// </summary>
        /// <param name="NtscStart">Inclusive. Minimum value is 0.</param>
        /// <param name="NtscEnd">Exclusive. Must be greater than <paramref name="NtscStart"/>. Maximum value is 192.</param>
        /// <param name="PalSecamStart">Inclusive. Minimum value is 0.</param>
        /// <param name="PalSecamEnd">Exclusive. Must be greater than <paramref name="PalSecamStart"/>. Maximum value is 228.</param>
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
