using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace VCSCompiler
{
    internal sealed class CompiledType : ProcessedType
    {
		public new IEnumerable<CompiledSubroutine> Subroutines => (IEnumerable<CompiledSubroutine>)base.Subroutines;

		public CompiledType(ProcessedType processedType, IEnumerable<CompiledSubroutine> compiledSubroutines)
			: base(processedType, compiledSubroutines)
		{
			if (compiledSubroutines.Count() != processedType.Subroutines.Count())
			{
				throw new FatalCompilationException($"Can not create CompiledType '{processedType.FullName}' with {compiledSubroutines.Count()} CompiledSubroutines because it has {processedType.Subroutines.Count()} ProcessedSubroutines!");
			}
		}

		public override string ToString() => $"{FullName} [Compiled]";
	}
}
