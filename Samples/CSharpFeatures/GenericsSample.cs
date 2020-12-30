using static VCSFramework.Registers;

namespace Samples.CSharpFeatures
{
    static class GenericsSample
    {
        public static void Main()
        {
            var instance = new SingleFieldGeneric<byte>
            {
                Field = 0x0E
            };
            while (true)
            {
                ColuBk = instance.Field;
            }
        }

        private struct SingleFieldGeneric<T> where T: unmanaged
        {
            public T Field;
        }
    }
}
