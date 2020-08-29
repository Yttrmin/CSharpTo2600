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
		private readonly ImmutableDictionary<Code, Func<Instruction, IEnumerable<AssemblyEntry>>> MethodMap;
		private readonly MethodDefinition MethodDefinition;
		private readonly ImmutableArray<AssemblyDefinition> Assemblies;

		public CilInstructionCompiler(MethodDefinition methodDefinition, AssemblyDefinition userAssembly)
        {
			MethodMap = CreateMethodMap();
			MethodDefinition = methodDefinition;
			Assemblies = AssemblyDefinitions.BuiltIn.Append(userAssembly).ToImmutableArray();
        }

		public IEnumerable<AssemblyEntry> Compile()
        {
			var labeledInstructions = MethodDefinition.Body.Instructions
				.Where(IsBranchInstruction)
				.Select(it => (Instruction)it.Operand)
				.ToImmutableArray();

			foreach (var instruction in MethodDefinition.Body.Instructions)
            {
				if (labeledInstructions.Contains(instruction))
                {
					yield return LabelGenerator.Instruction(instruction);
                }
				foreach (var entry in CompileInstruction(instruction))
                {
					yield return entry;
                }
            }
        }

        public IEnumerable<AssemblyEntry> CompileInstruction(Instruction instruction)
			=> MethodMap[instruction.OpCode.Code](instruction);

		private ImmutableDictionary<Code, Func<Instruction, IEnumerable<AssemblyEntry>>> CreateMethodMap()
		{
			var dictionary = new Dictionary<Code, Func<Instruction, IEnumerable<AssemblyEntry>>>();
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
					= (Func<Instruction, IEnumerable<AssemblyEntry>>?)method?.CreateDelegate(typeof(Func<Instruction, IEnumerable<AssemblyEntry>>), this)
					?? Unsupported;
			}
			return dictionary.ToImmutableDictionary();
		}

		private IEnumerable<AssemblyEntry> LoadConstant(Instruction instruction)
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

		private IEnumerable<AssemblyEntry> LoadConstant(Instruction instruction, byte value)
		{
			yield return new PushConstant(instruction, LabelGenerator.Constant(value), LabelGenerator.ByteType, LabelGenerator.ByteSize);
		}

		private IEnumerable<AssemblyEntry> Add(Instruction instruction)
        {
			yield return new AddFromStack(instruction, new(1), new(1), new(0), new(0));
        }

		private IEnumerable<AssemblyEntry> Br(Instruction instruction)
        {
			var targetInstruction = (Instruction)instruction.Operand;
			yield return new Branch(instruction, LabelGenerator.Instruction(targetInstruction));
        }

		private IEnumerable<AssemblyEntry> Br_S(Instruction instruction) => Br(instruction);

		private IEnumerable<AssemblyEntry> Call(Instruction instruction)
        {
			var methodReference = (MethodReference)instruction.Operand;

			// If it's not a MethodDefintion, it's not in the assembly we're compiling. Search for it in others.
			var method = methodReference as MethodDefinition
				?? Assemblies.CompilableTypes().CompilableMethods().SingleOrDefault(it => methodReference.FullName == it.FullName)
				?? throw new MissingMethodException($"Could not find '{methodReference.FullName}' in any assemblies.");
			var arity = method.Parameters.Count;
			
			if (method.TryGetFrameworkAttribute<OverrideWithStoreToSymbolAttribute>(out var overrideStore))
            {
				if (overrideStore.Strobe)
                {
					if (arity != 0)
                    {
						throw new InvalidOperationException($"Couldn't call {nameof(OverrideWithStoreToSymbolAttribute)}-marked '{method.Name}', methods to be replaced with a strobe must take 0 parameters");
                    }
					yield return new StoreTo(instruction, new GlobalLabel(overrideStore.Symbol, true));
                }
				else
                {
					if (arity != 1)
                    {
						throw new InvalidOperationException($"Couldn't call {nameof(OverrideWithStoreToSymbolAttribute)}-marked '{method.Name}', a non-strobe replacement should take 1 parameter.");
                    }
					yield return new PopToGlobal(instruction, new GlobalLabel(overrideStore.Symbol, true), LabelGenerator.ByteType, LabelGenerator.ByteSize, new(0), new(0));
				}
            }
			else
            {
				throw new InvalidOperationException($"Couldn't compile '{instruction}', 'call' has limited support now.");
            }
        }

		private IEnumerable<AssemblyEntry> Conv_U1(Instruction instruction)
        {
			// @TODO - Do we have to support this? We obviously can't expand byte+byte addition
			// to int+int addition.
			yield break;
        }

		private IEnumerable<AssemblyEntry> Dup(Instruction instruction)
        {
			yield return new Duplicate(instruction, new(0), new(0));
        }

		private IEnumerable<AssemblyEntry> Ldc_I4(Instruction instruction)
			=> LoadConstant(instruction);

		private IEnumerable<AssemblyEntry> Ldc_I4_S(Instruction instruction)
			=> LoadConstant(instruction);

		private IEnumerable<AssemblyEntry> Ldloc(Instruction instruction)
        {
			throw new NotImplementedException();
        }

		private IEnumerable<AssemblyEntry> Ldsfld(Instruction instruction)
        {
			var field = (FieldDefinition)instruction.Operand;
			var fieldLabel = LabelGenerator.Global(field);
			var fieldTypeLabel = LabelGenerator.Type(field.FieldType);
			var fieldSizeLabel = LabelGenerator.Size(field.FieldType);

			yield return new PushGlobal(instruction, fieldLabel, fieldTypeLabel, fieldSizeLabel);
		}

		private IEnumerable<AssemblyEntry> Nop(Instruction instruction)
        {
			yield break;
        }

		private IEnumerable<AssemblyEntry> Stsfld(Instruction instruction)
        {
			var field = (FieldReference)instruction.Operand;
			var fieldLabel = LabelGenerator.Global(field);
			var fieldTypeLabel = LabelGenerator.Type(field.FieldType);
			var fieldSizeLabel = LabelGenerator.Size(field.FieldType);

			yield return new PopToGlobal(instruction, fieldLabel, fieldTypeLabel, fieldSizeLabel, new(0), new(0));
        }

		private IEnumerable<AssemblyEntry> Unsupported(Instruction instruction) => throw new UnsupportedOpCodeException(instruction.OpCode);

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
	}
}
