using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace VCSCompiler
{
    internal class ProcessedType
    {
		public string Name => TypeDefinition.Name;
		public string FullName => TypeDefinition.FullName;
		public ProcessedType BaseType { get; }
		public IEnumerable<ProcessedField> Fields { get; }
		public IEnumerable<ProcessedSubroutine> Subroutines { get; }
		/// <summary>
		/// Total size in bytes of an instance of this type.
		/// </summary>
		public int TotalSize => ThisSize + BaseType?.TotalSize ?? 0;
		/// <summary>
		/// Size in bytes of just this type's members.
		/// </summary>
		public int ThisSize { get; }
		public TypeDefinition TypeDefinition { get; }
		public bool SystemType => TypeDefinition.Namespace.StartsWith("System");

		protected ProcessedType(ProcessedType processedType, IEnumerable<CompiledSubroutine> compiledSubroutines)
			: this(processedType.TypeDefinition, processedType.BaseType, processedType.Fields, compiledSubroutines, processedType.ThisSize)
		{ }

		public ProcessedType(TypeDefinition typeDefinition, ProcessedType baseType, IEnumerable<ProcessedField> fields, IEnumerable<ProcessedSubroutine> subroutines, int? size = null)
		{
			BaseType = baseType;
			TypeDefinition = typeDefinition;
			Fields = fields;
			Subroutines = subroutines;
			if (size.HasValue)
			{
				ThisSize = size.Value;
			}
			else
			{
				ThisSize = Fields.Sum(pf => pf.FieldType.TotalSize);
			}
		}

		public override string ToString() => $"{FullName} [Processed]";
	}
}
