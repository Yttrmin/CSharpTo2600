using System;
using System.Collections.Generic;
using System.Text;

namespace VCSFramework
{
    public static class Registers
    {
		public static byte A { [IgnoreImplementation][OverrideWithLoadToRegister("A")] set { } }
		public static byte X { [IgnoreImplementation][OverrideWithLoadToRegister("X")] set { } }
		public static byte Y { [IgnoreImplementation][OverrideWithLoadToRegister("Y")] set { } }

		public static byte ColuBk { [IgnoreImplementation][OverrideWithStoreToSymbol("COLUBK")] set { } }
		public static byte InTim { [IgnoreImplementation][OverrideWithLoadFromSymbol("INTIM")] get { throw new NotImplementedException(); } }
		public static byte Tim64T { [IgnoreImplementation][OverrideWithStoreToSymbol("TIM64T")] set { } }
		public static byte VBlank { [IgnoreImplementation][OverrideWithStoreToSymbol("VBLANK")] set { } }
		public static byte VSync { [IgnoreImplementation][OverrideWithStoreToSymbol("VSYNC") ] set { } }
		public static byte WSync { [IgnoreImplementation][OverrideWithStoreToSymbol("WSYNC")] set { } }
    }
}
