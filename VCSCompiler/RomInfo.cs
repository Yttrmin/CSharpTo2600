using System;
using System.Collections.Generic;
using System.Text;

namespace VCSCompiler
{
    public sealed class RomInfo
    {
		public bool IsSuccessful { get; }
		public string RomPath { get; }
		public string AssemblyPath { get; }
		public string SymbolsPath { get; }
		public string ListPath { get; }

		internal RomInfo(bool isSuccessful, string romPath, string assemblyPath, string symbolsPath, string listPath)
		{
			IsSuccessful = isSuccessful;
			RomPath = romPath;
			AssemblyPath = assemblyPath;
			SymbolsPath = symbolsPath;
			ListPath = listPath;
		}
    }
}
