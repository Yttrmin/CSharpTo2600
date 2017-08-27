using Microsoft.CodeAnalysis.CSharp;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;
using System.Collections.Immutable;
using VCSFramework;
using Mono.Cecil.Cil;
using VCSFramework.Assembly;

namespace VCSCompiler
{
    public sealed class Compiler
    {
		private readonly IDictionary<string, ProcessedType> Types;
		private readonly System.Reflection.Assembly FrameworkAssembly;
		private readonly IEnumerable<Type> FrameworkAttributes;

		private Compiler(System.Reflection.Assembly frameworkAssembly, IEnumerable<Type> frameworkAttributes)
		{
			Types = new Dictionary<string, ProcessedType>();
			FrameworkAssembly = frameworkAssembly;
			FrameworkAttributes = frameworkAttributes;
		}

		public static async Task<RomInfo> CompileFromFiles(IEnumerable<string> filePaths, string frameworkPath, string dasmPath)
		{
			var compilation = await CompilationCreator.CreateFromFilePaths(filePaths);
			var assemblyDefinition = GetAssemblyDefinition(compilation, out var assemblyStream);
			var frameworkAssembly = System.Reflection.Assembly.Load(new AssemblyName(Path.GetFileNameWithoutExtension(frameworkPath)));
			var frameworkAttributes = frameworkAssembly.ExportedTypes.Where(t => t.GetTypeInfo().BaseType == typeof(Attribute));
			var compiler = new Compiler(frameworkAssembly, frameworkAttributes.ToArray());
			compiler.AddPredefinedTypes();
			var frameworkCompiledAssembly = CompileAssembly(compiler, AssemblyDefinition.ReadAssembly(frameworkPath, new ReaderParameters { ReadSymbols = true }));
			var userCompiledAssembly = CompileAssembly(compiler, assemblyDefinition);
			var callGraph = CallGraph.CreateFromEntryMethod(userCompiledAssembly.EntryPoint);
			var program = new CompiledProgram(new[] { frameworkCompiledAssembly, userCompiledAssembly }, callGraph);
			var romInfo = RomCreator.CreateRom(program, dasmPath);
			return romInfo;
		}

		private static CompiledAssembly CompileAssembly(Compiler compiler, AssemblyDefinition assemblyDefinition)
		{
			// Compilation steps (WIP):
			// 1. Iterate over every type and collect basic information (Processsed*).
			var types = assemblyDefinition.MainModule.Types
				.Where(t => t.BaseType != null)
				.Where(t => t.CustomAttributes.All(a => a.AttributeType.Name != "DoNotCompileAttribute"));
			// TODO - Pass immutable copies of Types around instead of all mutating the field?
			compiler.ProcessTypes(types);
			// TODO - Do the above so we don't have to ToArray() to get around the fact we modify Types.
			var compiledTypes = compiler.CompileTypes(compiler.Types.Values.Where(x => x.GetType() == typeof(ProcessedType))).ToArray();
			foreach(var compiledType in compiledTypes.Select(t => (FullName: t.FullName, Type: t)))
			{
				compiler.Types[compiledType.FullName] = compiledType.Type;
			}
			var entryPoint = GetEntryPoint(compiler, assemblyDefinition) as CompiledSubroutine;
			return new CompiledAssembly(compiledTypes, entryPoint);
		}

	    private static ProcessedSubroutine GetEntryPoint(Compiler compiler, AssemblyDefinition assemblyDefinition)
	    {
			var cilEntryPoint = assemblyDefinition.MainModule.EntryPoint;
		    var cilEntryType = cilEntryPoint?.DeclaringType;
		    var entryPoint = cilEntryPoint != null ? compiler.Types[cilEntryType.FullName].Subroutines.Single(sub => sub.MethodDefinition == cilEntryPoint) : null;
		    return entryPoint;
	    }

		private static AssemblyDefinition GetAssemblyDefinition(CSharpCompilation compilation, out MemoryStream assemblyStream)
		{
			assemblyStream = new MemoryStream();
			
			// TODO - Emit debug symbols so Cecil can use them.
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
			var supportedTypes = new[] { "Object", "ValueType", "Void", "Byte", "Boolean" };
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

			var boolType = types.Single(x => x.Name == "Boolean");
			var boolCompiled = new CompiledType(new ProcessedType(boolType, valueTypeCompiled, Enumerable.Empty<ProcessedField>(), Enumerable.Empty<ProcessedSubroutine>(), 1), Enumerable.Empty<CompiledSubroutine>());
			Types[boolType.FullName] = boolCompiled;
		}

		/// <summary>
		/// Gives the compiler a complete list of types to compile.
		/// If a type relies on another type not in this list, this method will throw.
		/// </summary>
		private void ProcessTypes(IEnumerable<TypeDefinition> cecilTypes)
		{
			foreach (var type in cecilTypes)
			{
				var processedType = ProcessType(type);
				Types[type.FullName] = processedType;
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

			var processedFields = typeDefinition.Fields.Select(ProcessField);

			var methods = typeDefinition.Methods.Where(m => m.CustomAttributes.All(a => a.AttributeType.Name != nameof(DoNotCompileAttribute)));
			var processedSubroutines = methods.Select(ProcessMethod);

			var baseType = Types[typeDefinition.BaseType.FullName];

			return new ProcessedType(typeDefinition, baseType, processedFields, processedSubroutines);

			ProcessedField ProcessField(FieldDefinition field)
			{
				if (!Types.ContainsKey(field.FieldType.FullName))
				{
					throw new FatalCompilationException($"Field '{field.FullName}' is of an unknown type: {field.FieldType.FullName}");
				}
				if (!field.FieldType.IsValueType)
				{
					throw new FatalCompilationException($"Field '{field.FullName}' can not be a variable of a reference type.");
				}
				return new ProcessedField(field, Types[field.FieldType.FullName]);
			}

			ProcessedSubroutine ProcessMethod(MethodDefinition method)
			{
				if (!Types.ContainsKey(method.ReturnType.FullName))
				{
					throw new FatalCompilationException($"Method '{method.FullName}' has an unknown return type: {method.ReturnType.FullName}");
				}

				foreach (var parameter in method.Parameters)
				{
					ProcessParameter(parameter);
				}

				foreach (var local in method.Body.Variables)
				{
					ProcessLocal(local);
				}

				return new ProcessedSubroutine(method, 
					Types[method.ReturnType.FullName], 
					method.Parameters.Select(p => Types[p.ParameterType.FullName]),
					method.CustomAttributes.Where(a => FrameworkAttributes.Any(fa => fa.FullName == a.AttributeType.FullName)).Select(CreateFrameworkAttribute).ToArray());

				void ProcessParameter(ParameterDefinition parameter)
				{
					//TODO - Track in ProcessedType.
				}

				void ProcessLocal(VariableDefinition variable)
				{
					//TODO - Track in ProcessedType.
				}
			}
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
				dynamic providedImplementation = subroutine.FrameworkAttributes.SingleOrDefault(a => a.GetType().FullName == typeof(UseProvidedImplementationAttribute).FullName);
				if (providedImplementation != null)
				{
					var implementationDefinition = processedType.TypeDefinition.Methods.Single(m => m.Name == providedImplementation.ImplementationName);
					var implementation = FrameworkAssembly.GetType(processedType.FullName, true).GetTypeInfo().GetMethod(implementationDefinition.Name, BindingFlags.Static | BindingFlags.NonPublic);
					var compiledBody = (IEnumerable<AssemblyLine>)implementation.Invoke(null, null);
					compiledSubroutines.Add(new CompiledSubroutine(subroutine, compiledBody));
					Console.WriteLine($"Implementation provided by '{implementationDefinition.FullName}':");
					foreach(var line in compiledBody)
					{
						Console.WriteLine(line);
					}
					continue;
				}
				foreach (var line in subroutine.MethodDefinition.Body.Instructions)
				{
					Console.WriteLine(line);
				}
				Console.WriteLine("v  Compile  v");
				IEnumerable<AssemblyLine> body;
				if (subroutine.FrameworkAttributes.Any(a => a.GetType().FullName == typeof(IgnoreImplementationAttribute).FullName))
				{
					body = Enumerable.Empty<AssemblyLine>();
					Console.WriteLine($"Skipping CIL compilation due to {nameof(IgnoreImplementationAttribute)}, assuming an empty subroutine body.");
				}
				else
				{
					body = CilCompiler.CompileMethod(subroutine.MethodDefinition, Types.ToImmutableDictionary());
				}
				var compiledSubroutine = new CompiledSubroutine(subroutine, body);
				compiledSubroutines.Add(compiledSubroutine);
				Console.WriteLine($"{subroutine.FullName}, compilation finished");
			}
			return new CompiledType(processedType, compiledSubroutines);
		}
    }
}
