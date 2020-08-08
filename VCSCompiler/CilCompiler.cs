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
		public static IEnumerable<AssemblyLine> CompileMethod(MethodDefinition definition, ImmutableTypeMap types, Assembly frameworkAssembly, Auditor auditor)
		{
			var instructionCompiler = new CilInstructionCompiler(definition, types);
			var instructions = definition.Body.Instructions;
			var compilationActions = ProcessInstructions(instructions, types, frameworkAssembly);
			var instructionsToLabel = GetInstructionsToEmitLabelsFor(instructions, auditor).ToArray();
			var compiledBody = new List<AssemblyLine>();
			var compilationContext = new CompilationContext(instructionCompiler);
			foreach (var action in compilationActions)
			{
				var needLabel = action.ConsumedInstructions.Where(i => instructionsToLabel.Contains(i)).ToArray();
				foreach (var toLabel in needLabel)
				{
					compiledBody.Add(AssemblyFactory.Label(LabelGenerator.GetFromInstruction(toLabel)));
				}

				compiledBody.AddRange(action.Execute(compilationContext));
			}

			compiledBody = OptimizeMethod(compiledBody).ToList();
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Final assembly:");
            stringBuilder.AppendLine(string.Join(Environment.NewLine, compiledBody));
            auditor.RecordEntry(stringBuilder.ToString());

			return compiledBody;
		}

		private static IEnumerable<ICompilationAction> ProcessInstructions(IEnumerable<Instruction> instructions, ImmutableTypeMap types, Assembly frameworkAssembly)
		{
			var actions = new List<ICompilationAction>();

			// We iterate over the instructions backwards since compile time executable methods have constants loaded
			// prior to the actual call. We don't know they're meant for such a method until we hit the call instruction.
			// Whereas when going backwards we hit the call first, and then can work out which constant loads to 
			// avoid processing later on.
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
				if (types.TryGetType(methodDefinition.DeclaringType, out var processedType))
				{
					var processedSubroutine = processedType.Subroutines.SingleOrDefault(s => s.MethodDefinition.FullName == methodDefinition.FullName);
					return processedSubroutine?.TryGetFrameworkAttribute<VCSFramework.CompileTimeExecutedMethodAttribute>(out _) == true;
				}
				return false;
			}
		}

		private static ICompilationAction CreateExecuteCommand(Instruction instruction, ImmutableTypeMap types, Assembly frameworkAssembly)
		{
			var nextInstruction = instruction.Next;
			var methodDefinition = (MethodReference)instruction.Operand;
			var processedType = types[methodDefinition.DeclaringType];
			var processedSubroutine = processedType.Subroutines.Single(s => s.MethodDefinition.FullName == methodDefinition.FullName);

			return new ExecuteCompilationAction(instruction, processedSubroutine, frameworkAssembly);
		}

	    private static IEnumerable<AssemblyLine> OptimizeMethod(IEnumerable<AssemblyLine> body)
	    {
			// Pair up instructions.
			var pairedInstructions = body.Zip(body.Skip(1).Append(null), (lineA, lineB) => (lineA, lineB)).ToList();

			for (var i = 0; i < pairedInstructions.Count;)
			{
				var pair = pairedInstructions[i];
				// Skip over certain redundant instruction pairs.
				var (offset, toSkip) = pair switch
				{
					(AssemblyInstruction("PHA", _), AssemblyInstruction("PLA", _)) => (0, 2),
					(AssemblyInstruction("STA", var stArg), AssemblyInstruction("LDA", var ldArg)) when stArg == ldArg => (1, 1),
					_ => (0, 0)
				};
				if (offset + toSkip > 2)
                {
					throw new InvalidOperationException("Not allowed to optimize outside the bounds of the instruction pair.");
                }
				i += Math.Max(1, offset + toSkip);
				if (offset != 0 || toSkip == 0) yield return pair.lineA;
			}
	    }
		
		/// <summary>
		/// Gets instructions that need to be labeled, generally for branching instructions.
		/// </summary>
		private static IEnumerable<Instruction> GetInstructionsToEmitLabelsFor(IEnumerable<Instruction> instructions, Auditor auditor)
		{
			foreach(var instruction in instructions)
			{
				// Branch opcodes have an Instruction as their operand.
				var targetInstruction = instruction.Operand as Instruction;
				if (targetInstruction != null)
				{
					auditor.RecordEntry($"{instruction} references {targetInstruction}, marking to emit label.");
					yield return targetInstruction;
				}
			}
		}
    }
}
