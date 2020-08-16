#nullable enable
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCSFramework;
using VCSFramework.V2;

namespace VCSCompiler.V2
{
    internal static class CecilExtensions
    {
		public static IEnumerable<TypeDefinition> CompilableTypes(this AssemblyDefinition @this)
        {
			return @this.MainModule.Types
				.Where(t => t.BaseType != null) // @TODO - What's this for?
				.Where(t => t.CustomAttributes
					.All(a => a.AttributeType.Name != nameof(DoNotCompileAttribute)));
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
			var attribute = @this.CustomAttributes.
				SingleOrDefault(a => a.AttributeType.FullName == typeof(T).FullName);
			if (attribute != null)
			{
				var constructorArguments = attribute.ConstructorArguments.Select(a => a.Value).ToArray();
				result = AttributeReconstructor.ReconstructFrom<T>(constructorArguments);
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
