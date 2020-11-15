#nullable enable
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using VCSFramework;
using VCSFramework.V2;
using Instruction = Mono.Cecil.Cil.Instruction;

namespace VCSCompiler.V2
{
    internal class CilInstructionCompiler
    {
		public record Options
        {
			public bool InlineAllCalls { get; init; }
			public bool MustNotReturn { get; init; }
			public bool LiftLocals { get; init; }
        }

		private readonly ImmutableDictionary<Code, Func<Instruction, IEnumerable<IAssemblyEntry>>> MethodMap;
		private readonly MethodDefinition MethodDefinition;
		private readonly AssemblyDefinition UserAssembly;
		private readonly ImmutableArray<AssemblyDefinition> Assemblies;
		private readonly Options CompilationOptions;

		public CilInstructionCompiler(MethodDefinition methodDefinition, AssemblyDefinition userAssembly, Options? options = null)
        {
			MethodMap = CreateMethodMap();
			MethodDefinition = methodDefinition;
			UserAssembly = userAssembly;
			Assemblies = BuiltInDefinitions.Assemblies.Append(userAssembly).ToImmutableArray();
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
			yield return new PushConstant(instruction, new Constant(value), LabelGenerator.ByteType, LabelGenerator.ByteSize);
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
            }
			return LoadLocal((int)instruction.Operand);

			IEnumerable<IAssemblyEntry> LoadLocal(int index)
            {
				var variable = MethodDefinition.Body.Variables[index];
				yield return new PushLocal(instruction, new(MethodDefinition, index), LocalType(variable), LocalSize(variable));
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
				yield return new PopToLocal(instruction, new(MethodDefinition, index), LocalType(variable), LocalSize(variable), new(0), new(0));
            }
        }

		private IEnumerable<IAssemblyEntry> Add(Instruction instruction)
        {
			yield return new AddFromStack(instruction, new(1), new(1), new(0), new(0));
        }

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
				?? throw new MissingMethodException($"Could not find '{methodReference.FullName}' in any assemblies.");
			var arity = method.Parameters.Count;

			var mustInline = CompilationOptions.InlineAllCalls || method.TryGetFrameworkAttribute<AlwaysInlineAttribute>(out var _);
			if (method.TryGetFrameworkAttribute<OverrideWithStoreToSymbolAttribute>(out var overrideStore))
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
					yield return new PopToGlobal(instruction, new PredefinedGlobalLabel(overrideStore.Symbol), LabelGenerator.ByteType, LabelGenerator.ByteSize, new(0), new(0));
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
			else if (mustInline)
            {
				if (arity != 0 || method.ReturnType.Name != typeof(void).Name)
				{
					throw new InvalidOperationException($"Inline methods must have 0 arity and void return type for now");
				}
				yield return new InlineFunction(instruction, method);
				foreach (var entry in MethodCompiler.Compile(method, UserAssembly, true).Body)
                {
					yield return entry;
                }
            }
			else
            {
				if (arity != 0)
                {
					throw new InvalidOperationException($"Methods must have 0 arity for now");
                }
				if (method.ReturnType.Name == typeof(void).Name)
                {
					yield return new CallVoid(instruction, new(method));
                }
				else
                {
					// @TODO - Probably doesn't work for returning ptr/ref.
					// @TODO - We could maybe do a special case if the return type is 1-byte in size. Enregister
					// the value or something instead of having to jump over the return address.
					yield return new CallNonVoid(instruction, new(method), LabelGenerator.Type(method.ReturnType), LabelGenerator.Size(method.ReturnType));
                }
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
			var type = (TypeDefinition)instruction.Operand;
			yield return new InitializeObject(instruction, new TypeSizeLabel(type), new(0));
        }

		private IEnumerable<IAssemblyEntry> Ldc_I4(Instruction instruction)
			=> LoadConstant(instruction);

		private IEnumerable<IAssemblyEntry> Ldc_I4_S(Instruction instruction)
			=> LoadConstant(instruction);

		private IEnumerable<IAssemblyEntry> Ldfld(Instruction instruction)
        {
			var field = (FieldDefinition)instruction.Operand;
			var fieldType = LabelGenerator.Type(field.FieldType);
			var fieldSize = FieldSize(field);
			var offset = TypeData.Of(field.DeclaringType, UserAssembly).Fields.Single(f => f.Field == field).Offset;

			yield return new PushFieldFromStack(instruction, new(offset), fieldType, fieldSize, new(0), new(0));
		}

		private IEnumerable<IAssemblyEntry> Ldflda(Instruction instruction)
        {
			var field = (FieldDefinition)instruction.Operand;
			var offset = TypeData.Of(field.DeclaringType, UserAssembly).Fields.Single(f => f.Field == field).Offset;

			// Pops address of object off stack, adds offset to it, pushes it.
			yield return new PushAddressOfField(instruction, new(offset), new(field.FieldType), new(0));
		}

		private IEnumerable<IAssemblyEntry> Ldind_U1(Instruction instruction)
        {
			yield return new PushDereferenceFromStack(instruction, LabelGenerator.ByteType, LabelGenerator.ByteSize);
        }

		private IEnumerable<IAssemblyEntry> Ldloc(Instruction instruction) => LoadLocal(instruction);

		private IEnumerable<IAssemblyEntry> Ldloca(Instruction instruction)
        {
			var local = (VariableDefinition)instruction.Operand;
			yield return new PushAddressOfLocal(instruction, new LiftedLocalLabel(MethodDefinition, local.Index), new PointerTypeLabel(local.VariableType), new PointerSizeLabel(true));
        }

		private IEnumerable<IAssemblyEntry> Ldloca_S(Instruction instruction) => Ldloca(instruction);

		private IEnumerable<IAssemblyEntry> Ldsfld(Instruction instruction)
        {
			var field = (FieldDefinition)instruction.Operand;
			var fieldLabel = new GlobalFieldLabel(field);
			var fieldTypeLabel = LabelGenerator.Type(field.FieldType);
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
			if (MethodDefinition.ReturnType.FullName == typeof(void).FullName)
            {
				yield return new ReturnVoid(instruction);
			}
			else
            {
				// @TODO - Probably doesn't work for returning ptr/ref.
				var typeLabel = LabelGenerator.Type(MethodDefinition.ReturnType);
				var sizeLabel = LabelGenerator.Size(MethodDefinition.ReturnType);
				yield return new ReturnNonVoid(instruction, typeLabel, sizeLabel);
            }
        }

		private IEnumerable<IAssemblyEntry> Stfld(Instruction instruction)
        {
			var field = (FieldDefinition)instruction.Operand;
			var offset = TypeData.Of(field.DeclaringType, UserAssembly).Fields.Single(f => f.Field == field).Offset;

			// Value is at stack[0], pointer at stack[1]
			yield return new PopToFieldFromStack(instruction, new(offset), FieldType(field), FieldSize(field), new(1), new(1));
        }

		private IEnumerable<IAssemblyEntry> Stind_I1(Instruction instruction)
        {
			// Stind.i1 is also used to store to bytes.
			yield return new PopToAddressFromStack(instruction, LabelGenerator.ByteType, LabelGenerator.ByteSize);
        }

		private IEnumerable<IAssemblyEntry> Stloc(Instruction instruction) => StoreLocal(instruction);

		private IEnumerable<IAssemblyEntry> Stsfld(Instruction instruction)
        {
			var field = (FieldReference)instruction.Operand;
			var fieldLabel = new GlobalFieldLabel(field);
			var fieldTypeLabel = LabelGenerator.Type(field.FieldType);
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
				OpCodes.Br,
				OpCodes.Br_S,
				OpCodes.Brtrue,
				OpCodes.Brtrue_S,
				OpCodes.Brfalse,
				OpCodes.Brfalse_S,
			};

			return branchInstructions.Contains(instruction.OpCode);
        }

		private static ITypeLabel FieldType(FieldReference field)
        {
			var type = field.FieldType;
			if (type.IsPointer || type.IsPinned || type.IsByReference)
				return new PointerTypeLabel(type.Resolve());
			return new TypeLabel(type);
        }

		private static ITypeLabel LocalType(VariableReference variable)
		{
			var type = variable.VariableType;
			if (type.IsPointer || type.IsPinned || type.IsByReference)
				return new PointerTypeLabel(type.Resolve());
			return new TypeLabel(type);
		}

		private static ISizeLabel FieldSize(FieldReference field)
		{
			var type = field.FieldType;
			if (type.IsPointer || type.IsPinned || type.IsByReference)
			{
				// Fields are variables, so will always be stored in RAM, so can use a short pointer (for cartridges without extra RAM).
				return new PointerSizeLabel(true);
			}
			return new TypeSizeLabel(field.FieldType);
		}

		private static ISizeLabel LocalSize(VariableDefinition variable)
		{
			var type = variable.VariableType;
			if (type.IsPointer || type.IsPinned || type.IsByReference)
			{
				// Locals are variables, so will always be stored in RAM, so can use a short pointer (for cartridges without extra RAM).
				return new PointerSizeLabel(true);
			}
			return new TypeSizeLabel(type);
		}
	}
}
