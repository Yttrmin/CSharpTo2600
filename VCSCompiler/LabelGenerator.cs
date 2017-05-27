using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Text;

namespace VCSCompiler
{
	internal static class LabelGenerator
    {
		public static string GetFromField(FieldDefinition field)
		{
			if (field.IsStatic)
			{
				return $"{field.DeclaringType.Name}_{field.Name}";
			}
			else
			{
				throw new NotImplementedException("Instance field symbols not supported yet.");
			}
		}

		public static string GetFromMethod(MethodDefinition method)
		{
			return $"{method.DeclaringType.Name}_{method.Name}";
		}
    }
}
