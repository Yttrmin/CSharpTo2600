using System;
using System.Collections.Generic;
using System.Text;
using VCSFramework.Assembly;
using static VCSFramework.Assembly.AssemblyFactory;

namespace VCSFramework
{
    public static class Memory
    {
		[UseProvidedImplementation(nameof(SetRamInternal))]
		public static void SetRam(byte index, byte value) { }

		[DoNotCompile]
		private static IEnumerable<AssemblyLine> SetRamInternal()
		{
			yield return PLA();
			yield return STA("TempReg1");
			yield return PLA();
			yield return TAX();
			yield return LDA("TempReg1");
			yield return STA(0, Index.X);
			yield return RTS();
		}
    }
}
