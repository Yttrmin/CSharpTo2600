#nullable enable
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using VCSFramework;
using Instruction = Mono.Cecil.Cil.Instruction;

namespace VCSCompiler
{
    internal class CilInstructionCompiler
    {
		public record Options
        {
			public bool InlineAllCalls { get; init; }
			public bool MustNotReturn { get; init; }
			public bool LiftLocals { get; init; }
        }

		public static readonly Instruction NopInst = Instruction.Create(OpCodes.Ldstr, "NO INSTRUCTION");
		private static readonly TypeLabel ByteType = new(BuiltInDefinitions.Byte);
		private static readonly TypeSizeLabel ByteSize = new(BuiltInDefinitions.Byte);
		private readonly ImmutableDictionary<Code, Func<Instruction, IEnumerable<IAssemblyEntry>>> MethodMap;
		private readonly MethodDefinition MethodDefinition;
		private readonly AssemblyPair UserPair;
		private readonly ImmutableArray<AssemblyDefinition> Assemblies;
		private readonly Options CompilationOptions;

		public CilInstructionCompiler(MethodDefinition methodDefinition, AssemblyPair userPair, Options? options = null)
        {
			MethodMap = CreateMethodMap();
			MethodDefinition = methodDefinition;
			UserPair = userPair;
			Assemblies = BuiltInDefinitions.Assemblies.Append(userPair.Definition).ToImmutableArray();
			CompilationOptions = options ?? new Options();
        }

		public IEnumerable<IAssemblyEntry> Compile()
        {
			var labeledInstructions = MethodDefinition.Body.Instructions
				.Where(IsBranchInstruction)
				.Select(it => (Instruction)it.Operand)
				.ToImmutableArray();

			foreach (var instruction in MethodDefinition.Body.Instructions)
            {
				if (labeledInstructions.Contains(instruction))
                {
					yield return new InstructionLabel(instruction);
                }
				foreach (var entry in CompileInstruction(instruction))
                {
					yield return entry;
                }
            }
        }

        public IEnumerable<IAssemblyEntry> CompileInstruction(Instruction instruction)
			=> MethodMap[instruction.OpCode.Code](instruction);

		private ImmutableDictionary<Code, Func<Instruction, IEnumerable<IAssemblyEntry>>> CreateMethodMap()
		{
			var dictionary = new Dictionary<Code, Func<Instruction, IEnumerable<IAssemblyEntry>>>();
			var typeInfo = typeof(CilInstructionCompiler).GetTypeInfo();
			var opCodes = Enum.GetValues(typeof(Code)).Cast<Code>();
			foreach (var opCode in opCodes)
			{
				var name = Enum.GetName(typeof(Code), opCode) ?? throw new Exception();
				if (opCode >= Code.Ldc_I4_0 && opCode <= Code.Ldc_I4_8)
				{
					name = "Ldc_I4";
				}
				else if (opCode >= Code.Ldarg_0 && opCode <= Code.Ldarg_3)
				{
					name = "Ldarg";
				}
				else if (opCode >= Code.Ldloc_0 && opCode <= Code.Ldloc_3)
				{
					name = "Ldloc";
				}
				else if (opCode >= Code.Stloc_0 && opCode <= Code.Stloc_3)
				{
					name = "Stloc";
				}
				var method = typeInfo.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
				dictionary[opCode]
					= (Func<Instruction, IEnumerable<IAssemblyEntry>>?)method?.CreateDelegate(typeof(Func<Instruction, IEnumerable<IAssemblyEntry>>), this)
					?? Unsupported;
			}
			return dictionary.ToImmutableDictionary();
		}

		private IEnumerable<IAssemblyEntry> LoadArgument(Instruction instruction)
        {
			return instruction.OpCode.Code switch
			{
				Code.Ldarg_0 => LoadArgument(0),
				Code.Ldarg_1 => LoadArgument(1),
				Code.Ldarg_2 => LoadArgument(2),
				Code.Ldarg_3 => LoadArgument(3),
				Code.Ldarg_S => LoadArgument(((ParameterDefinition)instruction.Operand).Index),
				Code.Ldarg => LoadArgument(((ParameterDefinition)instruction.Operand).Index),
				_ => throw new InvalidOperationException()
			};

			IEnumerable<IAssemblyEntry> LoadArgument(int index)
            {
				if (MethodDefinition.IsStatic)
				{
					yield return PushArgument(index);
				}
				// ldarg.0 refers to 'this', but it's not considered part of the parameter list.
				else if (index == 0)
                {
					// Push 'this' pointer.
					// @TODO - This won't support instance methods on those stored in ROM.
					yield return new PushGlobal(instruction, new ThisPointerGlobalLabel(MethodDefinition), new PointerTypeLabel(MethodDefinition.DeclaringType), new ThisPointerSizeLabel(MethodDefinition));
                }
				else
				{
					yield return PushArgument(index - 1);
				}

				IAssemblyEntry PushArgument(int index)
                {
					var argument = MethodDefinition.Parameters[index];
					return new PushGlobal(instruction, new ArgumentGlobalLabel(MethodDefinition, index), TypeLabel(argument.ParameterType), SizeLabel(argument.ParameterType));
				}
			}
		}

		private IEnumerable<IAssemblyEntry> LoadConstant(Instruction instruction)
		{
			byte value = 0;
			if (instruction.Operand != null)
			{
				try
				{
					// @TODO - Don't limit to byte, also don't be this verbose.
					value = Convert.ToByte(instruction.Operand);
				}
				catch (OverflowException)
				{
					throw new InvalidInstructionException(instruction, $"Constant value '{instruction.Operand}'must fit in a byte!");
				}
			}
			else
			{
				switch (instruction.OpCode.Code)
				{
					case Code.Ldc_I4_0:
						return LoadConstant(instruction, 0);
					case Code.Ldc_I4_1:
						return LoadConstant(instruction, 1);
					case Code.Ldc_I4_2:
						return LoadConstant(instruction, 2);
					case Code.Ldc_I4_3:
						return LoadConstant(instruction, 3);
					case Code.Ldc_I4_4:
						return LoadConstant(instruction, 4);
					case Code.Ldc_I4_5:
						return LoadConstant(instruction, 5);
					case Code.Ldc_I4_6:
						return LoadConstant(instruction, 6);
					case Code.Ldc_I4_7:
						return LoadConstant(instruction, 7);
					case Code.Ldc_I4_8:
						return LoadConstant(instruction, 8);
				}
			}
			return LoadConstant(instruction, value);
		}

		private IEnumerable<IAssemblyEntry> LoadConstant(Instruction instruction, byte value)
		{
			yield return new PushConstant(instruction, new Constant(value), ByteType, ByteSize);
		}

		private IEnumerable<IAssemblyEntry> LoadLocal(Instruction instruction)
        {
			switch (instruction.OpCode.Code)
            {
				case Code.Ldloc_0:
					return LoadLocal(0);
				case Code.Ldloc_1:
					return LoadLocal(1);
				case Code.Ldloc_2:
					return LoadLocal(2);
				case Code.Ldloc_3:
					return LoadLocal(3);
				case Code.Ldloc_S:
					return LoadLocal(((VariableDefinition)instruction.Operand).Index);
            }
			return LoadLocal((int)instruction.Operand);

			IEnumerable<IAssemblyEntry> LoadLocal(int index)
            {
				var variable = MethodDefinition.Body.Variables[index];
				yield return new PushLocal(instruction, new(MethodDefinition, index), TypeLabel(variable.VariableType), LocalSize(variable));
            }
        }

		private IEnumerable<IAssemblyEntry> StoreLocal(Instruction instruction)
        {
			switch (instruction.OpCode.Code)
            {
				case Code.Stloc_0:
					return StoreLocal(0);
				case Code.Stloc_1:
					return StoreLocal(1);
				case Code.Stloc_2:
					return StoreLocal(2);
				case Code.Stloc_3:
					return StoreLocal(3);
            }
			return StoreLocal((int)instruction.Operand);

			IEnumerable<IAssemblyEntry> StoreLocal(int index)
            {
				var variable = MethodDefinition.Body.Variables[index];
				yield return new PopToLocal(instruction, new(MethodDefinition, index), TypeLabel(variable.VariableType), LocalSize(variable), new(0), new(0));
            }
        }

		private IEnumerable<IAssemblyEntry> Add(Instruction instruction)
        {
			yield return new AddFromStack(instruction, new(1), new(1), new(0), new(0));
        }

		private IEnumerable<IAssemblyEntry> Blt(Instruction instruction)
        {
			var targetInstruction = (Instruction)instruction.Operand;
			yield return new BranchIfLessThanFromStack(instruction, new InstructionLabel(targetInstruction));
        }

		private IEnumerable<IAssemblyEntry> Blt_S(Instruction instruction) => Blt(instruction);

		private IEnumerable<IAssemblyEntry> Br(Instruction instruction)
        {
			var targetInstruction = (Instruction)instruction.Operand;
			yield return new Branch(instruction, new InstructionLabel(targetInstruction));
        }

		private IEnumerable<IAssemblyEntry> Br_S(Instruction instruction) => Br(instruction);

		private IEnumerable<IAssemblyEntry> Brtrue(Instruction instruction)
        {
			var targetInstruction = (Instruction)instruction.Operand;
			yield return new BranchTrueFromStack(instruction, new InstructionLabel(targetInstruction));
        }

		private IEnumerable<IAssemblyEntry> Brtrue_S(Instruction instruction) => Brtrue(instruction);

		private IEnumerable<IAssemblyEntry> Brfalse(Instruction instruction)
        {
			var targetInstruction = (Instruction)instruction.Operand;
			yield return new BranchFalseFromStack(instruction, new InstructionLabel(targetInstruction));
        }

		private IEnumerable<IAssemblyEntry> Brfalse_S(Instruction instruction) => Brfalse(instruction);

		private IEnumerable<IAssemblyEntry> Call(Instruction instruction)
        {
			var methodReference = (MethodReference)instruction.Operand;

			// If it's not a MethodDefintion, it's not in the assembly we're compiling. Search for it in others.
			var method = methodReference as MethodDefinition
				?? Assemblies.CompilableTypes().CompilableMethods().SingleOrDefault(it => methodReference.FullName == it.FullName)
				?? methodReference.Resolve()
				?? throw new MissingMethodException($"Could not find '{methodReference.FullName}' in any assemblies.");
			var arity = method.Parameters.Count;

			var mustInline = CompilationOptions.InlineAllCalls || method.TryGetFrameworkAttribute<AlwaysInlineAttribute>(out var _);
			var isRecursiveCall = MethodDefinition.FullName == method.FullName || method.Calls(MethodDefinition);
			if (method.TryGetFrameworkAttribute<IgnoreCallAttribute>(out var _))
            {
				yield break;
            }
			else if (method.TryGetFrameworkAttribute<OverrideWithStoreToSymbolAttribute>(out var overrideStore))
            {
				if (overrideStore.Strobe)
                {
					if (arity != 0)
                    {
						throw new InvalidOperationException($"Couldn't call {nameof(OverrideWithStoreToSymbolAttribute)}-marked '{method.Name}', methods to be replaced with a strobe must take 0 parameters");
                    }
					yield return new StoreTo(instruction, new PredefinedGlobalLabel(overrideStore.Symbol));
                }
				else
                {
					if (arity != 1)
                    {
						throw new InvalidOperationException($"Couldn't call {nameof(OverrideWithStoreToSymbolAttribute)}-marked '{method.Name}', a non-strobe replacement should take 1 parameter.");
                    }
					yield return new PopToGlobal(instruction, new PredefinedGlobalLabel(overrideStore.Symbol), ByteType, ByteSize, new(0), new(0));
				}
            }
			else if (method.TryGetFrameworkAttribute<OverrideWithLoadFromSymbolAttribute>(out var overrideLoad))
            {
				var type = method.ReturnType;
				yield return new PushGlobal(instruction, new PredefinedGlobalLabel(overrideLoad.Symbol), new TypeLabel(type), new TypeSizeLabel(type));
            }
			else if (method.TryGetFrameworkAttribute<OverrideWithLoadToRegisterAttribute>(out var overrideLoadToRegister))
            {
				// @TODO - When we delete V1, should just switch to an enum.
				byte register = overrideLoadToRegister.Register switch
                {
					"A" => 0,
					"X" => 1,
					"Y" => 2,
					_ => throw new InvalidOperationException($"Unknown register '{overrideLoadToRegister.Register}'")
                };
				yield return new PopToRegister(instruction, new(register), new(0));
            }
			else if (method.TryGetFrameworkAttribute<ReplaceWithEntryAttribute>(out var replaceWithMacro))
            {
				yield return (IAssemblyEntry)(Activator.CreateInstance(replaceWithMacro.Type, instruction) 
					?? throw new InvalidOperationException($"Failed to replace call to [{nameof(ReplaceWithEntryAttribute)}]-attributed method '{method}' with {nameof(IMacroCall)} type {replaceWithMacro.Type}"));
            }
			else
            {
				// Pop callee args into appropriate globals.
				foreach (var parameter in method.Parameters.Reverse())
                {
					yield return new PopToGlobal(NopInst, new ArgumentGlobalLabel(method, parameter.Index), TypeLabel(parameter.ParameterType), SizeLabel(parameter.ParameterType), new(0), new(0));
                }
				if (!method.IsStatic)
                {
					yield return new PopToGlobal(NopInst, new ThisPointerGlobalLabel(method), new PointerTypeLabel(method.DeclaringType), new ThisPointerSizeLabel(method), new(0), new(0));
                }
				if (isRecursiveCall)
                {
					// Need to save our locals/args if we're going to be making a recursive call.
					foreach (var parameter in MethodDefinition.Parameters)
						yield return new PushGlobal(NopInst, new ArgumentGlobalLabel(MethodDefinition, parameter.Index), TypeLabel(parameter.ParameterType), SizeLabel(parameter.ParameterType));
					foreach (var local in MethodDefinition.Body.Variables)
						yield return new PushGlobal(NopInst, new LocalGlobalLabel(MethodDefinition, local.Index), TypeLabel(local.VariableType), SizeLabel(local.VariableType));
                }
				if (mustInline)
                {
					yield return new InlineFunction(instruction, method);
					foreach (var entry in MethodCompiler.Compile(method, UserPair, true).Body)
					{
						yield return entry;
					}
				}
				else
                {
					yield return new CallMethod(instruction, new(method));
				}
				if (isRecursiveCall)
				{
					// If we just returned from a recursive call, we need to restore the locals/args we saved.
					foreach (var local in MethodDefinition.Body.Variables.Reverse())
						yield return new PopToGlobal(NopInst, new LocalGlobalLabel(MethodDefinition, local.Index), TypeLabel(local.VariableType), SizeLabel(local.VariableType), new(0), new(0));
					foreach (var parameter in MethodDefinition.Parameters.Reverse())
						yield return new PopToGlobal(NopInst, new ArgumentGlobalLabel(MethodDefinition, parameter.Index), TypeLabel(parameter.ParameterType), SizeLabel(parameter.ParameterType), new(0), new(0));
				}
				if (method.ReturnType.Name != typeof(void).Name)
					yield return new PushGlobal(NopInst, new ReturnValueGlobalLabel(method), TypeLabel(method.ReturnType), SizeLabel(method.ReturnType));
			}
        }

		private IEnumerable<IAssemblyEntry> Ceq(Instruction instruction)
        {
			yield return new CompareEqualToFromStack(instruction, new(1), new(1), new(0), new(0));
        }

		private IEnumerable<IAssemblyEntry> Conv_I(Instruction instruction)
        {
			// @TODO - Do we need to support this?
			yield break;
        }

		private IEnumerable<IAssemblyEntry> Conv_U(Instruction instruction)
		{
			// @TODO - Do we need to support this?
			yield break;
		}

		private IEnumerable<IAssemblyEntry> Conv_U1(Instruction instruction)
        {
			// @TODO - Do we have to support this? We obviously can't expand byte+byte addition
			// to int+int addition.
			yield break;
        }

		private IEnumerable<IAssemblyEntry> Dup(Instruction instruction)
        {
			yield return new Duplicate(instruction, new(0), new(0));
        }

		private IEnumerable<IAssemblyEntry> Initobj(Instruction instruction)
        {
			var type = (TypeReference)instruction.Operand;
			yield return new InitializeObject(instruction, new TypeSizeLabel(type), new(0));
        }

		private IEnumerable<IAssemblyEntry> Ldarg(Instruction instruction)
			=> LoadArgument(instruction);

		private IEnumerable<IAssemblyEntry> Ldc_I4(Instruction instruction)
			=> LoadConstant(instruction);

		private IEnumerable<IAssemblyEntry> Ldc_I4_S(Instruction instruction)
			=> LoadConstant(instruction);

		private IEnumerable<IAssemblyEntry> Ldfld(Instruction instruction)
        {
			var field = (FieldReference)instruction.Operand;
			var fieldType = FieldType(field);
			var fieldSize = FieldSize(field);

			yield return new PushFieldFromStack(instruction, FieldOffset(field), fieldType, fieldSize, new(0), new(0));
		}

		private IEnumerable<IAssemblyEntry> Ldflda(Instruction instruction)
        {
			var field = (FieldDefinition)instruction.Operand;

			// Pops address of object off stack, adds offset to it, pushes it.
			yield return new PushAddressOfField(instruction, FieldOffset(field), new(field.FieldType), new(0));
		}

		private IEnumerable<IAssemblyEntry> Ldind_U1(Instruction instruction)
        {
			yield return new PushDereferenceFromStack(instruction, new StackSizeArrayAccess(0), ByteType, ByteSize);
        }

		private IEnumerable<IAssemblyEntry> Ldloc(Instruction instruction) => LoadLocal(instruction);

		private IEnumerable<IAssemblyEntry> Ldloc_S(Instruction instruction) => Ldloc(instruction);

		private IEnumerable<IAssemblyEntry> Ldloca(Instruction instruction)
        {
			var local = (VariableDefinition)instruction.Operand;
			yield return new PushAddressOfLocal(instruction, new LocalGlobalLabel(MethodDefinition, local.Index), new PointerTypeLabel(local.VariableType), new PointerSizeLabel(true));
        }

		private IEnumerable<IAssemblyEntry> Ldloca_S(Instruction instruction) => Ldloca(instruction);

		private IEnumerable<IAssemblyEntry> Ldobj(Instruction instruction)
        {
			var type = (TypeDefinition)instruction.Operand;
			yield return new PushDereferenceFromStack(instruction, new(0), TypeLabel(type), SizeLabel(type));
        }

		private IEnumerable<IAssemblyEntry> Ldsfld(Instruction instruction)
        {
			var field = (FieldReference)instruction.Operand;
			var fieldLabel = new GlobalFieldLabel(field);
			var fieldTypeLabel = FieldType(field);
			var fieldSizeLabel = FieldSize(field);

			yield return new PushGlobal(instruction, fieldLabel, fieldTypeLabel, fieldSizeLabel);
		}

		private IEnumerable<IAssemblyEntry> Ldsflda(Instruction instruction)
        {
			var field = (FieldDefinition)instruction.Operand;
			var fieldLabel = new GlobalFieldLabel(field);

			yield return new PushAddressOfGlobal(instruction, fieldLabel, new(field.FieldType), new(true));
		}

		private IEnumerable<IAssemblyEntry> Ldstr(Instruction instruction)
        {
			// NOTE: This can only be used for compile-time purposes and MUST be optimized out.
			// If it is found after optimizing, an exception will be thrown.
			yield return new LoadString(instruction);
        }

		private IEnumerable<IAssemblyEntry> Nop(Instruction instruction)
        {
			yield break;
        }

		private IEnumerable<IAssemblyEntry> Pop(Instruction instruction)
        {
			yield return new PopStack(instruction, new(0));
        }

		private IEnumerable<IAssemblyEntry> Or(Instruction instruction)
        {
			yield return new OrFromStack(instruction, new(1), new(1), new(0), new(0));
        }

		private IEnumerable<IAssemblyEntry> Ret(Instruction instruction)
        {
			if (MethodDefinition.ReturnType.FullName != typeof(void).FullName)
            {
				yield return new PopToGlobal(NopInst, new ReturnValueGlobalLabel(MethodDefinition), TypeLabel(MethodDefinition.ReturnType), SizeLabel(MethodDefinition.ReturnType), new(0), new(0));
            }
			yield return new ReturnFromMethod(instruction);
        }

		private IEnumerable<IAssemblyEntry> Stfld(Instruction instruction)
        {
			var field = (FieldReference)instruction.Operand;

			// Value is at stack[0], pointer at stack[1]
			yield return new PopToFieldFromStack(instruction, FieldOffset(field), FieldType(field), FieldSize(field), new(1), new(1));
        }

		private IEnumerable<IAssemblyEntry> Stind_I1(Instruction instruction)
        {
			// Stind.i1 is also used to store to bytes.
			yield return new PopToAddressFromStack(instruction, ByteType, ByteSize);
        }

		private IEnumerable<IAssemblyEntry> Stloc(Instruction instruction) => StoreLocal(instruction);

		private IEnumerable<IAssemblyEntry> Stsfld(Instruction instruction)
        {
			var field = (FieldReference)instruction.Operand;
			var fieldLabel = new GlobalFieldLabel(field);
			var fieldTypeLabel = FieldType(field);
			var fieldSizeLabel = FieldSize(field);

			yield return new PopToGlobal(instruction, fieldLabel, fieldTypeLabel, fieldSizeLabel, new(0), new(0));
        }

		private IEnumerable<IAssemblyEntry> Sub(Instruction instruction)
        {
			yield return new SubFromStack(instruction, new(1), new(1), new(0), new(0));
        }

		private IEnumerable<IAssemblyEntry> Unsupported(Instruction instruction) => throw new UnsupportedOpCodeException(instruction.OpCode);

		private bool IsBranchInstruction(Instruction instruction)
        {
			var branchInstructions = new[]
			{
				OpCodes.Blt,
				OpCodes.Blt_S,
				OpCodes.Br,
				OpCodes.Br_S,
				OpCodes.Brtrue,
				OpCodes.Brtrue_S,
				OpCodes.Brfalse,
				OpCodes.Brfalse_S,
			};

			return branchInstructions.Contains(instruction.OpCode);
        }

		private static ITypeLabel TypeLabel(TypeReference type)
        {
			if (type.IsPointer || type.IsPinned || type.IsByReference)
				return new PointerTypeLabel(type.Resolve());
			return new TypeLabel(type);
		}

		private static ISizeLabel SizeLabel(TypeReference type)
        {
			if (type.IsPointer || type.IsPinned || type.IsByReference)
			{
				// @TODO - Pointers to ROM are probably gonna need special support here. Pointers _TO_ RAM will always
				// be short (barring different cartridge types), but don't know if a field/local is holding a pointer
				// to ROM or expanded RAM (for other cartridges).
				return new PointerSizeLabel(true);
			}
			return new TypeSizeLabel(type);
		}

		private static ISizeLabel ReturnSize(MethodDefinition method)
			=> SizeLabel(method.ReturnType);

		private ITypeLabel FieldType(FieldReference field)
			=> TypeLabel(GetFieldData(field).FieldType);

		private ISizeLabel FieldSize(FieldReference field)
			=> SizeLabel(GetFieldData(field).FieldType);

		private Constant FieldOffset(FieldReference field)
			=> new Constant(GetFieldData(field).Offset);

		private static ISizeLabel LocalSize(VariableDefinition variable)
			=> SizeLabel(variable.VariableType);

		private TypeData.FieldData GetFieldData(FieldReference field)
			=> TypeData.Of(field.DeclaringType, UserPair.Definition).Fields.Single(f => f.Field.Name == field.Name);
	}
}
