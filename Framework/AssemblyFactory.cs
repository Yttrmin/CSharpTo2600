using System;

namespace CSharpTo2600.Framework.Assembly
{
    public static class AssemblyFactory
    {
        #region Trivia
        public static Trivia Comment(string Comment, int IndentationLevel = 1)
        {
            return new Trivia($"{new string('\t', IndentationLevel)}; {Comment}");
        }
        public static Trivia BlankLine()
        {
            return new Trivia(String.Empty);
        }
        #endregion

        #region Symbols
        public static Symbol Label(string Name)
        {
            return new Symbol(Name);
        }
        public static Symbol DefineSymbol(string Name, int Value)
        {
            if (Value < ushort.MinValue || Value > ushort.MaxValue)
            {
                throw new ArgumentException("Value must fit in a short.");
            }
            return new Symbol(Name, (ushort)Value);
        }
        #endregion

        #region PsuedoOps
        public static PsuedoOp Processor()
        {
            return new PsuedoOp("\tprocessor 6502");
        }

        public static PsuedoOp Org(int Address)
        {
            //@TODO - Min?
            if (Address > 0xFFFF)
            {
                throw new ArgumentException($"org address is out of range: 0x{Address.ToString("X4")}");
            }
            return new PsuedoOp($"\torg ${Address.ToString("X4")}");
        }

        public static PsuedoOp Repeat(int Count)
        {
            return new PsuedoOp($"\trepeat {Count}");
        }

        public static PsuedoOp Repend()
        {
            return new PsuedoOp("\trepend");
        }

        public static PsuedoOp Subroutine(Symbol Label)
        {
            return new PsuedoOp($"{Label.Name} subroutine");
        }

        public static PsuedoOp Include(string FileName)
        {
            return new PsuedoOp($"\tinclude \"{FileName}\"");
        }

        public static PsuedoOp Word(Symbol Label)
        {
            return new PsuedoOp($"\t.word {Label.Name}");
        }
        #endregion

        #region Instructions
        public enum Index
        {
            X,
            Y
        }

        /// <summary>
        /// Add with Carry [Immediate] (2 cycles)
        /// </summary>
        public static Instruction ADC(byte Constant)
        {
            return new Instruction("ADC", $"#${Constant.ToString("X2")}", 2);
        }

        /// <summary>
        /// Add with Carry [Zero-page indexed] (4 cycles)
        /// </summary>
        public static Instruction ADC(byte Offset, Index IndexRegister)
        {
            if (IndexRegister != Index.X)
            {
                throw new ArgumentException("Invalid index register.", nameof(IndexRegister));
            }
            return new Instruction("ADC", $"${Offset.ToString("X2")},{IndexRegister}", 4);
        }

        /// <summary>
        /// Branch if Not Equal (2-4 cycles)
        /// </summary>
        public static Instruction BNE(Symbol Label)
        {
            // 4 if branch to new page.
            if (Label.Value.HasValue)
            {
                throw new ArgumentException("Can only branch to label.");
            }
            return new Instruction("BNE", Label.Name, 4);
        }

        /// <summary>
        /// Clear Carry Flag (2 cycles)
        /// </summary>
        public static Instruction CLC()
        {
            return new Instruction("CLC", 2);
        }

        /// <summary>
        /// Clear Decimal Mode (2 cycles)
        /// </summary>
        public static Instruction CLD()
        {
            return new Instruction("CLD", 2);
        }

        /// <summary>
        /// Compare X Register [Immediate] (2 cycles)
        /// </summary>
        public static Instruction CPX(byte Value)
        {
            return new Instruction("CPX", $"#${Value.ToString("X2")}", 2);
        }

        /// <summary>
        /// Decrement X Register (2 cycles)
        /// </summary>
        public static Instruction DEX()
        {
            return new Instruction("DEX", 2);
        }

        /// <summary>
        /// Jump [Absolute] (3 cycles)
        /// </summary>
        public static Instruction JMP(Symbol Label)
        {
            return new Instruction("JMP", Label.Name, 3);
        }

        /// <summary>
        /// Jump to Subroutine [Absolute] (6 cycles)
        /// </summary>
        public static Instruction JSR(Symbol Label)
        {
            return new Instruction("JSR", Label.Name, 6);
        }

        /// <summary>
        /// Load Accumulator with Memory [Immediate] (2 cycles)
        /// </summary>
        public static Instruction LDA(byte Value)
        {
            return new Instruction("LDA", $"#${Value.ToString("X2")}", 2);
        }

        /// <summary>
        /// Load Accumulator with Memory [Zero-page] (3 cycles)
        /// </summary>
        public static Instruction LDA(Symbol Symbol, int Offset = 0)
        {
            //@TODO - Technically the symbol could refer to any point in the ROM,
            // maybe not zero-page. Figure out a solution.
            string Argument;
            if (Offset == 0)
            {
                Argument = Symbol.Name;
            }
            else
            {
                Argument = $"{Symbol.Name}+{Offset}";
            }
            return new Instruction("LDA", Argument, 3);
        }

        /// <summary>
        /// Load X Register [Immediate] (2 cycles)
        /// </summary>
        public static Instruction LDX(byte Value)
        {
            return new Instruction("LDX", $"#${Value.ToString("X2")}", 2);
        }

        /// <summary>
        /// Load Y Register [Immediate] (2 cycles)
        /// </summary>
        public static Instruction LDY(byte Value)
        {
            return new Instruction("LDY", $"#${Value.ToString("X2")}", 2);
        }

        /// <summary>
        /// Push Accumulator (3 cycles)
        /// </summary>
        public static Instruction PHA()
        {
            return new Instruction("PHA", 3);
        }

        /// <summary>
        /// Pull Accumulator (4 cycles)
        /// </summary>
        public static Instruction PLA()
        {
            return new Instruction("PLA", 4);
        }

        /// <summary>
        /// Return from Subroutine [Implied] (6 cycles)
        /// </summary>
        /// <returns></returns>
        public static Instruction RTS()
        {
            return new Instruction("RTS", 6);
        }

        /// <summary>
        /// Subtract with Carry [Zero-page indexed] (4 cycles)
        /// </summary>
        //@TODO - Can only be zp-indexed with X register. But if we remove that
        // parameter it'll conflict with the future immediate mode one.
        public static Instruction SBC(byte Offset, Index IndexRegister)
        {
            return new Instruction("SBC", $"${Offset.ToString("X2")},{IndexRegister}", 4);
        }

        /// <summary>
        /// Set Interrupt Disable (2 cycles)
        /// </summary>
        public static Instruction SEI()
        {
            return new Instruction("SEI", 2);
        }

        /// <summary>
        /// Set Carry Flag (2 cycles)
        /// </summary>
        public static Instruction SEC()
        {
            return new Instruction("SEC", 2);
        }

        /// <summary>
        /// Store Accumulator [Zero-page] (3 cycles)
        /// </summary>
        public static Instruction STA(byte Address)
        {
            return new Instruction("STA", $"${Address.ToString("X2")}", 3);
        }

        /// <summary>
        /// Store Accumulator [Zero-page] (3 cycles)
        /// </summary>
        public static Instruction STA(Symbol Symbol, int Offset = 0)
        {
            string Argument;
            if (Offset == 0)
            {
                Argument = Symbol.Name;
            }
            else
            {
                Argument = $"{Symbol.Name}+{Offset}";
            }
            return new Instruction("STA", Argument, 3);
        }

        /// <summary>
        /// Store Accumulator [Zero-page indexed] (4 cycles)
        /// </summary>
        public static Instruction STA(byte Offset, Index IndexRegister)
        {
            return new Instruction("STA", $"${Offset.ToString("X2")},{IndexRegister}", 4);
        }

        /// <summary>
        /// Transfer Accumulator to X Register (2 cycles)
        /// </summary>
        public static Instruction TAX()
        {
            return new Instruction("TAX", 2);
        }

        /// <summary>
        /// Transfer Stack Pointer to X Register (2 cycles)
        /// </summary>
        public static Instruction TSX()
        {
            return new Instruction("TSX", 2);
        }

        /// <summary>
        /// Transfer X Register to Accumulator (2 cycles)
        /// </summary>
        public static Instruction TXA()
        {
            return new Instruction("TXA", 2);
        }

        /// <summary>
        /// Transfer X Register to Stack Pointer (2 cycles)
        /// </summary>
        public static Instruction TXS()
        {
            return new Instruction("TXS", 2);
        }
        #endregion
    }
}
