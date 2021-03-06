﻿using static VCSFramework.Registers;

namespace Samples.CSharpFeatures
{
    static class GenericsSample
    {
        private static SingleFieldGeneric<UserStruct> Static;

        public static void Main()
        {
            SingleFieldGeneric<byte>.StaticField = 0x0C;
            var byteInstance = new SingleFieldGeneric<byte> { Field = SingleFieldGeneric<byte>.StaticField };
            Static = new SingleFieldGeneric<UserStruct> { Field = new UserStruct { Byte = byteInstance.Field } };
            var instance = Static;
            while (true)
            {
                ColuBk = instance.Field.Byte;
            }
        }

        private struct SingleFieldGeneric<T> where T: unmanaged
        {
            public static T StaticField;
            public T Field;
        }

        private struct UserStruct
        {
            public byte Byte;
        }
    }
}
