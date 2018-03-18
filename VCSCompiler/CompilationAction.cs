using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using VCSFramework.Assembly;
using VCSFramework;
using System.Linq;
using System.Collections.Immutable;

namespace VCSCompiler
{
	internal interface ICompilationAction
	{
		IEnumerable<AssemblyLine> Execute(ICompilationContext context);
		IImmutableSet<Instruction> ConsumedInstructions { get; }
	}

	internal class CompileCompilationAction : ICompilationAction
	{
		private readonly Instruction Instruction;

		public IImmutableSet<Instruction> ConsumedInstructions { get; }

		public CompileCompilationAction(Instruction instruction)
		{
			Instruction = instruction;
			ConsumedInstructions = new[] { Instruction }.ToImmutableHashSet();
		}

		public IEnumerable<AssemblyLine> Execute(ICompilationContext context)
		{
			return context.CilInstructionCompiler.CompileInstruction(Instruction);
		}
	}

	internal class ExecuteCompilationAction : ICompilationAction
	{
		private readonly MethodInfo MethodInfo;
		private readonly ImmutableArray<byte> Arguments;

		public IImmutableSet<Instruction> ConsumedInstructions { get; }

		public ExecuteCompilationAction(Instruction callInstruction, ProcessedSubroutine subroutine, Assembly assembly)
		{
			var consumedInstructions = new List<Instruction>();
			subroutine.TryGetFrameworkAttribute<CompileTimeExecutedMethodAttribute>(out var attribute);

			var parameterCount = subroutine.Parameters.Count;
			var arguments = new List<byte>();
			var previousInstruction = callInstruction.Previous;
			for (var i = 0; i < parameterCount; i++)
			{
				arguments.Add((Convert.ToByte(previousInstruction.Operand)));
				consumedInstructions.Add(previousInstruction);
				previousInstruction = previousInstruction.Previous;
			}
			consumedInstructions.Add(callInstruction);

			var methodDefinition = subroutine.MethodDefinition;
			var method = assembly.DefinedTypes
				.Single(ti => ti.FullName == methodDefinition.DeclaringType.FullName)
				.GetMethod(attribute.ImplementationName, BindingFlags.Static | BindingFlags.NonPublic);

			ConsumedInstructions = consumedInstructions.ToImmutableHashSet();
			MethodInfo = method;
			Arguments = arguments.ToImmutableArray();
		}

		public IEnumerable<AssemblyLine> Execute(ICompilationContext context)
		{
			return (IEnumerable<AssemblyLine>)MethodInfo.Invoke(null, Arguments.Cast<object>().ToArray());
		}
	}

	internal interface ICompilationContext
	{
		CilInstructionCompiler CilInstructionCompiler { get; }
	}

	internal class CompilationContext : ICompilationContext
	{
		public CilInstructionCompiler CilInstructionCompiler { get; }

		public CompilationContext(CilInstructionCompiler cilInstructionCompiler)
		{
			CilInstructionCompiler = cilInstructionCompiler;
		}
	}
}
