using System;
using System.Collections.Generic;

namespace VCSCompiler
{
    public static class Compiler
    {
		public static bool CompileFromFiles(IEnumerable<string> filePaths)
		{
			var compilation = CompilationCreator.CreateFromFilePaths(filePaths);
			return true;
		}
    }
}
