#nullable enable
using System;
using System.Linq;
using System.Text;
using VCSCompiler.V2;

namespace VCSCompilerCLI
{
    class Program
    {
		/// <summary>
		/// 
		/// </summary>
		/// /// <param name="arguments">A list of C# source files to compile.</param>
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
		/// <param name="disableOptimizations">True to disable optimizations. Main use is to observe output of primitive
		/// VIL macros and stack operations. Unoptimized code generally will not run correctly due to excessive cycles consumed.</param>
		static int Main(
			string[] arguments,
			string? outputPath = null,
			string? emulatorPath = null,
			string? textEditorPath = null,
			bool disableOptimizations = false,
			SourceAnnotation sourceAnnotations = SourceAnnotation.CSharp
			)
        {
			var options = new CompilerOptions
			{
				OutputPath = outputPath,
				EmulatorPath = emulatorPath,
				TextEditorPath = textEditorPath,
				DisableOptimizations = disableOptimizations,
				SourceAnnotations = sourceAnnotations
			};
			var file = arguments.SingleOrDefault() ?? throw new ArgumentException("Missing file");
			var result = Compiler.CompileFromFile(file, options);
			var builder = new StringBuilder();
			builder.AppendLine($"  {nameof(RomInfo.IsSuccessful)}: {result.IsSuccessful}");
			builder.AppendLine($"  {nameof(RomInfo.RomPath)}: {result.RomPath}");
			builder.AppendLine($"  {nameof(RomInfo.ListPath)}: {result.ListPath}");
			builder.AppendLine($"  {nameof(RomInfo.AssemblyPath)}: {result.AssemblyPath}");
			Console.WriteLine("Result:");
			Console.WriteLine(builder.ToString());

			return result.IsSuccessful ? 0 : 1;
		}
    }
}