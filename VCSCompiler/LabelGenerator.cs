using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Text;

namespace VCSCompiler
{
	internal static class LabelGenerator
    {
		public static string TemporaryRegister1 => "TempReg1";
		public static string TemporaryRegister2 => "TempReg2";

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

		public static string GetFromMethod(MethodReference method)
		{
			return $"{method.DeclaringType.Name}_{method.Name}";
		}

	    public static string GetFromParameter(ParameterDefinition parameter)
	    {
		    var method = (MethodReference) parameter.Method;
		    return $"{GetFromMethod(method)}_{parameter.Name}";
	    }

	    public static string GetFromVariable(MethodDefinition methodDefinition, VariableDefinition variable)
	    {
			// TODO - Use symbols to get true name of variable.
		    return $"{GetFromMethod(methodDefinition)}_Local_{variable.Index}";
	    }

		public static string GetFromInstruction(Instruction instruction)
		{
			// We assume all instructions are contained in subroutine psuedo-ops
			return $".{instruction.ToString().Substring(0, 7)}";
		}
	}
}
