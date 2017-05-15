using Mono.Cecil;
using System.Collections.Generic;

namespace VCSCompiler
{
	internal class ProcessedSubroutine
	{
		public string Name => MethodDefinition.Name;
		public ProcessedType ReturnType { get; }
		public IEnumerable<ProcessedType> Parameters { get; }
		public MethodDefinition MethodDefinition { get; }

		protected ProcessedSubroutine(ProcessedSubroutine processedSubroutine)
			: this(processedSubroutine.MethodDefinition, processedSubroutine.ReturnType, processedSubroutine.Parameters)
		{ }

		public ProcessedSubroutine(MethodDefinition methodDefinition, ProcessedType returnType, IEnumerable<ProcessedType> parameters)
		{
			MethodDefinition = methodDefinition;
			ReturnType = returnType;
			Parameters = parameters;
		}
	}
}
