using System;

namespace CSharpTo2600.Framework
{
    public static class Instructions
    {
        public static InstructionInfo Comment(string Comment)
        {
            return new InstructionInfo("; \{Comment}", 0);
        }

        // Clear decimal bit.
        public static InstructionInfo CLD()
        {
            throw new NotImplementedException();
        }

        // Load index X with memory.
        public static InstructionInfo LDX()
        {
            throw new NotImplementedException();
        }

        // Set interrupt disable status.
        public static InstructionInfo SEI()
        {
            throw new NotImplementedException();
        }

        // Store accumulator in memory.
        // Zero page addressing.
        public static InstructionInfo STA(byte Address)
        {
            return new InstructionInfo("STA $\{Address.ToString("X")}", 3);
        }
        public static InstructionInfo STA(string Name)
        {
            return new InstructionInfo("STA \{Name}", 3);
        }

        // Transfer X to stack pointer.
        public static InstructionInfo TXS()
        {
            throw new NotImplementedException();
        }

        // Load accumulator with memory.
        // Immediate addressing.
        public static InstructionInfo LDA(byte Value, bool HexString = true)
        {
            // Hex numbers have a dollar sign before them in DASM.
            var ValString = HexString ? "$"+Value.ToString("X") : Value.ToString();
            var Text = string.Format("LDA #\{ValString}");
            return new InstructionInfo(Text, 2);
        }
    }

    public struct InstructionInfo
    {
        /// <summary>
        /// The full line of assembly code for this instruction.
        /// </summary>
        public readonly string Text;
        /// <summary>
        /// The number of CPU cycles it takes to execute this instruction.
        /// </summary>
        public readonly byte Cycles;

        public InstructionInfo(string Text, byte Cycles)
        {
            this.Text = Text;
            this.Cycles = Cycles;
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
