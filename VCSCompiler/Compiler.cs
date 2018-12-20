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
using static VCSFramework.Assembly.AssemblyFactory;

namespace VCSCompiler
{
    public sealed class Compiler
    {
		private readonly TypeMap Types;
		private readonly Assembly FrameworkAssembly;
		private readonly AssemblyDefinition UserAssemblyDefinition;
		private readonly IEnumerable<Type> FrameworkAttributes;

		private Compiler(Assembly frameworkAssembly, AssemblyDefinition userAssemblyDefinition, IEnumerable<Type> frameworkAttributes)
		{
			Types = new TypeMap();
			FrameworkAssembly = frameworkAssembly;
			UserAssemblyDefinition = userAssemblyDefinition;
			FrameworkAttributes = frameworkAttributes;
		}

		public static async Task<RomInfo> CompileFromText(string source, string frameworkPath, string dasmPath)
		{
			// TODO - How does Roslyn react to an empty source file?
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			var tempFileName = Path.GetTempFileName();
			File.WriteAllText(tempFileName, source);
			return await CompileFromFiles(new[] { tempFileName }, frameworkPath, dasmPath);
		}

		public static Task<RomInfo> CompileFromFiles(IEnumerable<string> filePaths, string frameworkPath, string dasmPath)
		{
			if (filePaths == null)
			{
				throw new ArgumentNullException(nameof(filePaths));
			}
			else if (!filePaths.Any())
			{
				throw new ArgumentException("No source files specified.", nameof(filePaths));
			}

			if (frameworkPath == null)
			{
				throw new ArgumentNullException(nameof(frameworkPath));
			}
			else if (string.IsNullOrWhiteSpace(frameworkPath))
			{
				throw new ArgumentException("VCS framework DLL path must be specified.", nameof(frameworkPath));
			}

			// TODO - No DASM path should mean the caller just wants the assembly output, not a compiled binary.

			var compilation = CompilationCreator.CreateFromFilePaths(filePaths);
			var assemblyDefinition = GetAssemblyDefinition(compilation, out var assemblyStream);
			var frameworkAssembly = System.Reflection.Assembly.Load(new AssemblyName(Path.GetFileNameWithoutExtension(frameworkPath)));
			var frameworkAttributes = frameworkAssembly.ExportedTypes.Where(t => t.GetTypeInfo().BaseType == typeof(Attribute));
			var compiler = new Compiler(frameworkAssembly, assemblyDefinition, frameworkAttributes.ToArray());
            try
            {
                var frameworkCompiledAssembly = CompileAssembly(compiler, AssemblyDefinition.ReadAssembly(frameworkPath, new ReaderParameters { ReadSymbols = true }));
                var userCompiledAssembly = CompileAssembly(compiler, assemblyDefinition);
                var callGraph = CallGraph.CreateFromEntryMethod(userCompiledAssembly.EntryPoint);
                var program = new CompiledProgram(new[] { frameworkCompiledAssembly, userCompiledAssembly }, callGraph);
                var romInfo = RomCreator.CreateRom(program, dasmPath);
                return Task.FromResult(romInfo);
            }
            finally
            {
                AuditorManager.Instance.WriteLog(Path.Combine(Directory.GetCurrentDirectory(), "outlog.html"));
            }
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
			// TODO - Come up with determinate order to compile types in, they could have dependencies between each other.
			var compiledTypes = compiler.CompileTypes(compiler.Types.ProcessedTypes).ToArray();
			foreach(var compiledType in compiledTypes.Select(t => (FullName: t.FullName, Type: t)))
			{
				compiler.Types[compiledType.Type] = compiledType.Type;
			}
			var entryPoint = GetEntryPoint() as CompiledSubroutine;
			return new CompiledAssembly(compiledTypes, entryPoint);

            ProcessedSubroutine GetEntryPoint()
            {
                var cilEntryPoint = assemblyDefinition.MainModule.EntryPoint;
                var cilEntryType = cilEntryPoint?.DeclaringType;
                return cilEntryPoint != null ? compiler.Types[cilEntryType].Subroutines.Single(sub => sub.MethodDefinition == cilEntryPoint) : null;
            }
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

		/// <summary>
		/// Gives the compiler a complete list of types to compile.
		/// If a type relies on another type not in this list, this method will throw.
		/// </summary>
		private void ProcessTypes(IEnumerable<TypeDefinition> cecilTypes)
		{
			foreach (var type in cecilTypes)
			{
				if (!TypeChecker.IsValidType(type, Types.ToImmutableTypeMap(), out var typeError))
				{
					throw new FatalCompilationException(typeError);
				}
				var processedType = ProcessType(type);
				Types[type] = processedType;
			}
		}

		private ProcessedType ProcessType(TypeDefinition typeDefinition)
		{
            var auditor = AuditorManager.Instance.GetAuditor(typeDefinition.Name, AuditTag.TypeProcessing);
			var processedFields = typeDefinition.Fields.Select(ProcessField).ToArray();
			var fieldOffsets = ProcessFieldOffsets(processedFields);

			var methods = typeDefinition.CompilableMethods();
			var processedSubroutines = methods.Select(ProcessMethod);

			var baseType = Types[typeDefinition.BaseType];
            
			return new ProcessedType(typeDefinition, baseType, processedFields, fieldOffsets, processedSubroutines.ToImmutableList());

			ProcessedField ProcessField(FieldDefinition field)
			{
                auditor.RecordEntry($"Processing field {field.FieldType.Name} {field.Name}...");
				return new ProcessedField(field, Types[field.FieldType]);
			}

			IImmutableDictionary<ProcessedField, byte> ProcessFieldOffsets(IEnumerable<ProcessedField> fields)
			{
				var instanceFields = fields.Where(pf => !pf.FieldDefinition.IsStatic);
				var offsets = new Dictionary<ProcessedField, byte>();

				var nextOffset = 0;
				foreach(var field in instanceFields)
				{
					offsets.Add(field, (byte)nextOffset);
                    auditor.RecordEntry($"Recorded field offset of {nextOffset} bytes for '{field.Name}'.");
                    nextOffset += field.FieldType.TotalSize;
				}

				return offsets.ToImmutableDictionary();
			}

			ProcessedSubroutine ProcessMethod(MethodDefinition method)
			{
				var parameters = method.Parameters.Select(p => Types[p.ParameterType]).ToList();
				var locals = method.Body.Variables.Select(l => Types[l.VariableType]).ToList();
                var methodAuditor = AuditorManager.Instance.GetAuditor(method.FullName, AuditTag.MethodProcessing);
                var graphAuditor = AuditorManager.Instance.GetAuditor(nameof(ControlFlowGraph), AuditTag.MethodProcessing);
				var controlFlowGraph = ControlFlowGraphBuilder.Build(method, graphAuditor);
                methodAuditor.RecordAuditor(graphAuditor);
                methodAuditor.RecordEntry("Finished processing.");
                auditor.RecordAuditor(methodAuditor);
                
				return new ProcessedSubroutine(
					method,
					controlFlowGraph,
					method == UserAssemblyDefinition.EntryPoint,
					Types[method.ReturnType], 
					parameters,
					locals,
					method.CustomAttributes.Where(a => FrameworkAttributes.Any(fa => fa.FullName == a.AttributeType.FullName)).Select(CreateFrameworkAttribute).ToArray());
			}
		}

		private Attribute CreateFrameworkAttribute(CustomAttribute attribute)
		{
			var type = FrameworkAttributes.Single(t => t.FullName == attribute.AttributeType.FullName);
			var parameters = attribute.ConstructorArguments.Select(a => a.Value).ToArray();
			var instance = (Attribute)Activator.CreateInstance(type, parameters);
			return instance;
		}

		private IEnumerable<CompiledType> CompileTypes(IEnumerable<ProcessedType> processedTypes)
		{
			// ToArray because we're going to be modifying the underlying Types dictionary.
			foreach(var type in processedTypes.ToArray())
			{
				yield return CompileType(type);
			}
		}

		private CompiledType CompileType(ProcessedType processedType)
		{
            var auditor = AuditorManager.Instance.GetAuditor(processedType.FullName, AuditTag.TypeCompiling);
			var compiledSubroutines = new List<CompiledSubroutine>();
			var callGraph = CreateCallGraph(processedType);
			var compilationOrder = callGraph.TopologicalSort();
			foreach(var subroutine in compilationOrder)
			{
                var subroutineAuditor = AuditorManager.Instance.GetAuditor(subroutine.FullName, AuditTag.MethodCompiling);
				if (subroutine.TryGetFrameworkAttribute<UseProvidedImplementationAttribute>(out var providedImplementation))
				{
					var implementationDefinition = processedType.TypeDefinition.Methods.Single(m => m.Name == providedImplementation.ImplementationName);
					var implementation = FrameworkAssembly.GetType(processedType.FullName, true).GetTypeInfo().GetMethod(implementationDefinition.Name, BindingFlags.Static | BindingFlags.NonPublic);
					var compiledBody = (IEnumerable<AssemblyLine>)implementation.Invoke(null, null);
					compiledSubroutines.Add(new CompiledSubroutine(subroutine, compiledBody));
					auditor.RecordEntry($"Implementation provided by '{implementationDefinition.FullName}':{Environment.NewLine}{string.Join(Environment.NewLine, compiledBody)}");
					continue;
				}
                
				IEnumerable<AssemblyLine> body;
				if (subroutine.TryGetFrameworkAttribute<IgnoreImplementationAttribute>(out _))
				{
					body = Enumerable.Empty<AssemblyLine>();
                    auditor.RecordEntry($"Skipping CIL compilation of {subroutine.FullName} due to {nameof(IgnoreImplementationAttribute)}, assuming an empty subroutine body.");
				}
				else
				{
                    var lineAuditor = AuditorManager.Instance.GetAuditor($"[CIL]", AuditTag.MethodCompiling);
                    lineAuditor.RecordEntry(string.Join(Environment.NewLine, subroutine.MethodDefinition.Body.Instructions));
                    subroutineAuditor.RecordAuditor(lineAuditor);

                    var assemblyAuditor = AuditorManager.Instance.GetAuditor($"[6502 Assembly]", AuditTag.MethodCompiling);
					body = CilCompiler.CompileMethod(subroutine.MethodDefinition, Types.ToImmutableTypeMap(), FrameworkAssembly, assemblyAuditor);
                    subroutineAuditor.RecordAuditor(assemblyAuditor);
				}

				if (subroutine.IsEntryPoint)
				{
                    subroutineAuditor.RecordEntry("Injecting entry point code.");
					body = GetEntryPointPrependedCode().Concat(body);
				}

				var compiledSubroutine = new CompiledSubroutine(subroutine, body);
				compiledSubroutines.Add(compiledSubroutine);
				Types[processedType] = Types[processedType].ReplaceSubroutine(subroutine, compiledSubroutine);
                auditor.RecordAuditor(subroutineAuditor);
			}
			return new CompiledType(processedType, compiledSubroutines.ToImmutableList());
		}

		private ImmutableGraph<ProcessedSubroutine> CreateCallGraph(ProcessedType processedType)
		{
			var graph = ImmutableGraph<ProcessedSubroutine>.Empty;

			foreach (var subroutine in processedType.Subroutines)
			{
				graph = graph.AddNode(subroutine);
			}

			foreach (var subroutine in processedType.Subroutines)
			{
				var calls = subroutine.MethodDefinition.Body.Instructions
								.Where(i => i.OpCode == OpCodes.Call)
								.Select(i => i.Operand)
								.OfType<MethodDefinition>()
								.Where(md => processedType.Subroutines.Any(s => s.MethodDefinition == md))
								.Select(md => processedType.Subroutines.Single(s => s.MethodDefinition == md));

				foreach (var call in calls)
				{
					graph = graph.AddEdge(subroutine, call);
				}
			}

			return graph;
		}

		private IEnumerable<AssemblyLine> GetEntryPointPrependedCode()
		{
			yield return Comment("Begin injected entry point code.");

			// Clear processor flags, intialize stack pointer to 0xFF.
			yield return SEI();
			yield return CLD();
			yield return LDX(0xFF);
			yield return TXS();

			// Initialize all memory to 0s (and skip RTS).
			var clearMemoryLines = Memory.ClearMemoryInternal().ToArray();
			foreach (var line in clearMemoryLines.Take(clearMemoryLines.Length - 1))
			{
				yield return line;
			}
			yield return Comment("End injected entry point code.");
		}
	}
}
