using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VCSFramework.Assembly;
using static VCSFramework.Assembly.AssemblyFactory;

namespace VCSCompiler
{
    internal sealed class MemoryManager
    {
		private const int MaxRAM = 128;
		private const int GlobalsStart = 0x80;
		private const int StackStart = 0xFF;

		private readonly CompiledProgram Program;
		private int NextGlobal = GlobalsStart;

		public IEnumerable<Symbol> AllSymbols { get; }

		public MemoryManager(CompiledProgram program)
		{
			Program = program;
			AllSymbols = AddPredefinedGlobals().Concat(LayoutGlobals()).Concat(AllocateLocalAddresses(program.CallGraph));
		}

	    private IEnumerable<Symbol> AllocateLocalAddresses(CallGraph callGraph)
	    {
		    foreach (var node in callGraph.AllNodes)
		    {
			    foreach (var parameter in node.Value.Parameters)
			    {
				    yield return DefineSymbol(LabelGenerator.GetFromParameter(parameter), NextGlobal);
					AdvanceNextGlobal(1);
			    }
			    foreach (var local in node.Value.Body.Variables)
			    {
				    yield return DefineSymbol(LabelGenerator.GetFromVariable(node.Value, local), NextGlobal);
					AdvanceNextGlobal(1);
			    }
		    }
	    }

		private IEnumerable<Symbol> AddPredefinedGlobals()
		{
			yield return DefineSymbol(LabelGenerator.TemporaryRegister1, NextGlobal);
			AdvanceNextGlobal(1);
			yield return DefineSymbol(LabelGenerator.TemporaryRegister2, NextGlobal);
			AdvanceNextGlobal(1);
		}

		private IEnumerable<Symbol> LayoutGlobals()
		{
			var staticVariablesByType = Program.Types.SelectMany(t => t.Fields).GroupBy(f => f.FieldDefinition.DeclaringType);
			foreach(var fieldGrouping in staticVariablesByType)
			{
				foreach(var field in fieldGrouping.Where(f => f.FieldDefinition.IsStatic).OrderBy(f => f.Name))
				{
					yield return DefineSymbol(LabelGenerator.GetFromField(field.FieldDefinition), NextGlobal).WithComment($"{field.FieldType.Name} - {field.FieldType.TotalSize} bytes");
					AdvanceNextGlobal(field.FieldType.TotalSize);
				}
			}
		}

		private void AdvanceNextGlobal(int bytes)
		{
			NextGlobal += bytes;
			if (NextGlobal > StackStart)
			{
				throw new FatalCompilationException("Globals collided with stack.");
			}
		}
    }
}
