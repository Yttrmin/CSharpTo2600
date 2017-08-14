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

		[UseProvidedImplementation(nameof(ClearMemoryInternal))]
		[AlwaysInline]
		public static void ClearMemory() { }

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

		[DoNotCompile]
		private static IEnumerable<AssemblyLine> ClearMemoryInternal()
		{
			yield return LDA(0);
			yield return LDX(0xFF);
			var loopTarget = Label("__CLEAR_MEMORY_LOOP__");
			yield return loopTarget;
			yield return STA(0, Index.X);
			yield return DEX();
			yield return BNE(loopTarget);
			yield return RTS();
		}
    }
}
