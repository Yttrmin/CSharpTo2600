using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Collections.Immutable;

namespace VCSCompiler
{
    internal sealed class CompiledType : ProcessedType
    {
		public new IImmutableList<CompiledSubroutine> Subroutines => base.Subroutines.Cast<CompiledSubroutine>().ToImmutableList();

		public CompiledType(ProcessedType processedType, IEnumerable<CompiledSubroutine> compiledSubroutines)
			: base(processedType, compiledSubroutines.ToImmutableList())
		{
			if (compiledSubroutines.Count() != processedType.Subroutines.Count())
			{
				throw new FatalCompilationException($"Can not create CompiledType '{processedType.FullName}' with {compiledSubroutines.Count()} CompiledSubroutines because it has {processedType.Subroutines.Count()} ProcessedSubroutines!");
			}
		}

		public override string ToString() => $"{FullName} [Compiled]";
	}
}
