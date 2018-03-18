using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;

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
		public bool IsEntryPoint { get; }
		public ControlFlowGraph ControlFlowGraph { get; }

		protected ProcessedSubroutine(ProcessedSubroutine processedSubroutine)
			: this(processedSubroutine.MethodDefinition, processedSubroutine.ControlFlowGraph, processedSubroutine.IsEntryPoint, processedSubroutine.ReturnType, processedSubroutine.Parameters, processedSubroutine.Locals, processedSubroutine.FrameworkAttributes)
		{ }

		public ProcessedSubroutine(
			MethodDefinition methodDefinition,
			ControlFlowGraph controlFlowGraph,
			bool isEntryPoint,
			ProcessedType returnType, 
			IList<ProcessedType> parameters, 
			IList<ProcessedType> locals,
			IEnumerable<Attribute> frameworkAttributes)
		{
			MethodDefinition = methodDefinition;
			ControlFlowGraph = controlFlowGraph;
			IsEntryPoint = isEntryPoint;
			ReturnType = returnType;
			Parameters = parameters;
			Locals = locals;
			FrameworkAttributes = frameworkAttributes;
		}

		public bool TryGetFrameworkAttribute<T>(out T result) where T : Attribute
		{
			dynamic attribute = FrameworkAttributes.SingleOrDefault(a => a.GetType().FullName == typeof(T).FullName);
			if (attribute != null)
			{
				result = AttributeReconstructor.ReconstructFrom<T>(attribute);
				return true;
			}
			else
			{
				result = default;
				return false;
			}
		}

		public override string ToString() => $"{FullName} [Processed]";
	}
}
