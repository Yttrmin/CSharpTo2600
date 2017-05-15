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
		private readonly IDictionary<string, ProcessedType> Types;

		private Compiler()
		{
			Types = new Dictionary<string, ProcessedType>();
		}

		public async static Task<bool> CompileFromFiles(IEnumerable<string> filePaths)
		{
			var compilation = await CompilationCreator.CreateFromFilePaths(filePaths);
			var assemblyDefinition = GetAssemblyDefinition(compilation, out var assemblyStream);
			var compiler = new Compiler();
			var program = CompileAssembly(compiler, assemblyDefinition);
			return true;
		}

		private static CompiledProgram CompileAssembly(Compiler compiler, AssemblyDefinition assemblyDefinition)
		{
			// Compilation steps (WIP):
			// 1. Iterate over every type and collect basic information (Processsed*).
			var types = assemblyDefinition.MainModule.Types.Where(t => t.BaseType != null);
			var entryType = assemblyDefinition.MainModule.EntryPoint.DeclaringType;
			// TODO - Process predefined types before user-defined types.
			compiler.ProcessTypes(types);
			return null;
		}

		private static CompiledType CompileType(TypeDefinition type)
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

		/// <summary>
		/// Gives the compiler a complete list of types to compile.
		/// If a type relies on another type not in this list, this method will throw.
		/// </summary>
		private void ProcessTypes(IEnumerable<TypeDefinition> cecilTypes)
		{
			var toProcessFields = new Queue<TypeDefinition>(cecilTypes);

			foreach (var type in cecilTypes)
			{
				Types[type.FullName] = null;
			}
			while (toProcessFields.Count > 0)
			{
				var type = toProcessFields.Dequeue();
				var processedType = ProcessType(type);
				if (processedType == null)
				{
					toProcessFields.Enqueue(type);
				}
				else
				{
					Types[type.FullName] = processedType;
				}
			}
		}

		private ProcessedType ProcessType(TypeDefinition typeDefinition)
		{
			if (!(typeDefinition.IsValueType || (typeDefinition.IsAbstract && typeDefinition.IsSealed)))
			{
				throw new FatalCompilationException($"Type '{typeDefinition.FullName}' must be a value type or static reference type.");
			}
			foreach(var field in typeDefinition.Fields)
			{
				if (!Types.ContainsKey(field.FieldType.FullName))
				{
					throw new FatalCompilationException($"Field '{field.FullName}' is of an unknown type: {field.FieldType.FullName}");
				}
				if (!field.FieldType.IsValueType)
				{
					throw new FatalCompilationException($"Field '{field.FullName}' can not be a variable of a reference type.");
				}
				if (Types[field.FieldType.FullName] == null)
				{
					return null;
				}
			}
			foreach(var method in typeDefinition.Methods)
			{
				if (!Types.ContainsKey(method.ReturnType.FullName))
				{
					throw new FatalCompilationException($"Method '{method.FullName}' has an unknown return type: {method.ReturnType.FullName}");
				}
				if (method.Parameters.Count != 0)
				{
					throw new FatalCompilationException($"Method '{method.FullName}' takes {method.Parameters.Count} parameters, only parameterless methods are currently supported.");
				}
				if (Types[method.ReturnType.FullName] == null)
				{
					return null;
				}
			}
			return new ProcessedType(typeDefinition,
				typeDefinition.Fields.Select(fd => new ProcessedField(fd, Types[fd.FieldType.FullName])),
				typeDefinition.Methods.Select(md => new ProcessedSubroutine(md, Types[md.ReturnType.FullName], Enumerable.Empty<ProcessedType>())));
		}
    }
}
