using CSharpTo2600.Framework;
using CSharpTo2600.Framework.IOPorts;

namespace ScreenColors
{
    // Game class must be marked with an attribute.
    //@TODO - Use a base class instead?
    [Atari2600Game]
    // Game class must be:
    // Static - This class has tons of special treatment by the compiler. Letting it be instantiated makes no sense.
    static class TestGame
    {
        // Just to test multi-byte vars and endianness.
        private static long ULongVar;
        // Static fields become globals of the same name.
        // May have to be mangled when multiple classes are added.
        private static byte Color;

        // Called exactly once at startup. Called Main() to please compiler since we need an entry point.
        // These attributes are mandatory. The compiler will not infer speciality from the name alone.
        // Attributes have the added advantage of making speciality very clear.
        [SpecialMethod(MethodType.Initialize)]
        static void Main()
        {
            Color = 0xAB;
            ULongVar = 0x0123456789ABCDEF;
        }

        // Called every frame during VSync (after overscan).
        [SpecialMethod(MethodType.Tick)]
        static void Tick()
        {
            BackgroundColor = Color;
            Color++;
        }

        // Contains all kernels. Called after VSync.
        //@TODO - This test won't need this, but I want to keep this here to test special methods.
        [SpecialMethod(MethodType.Kernel)]
        static void Kernel()
        {
            for (var i = 0; i < 192; i++)
            {
                WSync();
            }
        }
    }
}
