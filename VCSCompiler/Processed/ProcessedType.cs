using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Collections.Immutable;

namespace VCSCompiler
{
    internal class ProcessedType
    {
		public string Name => TypeDefinition.Name;
		public string FullName => TypeDefinition.FullName;
		public ProcessedType BaseType { get; }
		public IEnumerable<ProcessedField> Fields { get; }
		/// <summary>
		/// Instance field byte offsets.
		/// </summary>
		public IImmutableDictionary<ProcessedField, byte> FieldOffsets;
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
		/// <summary>
		/// Whether variables of this type are allowed.
		/// </summary>
		public bool AllowedAsLValue { get; }
		public bool SystemType => TypeDefinition.Namespace.StartsWith("System");

		protected ProcessedType(ProcessedType processedType, IEnumerable<CompiledSubroutine> compiledSubroutines)
			: this(processedType.TypeDefinition, processedType.BaseType, processedType.Fields, processedType.FieldOffsets, compiledSubroutines, processedType.ThisSize, processedType.AllowedAsLValue)
		{ }

		public ProcessedType(
			TypeDefinition typeDefinition, 
			ProcessedType baseType,
			IEnumerable<ProcessedField> fields,
			IImmutableDictionary<ProcessedField, byte> fieldOffsets,
			IEnumerable<ProcessedSubroutine> subroutines, 
			int? size = null, 
			bool allowedAsLValue = true)
		{
			BaseType = baseType;
			TypeDefinition = typeDefinition;
			Fields = fields;
			FieldOffsets = fieldOffsets;
			Subroutines = subroutines;
			AllowedAsLValue = allowedAsLValue;
			ThisSize = size ?? Fields.Sum(pf => pf.FieldType.TotalSize);
		}

		public ProcessedType ReplaceSubroutine(ProcessedSubroutine oldSubroutine, CompiledSubroutine newSubroutine)
		{
			var finalSubroutines = new List<ProcessedSubroutine>(Subroutines);
			var oldSubroutineToRemove = finalSubroutines.Single(s => s.MethodDefinition == oldSubroutine.MethodDefinition);
			finalSubroutines.Remove(oldSubroutineToRemove);
			finalSubroutines.Add(newSubroutine);

			return new ProcessedType(
				TypeDefinition,
				BaseType,
				Fields,
				FieldOffsets,
				finalSubroutines,
				ThisSize,
				AllowedAsLValue);
		}

		public override string ToString() => $"{FullName} [Processed]";
	}
}
