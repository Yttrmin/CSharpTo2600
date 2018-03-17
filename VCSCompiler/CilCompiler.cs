using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using Mono.Cecil.Cil;
using Mono.Cecil;
using VCSFramework.Assembly;
using System.Collections.Immutable;
using System.Reflection;

namespace VCSCompiler
{
    internal class CilCompiler
    {
		public static IEnumerable<AssemblyLine> CompileMethod(MethodDefinition definition, IImmutableDictionary<string, ProcessedType> types, Assembly frameworkAssembly)
		{
			// TODO - If this is the entry point, automatically initialize memory and invoke static constructor.
			var instructionCompiler = new CilInstructionCompiler(definition, types);
			var instructions = definition.Body.Instructions;
			var compilationActions = ProcessInstructions(instructions, types, frameworkAssembly);
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

		private static IEnumerable<ICompilationAction> ProcessInstructions(IEnumerable<Instruction> instructions, IImmutableDictionary<string, ProcessedType> types, Assembly frameworkAssembly)
		{
			var actions = new List<ICompilationAction>();

			foreach(var instruction in instructions.Reverse())
			{
				if (actions.Any(a => a.ConsumedInstructions.Contains(instruction)))
				{
					continue;
				}

				if (instruction.OpCode == OpCodes.Call && IsCompileTimeExecutable((MethodReference)instruction.Operand))
				{
					actions.Add(CreateExecuteCommand(instruction, types, frameworkAssembly));
				}
				else
				{
					actions.Add(new CompileCompilationAction(instruction));
				}
			}

			return ((IEnumerable<ICompilationAction>)actions).Reverse();

			bool IsCompileTimeExecutable(MethodReference methodDefinition)
			{
				if (types.TryGetValue(methodDefinition.DeclaringType.FullName, out var processedType))
				{
					var processedSubroutine = processedType.Subroutines.SingleOrDefault(s => s.MethodDefinition.FullName == methodDefinition.FullName);
					return processedSubroutine?.TryGetFrameworkAttribute<VCSFramework.CompileTimeExecutedMethodAttribute>(out _) == true;
				}
				return false;
			}
		}

		private static ICompilationAction CreateExecuteCommand(Instruction instruction, IImmutableDictionary<string, ProcessedType> types, Assembly frameworkAssembly)
		{
			var nextInstruction = instruction.Next;
			var methodDefinition = (MethodReference)instruction.Operand;
			var processedType = types[methodDefinition.DeclaringType.FullName];
			var processedSubroutine = processedType.Subroutines.Single(s => s.MethodDefinition.FullName == methodDefinition.FullName);

			return new ExecuteCompilationAction(instruction, processedSubroutine, frameworkAssembly);
		}

	    private static IEnumerable<AssemblyLine> OptimizeMethod(IEnumerable<AssemblyLine> body)
	    {
		    var mutableBody = body.ToList();

			// Remove redundant PHA/PLA pairs.
			var pairs = mutableBody.OfType<AssemblyInstruction>().Zip(mutableBody.OfType<AssemblyInstruction>().Skip(1), Tuple.Create);
			var phaPlaPairs = pairs.Where(p => p.Item1.OpCode == "PHA" && p.Item2.OpCode == "PLA").ToArray();

			foreach(var pair in phaPlaPairs)
			{
				mutableBody.RemoveAll(line => ReferenceEquals(line, pair.Item1));
				mutableBody.RemoveAll(line => ReferenceEquals(line, pair.Item2));
			}
			
			Console.WriteLine($"Eliminated {phaPlaPairs.Length} redundant PHA/PLA pairs.");

			// Remove LDAs following a STA to the same argument.
			pairs = mutableBody.OfType<AssemblyInstruction>().Zip(mutableBody.OfType<AssemblyInstruction>().Skip(1), Tuple.Create);
			var staLdaPairs = pairs
				.Where(p => p.Item1.OpCode == "STA" && p.Item2.OpCode == "LDA")
				.Where(p => p.Item1.Argument == p.Item2.Argument)
				.ToArray();

			foreach(var pair in staLdaPairs)
			{
				mutableBody.RemoveAll(line => ReferenceEquals(line, pair.Item2));
			}
			
			Console.WriteLine($"Eliminated {staLdaPairs.Length} redundant LDAs.");
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
