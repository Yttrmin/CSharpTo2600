using Mono.Cecil;
using System;
using System.Collections.Generic;

namespace VCSCompiler
{
	internal class ProcessedSubroutine
	{
		public string Name => MethodDefinition.Name;
		public string FullName => MethodDefinition.FullName;
		public ProcessedType ReturnType { get; }
		public IList<ProcessedType> Parameters { get; }
		public IList<ProcessedType> Locals { get; }
		public IEnumerable<Attribute> FrameworkAttributes { get; }
		public MethodDefinition MethodDefinition { get; }
		public ControlFlowGraph ControlFlowGraph { get; }

		protected ProcessedSubroutine(ProcessedSubroutine processedSubroutine)
			: this(processedSubroutine.MethodDefinition, processedSubroutine.ControlFlowGraph, processedSubroutine.ReturnType, processedSubroutine.Parameters, processedSubroutine.Locals, processedSubroutine.FrameworkAttributes)
		{ }

		public ProcessedSubroutine(
			MethodDefinition methodDefinition,
			ControlFlowGraph controlFlowGraph,
			ProcessedType returnType, 
			IList<ProcessedType> parameters, 
			IList<ProcessedType> locals,
			IEnumerable<Attribute> frameworkAttributes)
		{
			MethodDefinition = methodDefinition;
			ControlFlowGraph = controlFlowGraph;
			ReturnType = returnType;
			Parameters = parameters;
			Locals = locals;
			FrameworkAttributes = frameworkAttributes;
		}

		public override string ToString() => $"{FullName} [Processed]";
	}
}
