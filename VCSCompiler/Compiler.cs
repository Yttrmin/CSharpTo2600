using Microsoft.CodeAnalysis.CSharp;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;
using System.Collections.Immutable;

namespace VCSCompiler
{
    public sealed class Compiler
    {
		private readonly IDictionary<string, ProcessedType> Types;
		private readonly IEnumerable<Type> FrameworkAttributes;

		private Compiler(IEnumerable<Type> frameworkAttributes)
		{
			Types = new Dictionary<string, ProcessedType>();
			FrameworkAttributes = frameworkAttributes;
		}

		public async static Task<bool> CompileFromFiles(IEnumerable<string> filePaths, string frameworkPath, string dasmPath)
		{
			var compilation = await CompilationCreator.CreateFromFilePaths(filePaths);
			var assemblyDefinition = GetAssemblyDefinition(compilation, out var assemblyStream);
			var frameworkAssembly = System.Reflection.Assembly.Load(new AssemblyName(Path.GetFileNameWithoutExtension(frameworkPath)));
			var frameworkAttributes = frameworkAssembly.ExportedTypes.Where(t => t.GetTypeInfo().BaseType == typeof(Attribute));
			var compiler = new Compiler(frameworkAttributes.ToArray());
			compiler.AddPredefinedTypes();
			var frameworkCompiledAssembly = CompileAssembly(compiler, AssemblyDefinition.ReadAssembly(frameworkPath));
			var userCompiledAssembly = CompileAssembly(compiler, assemblyDefinition);
			var program = new CompiledProgram(new[] { frameworkCompiledAssembly, userCompiledAssembly });
			var romInfo = RomCreator.CreateRom(program);
			return true;
		}

		private static CompiledAssembly CompileAssembly(Compiler compiler, AssemblyDefinition assemblyDefinition)
		{
			// Compilation steps (WIP):
			// 1. Iterate over every type and collect basic information (Processsed*).
			var types = assemblyDefinition.MainModule.Types
				.Where(t => t.BaseType != null)
				.Where(t => !t.CustomAttributes.Any(a => a.AttributeType.Name == "DoNotCompileAttribute"));
			// TODO - Pass immutable copies of Types around instead of all mutating the field?
			compiler.ProcessTypes(types);
			// TODO - Do the above so we don't have to ToArray() to get around the fact we modify Types.
			var compiledTypes = compiler.CompileTypes(compiler.Types.Values.Where(x => x.GetType() == typeof(ProcessedType))).ToArray();
			foreach(var compiledType in compiledTypes.Select(t => (FullName: t.FullName, Type: t)))
			{
				compiler.Types[compiledType.FullName] = compiledType.Type;
			}
			var cilEntryPoint = assemblyDefinition.MainModule.EntryPoint;
			var cilEntryType = cilEntryPoint?.DeclaringType;
			var entryPoint = cilEntryPoint != null ? (CompiledSubroutine)compiler.Types[cilEntryType.FullName].Subroutines.Single(sub => sub.MethodDefinition == cilEntryPoint) : null;
			return new CompiledAssembly(compiledTypes, entryPoint);
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

		private void AddPredefinedTypes()
		{
			var system = AssemblyDefinition.ReadAssembly(typeof(object).GetTypeInfo().Assembly.Location);
			var supportedTypes = new[] { "Object", "ValueType", "Void", "Byte" };
			var types = system.Modules[0].Types.Where(td => supportedTypes.Contains(td.Name));

			var objectType = types.Single(x => x.Name == "Object");
			var objectCompiled = new CompiledType(new ProcessedType(objectType, null, Enumerable.Empty<ProcessedField>(), Enumerable.Empty<ProcessedSubroutine>(), 0), Enumerable.Empty<CompiledSubroutine>());
			Types[objectType.FullName] = objectCompiled;

			var valueType = types.Single(x => x.Name == "ValueType");
			var valueTypeCompiled = new CompiledType(new ProcessedType(valueType, objectCompiled, Enumerable.Empty<ProcessedField>(), Enumerable.Empty<ProcessedSubroutine>(), 0), Enumerable.Empty<CompiledSubroutine>());
			Types[valueType.FullName] = valueTypeCompiled;

			var voidType = types.Single(x => x.Name == "Void");
			var voidCompiled = new CompiledType(new ProcessedType(voidType, valueTypeCompiled, Enumerable.Empty<ProcessedField>(), Enumerable.Empty<ProcessedSubroutine>(), 0), Enumerable.Empty<CompiledSubroutine>());
			Types[voidType.FullName] = voidCompiled;

			var byteType = types.Single(x => x.Name == "Byte");
			var byteCompiled = new CompiledType(new ProcessedType(byteType, valueTypeCompiled, Enumerable.Empty<ProcessedField>(), Enumerable.Empty<ProcessedSubroutine>(), 1), Enumerable.Empty<CompiledSubroutine>());
			Types[byteType.FullName] = byteCompiled;
		}

		/// <summary>
		/// Gives the compiler a complete list of types to compile.
		/// If a type relies on another type not in this list, this method will throw.
		/// </summary>
		private void ProcessTypes(IEnumerable<TypeDefinition> cecilTypes)
		{
			// TODO - Probably should just build up a dependency graph instead of wastefully reprocessing types.
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
			if (!Types.ContainsKey(typeDefinition.BaseType.FullName))
			{
				throw new FatalCompilationException($"Type '{typeDefinition.FullName}' has a base type of an unknown type: '{typeDefinition.BaseType.FullName}'");
			}
			else if (Types[typeDefinition.BaseType.FullName] == null)
			{
				return null;
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
				foreach(var parameter in method.Parameters)
				{
					if (Types[parameter.ParameterType.FullName] == null)
					{
						return null;
					}
				}
				if (Types[method.ReturnType.FullName] == null)
				{
					return null;
				}
			}

			var baseType = Types[typeDefinition.BaseType.FullName];
			var processedFields = typeDefinition.Fields.Select(fd => new ProcessedField(fd, Types[fd.FieldType.FullName]));
			var processedSubroutines = typeDefinition.Methods.Select(md => new ProcessedSubroutine(md, Types[md.ReturnType.FullName],
					md.Parameters.Select(p => Types[p.ParameterType.FullName]),
					md.CustomAttributes.Where(a => FrameworkAttributes.Any(fa => fa.FullName == a.AttributeType.FullName))
						.Select(a => CreateFrameworkAttribute(a)).ToArray()));

			return new ProcessedType(typeDefinition, baseType, processedFields, processedSubroutines);
		}

		private Attribute CreateFrameworkAttribute(CustomAttribute attribute)
		{
			var type = FrameworkAttributes.Where(t => t.FullName == attribute.AttributeType.FullName).Single();
			var parameters = attribute.ConstructorArguments.Select(a => a.Value).ToArray();
			var instance = (Attribute)Activator.CreateInstance(type, parameters);
			return instance;
		}

		private IEnumerable<CompiledType> CompileTypes(IEnumerable<ProcessedType> processedTypes)
		{
			foreach(var type in processedTypes)
			{
				yield return CompileType(type);
			}
		}

		private CompiledType CompileType(ProcessedType processedType)
		{
			var compiledSubroutines = new List<CompiledSubroutine>();
			foreach(var subroutine in processedType.Subroutines)
			{
				Console.WriteLine($"Compiling {subroutine.FullName}");
				foreach(var line in subroutine.MethodDefinition.Body.Instructions)
				{
					Console.WriteLine(line);
				}
				Console.WriteLine("v  Compile  v");
				var assembly = CilCompiler.CompileBody(subroutine.MethodDefinition.Body.Instructions, Types.ToImmutableDictionary());
				var compiledSubroutine = new CompiledSubroutine(subroutine, assembly);
				compiledSubroutines.Add(compiledSubroutine);
				Console.WriteLine($"{subroutine.FullName}, compilation finished");
			}
			return new CompiledType(processedType, compiledSubroutines);
		}
    }
}
