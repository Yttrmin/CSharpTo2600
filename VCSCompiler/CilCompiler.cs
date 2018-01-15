using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using Mono.Cecil.Cil;
using Mono.Cecil;
using VCSFramework.Assembly;
using System.Collections.Immutable;

namespace VCSCompiler
{
    internal class CilCompiler
    {
		public static IEnumerable<AssemblyLine> CompileMethod(MethodDefinition definition, IImmutableDictionary<string, ProcessedType> types)
		{
			var instructionCompiler = new CilInstructionCompiler(definition, types);
			var instructions = definition.Body.Instructions;
			var instructionsToLabel = GetInstructionsToEmitLabelsFor(instructions).ToArray();
			var compiledBody = new List<AssemblyLine>();
			// Iterate over Instruction::Next so we can rewrite instructions while processing.
			for(var instruction = instructions.First(); instruction != null; instruction = instruction.Next)
			{
				Console.WriteLine($"{instruction}  -->");
				if (instructionsToLabel.Contains(instruction))
				{
					compiledBody.Add(AssemblyFactory.Label(LabelGenerator.GetFromInstruction(instruction)));
				}
				var vcsInstructions = instructionCompiler.CompileInstruction(instruction).ToArray();
				compiledBody.AddRange(vcsInstructions);
				foreach(var vcsInstruction in vcsInstructions)
				{
					Console.WriteLine($"  {vcsInstruction}");
				}
			}
			compiledBody = OptimizeMethod(compiledBody).ToList();
			return compiledBody;
		}

	    private static IEnumerable<AssemblyLine> OptimizeMethod(IEnumerable<AssemblyLine> body)
	    {
		    var mutableBody = body.ToList();
		    var toDelete = new List<int>();

			// Optimize out redundant PHA/PLA
		    for (var i = 0; i < mutableBody.Count; i++)
		    {
			    if ((mutableBody[i] as AssemblyInstruction)?.OpCode == "PHA"
			        && (mutableBody[i + 1] as AssemblyInstruction)?.OpCode == "PLA")
			    {
				    toDelete.Add(i);
					toDelete.Add(i + 1);
			    }
		    }
		    toDelete.Reverse();
		    foreach (var index in toDelete)
		    {
			    mutableBody.RemoveAt(index);
		    }
			Console.WriteLine($"Eliminated {toDelete.Count} redundant PHA/PLA pairs.");

			toDelete.Clear();
			// Optimize out redundant LDAs following STAs.
			for (var i = 0; i < mutableBody.Count - 1; i++)
			{
				var line1 = mutableBody[i] as AssemblyInstruction;
				var line2 = mutableBody[i + 1] as AssemblyInstruction;

				if (line1?.OpCode == "STA" && line2?.OpCode == "LDA")
				{
					if (line1?.Argument != null && line1?.Argument == line2?.Argument)
					{
						toDelete.Add(i + 1);
					}
				}
			}
			toDelete.Reverse();
			foreach(var index in toDelete)
			{
				mutableBody.RemoveAt(index);
			}
			Console.WriteLine($"Eliminated {toDelete.Count} redundant LDAs.");
		    return mutableBody;
	    }
		
		/// <summary>
		/// Gets instructions that need to be labeled, generally for branching instructions.
		/// </summary>
		private static IEnumerable<Instruction> GetInstructionsToEmitLabelsFor(IEnumerable<Instruction> instructions)
		{
			foreach(var instruction in instructions)
			{
				// Branch opcodes have an Instruction as their operand.
				var targetInstruction = instruction.Operand as Instruction;
				if (targetInstruction != null)
				{
					Console.WriteLine($"{instruction} references {targetInstruction}, marking to emit label.");
					yield return targetInstruction;
				}
			}
		}
    }
}
