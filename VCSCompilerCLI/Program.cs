﻿#nullable enable
using System;
using System.Linq;
using VCSCompiler.V2;

namespace VCSCompilerCLI
{
    class Program
    {
		/// <summary>
		/// 
		/// </summary>
		/// <param name="outputPath">The path to save the compiled binary to. The same path with a different extension will be used for related files.
		/// If a path is not provided, no files will be saved.</param>
		/// <param name="frameworkPath">The path to the VCSFramework DLL to compile against. 
		/// Defaults to looking in the same directory as this EXE.</param>
		/// <param name="emulatorPath">Path of the emulator executable. 
		/// If provided, it will be launched with the path to the output binary passed as an argument.</param>
		/// <param name="textEditorPath">Path of the text edit executable.
		/// If provided, it will be launched with the path to the output ASM file passed as an argument.</param>
		/// <param name="sourceAnnotations">Whether to include C#, CIL, neither, or both source lines as comments
		/// above the macros that they were compiled to.</param>
		/// <param name="arguments">A list of C# source files to compile.</param>
		static void Main(
			string[] arguments,
			string? outputPath = null, 
			string frameworkPath = "./VCSFramework.dll",
			string? emulatorPath = null,
			string? textEditorPath = null,
			SourceAnnotation sourceAnnotations = SourceAnnotation.CSharp
			)
        {
			var options = new CompilerOptions
			{
				OutputPath = outputPath,
				FrameworkPath = frameworkPath,
				EmulatorPath = emulatorPath,
				TextEditorPath = textEditorPath,
				SourceAnnotations = sourceAnnotations
			};
			var file = arguments.SingleOrDefault() ?? throw new ArgumentException("Missing file");
			var result = Compiler.CompileFromFile(file, options);
			//var result = Compiler.CompileFromFiles(new[] { filePath }, frameworkPath, dasmPath).Result;
#if DEBUG
			Console.ReadLine();
#endif
		}
    }
}