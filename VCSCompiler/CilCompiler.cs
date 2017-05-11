using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using Mono.Cecil.Cil;
using Mono.Cecil;

namespace VCSCompiler
{
    internal class CilCompiler
    {
		private static IEnumerable<AssemblyLine> CompileInstruction(Instruction instruction)
		{
			return CilInstructionCompiler.CompileInstruction(instruction);
		}
    }
}
