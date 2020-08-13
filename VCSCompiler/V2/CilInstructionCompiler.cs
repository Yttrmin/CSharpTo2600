#nullable enable
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using VCSFramework.V2;
using Instruction = Mono.Cecil.Cil.Instruction;

namespace VCSCompiler.V2
{
    internal class CilInstructionCompiler
    {
		private readonly ImmutableDictionary<Code, Func<Instruction, IEnumerable<AssemblyEntry>>> MethodMap;
		private readonly MethodDefinition MethodDefinition;

		public CilInstructionCompiler(MethodDefinition methodDefinition)
        {
			MethodMap = CreateMethodMap();
			MethodDefinition = methodDefinition;
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

		public IEnumerable<AssemblyEntry> CompileInstruction(Instruction instruction) => MethodMap[instruction.OpCode.Code](instruction);

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
						return LoadConstant(0);
					case Code.Ldc_I4_1:
						return LoadConstant(1);
					case Code.Ldc_I4_2:
						return LoadConstant(2);
					case Code.Ldc_I4_3:
						return LoadConstant(3);
					case Code.Ldc_I4_4:
						return LoadConstant(4);
					case Code.Ldc_I4_5:
						return LoadConstant(5);
					case Code.Ldc_I4_6:
						return LoadConstant(6);
					case Code.Ldc_I4_7:
						return LoadConstant(7);
					case Code.Ldc_I4_8:
						return LoadConstant(8);
				}
			}
			return LoadConstant(value);
		}

		private IEnumerable<AssemblyEntry> LoadConstant(byte value)
		{
			yield return new PushConstant(LabelGenerator.Constant(value), LabelGenerator.ByteSize(typeof(byte)));
		}

		private IEnumerable<AssemblyEntry> Call(Instruction instruction)
        {
			var method = (MethodReference)instruction.Operand;

			throw new NotImplementedException();
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
			var fieldSizeLabel = LabelGenerator.ByteSize(field.FieldType);

			yield return new PushGlobal(fieldLabel, fieldSizeLabel);
		}

		private IEnumerable<AssemblyEntry> Nop(Instruction instruction)
        {
			yield break;
        }

		private IEnumerable<AssemblyEntry> Stsfld(Instruction instruction)
        {
			var field = (FieldReference)instruction.Operand;
			yield return new PopToGlobal(LabelGenerator.Global(field));
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
