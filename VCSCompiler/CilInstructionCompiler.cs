using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Reflection;
using static VCSCompiler.AssemblyFactory;

namespace VCSCompiler
{
	/// <summary>
	/// Contains the logic for compiling individual CIL instructions to 6502 instructions.
	/// </summary>
    internal static class CilInstructionCompiler
    {
		private static readonly IImmutableDictionary<OpCode, Func<Instruction, IEnumerable<AssemblyInstruction>>> MethodMap;

		static CilInstructionCompiler()
		{
			var dictionary = new Dictionary<OpCode, Func<Instruction, IEnumerable<AssemblyInstruction>>>();
			var typeInfo = typeof(CilInstructionCompiler).GetTypeInfo();
			var opCodes = typeof(OpCodes).GetTypeInfo().GetFields(BindingFlags.Public | BindingFlags.Static);
			foreach (var opCode in opCodes)
			{
				var method = typeInfo.GetMethod(opCode.Name, BindingFlags.NonPublic | BindingFlags.Static);
				dictionary[(OpCode)opCode.GetValue(null)] 
					= (Func<Instruction, IEnumerable<AssemblyInstruction>>)method?.CreateDelegate(typeof(Func<Instruction, IEnumerable<AssemblyInstruction>>))
					?? Unsupported;
			}
			MethodMap = dictionary.ToImmutableDictionary();
		}

		public static IEnumerable<AssemblyLine> CompileInstruction(Instruction instruction) => MethodMap[instruction.OpCode](instruction);

		/// <summary>
		/// Pushes a constant uint8 onto the stack.
		/// </summary>
		/// <remarks>The spec says to push an int32, but that's impractical.</remarks>
		private static IEnumerable<AssemblyInstruction> Ldc_I4(Instruction instruction)
		{
			byte value;
			try { value = Convert.ToByte((int)instruction.Operand); }
			catch (OverflowException e)
			{
				throw new InvalidInstructionException(instruction, $"Constant value '{instruction.Operand}'must fit in a byte!");
			}

			yield return LDA(value);
			yield return PHA();
		}

		/// <summary>
		/// Pushes a constant uint8 onto the stack.
		/// </summary>
		/// <remarks>The spec says to push an int32, but that's impractical.</remarks>
		private static IEnumerable<AssemblyInstruction> Ldc_I4_S(Instruction instruction) => Ldc_I4(instruction);

		private static IEnumerable<AssemblyInstruction> Unsupported(Instruction instruction) => throw new UnsupportedOpCodeException(instruction.OpCode);
	}
}
