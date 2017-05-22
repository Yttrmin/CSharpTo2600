using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Reflection;
using static VCSCompiler.AssemblyFactory;
using System.Linq;

namespace VCSCompiler
{
	/// <summary>
	/// Contains the logic for compiling individual CIL instructions to 6502 instructions.
	/// </summary>
    internal static class CilInstructionCompiler
    {
		private static readonly IImmutableDictionary<Code, Func<Instruction, IEnumerable<AssemblyLine>>> MethodMap;

		static CilInstructionCompiler()
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
				var method = typeInfo.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
				dictionary[opCode] 
					= (Func<Instruction, IEnumerable<AssemblyLine>>)method?.CreateDelegate(typeof(Func<Instruction, IEnumerable<AssemblyLine>>))
					?? Unsupported;
			}
			MethodMap = dictionary.ToImmutableDictionary();
		}

		public static IEnumerable<AssemblyLine> CompileInstruction(Instruction instruction) => MethodMap[instruction.OpCode.Code](instruction);

		private static IEnumerable<AssemblyLine> LoadConstant(Instruction instruction)
		{
			byte value = 0;
			if (instruction.Operand != null)
			{
				try { value = Convert.ToByte((int)instruction.Operand); }
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

		private static IEnumerable<AssemblyLine> LoadConstant(byte value)
		{
			yield return LDA(value);
			yield return PHA();
		}

		/// <summary>
		/// Pushes a constant uint8 onto the stack.
		/// </summary>
		/// <remarks>The spec says to push an int32, but that's impractical.</remarks>
		private static IEnumerable<AssemblyLine> Ldc_I4(Instruction instruction) => LoadConstant(instruction);

		/// <summary>
		/// Pushes a constant uint8 onto the stack.
		/// </summary>
		/// <remarks>The spec says to push an int32, but that's impractical.</remarks>
		private static IEnumerable<AssemblyLine> Ldc_I4_S(Instruction instruction) => Ldc_I4(instruction);

		private static IEnumerable<AssemblyLine> Nop(Instruction instruction) => Enumerable.Empty<AssemblyLine>();

		private static IEnumerable<AssemblyLine> Unsupported(Instruction instruction) => throw new UnsupportedOpCodeException(instruction.OpCode);
	}
}
