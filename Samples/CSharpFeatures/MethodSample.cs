using VCSFramework;
using VCSFramework.Templates.Standard;
using static VCSFramework.Registers;

namespace Samples.CSharpFeatures
{
    [TemplatedProgram(typeof(StandardTemplate))]
    static class MethodSample
    {
        private static byte BackgroundColor;
        private static ReturnStruct StaticReturnStruct;

        [VBlank]
        public static void Main()
        {
            ref readonly var refRet = ref ReturnRefStruct(new ReturnStruct
            {
                ValueA = 0x88,
                ValueB = 0x0E,
                ValueC = 0x44,
                ValueD = 0
            });
            var ret = ReturnValueStruct(in refRet);
            ColuBk = ret.InstanceMethod(0x2E);
        }

        [Kernel(KernelType.EveryScanline)]
        public static void NopKernel() { }

        private static ref byte RefByteReturn(byte a)
        {
            BackgroundColor = a;
            return ref BackgroundColor;
        }

        private static ref ReturnStruct ReturnRefStruct(ReturnStruct a)
        {
            StaticReturnStruct = a;
            return ref StaticReturnStruct;
        }

        private static ReturnStruct ReturnValueStruct(in ReturnStruct a)
        {
            return a;
        }

        private static ReturnStruct GetColorContainingStruct()
        {
            return new ReturnStruct
            {
                ValueA = default,
                ValueB = BackgroundColor,
                ValueC = 0x0E
            };
        }

        private struct ReturnStruct
        {
            public byte ValueA;
            public byte ValueB;
            public byte ValueC;
            public byte ValueD;

            public byte InstanceMethod(byte foo)
            {
                return (byte)(ValueB + foo);
            }
        }
    }
}
