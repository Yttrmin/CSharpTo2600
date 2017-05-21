using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Text;

namespace VCSCompiler
{
    internal sealed class CompiledType : ProcessedType
    {
		public CompiledType(ProcessedType processedType)
			: base(processedType)
		{
		}

		public override string ToString() => $"{FullName} [Compiled]";
	}
}
