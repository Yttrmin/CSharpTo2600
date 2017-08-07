using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace VCSCompiler
{
    internal sealed class CompiledProgram
    {
		public IEnumerable<CompiledAssembly> Assemblies { get; }
		public CallGraph CallGraph { get; }
		public IEnumerable<CompiledType> Types => Assemblies.SelectMany(a => a.Types);
		//TODO - Support multiple assemblies with entry points (marker in CompiledAssembly?)
		public CompiledSubroutine EntryPoint => Assemblies.Select(a => a.EntryPoint).Single(s => s != null);

		public CompiledProgram(IEnumerable<CompiledAssembly> assemblies, CallGraph callGraph)
		{
			Assemblies = assemblies;
			CallGraph = callGraph;
		}
    }
}
