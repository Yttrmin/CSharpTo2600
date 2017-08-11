using System;
using System.Collections.Generic;
using System.Text;

namespace VCSFramework
{
    public static class Registers
    {
		public static byte A { [OverrideWithLoadToRegister("A")] set { } }
		public static byte X { [OverrideWithLoadToRegister("X")] set { } }
		public static byte Y { [OverrideWithLoadToRegister("Y")] set { } }

		public static byte ColuBk { [OverrideWithStoreToSymbol("COLUBK")] set { } }
		public static byte Tim64T { [OverrideWithStoreToSymbol("TIM64T")] set { } }
		public static byte VBlank { [OverrideWithStoreToSymbol("VBLANK")] set { } }
		public static byte VSync { [OverrideWithStoreToSymbol("VSYNC") ] set { } }
		public static byte WSync { [OverrideWithStoreToSymbol("WSYNC")] set { } }
    }
}
