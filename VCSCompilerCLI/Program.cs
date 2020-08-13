using System;
using VCSCompiler.V2;

namespace VCSCompilerCLI
{
    class Program
    {
        static void Main(string[] args)
        {
			//TODO - Error handling, actual argument parsing, etc.
			var filePath = args[0];
			var frameworkPath = args[1];
			Console.WriteLine($"Beginning compilation of {filePath}");
			var result = Compiler.CompileFromFile(filePath, frameworkPath);
			//var result = Compiler.CompileFromFiles(new[] { filePath }, frameworkPath, dasmPath).Result;
			Console.WriteLine("Compilation complete.");
#if DEBUG
			Console.ReadLine();
#endif
		}
    }
}