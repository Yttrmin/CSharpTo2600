﻿#nullable enable
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
				var constructorArguments = attribute.ConstructorArguments.Select(a => a.Value).ToArray();
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
		}

		public static IEnumerable<Instruction> AllInstructions(this (Macro a, Macro b) @this)
			=> @this.a.Instructions.Concat(@this.b.Instructions);
	}
}
