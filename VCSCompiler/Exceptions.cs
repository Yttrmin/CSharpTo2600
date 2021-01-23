using Mono.Cecil.Cil;
using System;

namespace VCSCompiler
{
    public class FatalCompilationException : Exception
    {
		public FatalCompilationException(string message) : base(message) { }
    }

	public class InvalidInstructionException : FatalCompilationException
	{
		public InvalidInstructionException(Instruction instruction, string message)
			: base($"{instruction}  is invalid! {message}") { }
	}

	public class UnsupportedOpCodeException : FatalCompilationException
	{
		public UnsupportedOpCodeException(OpCode opCode)
			: base($"Opcode {opCode} is not supported.") { }
	}
}
