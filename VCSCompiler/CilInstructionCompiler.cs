using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Reflection;
using System.Linq;
using VCSFramework.Assembly;
using static VCSFramework.Assembly.AssemblyFactory;
using Mono.Cecil;
using VCSFramework;

namespace VCSCompiler
{
	/// <summary>
	/// Contains the logic for compiling individual CIL instructions to 6502 instructions.
	/// </summary>
    internal class CilInstructionCompiler
    {
		private readonly IImmutableDictionary<Code, Func<Instruction, IEnumerable<AssemblyLine>>> MethodMap;
		private readonly IImmutableDictionary<string, ProcessedType> Types;

		public CilInstructionCompiler(IImmutableDictionary<string, ProcessedType> types)
		{
			MethodMap = CreateMethodMap();
			Types = types;
		}

		public IEnumerable<AssemblyLine> CompileInstruction(Instruction instruction) => MethodMap[instruction.OpCode.Code](instruction);

		private IImmutableDictionary<Code, Func<Instruction, IEnumerable<AssemblyLine>>> CreateMethodMap()
		{
			var dictionary = new Dictionary<Code, Func<Instruction, IEnumerable<AssemblyLine>>>();
			var typeInfo = typeof(CilInstructionCompiler).GetTypeInfo();
			var opCodes = Enum.GetValues(typeof(Code)).Cast<Code>();
			foreach (var opCode in opCodes)
			{
				var name = Enum.GetName(typeof(Code), opCode);
				if (opCode >= Code.Ldc_I4_0 && opCode <= Code.Ldc_I4_8)
				{
					name = "Ldc_I4";
				}
				var method = typeInfo.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
				dictionary[opCode]
					= (Func<Instruction, IEnumerable<AssemblyLine>>)method?.CreateDelegate(typeof(Func<Instruction, IEnumerable<AssemblyLine>>), this)
					?? Unsupported;
			}
			return dictionary.ToImmutableDictionary();
		}

		private IEnumerable<AssemblyLine> LoadConstant(Instruction instruction)
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
				switch(instruction.OpCode.Code)
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

		private IEnumerable<AssemblyLine> LoadConstant(byte value)
		{
			yield return LDA(value);
			yield return PHA();
		}

		private IEnumerable<AssemblyLine> Br_S(Instruction instruction)
		{
			yield return JMP(LabelGenerator.GetFromInstruction((Instruction)instruction.Operand));
		}

		private IEnumerable<AssemblyLine> Call(Instruction instruction)
		{
			// Could be either a MethodDefinition or MethodReference.
			dynamic method = instruction.Operand;

			var methodFullName = (string)method.DeclaringType.FullName;
			var processedSubroutine = Types[methodFullName].Subroutines.Single(s => s.FullName == method.FullName);
			// Don't directly compare types since we may have received a different Framework assembly than what this library was built against.
			dynamic overrideStore = processedSubroutine.FrameworkAttributes.SingleOrDefault(a => a.GetType().FullName == typeof(OverrideWithStoreToSymbolAttribute).FullName);
			if (overrideStore != null)
			{
				//TODO - We assume this is a 1-arg void method. Actually enforce this at the processing stage.
				yield return PLA();
				yield return STA(overrideStore.Symbol);
			}
			else
			{
				yield return JSR(LabelGenerator.GetFromMethod(method));
			}
		}

		/// <summary>
		/// Pushes a constant uint8 onto the stack.
		/// </summary>
		/// <remarks>The spec says to push an int32, but that's impractical.</remarks>
		private IEnumerable<AssemblyLine> Ldc_I4(Instruction instruction) => LoadConstant(instruction);

		/// <summary>
		/// Pushes a constant uint8 onto the stack.
		/// </summary>
		/// <remarks>The spec says to push an int32, but that's impractical.</remarks>
		private IEnumerable<AssemblyLine> Ldc_I4_S(Instruction instruction) => Ldc_I4(instruction);

		private IEnumerable<AssemblyLine> Ldsfld(Instruction instruction)
		{
			var fieldDefinition = (FieldDefinition)instruction.Operand;
			yield return LDA(LabelGenerator.GetFromField(fieldDefinition));
			yield return PHA();
		}

		private IEnumerable<AssemblyLine> Nop(Instruction instruction) => Enumerable.Empty<AssemblyLine>();

		private IEnumerable<AssemblyLine> Ret(Instruction instruction)
		{
			yield return RTS();
		}

		private IEnumerable<AssemblyLine> Stsfld(Instruction instruction)
		{
			yield return PLA();
			var fieldDefinition = (FieldDefinition)instruction.Operand;
			yield return STA(LabelGenerator.GetFromField(fieldDefinition));
		}

		private IEnumerable<AssemblyLine> Unsupported(Instruction instruction) => throw new UnsupportedOpCodeException(instruction.OpCode);
	}
}
