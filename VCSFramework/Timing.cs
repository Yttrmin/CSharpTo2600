using System.Collections.Generic;
using VCSFramework.Assembly;
using static VCSFramework.Assembly.AssemblyFactory;
using System.Linq;

namespace VCSFramework
{
	public static class Timing
    {
		[CompileTimeExecutedMethod(nameof(ConsumeCyclesInternal))]
		public static void ConsumeCycles(byte cycleCount)
		{

		}

		[DoNotCompile]
		internal static IEnumerable<AssemblyLine> ConsumeCyclesInternal(byte cycleCount)
		{
			if (cycleCount == 0)
			{
				yield break;
			}

			if (cycleCount == 1)
			{
				throw new System.ArgumentException("Can not consume 1 cycle.", nameof(cycleCount));
			}

			int remainingCycles = cycleCount;
			var bitInstructions = remainingCycles / 3;
			remainingCycles -= bitInstructions * 3;
			if (remainingCycles == 1)
			{
				remainingCycles += 3;
				bitInstructions--;
			}

			var nopInstructions = remainingCycles / 2;
			remainingCycles -= nopInstructions * 2;
			if (remainingCycles != 0)
			{
				throw new System.Exception();
			}

			for(var i = 0; i < bitInstructions; i++)
			{
				yield return BIT(0);
			}

			for (var i = 0; i < nopInstructions; i++)
			{
				yield return NOP();
			}
		}
    }
}
