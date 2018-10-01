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
		public static bool IsValidType(TypeDefinition type, ImmutableTypeMap types, out string error)
		{
			error = string.Empty;

			var typeErrorHeader = $"Type '{type.FullName}'";
			if (!(type.IsValueType || (type.IsAbstract && type.IsSealed)))
			{
				error = $"{typeErrorHeader} must be a value type or static reference type.";
				return false;
			}

			if (!types.Contains(type.BaseType))
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

		public static bool IsValidField(FieldDefinition field, ImmutableTypeMap types, out string error)
		{
			error = string.Empty;

			if (!types.Contains(field.FieldType))
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

		public static bool IsValidMethod(MethodDefinition method, ImmutableTypeMap types, out string error)
		{
			error = string.Empty;

			var methodErrorHeader = $"Method '{method.FullName}'";
			if (!types.Contains(method.ReturnType))
			{
				error = $"{methodErrorHeader} has an unknown return type: {method.ReturnType.FullName}";
				return false;
			}

			// Static constructors are most likely being used for field initializers. Since when exactly memory is
			// cleared is up to the user, we don't have a safe place to invoke it.
			// So we block the feature altogether.
			if (method.IsConstructor && method.IsStatic)
			{
				error = $"{methodErrorHeader} must not be a static constructor.";
				return false;
			}

			foreach(var parameter in method.Parameters)
			{
				if (!IsValidParameter(parameter, types, out string parameterError))
				{
					error = $"{methodErrorHeader} has an invalid parameter: {parameterError}";
					return false;
				}
			}

			foreach(var local in method.Body.Variables)
			{
				if (!IsValidLocal(local, types, out string localError))
				{
					error = $"{methodErrorHeader} has an invalid local: {localError}";
					return false;
				}
			}

			return true;
		}

	    public static bool IsValidParameter(ParameterDefinition parameter, ImmutableTypeMap types, out string error)
	    {
			error = string.Empty;

			var parameterErrorHeader = $"Parameter {parameter.Index} ('{parameter.Name}') has";
			if (!types.Contains(parameter.ParameterType))
			{
				error = $"{parameterErrorHeader} an unknown type: '{parameter.ParameterType.Name}'";
				return false;
			}

			var type = types[parameter.ParameterType];
			if (!type.AllowedAsLValue)
			{
				error = $"{parameterErrorHeader} a type that can't be used as a variable: '{parameter.ParameterType.Name}'";
				return false;
			}

			return true;
		}

	    public static bool IsValidLocal(VariableReference local, ImmutableTypeMap types, out string error)
	    {
			error = string.Empty;

			var localErrorHeader = $"Local {local.Index} has";
			if (!types.Contains(local.VariableType))
			{
				error = $"{localErrorHeader} unknown type: '{local.VariableType.Name}'";
				return false;
			}

			var type = types[local.VariableType];
			if (!type.AllowedAsLValue)
			{
				error = $"{localErrorHeader} a type that can't be used as a variable: '{local.VariableType.Name}'";
				return false;
			}

			return true;
		}
    }
}
