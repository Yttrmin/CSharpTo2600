using System;
using VCSCompiler;

namespace VCSCompilerCLI
{
    class Program
    {
        static void Main(string[] args)
        {
			var filePath = args[0];
			var frameworkPath = args[1];
			var dasmPath = args[2];
			Console.WriteLine($"Beginning compilation of {filePath}");
			var result = Compiler.CompileFromFiles(new[] { filePath }, frameworkPath, dasmPath).Result;
			Console.WriteLine("Compilation complete.");
#if DEBUG
			Console.ReadLine();
#endif
		}
    }
}