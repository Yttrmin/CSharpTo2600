using static CSharpTo2600.Framework.Assembly.ReservedSymbols;

namespace CSharpTo2600.Framework
{
    public static class TIARegisters
    {
        // All implementations are purposefully blank. They'll be treated
        // as directly accessing the TIA addresses by the compiler.
        //
        // They can't just be declared 'extern' because then we can't load
        // the assembly for reflection.

        [CompilerIntrinsicGlobal(nameof(COLUBK))]
        public static byte BackgroundColor { set { } }
    }
}
