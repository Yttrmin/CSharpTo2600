using System.Collections.Generic;
using VCSFramework.Assembly;

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
			throw new System.NotImplementedException();
		}
    }
}
