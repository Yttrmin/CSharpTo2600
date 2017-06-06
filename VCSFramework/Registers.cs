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

		public static byte BackgroundColor { [OverrideWithStoreToSymbol("COLUBK")] set { } }
    }
}
