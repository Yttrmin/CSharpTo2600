using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Linq;
using System.IO;
using VCSFramework;
using VCSFramework.Assembly;
using static VCSFramework.Assembly.AssemblyFactory;

namespace VCSCompiler
{
    internal static class RomCreator
    {
		// @TODO - Name
		private const string EntryPoint = "aaEntryPoint";
		private const string AssemblyFileName = "out.asm";
		private const string BinaryFileName = "out.bin";
		private const string SymbolsFileName = "out.sym";
		private const string ListFileName = "out.lst";

	    public static RomInfo CreateRom(CompiledProgram program, string assemblerPath)
		{
			var memoryManager = new MemoryManager(program);
			var lines = new List<AssemblyLine>();
			lines.AddRange(CreateHeader());
			lines.AddRange(CreateStaticVariables(program.Types, memoryManager));
			lines.AddRange(CreateEntryPoint(program.EntryPoint));
			lines.AddRange(CreateMethods(program));
			lines.AddRange(CreateInterruptVectors());

			File.WriteAllLines(AssemblyFileName, lines.Select(l => l.ToString()));
			var assembled = AssembleOutput(assemblerPath);
			return new RomInfo(assembled,
				Path.GetFullPath(BinaryFileName),
				Path.GetFullPath(AssemblyFileName),
				Path.GetFullPath(SymbolsFileName),
				Path.GetFullPath(ListFileName));
		}

		private static IEnumerable<AssemblyLine> CreateHeader()
		{
			yield return Comment("Beginning of compiler-generated source file.", 0);
			yield return Comment($"Date of generation: {DateTime.UtcNow}", 0);
			yield return Processor();
			yield return Format();
			yield return Include("vcs.h");
			yield return Org(0xF000);
			yield return BlankLine();
		}

		private static IEnumerable<AssemblyLine> CreateStaticVariables(IEnumerable<CompiledType> types, MemoryManager memoryManager)
		{
			yield return Comment("Global variables:", 0);
			foreach(var symbol in memoryManager.AllSymbols)
			{
				yield return symbol;
			}
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
			yield return EndBlock();
			yield return Comment("End entry point code.", 0);
			yield return BlankLine();
		}

		private static IEnumerable<AssemblyLine> CreateMethods(CompiledProgram program)
		{
			var nodes = program.CallGraph.AllNodes;
			var methods = program.Types.SelectMany(t => t.Subroutines)
				.Where(s => s != program.EntryPoint)
				.Where(s => s.Body.Any()) // Don't emit empty methods.
				.Where(s => nodes.Any(n => n.Value == s.MethodDefinition)); // Don't emit methods that are never called.
			yield return Comment("Begin subroutine emit.", 0);
			yield return BlankLine();
			foreach(var method in methods)
			{
				// Do not emit subroutines that will never be JSR'd.
				if (method.TryGetFrameworkAttribute<AlwaysInlineAttribute>(out _))
				{
					continue;
				}
				foreach(var line in CreateMethod(method))
				{
					yield return line;
				}
			}
			yield return Comment("End subroutine emit.", 0);
			yield return BlankLine();

			IEnumerable<AssemblyLine> CreateMethod(CompiledSubroutine subroutine)
			{
				yield return Comment(subroutine.MethodDefinition.ToString(), 0);
				yield return Label(LabelGenerator.GetFromMethod(subroutine.MethodDefinition));
				foreach(var line in subroutine.Body)
				{
					yield return line;
				}
				yield return BlankLine();
			}
		}

		private static IEnumerable<AssemblyLine> CreateInterruptVectors()
		{
			yield return Comment("Interrupt vectors:", 0);
			yield return Org(0xFFFC);
			yield return Word(EntryPoint);
			yield return Word(EntryPoint);
		}

	    private static bool AssembleOutput(string assemblerPath)
	    {
			// @TODO - Obviously some remaining work to properly support this and 
			// clean up all the DASM assumptions.
			var args = new string[]
			{
				Path.GetFullPath(Path.GetFullPath(AssemblyFileName)),
				$"--output={BinaryFileName}",
				$"--list={ListFileName}"
			};
			Core6502DotNet.Core6502DotNet.Main(args);
			return false;
	    }
	}
}
