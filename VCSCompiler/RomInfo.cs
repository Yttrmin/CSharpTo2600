using System;
using System.Collections.Generic;
using System.Text;

namespace VCSCompiler
{
    public sealed class RomInfo
    {
		public string RomPath { get; }
		public string AssemblyPath { get; }
		public string SymbolsPath { get; }
		public string ListPath { get; }

		internal RomInfo(string romPath, string assemblyPath, string symbolsPath, string listPath)
		{
			RomPath = romPath;
			AssemblyPath = assemblyPath;
			SymbolsPath = symbolsPath;
			ListPath = listPath;
		}
    }
}
