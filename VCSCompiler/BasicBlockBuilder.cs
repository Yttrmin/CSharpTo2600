using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace VCSCompiler
{
    class BasicBlockBuilder
    {
		private static readonly OpCode[] ConditionalBranchInstructions
			= new[] 
			{
				OpCodes.Brfalse, OpCodes.Brfalse_S, OpCodes.Brtrue, OpCodes.Brtrue_S,
				OpCodes.Beq, OpCodes.Beq_S,
				OpCodes.Bge, OpCodes.Bge_S, OpCodes.Bge_Un, OpCodes.Bge_Un_S,
				OpCodes.Bgt, OpCodes.Bgt_S, OpCodes.Bgt_Un, OpCodes.Bgt_Un_S,
				OpCodes.Ble, OpCodes.Ble_S, OpCodes.Ble_Un, OpCodes.Ble_Un_S,
				OpCodes.Blt, OpCodes.Blt_S, OpCodes.Blt_Un, OpCodes.Blt_Un_S
			};

		private static readonly OpCode[] UnconditionalBranchInstructions
			= new[]
			{
				OpCodes.Br, OpCodes.Br_S, OpCodes.Jmp, OpCodes.Call, OpCodes.Calli, OpCodes.Callvirt, OpCodes.Ret
			};

		private static readonly OpCode[] AllBranchInstructions = ConditionalBranchInstructions.Concat(UnconditionalBranchInstructions).ToArray();

		public static void Build(MethodDefinition method)
		{
			var leaders = new List<Instruction>();

			foreach(var instruction in method.Body.Instructions)
			{
				// The first instruction in a method is always a leader.
				if (method.Body.Instructions.First() == instruction)
				{
					leaders.Add(instruction);
					Console.Write("LEADER >>");
				}

				// The targets of branches are leaders.
				if (AllBranchInstructions.Contains(instruction.OpCode))
				{
					if (instruction.Operand is Instruction targetInstruction)
					{
						leaders.Add(targetInstruction);
						Console.Write($"LEADERIFY {targetInstruction} <<");
					}
				}

				// The instruction that follows a branch instruction is a leader.
				if (AllBranchInstructions.Contains(instruction.Previous?.OpCode ?? default(OpCode)))
				{
					leaders.Add(instruction);
					Console.Write("LEADER >>");
				}

				Console.WriteLine(instruction);
			}

			// Make sure leaders are in order or you'll get strange results.
			leaders = leaders.OrderBy(i => i.Offset).ToList();
			
			// Pair leaders up and turn them into blocks.
			// Blocks span from leaderA -> leaderB-1.
			var blocks = new List<BasicBlock>();
			var pairs = leaders.Zip(leaders.Skip(1), Tuple.Create);
			foreach(var leaderPair in pairs)
			{
				var start = method.Body.Instructions.IndexOf(leaderPair.Item1);
				var end = method.Body.Instructions.IndexOf(leaderPair.Item2);
				var instructions = method.Body.Instructions.Skip(start).Take(end - start);
				blocks.Add(new BasicBlock(instructions));
			}

			// Add final block based off final leader and the remaining portion of instructions.
			var finalStart = method.Body.Instructions.IndexOf(leaders.Last());
			var finalInstructions = method.Body.Instructions.Skip(finalStart);
			blocks.Add(new BasicBlock(finalInstructions));
		}
    }

	internal class BasicBlock
	{
		public IImmutableList<Instruction> Instructions { get; }

		public BasicBlock(IEnumerable<Instruction> instructions)
		{
			Instructions = instructions.ToImmutableList();
		}
	}
}
