using System;
using System.Collections.Generic;
using System.Text;

namespace VCSFramework
{
    public static class Registers
    {
		public static byte BackgroundColor { [OverrideWithStoreToSymbol("COLUBK")] set { } }
    }
}
