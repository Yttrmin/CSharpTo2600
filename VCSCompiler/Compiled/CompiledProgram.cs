using System;
using System.Collections.Generic;
using System.Text;

namespace VCSCompiler
{
    internal sealed class CompiledProgram
    {
		private readonly IEnumerable<CompiledType> Types;

		public CompiledProgram(IEnumerable<CompiledType> types)
		{
			Types = types;
		}
    }
}
