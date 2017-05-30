using System;
using VCSCompiler;

namespace VCSCompilerCLI
{
    class Program
    {
        static void Main(string[] args)
        {
			var filePath = args[0];
			var dasmPath = args[1];
			Console.WriteLine($"Beginning compilation of {filePath}");
			var result = Compiler.CompileFromFiles(new[] { filePath }, dasmPath).Result;
			Console.WriteLine(result ? "Success!" : "Compilation failed");
#if DEBUG
			Console.ReadLine();
#endif
		}
    }
}