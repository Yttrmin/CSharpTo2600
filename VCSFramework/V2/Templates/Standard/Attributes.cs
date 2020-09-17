using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class KernelScanlineRangeAttribute : Attribute
    {
        public Range Ntsc { get; }
        public Range PalSecam { get; }

        public KernelScanlineRangeAttribute(int NtscStart = -1, int NtscEnd = -1, int PalSecamStart = -1, int PalSecamEnd = -1)
        {
            Ntsc = NtscStart..NtscEnd;
            PalSecam = PalSecamStart..PalSecamEnd;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class OverscanAttribute : Attribute { }
}
