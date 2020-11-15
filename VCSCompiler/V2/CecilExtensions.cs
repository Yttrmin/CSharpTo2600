#nullable enable
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using VCSFramework;
using VCSFramework.V2;

namespace VCSCompiler.V2
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

		public static bool TryGetFrameworkAttribute<T>(
			this MethodDefinition @this, 
			[NotNullWhen(true)] out T? result) where T : Attribute
		{
			var type = typeof(T);
			var attribute = @this.CustomAttributes.
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

		public static bool IsRecursive(this MethodDefinition @this)
        {
			return IsRecursiveInternal(@this, @this);

			static bool IsRecursiveInternal(MethodDefinition method, MethodDefinition sourceMethod)
            {
				foreach (var instruction in method.Body.Instructions.Where(i => i.OpCode == OpCodes.Call))
                {
					// @TODO - Don't know if Resolve() is better versus looking it up ourselves.
					var calleeMethod = ((MethodReference)instruction.Operand).Resolve();
					if (calleeMethod.FullName == sourceMethod.FullName)
						return false;

					if (!IsRecursiveInternal(calleeMethod, sourceMethod))
						return false;
                }
				return true;
            }
        }

		public static string NamespaceAndName(this TypeReference @this)
		{
			var formattedNamespace = @this.Namespace.Replace('.', '_');
			return $"{formattedNamespace}_{@this.Name}";
		}

		public static string NamespaceAndName(this TypeRef @this) => ((TypeReference)@this).NamespaceAndName();

		// @TODO - Probably move to another file, or make this one more generic.
		public static IEnumerable<Instruction> Concat(this Instruction @this, Instruction other)
			=> Enumerable.Repeat(@this, 1).Concat(Enumerable.Repeat(other, 1));

		public static IEnumerable<Instruction> Concat(this Instruction @this, IEnumerable<Instruction> other)
			=> Enumerable.Repeat(@this, 1).Concat(other);

		public static IEnumerable<Instruction> Concat(this IEnumerable<Instruction> @this, Instruction other)
			=> @this.Concat(Enumerable.Repeat(other, 1));
	}
}
