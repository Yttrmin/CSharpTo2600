using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace VCSCompiler
{
    internal sealed class CompiledProgram
    {
		public IEnumerable<CompiledType> Types { get; }
		public CompiledSubroutine EntryPoint { get; }

		public CompiledProgram(IEnumerable<CompiledType> types, CompiledSubroutine entryPoint)
		{
			Types = types;
			EntryPoint = entryPoint;
		}
    }
}
