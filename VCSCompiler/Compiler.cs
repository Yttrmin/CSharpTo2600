using Microsoft.CodeAnalysis.CSharp;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace VCSCompiler
{
    public sealed class Compiler
    {
		private Compiler()
		{
			
		}

		public async static Task<bool> CompileFromFiles(IEnumerable<string> filePaths)
		{
			var compilation = await CompilationCreator.CreateFromFilePaths(filePaths);
			MemoryStream assemblyStream;
			var assemblyDefinition = GetAssemblyDefinition(compilation, out assemblyStream);

			using (assemblyStream)
			{
				var types = assemblyDefinition.MainModule.Types.Where(t => t.BaseType != null);
				var entryType = assemblyDefinition.MainModule.EntryPoint.DeclaringType;
				return true;
			}
		}

		private static void CompileType(TypeDefinition type)
		{
			throw new NotImplementedException();
		}

		private static AssemblyDefinition GetAssemblyDefinition(CSharpCompilation compilation, out MemoryStream assemblyStream)
		{
			assemblyStream = new MemoryStream();
			
			var result = compilation.Emit(assemblyStream);
			if (!result.Success)
			{
				foreach (var diagnostic in result.Diagnostics)
				{
					Console.WriteLine(diagnostic.ToString());
				}
				throw new FatalCompilationException("Failed to emit compiled assembly.");
			}
			assemblyStream.Position = 0;

			return AssemblyDefinition.ReadAssembly(assemblyStream);
		}
    }
}
