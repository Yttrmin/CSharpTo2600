using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Text;

namespace VCSCompiler
{
    internal class ProcessedType
    {
		public string Name => TypeDefinition.Name;
		public string FullName => TypeDefinition.FullName;
		public ProcessedType BaseType { get; }
		public IEnumerable<ProcessedField> Fields { get; }
		public IEnumerable<ProcessedSubroutine> Subroutines { get; }
		public TypeDefinition TypeDefinition { get; }
		public bool SystemType => TypeDefinition.Namespace.StartsWith("System");

		protected ProcessedType(ProcessedType processedType)
			: this(processedType.TypeDefinition, processedType.BaseType, processedType.Fields, processedType.Subroutines)
		{ }

		public ProcessedType(TypeDefinition typeDefinition, ProcessedType baseType, IEnumerable<ProcessedField> fields, IEnumerable<ProcessedSubroutine> subroutines)
		{
			BaseType = baseType;
			TypeDefinition = typeDefinition;
			Fields = fields;
			Subroutines = subroutines;
		}

		public override string ToString() => $"{FullName} [Processed]";
	}
}
