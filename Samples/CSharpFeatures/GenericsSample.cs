﻿using static VCSFramework.Registers;

namespace Samples.CSharpFeatures
{
    static class GenericsSample
    {
        private static SingleFieldGeneric<byte> Static;

        public static void Main()
        {
            SingleFieldGeneric<byte>.StaticField = 0x0C;
            Static = new SingleFieldGeneric<byte> { Field = SingleFieldGeneric<byte>.StaticField };
            var instance = Static;
            while (true)
            {
                ColuBk = instance.Field;
            }
        }

        private struct SingleFieldGeneric<T> where T: unmanaged
        {
            public static T StaticField;
            public T Field;
        }
    }
}
