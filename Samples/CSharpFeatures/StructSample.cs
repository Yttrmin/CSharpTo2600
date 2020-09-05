using System.Runtime.InteropServices;
using static VCSFramework.Registers;

namespace Samples.CSharpFeatures
{
    // Not a proper VCS program.
    static class StructSample
    {
        private static SingleByteStruct SingleByteStruct;

        public static void Main()
        {
            while (true)
            {
                SingleByteStruct.Value++;
                ColuBk = SingleByteStruct.Value;
            }
        }
    }

    // Doesn't matter if this is declared inside or outside of StructSample.
    [StructLayout(LayoutKind.Explicit)]
    struct SingleByteStruct
    {
        // Note static values already work out of the box.
        [FieldOffset(0)]
        public byte Value;
    }
}
