using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Text;

namespace VCSCompiler
{
    internal class ProcessedField
    {
		public string Name => FieldDefinition.Name;
		public string FullName => FieldDefinition.FullName;
		public ProcessedType FieldType { get; }
		public FieldDefinition FieldDefinition { get; }

		public ProcessedField(FieldDefinition fieldDefinition, ProcessedType fieldType)
		{
			FieldDefinition = fieldDefinition;
			FieldType = fieldType;
		}

		public override string ToString() => $"{FullName}";
	}
}
