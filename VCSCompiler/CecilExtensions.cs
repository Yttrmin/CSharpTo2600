using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;
using VCSFramework;

namespace VCSCompiler
{
	internal static class CecilExtensions
    {
		/// <summary>
		/// Returns all methods that do not have a DoNotCompileAttribute.
		/// </summary>
		public static IEnumerable<MethodDefinition> CompilableMethods(this TypeDefinition @this)
		{
			return @this.Methods.Where(m => m.CustomAttributes.All(a => a.AttributeType.Name != nameof(DoNotCompileAttribute)));
		}
    }
}
