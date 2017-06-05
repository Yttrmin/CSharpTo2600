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

		private CompiledProgram Program;
		private int NextGlobal = GlobalsStart;

		public IEnumerable<Symbol> AllSymbols { get; }

		public MemoryManager(CompiledProgram program)
		{
			Program = program;
			AllSymbols = LayoutGlobals();
		}

		private IEnumerable<Symbol> LayoutGlobals()
		{
			var staticVariablesByType = Program.Types.SelectMany(t => t.Fields).GroupBy(f => f.FieldDefinition.DeclaringType);
			foreach(var fieldGrouping in staticVariablesByType)
			{
				foreach(var field in fieldGrouping.OrderBy(f => f.Name))
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
