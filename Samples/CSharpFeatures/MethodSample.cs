using static VCSFramework.Registers;

namespace Samples.CSharpFeatures
{
    static class MethodSample
    {
        private static byte BackgroundColor;
        private static ReturnStruct StaticReturnStruct;

        public static void Main()
        {
            while (true)
            {
                var ret = ReturnRefStruct(new ReturnStruct
                {
                    ValueA = 0x88,
                    ValueB = 0x0E,
                    ValueC = 0x44,
                    ValueD = 0
                });
                ColuBk = ret.ValueC;
            }
        }

        private static void VoidReturnNoParam()
        {
            ColuBk = 0x0E;
        }

        private static byte ByteReturn(byte a)
        {
            byte returnValue = a;
            return returnValue;
        }

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

        private static ReturnStruct ReturnValueStruct(ReturnStruct a)
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
        }
    }
}
