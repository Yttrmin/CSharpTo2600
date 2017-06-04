using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using Mono.Cecil.Cil;
using Mono.Cecil;
using VCSCompiler.Assembly;
using System.Collections.Immutable;

namespace VCSCompiler
{
    internal class CilCompiler
    {
		public static IEnumerable<AssemblyLine> CompileBody(IEnumerable<Instruction> instructions, IImmutableDictionary<string, ProcessedType> types)
		{
			var instructionCompiler = new CilInstructionCompiler(types);
			var compiledBody = new List<AssemblyLine>();
			foreach(var instruction in instructions)
			{
				Console.WriteLine($"{instruction}  -->");
				var vcsInstructions = instructionCompiler.CompileInstruction(instruction);
				compiledBody.AddRange(vcsInstructions);
				foreach(var vcsInstruction in vcsInstructions)
				{
					Console.WriteLine($"  {vcsInstruction}");
				}
			}
			return compiledBody;
		}
    }
}
