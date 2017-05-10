using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VCSCompiler
{
    public static class Compiler
    {
		public async static Task<bool> CompileFromFiles(IEnumerable<string> filePaths)
		{
			var compilation = await CompilationCreator.CreateFromFilePaths(filePaths);
			return true;
		}
    }
}
