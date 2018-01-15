using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace VCSCompiler
{
    internal static class TypeChecker
    {
		public static bool IsValidType(TypeDefinition type, IDictionary<string, ProcessedType> types, out string error)
		{
			error = string.Empty;

			var typeErrorHeader = $"Type '{type.FullName}'";
			if (!(type.IsValueType || (type.IsAbstract && type.IsSealed)))
			{
				error = $"{typeErrorHeader} must be a value type or static reference type.";
				return false;
			}

			if (!types.ContainsKey(type.BaseType.FullName))
			{
				error = $"{typeErrorHeader} has an unknown base type: '{type.BaseType.FullName}'";
				return false;
			}

			foreach (var field in type.Fields)
			{
				if (!IsValidField(field, types, out var fieldError))
				{
					error = $"{typeErrorHeader} has an invalid field: {fieldError}";
					return false;
				}
			}

			foreach (var method in type.CompilableMethods())
			{
				if (!IsValidMethod(method, types, out var methodError))
				{
					error = $"{typeErrorHeader} has an invalid method: {methodError}";
					return false;
				}
			}

			return true;
		}

		public static bool IsValidField(FieldDefinition field, IDictionary<string, ProcessedType> types, out string error)
		{
			error = string.Empty;

			if (!types.ContainsKey(field.FieldType.FullName))
			{
				error = $"Field '{field.FullName}' is of an unknown type: {field.FieldType.FullName}";
				return false;
			}

			if (!field.FieldType.IsValueType)
			{
				error = $"Field '{field.FullName}' can not be a variable of a reference type.";
				return false;
			}

			return true;
		}

		public static bool IsValidMethod(MethodDefinition method, IDictionary<string, ProcessedType> types, out string error)
		{
			error = string.Empty;

			var methodErrorHeader = $"Method '{method.FullName}' has";
			if (!types.ContainsKey(method.ReturnType.FullName))
			{
				error = $"Method '{method.FullName}' has an unknown return type: {method.ReturnType.FullName}";
				return false;
			}

			foreach(var parameter in method.Parameters)
			{
				if (!IsValidParameter(parameter, types, out string parameterError))
				{
					error = $"{methodErrorHeader} an invalid parameter: {parameterError}";
					return false;
				}
			}

			foreach(var local in method.Body.Variables)
			{
				if (!IsValidLocal(local, types, out string localError))
				{
					error = $"{methodErrorHeader} an invalid local: {localError}";
					return false;
				}
			}

			return true;
		}

	    public static bool IsValidParameter(ParameterDefinition parameter, IDictionary<string, ProcessedType> types, out string error)
	    {
			error = string.Empty;

			var parameterErrorHeader = $"Parameter {parameter.Index} ('{parameter.Name}') has";
			if (!types.ContainsKey(parameter.ParameterType.FullName))
			{
				error = $"{parameterErrorHeader} an unknown type: '{parameter.ParameterType.Name}'";
				return false;
			}

			var type = types[parameter.ParameterType.FullName];
			if (!type.AllowedAsLValue)
			{
				error = $"{parameterErrorHeader} a type that can't be used as a variable: '{parameter.ParameterType.Name}'";
				return false;
			}

			return true;
		}

	    public static bool IsValidLocal(VariableReference local, IDictionary<string, ProcessedType> types, out string error)
	    {
			error = string.Empty;

			var localErrorHeader = $"Local {local.Index} has";
			if (!types.ContainsKey(local.VariableType.FullName))
			{
				error = $"{localErrorHeader} unknown type: '{local.VariableType.Name}'";
				return false;
			}

			var type = types[local.VariableType.FullName];
			if (!type.AllowedAsLValue)
			{
				error = $"{localErrorHeader} a type that can't be used as a variable: '{local.VariableType.Name}'";
				return false;
			}

			return true;
		}
    }
}
