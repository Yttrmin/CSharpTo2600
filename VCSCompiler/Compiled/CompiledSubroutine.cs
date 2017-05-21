using System;
using System.Collections.Generic;
using System.Text;

namespace VCSCompiler
{
    internal sealed class CompiledSubroutine : ProcessedSubroutine
    {
		public IEnumerable<AssemblyLine> Body;

		public CompiledSubroutine(ProcessedSubroutine processedSubroutine, IEnumerable<AssemblyLine> body)
			: base(processedSubroutine)
		{
			Body = body;
		}

		public override string ToString() => $"{FullName} [Compiled]";
	}
}
