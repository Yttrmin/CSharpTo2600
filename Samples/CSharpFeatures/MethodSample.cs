using System.Collections.Generic;
using VCSFramework;
using VCSFramework.Templates.Standard;
using static VCSFramework.Registers;

namespace Samples.CSharpFeatures
{
    [TemplatedProgram(typeof(StandardTemplate))]
    public static class MethodSample
    {
        [RomDataGenerator(nameof(GenerateData))]
        private static RomData<ReturnStruct> RomData;
        private static byte RefByte;
        private static ReturnStruct StaticReturnStruct;

        [VBlank]
        public static void Main()
        {
            ref readonly var refRet = ref ReturnRefStruct(new ReturnStruct
            {
                ValueA = 0x88,
                ValueB = RomData[0].ValueB,
                ValueC = 0x44,
                ValueD = 0
            });
            var ret = ReturnValueStruct(in refRet);
            var fibonacciCount = RomData[0].ReadOnlyMethod(RefByteReturn(0));
            var fib = Fibonacci(fibonacciCount);
            // Note Stella ignores the last bit assigned to COLUBK. So assigning 21 results in 20 getting assigned.
            // (The VCS also ignores the last bit, just didn't expect Stella to drop it in its debugger output.)
            ColuBk = ret.ReadOnlyMethod(fib);
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

        private static IEnumerable<ReturnStruct> GenerateData()
        {
            yield return new ReturnStruct
            {
                ValueA = 0x88,
                ValueB = 0x08,
                ValueC = 0x44,
                ValueD = 0
            };

            yield return new ReturnStruct
            {
                ValueA = 0x44,
                ValueB = 0x55,
                ValueC = 0x66,
                ValueD = 0x77
            };
        }

        public struct ReturnStruct
        {
            public byte ValueA;
            public byte ValueB;
            public byte ValueC;
            public byte ValueD;

            // Since RomData<> returns a ref readonly, the (Roslyn) compiler will create defensive copies as needed.
            // That means that any non-readonly methods are guaranteed to be called with a short pointer pointer.
            // Which conversely means any readonly methods MAY be called with a long pointer.
            public byte InstanceMethod(byte foo)
            {
                return (byte)(ValueB + foo);
            }

            public readonly byte ReadOnlyMethod(byte foo)
            {
                return (byte)(ValueB + foo);
            }
        }
    }
}
