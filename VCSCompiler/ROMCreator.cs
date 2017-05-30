using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using VCSCompiler.Assembly;
using static VCSCompiler.Assembly.AssemblyFactory;

namespace VCSCompiler
{
    internal sealed class RomCreator
    {
		private const string EntryPoint = "__EntryPoint";
		private const string AssemblyFileName = "out.asm";
		private const string BinaryFileName = "out.bin";
		private const string SymbolsFileName = "out.sym";
		private const string ListFileName = "out.lst";

		private RomCreator() { }

		public static RomInfo CreateRom(CompiledProgram program)
		{
			var lines = new List<AssemblyLine>();
			lines.AddRange(CreateHeader());
			lines.AddRange(CreateEntryPoint(program.EntryPoint));
			lines.AddRange(CreateInterruptVectors());

			File.WriteAllLines(AssemblyFileName, lines.Select(l => l.ToString()));
			throw new NotImplementedException();
		}

		private static IEnumerable<AssemblyLine> CreateHeader()
		{
			yield return Comment("Beginning of compiler-generated source file.", 0);
			yield return Processor();
			yield return Include("vcs.h");
			yield return Org(0xF000);
			yield return BlankLine();
		}

		private static IEnumerable<AssemblyLine> CreateEntryPoint(CompiledSubroutine entryPoint)
		{
			yield return Comment($"Entry point '{entryPoint.FullName}':", 0);
			yield return Subroutine(EntryPoint);
			foreach(var line in entryPoint.Body)
			{
				yield return line;
			}
			yield return Comment("End entry point code.", 0);
			yield return BlankLine();
		}

		private static IEnumerable<AssemblyLine> CreateInterruptVectors()
		{
			yield return Comment("Interrupt vectors:", 0);
			yield return Org(0xFFFC);
			yield return Word(EntryPoint);
		}
	}
}
