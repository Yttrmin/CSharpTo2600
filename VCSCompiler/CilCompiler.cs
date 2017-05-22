using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using Mono.Cecil.Cil;
using Mono.Cecil;

namespace VCSCompiler
{
    internal class CilCompiler
    {
		public static IEnumerable<AssemblyLine> CompileBody(IEnumerable<Instruction> instructions)
		{
			var compiledBody = new List<AssemblyLine>();
			foreach(var instruction in instructions)
			{
				Console.WriteLine($"{instruction}  -->");
				var vcsInstructions = CilInstructionCompiler.CompileInstruction(instruction);
				compiledBody.AddRange(vcsInstructions);
				foreach(var vcsInstruction in vcsInstructions)
				{
					Console.WriteLine($"  {vcsInstruction}");
				}
			}
			return compiledBody;
		}

		private static IEnumerable<AssemblyLine> CompileInstruction(Instruction instruction)
		{
			return CilInstructionCompiler.CompileInstruction(instruction);
		}
    }
}
