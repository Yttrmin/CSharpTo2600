using VCSFramework;
using VCSFramework.Templates.Standard;
using static VCSFramework.Registers;

namespace Samples.CSharpFeatures
{
    [TemplatedProgram(typeof(StandardTemplate))]
    static class MethodSample
    {
        private static byte RefByte;
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
            ref var fibonacciCount = ref RefByteReturn(8);
            // Note Stella ignores the last bit assigned to COLUBK. So assigning 21 results in 20 getting assigned.
            // (The VCS also ignores the last bit, just didn't expect Stella to drop it in its debugger output.)
            ColuBk = ret.InstanceMethod(Fibonacci(fibonacciCount));
        }

        [Kernel(KernelType.EveryScanline)]
        public static void NopKernel() { }

        private static ref byte RefByteReturn(byte a)
        {
            RefByte = a;
            return ref RefByte;
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

        private static byte Fibonacci(byte count)
        {
            return FibonacciInternal(0, 1, count);

            static byte FibonacciInternal(byte a, byte b, byte count)
            {
                if (count == 0)
                    return a;
                byte sum = (byte)(a + b);
                return FibonacciInternal(b, sum, (byte)(count - 1));
            }
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
