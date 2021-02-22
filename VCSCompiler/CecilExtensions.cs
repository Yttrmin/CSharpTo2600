#nullable enable
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using VCSFramework;
using VCSFramework;

namespace VCSCompiler
{
    internal static class CecilExtensions
    {
		public static IEnumerable<TypeDefinition> CompilableTypes(this AssemblyDefinition @this)
        {
			var allTypes = @this.MainModule.Types.SelectMany(GetAllTypes);
			return allTypes
				.Where(t => t.BaseType != null) // @TODO - What's this for?
				.Where(t => t.CustomAttributes
					.All(a => a.AttributeType.Name != nameof(DoNotCompileAttribute)));

			static IEnumerable<TypeDefinition> GetAllTypes(TypeDefinition rootType)
            {
				yield return rootType;
				foreach (var nestedType in rootType.NestedTypes)
                {
					foreach (var type in GetAllTypes(nestedType))
                    {
						yield return type;
                    }
                }
            }
        }

		public static IEnumerable<TypeDefinition> CompilableTypes(this IEnumerable<AssemblyDefinition> @this)
        {
			return @this.SelectMany(it => it.CompilableTypes());
        }

		public static IEnumerable<MethodDefinition> CompilableMethods(this TypeDefinition @this)
        {
			return @this.Methods
				.Where(m => m.CustomAttributes
					.All(a => a.AttributeType.Name != nameof(DoNotCompileAttribute)));
        }

		public static IEnumerable<MethodDefinition> CompilableMethods(this IEnumerable<TypeDefinition> @this)
		{
			return @this.SelectMany(it => it.CompilableMethods());
		}

		public static IEnumerable<FieldDefinition> InstanceFields(this TypeDefinition @this)
			=> @this.Fields.Where(field => !field.IsStatic);

		public static IEnumerable<FieldDefinition> StaticFields(this TypeDefinition @this)
			=> @this.Fields.Where(field => field.IsStatic);

		public static bool TryGetFrameworkAttribute<T>(
			this ParameterDefinition @this,
			[NotNullWhen(true)] out T? result) where T : Attribute
		=> TryGetFrameworkAttribute(@this.CustomAttributes, out result);

		public static bool TryGetFrameworkAttribute<T>(
			this MethodReturnType @this,
			[NotNullWhen(true)] out T? result) where T : Attribute
		=> TryGetFrameworkAttribute(@this.CustomAttributes, out result);

		public static bool TryGetFrameworkAttribute<T>(
			this MethodDefinition @this,
			[NotNullWhen(true)] out T? result) where T : Attribute
		=> TryGetFrameworkAttribute(@this.CustomAttributes, out result);

		public static bool TryGetFrameworkAttribute<T>(
			this FieldDefinition @this,
			[NotNullWhen(true)] out T? result) where T : Attribute
		=> TryGetFrameworkAttribute(@this.CustomAttributes, out result);

		private static bool TryGetFrameworkAttribute<T>(
			Collection<CustomAttribute> attributes,
			[NotNullWhen(true)] out T? result) where T : Attribute
		{
			var type = typeof(T);
			var attribute = attributes.
				SingleOrDefault(a => a.AttributeType.FullName == type.FullName);
			if (attribute != null)
			{
				var constructorArguments = attribute.ConstructorArguments.Select(a => a.Value).Select(Process).ToArray();
				var newAttribute = (T?)Activator.CreateInstance(type, constructorArguments);
				if (newAttribute == null)
					throw new InvalidOperationException($"Could not recreate attribute of type: {type.FullName}");
				result = newAttribute;
				return true;
			}
			else
			{
				result = default;
				return false;
			}

			static object Process(object obj)
			{
				// Attributes that take Types (e.g. [MyAttr(typeof(string))]) end up as TypeDefinitions, so we can't just
				// pass them into the constructor. Look them up instead.
				if (obj is TypeDefinition typeDefinition)
				{
					// @TODO - Probably shouldn't be limited to just Framework Macros.
					return typeof(IAssemblyEntry).GetTypeInfo().Assembly.GetTypes().Single(t => t.FullName == typeDefinition.FullName);
				}
				return obj;
			}
		}

		/// <summary>Returns TRUE if <paramref name="this"/> calls <paramref name="target"/> through itself or any other callees.</summary>
		public static bool Calls(this MethodDefinition @this, MethodDefinition target)
        {
			var explored = new HashSet<string>();
			return CallsRecursive(@this, target);

			bool CallsRecursive(MethodDefinition source, MethodDefinition target)
            {
				explored.Add(source.FullName);
				foreach (var instruction in source.Body.Instructions.Where(i => i.OpCode == OpCodes.Call))
                {
					var calleeMethod = ((MethodReference)instruction.Operand).Resolve();
					if (calleeMethod.FullName == target.FullName)
						return true;

					if (!explored.Contains(calleeMethod.FullName) && CallsRecursive(calleeMethod, target))
						return true;
                }
				return false;
			}
        }

		public static bool IsRecursive(this MethodDefinition @this)
        {
			var explored = new HashSet<string>();
			return IsRecursiveInternal(@this, @this);

			bool IsRecursiveInternal(MethodDefinition method, MethodDefinition sourceMethod)
            {
				explored.Add(method.FullName);
				foreach (var instruction in method.Body.Instructions.Where(i => i.OpCode == OpCodes.Call))
                {
					// @TODO - Don't know if Resolve() is better versus looking it up ourselves.
					var calleeMethod = ((MethodReference)instruction.Operand).Resolve();
					if (calleeMethod.FullName == sourceMethod.FullName)
						return true;

					if (!explored.Contains(calleeMethod.FullName) && IsRecursiveInternal(calleeMethod, sourceMethod))
						return true;
                }
				return false;
            }
        }

		public static string NamespaceAndName(this TypeReference @this)
		{
			var formattedNamespace = @this.Namespace.Replace('.', '_');
			var formattedName = @this.Name.Replace("`", "_");
			var formattedArgs = "";
			if (@this is GenericInstanceType genericInstanceType)
            {
				// Symbols aren't really supported in labels so going with GenericsStart, GenericsNext, GenericsEnd (abbreviated).
				formattedArgs = $"_GS_{string.Join("_GN_", genericInstanceType.GenericArguments.Select(a => a.NamespaceAndName()))}_GE";
            }
			return $"{formattedNamespace}_{formattedName}{formattedArgs}";
		}

		/// <summary>Replaces chars in a method name that the assembler doesn't like.</summary>
		public static string SafeName(this MethodDef @this)
			// Example: A nested function called "FibonacciInternal" inside a normal method called "Fibonacci": <Fibonacci>g__FibonacciInternal|7_0
			// I'm just doing al=AngleLeft, ar=AngleRight, p=Pipe
			=> @this.Name.Replace("<", "_al_").Replace(">", "_ar_").Replace("|", "_p_");

		public static dynamic InvokeRomDataGenerator(this Assembly userAssembly, MethodDefinition generator)
		{
			var compiledMethod = userAssembly.Modules.Single().ResolveMethod(generator.MetadataToken.ToInt32()) ?? throw new InvalidOperationException($"Failed to lookup RomData generator '{generator.FullName}' in user assembly '{userAssembly}'.");
			return (dynamic)(compiledMethod.Invoke(null, null) ?? throw new InvalidOperationException($"Return value of RomData generator '{generator.FullName}' was NULL."));
		}

		public static string NamespaceAndName(this TypeRef @this) => ((TypeReference)@this).NamespaceAndName();

		// @TODO - Probably move to another file, or make this one more generic.
		public static IEnumerable<Instruction> Concat(this Instruction? @this, Instruction? other)
			=> new[] { @this, other }.OfType<Instruction>();

		public static IEnumerable<Instruction> Concat(this Instruction? @this, IEnumerable<Instruction> other)
			=> new[] { @this }.OfType<Instruction>().Concat(other);

		public static IEnumerable<Instruction> Concat(this IEnumerable<Instruction> @this, Instruction? other)
			=> @this.Append(other).OfType<Instruction>();
	}
}
