using System;

namespace CSharpTo2600.Framework
{
    public static class Instructions
    {
        public static InstructionInfo Comment(string Comment)
        {
            return new InstructionInfo("; \{Comment}", 0);
        }

        /// <summary>
        /// Add with Carry [Absolute indexed] (4 cycles)
        /// </summary>
        public static InstructionInfo ADC(int Offset, Index IndexRegister)
        {
            return new InstructionInfo("ADC $\{Offset.ToString("X4")},\{IndexRegister}", 4);
        }

        /// <summary>
        /// Add with Carry [Zero-page indexed] (4 cycles)
        /// </summary>
        public static InstructionInfo ADC(byte Offset, Index IndexRegister)
        {
            if (IndexRegister != Index.X)
            {
                throw new ArgumentException("Invalid index register.", nameof(IndexRegister));
            }
            return new InstructionInfo("ADC $\{Offset.ToString("X2")},\{IndexRegister}", 4);
        }

        /// <summary>
        /// Clear Carry Flag (2 cycles)
        /// </summary>
        public static InstructionInfo CLC()
        {
            return new InstructionInfo("CLC", 2);
        }

        // Clear decimal bit.
        public static InstructionInfo CLD()
        {
            throw new NotImplementedException();
        }

        // Load accumulator with memory.
        // Immediate addressing.
        public static InstructionInfo LDA(byte Value, bool HexString = true)
        {
            // Hex numbers have a dollar sign before them in DASM.
            var ValString = HexString ? "$" + Value.ToString("X") : Value.ToString();
            var Text = string.Format("LDA #\{ValString}");
            return new InstructionInfo(Text, 2);
        }

        /// <summary>
        /// Load Accumulator with Memory [Zero-page] (3 cycles)
        /// </summary>
        public static InstructionInfo LDA(string Name, int Offset)
        {
            //@TODO - Technically the symbol could refer to any point in the ROM,
            // maybe not zero-page. Figure out a solution.
            return new InstructionInfo("LDA \{Name}+\{Offset}", 3);
        }

        // Load index X with memory.
        public static InstructionInfo LDX()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Push Accumulator (3 cycles)
        /// </summary>
        public static InstructionInfo PHA()
        {
            return new InstructionInfo("PHA", 3);
        }

        /// <summary>
        /// Pull Accumulator (4 cycles)
        /// </summary>
        public static InstructionInfo PLA()
        {
            return new InstructionInfo("PLA", 4);
        }

        /// <summary>
        /// Subtract with Carry [Absolute indexed] (4 cycles)
        /// </summary>
        public static InstructionInfo SBC(int Offset, Index IndexRegister)
        {
            return new InstructionInfo("SBC $\{Offset.ToString("X4")},\{IndexRegister}", 4);
        }

        // Set interrupt disable status.
        public static InstructionInfo SEI()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Set Carry Flag (2 cycles)
        /// </summary>
        public static InstructionInfo SEC()
        {
            return new InstructionInfo("SEC", 2);
        }

        // Store accumulator in memory.
        // Zero page addressing.
        public static InstructionInfo STA(byte Address)
        {
            return new InstructionInfo("STA $\{Address.ToString("X")}", 3);
        }

        /// <summary>
        /// Store Accumulator in Memory [Zero-page indexed] (4 cycles)
        /// </summary>
        public static InstructionInfo STA(byte Offset, Index IndexRegister)
        {
            return new InstructionInfo("STA $\{Offset.ToString("X2")},\{IndexRegister}", 4);
        }
        public static InstructionInfo STA(int Offset, Index IndexRegister)
        {
            return new InstructionInfo("STA $\{Offset.ToString("X4")},\{IndexRegister}", 4);
        }
        [Obsolete]
        public static InstructionInfo STA(string Name)
        {
            return new InstructionInfo("STA \{Name}", 3);
        }
        /// <summary>
        /// Store Accumulator in Memory [Zero-page] (3 cycles)
        /// </summary>
        public static InstructionInfo STA(string Name, int Offset)
        {
            return new InstructionInfo("STA \{Name}+\{Offset}", 3);
        }

        /// <summary>
        /// Transfer Accumulator to X Register (2 cycles)
        /// </summary>
        public static InstructionInfo TAX()
        {
            return new InstructionInfo("TAX", 2);
        }

        /// <summary>
        /// Transfer Stack Pointer to X Register (2 cycles)
        /// </summary>
        public static InstructionInfo TSX()
        {
            return new InstructionInfo("TSX", 2);
        }

        /// <summary>
        /// Transfer X Register to Accumulator (2 cycles)
        /// </summary>
        public static InstructionInfo TXA()
        {
            return new InstructionInfo("TXA", 2);
        }

        /// <summary>
        /// Transfer X Register to Stack Pointer (2 cycles)
        /// </summary>
        public static InstructionInfo TXS()
        {
            return new InstructionInfo("TXS", 2);
        }
    }

    public enum Index
    {
        X,
        Y
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
