using System;

namespace VCSFramework.Templates.Standard
{
    /// <summary>
    /// Marks a method that is also by marked by one of: <see cref="VBlankAttribute"/>, <see cref="KernelAttribute"/>, or <see cref="OverscanAttribute"/> as only
    /// executing for a specific set of "screen"s. The current screens can be set by using <see cref="Utilities.SetScreen(ulong)"/>.
    /// 
    /// Screens can be used to separate code based on logical states of your program. You may want a "Title Screen", an "Inventory Screen", and a "Combat Screen".
    /// All these screens would likely use different kernels, and it would be prohibitively slow to do this by checking the state of your program in your kernel.
    /// 
    /// Multiple screens can be active at once, but the rules of <see cref="KernelScanlineRangeAttribute"/> still apply. For exmaple you can have "Combat | Status" 
    /// or "Combat | Inventory" as your active screen, where "Combat" has a kernel for the first 182 scanlines, and "Status" and "Inventory" have kernels for the last 10 scanlines.
    /// 
    /// Screens are an optional, opt-in feature. If no method is marked with this attribute, they are all treated as executing for "Screen 1" (e.g. 0b1).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ScreenAttribute : Attribute
    {
        public ulong ScreenBits;

        /// <param name="screenBits">A bit mask representing which screens this method executes for.</param>
        public ScreenAttribute(ulong screenBits) => ScreenBits = screenBits;
    }

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

    public readonly struct ScanlineRange
    {
        /// <summary>Inclusive start.</summary>
        public int Start { get; }
        /// <summary>Exclusive end.</summary>
        public int End { get; }

        public ScanlineRange(int start, int end)
        {
            if (end >= start)
                throw new ArgumentException($"{nameof(end)} must not be greater than or equal to {nameof(start)} ({end} >= {start}).");
            if (start < 0)
                throw new ArgumentException($"{nameof(start)} ({start}) must not be negative.");
            if (end < 0)
                throw new ArgumentException($"{nameof(end)} ({end}) must not be negative.");
            Start = start;
            End = end;
        }

        public bool Overlaps(ScanlineRange other)
        {
            return Contains(this, other) || Contains(other, this);

            static bool Contains(ScanlineRange a, ScanlineRange b)
                => b.Start > a.End && b.Start <= a.Start;
        }

        public override string ToString() => $"({Start}..{End}]";
    }

    /// <summary>
    /// Specifies the inclusive start and inclusive end of a range of scanlines that this method handles.
    /// When all range attributes are taken together, they must be exhaustive of the entire range of scanlines.
    /// The scanline index counts down to 0 over the frame. Keep this in mind when laying out data you intend to index into.
    /// The first scanline is 191 for NTSC and 227 for PAL/SECAM. The last scanline is 0.
    /// The parameters should be treated as if they used in a loop like: <code>for (var i = Start; i >= End; i--) { }</code>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class KernelScanlineRangeAttribute : Attribute
    {
        public ScanlineRange? Ntsc { get; }
        public ScanlineRange? PalSecam { get; }

        /// <summary>
        /// Specifies a range of NTSC scanlines that this method handles.
        /// Note scanlines count down, so <paramref name="ntscStart"/> should be greater than or equal to <paramref name="ntscEnd"/>.
        /// </summary>
        /// <param name="ntscStart">Inclusive. Maximum value is 191.</param>
        /// <param name="ntscEnd">Exclusive. Minimum value is 0. Must be less than <paramref name="ntscStart"/>.</param>
        public KernelScanlineRangeAttribute(int ntscStart, int ntscEnd) : this(ntscStart, ntscEnd, -1, -1) { }

        /// <summary>
        /// Specifies a range of scanlines that this method handles.
        /// Note scanlines count down, so the start value should be greater than or equal to the end value.
        /// </summary>
        /// <param name="ntscStart">Inclusive. Maximum value is 191.</param>
        /// <param name="ntscEnd">Exclusive. Minimum value is 0. Must be less than <paramref name="ntscStart"/>.</param>
        /// <param name="palSecamStart">Inclusive. Maximum value is 227.</param>
        /// <param name="palSecamEnd">Exclusive. Minimum value is 0. Must be less than <paramref name="palSecamStart"/>.</param>
        public KernelScanlineRangeAttribute(int ntscStart = -1, int ntscEnd = -1, int palSecamStart = -1, int palSecamEnd = -1)
        {
            Ntsc = (ntscStart != -1 && ntscEnd != -1) ? new ScanlineRange(ntscStart, ntscEnd) : null;
            PalSecam = (palSecamStart != -1 && palSecamEnd != -1) ? new ScanlineRange(palSecamStart, palSecamEnd) : null;
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
