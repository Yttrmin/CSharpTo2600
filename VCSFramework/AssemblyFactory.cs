using System;

namespace VCSFramework.Assembly
{
	[DoNotCompile]
    internal static class AssemblyFactory
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

		public static PsuedoOp Subroutine(string Label)
		{
			return new PsuedoOp($"{Label} subroutine");
		}

		public static PsuedoOp Include(string FileName)
        {
            return new PsuedoOp($"\tinclude \"{FileName}\"");
        }

        public static PsuedoOp Word(Symbol Label)
        {
            return new PsuedoOp($"\t.word {Label.Name}");
        }

		public static PsuedoOp Word(string Label)
		{
			return new PsuedoOp($"\t.word {Label}");
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
        public static AssemblyInstruction ADC(byte Constant)
        {
            return new AssemblyInstruction("ADC", $"#${Constant.ToString("X2")}", 2, 2);
        }

		/// <summary>
		/// Add with Carry [Zero-page] (2 cycles)
		/// </summary>
		public static AssemblyInstruction ADC(string Label)
		{
			return new AssemblyInstruction("ADC", Label, 3, 2);
		}

		/// <summary>
		/// Add with Carry [Zero-page indexed] (4 cycles)
		/// </summary>
		public static AssemblyInstruction ADC(byte Offset, Index IndexRegister)
        {
            if (IndexRegister != Index.X)
            {
                throw new ArgumentException("Invalid index register.", nameof(IndexRegister));
            }
            return new AssemblyInstruction("ADC", $"${Offset.ToString("X2")},{IndexRegister}", 4, 2);
        }

		/// <summary>
		/// Branch on Carry Set (2-4 cycles)
		/// </summary>
	    public static AssemblyInstruction BCS(string label)
	    {
		    return new AssemblyInstruction("BCS", label, 4, 2);
	    }

		/// <summary>
		/// Branch if Equal (2-4 cycles)
		/// </summary>
	    public static AssemblyInstruction BEQ(string label)
	    {
		    return new AssemblyInstruction("BEQ", label, 4, 2);
	    }

		/// <summary>
		/// Test BITs (3 cycles)
		/// </summary>
		public static AssemblyInstruction BIT(byte address)
		{
			return new AssemblyInstruction("BIT", $"${address.ToString("X2")}", 3, 2);
		}

	    /// <summary>
	    /// Branch if Not Equal (2-4 cycles)
	    /// </summary>
	    public static AssemblyInstruction BNE(string label)
	    {
		    return new AssemblyInstruction("BNE", label, 4, 2);
	    }

		/// <summary>
		/// Branch if Not Equal (2-4 cycles)
		/// </summary>
		public static AssemblyInstruction BNE(Symbol Label)
        {
            // 4 if branch to new page.
            if (Label.Value.HasValue)
            {
                throw new ArgumentException("Can only branch to label.");
            }
	        return BNE(Label.Name);
        }

        /// <summary>
        /// Clear Carry Flag (2 cycles)
        /// </summary>
        public static AssemblyInstruction CLC()
        {
            return new AssemblyInstruction("CLC", 2, 1);
        }

        /// <summary>
        /// Clear Decimal Mode (2 cycles)
        /// </summary>
        public static AssemblyInstruction CLD()
        {
            return new AssemblyInstruction("CLD", 2, 1);
        }

		/// <summary>
		/// Compare Accumulator [Zero-page] (3 cycles)
		/// </summary>
	    public static AssemblyInstruction CMP(string label)
		{
			return new AssemblyInstruction("CMP", label, 3, 2);
		}

        /// <summary>
        /// Compare X Register [Immediate] (2 cycles)
        /// </summary>
        public static AssemblyInstruction CPX(byte Value)
        {
            return new AssemblyInstruction("CPX", $"#${Value.ToString("X2")}", 2, 2);
        }

        /// <summary>
        /// Decrement X Register (2 cycles)
        /// </summary>
        public static AssemblyInstruction DEX()
        {
            return new AssemblyInstruction("DEX", 2, 1);
        }

        /// <summary>
        /// Jump [Absolute] (3 cycles)
        /// </summary>
        public static AssemblyInstruction JMP(Symbol Label)
        {
            return new AssemblyInstruction("JMP", Label.Name, 3, 3);
        }

		/// <summary>
		/// Jump [Absolute] (3 cycles)
		/// </summary>
		public static AssemblyInstruction JMP(string Label)
		{
			return new AssemblyInstruction("JMP", Label, 3, 3);
		}

		/// <summary>
		/// Jump to Subroutine [Absolute] (6 cycles)
		/// </summary>
		public static AssemblyInstruction JSR(Symbol Label)
        {
            return new AssemblyInstruction("JSR", Label.Name, 6, 3);
        }

		/// <summary>
		/// Jump to Subroutine [Absolute] (6 cycles)
		/// </summary>
		public static AssemblyInstruction JSR(string Label)
		{
			return new AssemblyInstruction("JSR", Label, 6, 3);
		}

		/// <summary>
		/// Load Accumulator with Memory [Immediate] (2 cycles)
		/// </summary>
		public static AssemblyInstruction LDA(byte Value)
        {
            return new AssemblyInstruction("LDA", $"#${Value.ToString("X2")}", 2, 2);
        }

        /// <summary>
        /// Load Accumulator with Memory [Zero-page] (3 cycles)
        /// </summary>
        public static AssemblyInstruction LDA(Symbol Symbol, int Offset = 0)
        {
	        //@TODO - Technically the symbol could refer to any point in the ROM,
            // maybe not zero-page. Figure out a solution.
	        var Argument = Offset == 0 ? Symbol.Name : $"{Symbol.Name}+{Offset}";
	        return new AssemblyInstruction("LDA", Argument, 3, 2);
        }

		/// <summary>
		/// Load Accumulator with Memory [Zero-page] (3 cycles)
		/// </summary>
		public static AssemblyInstruction LDA(string Symbol, int Offset = 0)
		{
			//@TODO - Technically the symbol could refer to any point in the ROM,
			// maybe not zero-page. Figure out a solution.
			var Argument = Offset == 0 ? Symbol : $"{Symbol}+{Offset}";
			return new AssemblyInstruction("LDA", Argument, 3, 2);
		}

		// <summary>
		/// Load Accumulator with Memory [Zero-page indexed] (4 cycles)
		/// </summary>
		public static AssemblyInstruction LDA(byte Offset, Index IndexRegister)
		{
			if (IndexRegister != Index.X)
			{
				throw new ArgumentException("LDA zero-page indexed must use X register", nameof(IndexRegister));
			}
			return new AssemblyInstruction("LDA", $"${Offset.ToString("X2")},{IndexRegister}", 4, 2);
		}

		/// <summary>
		/// Load X Register [Immediate] (2 cycles)
		/// </summary>
		public static AssemblyInstruction LDX(byte Value)
        {
            return new AssemblyInstruction("LDX", $"#${Value.ToString("X2")}", 2, 2);
        }

        /// <summary>
        /// Load Y Register [Immediate] (2 cycles)
        /// </summary>
        public static AssemblyInstruction LDY(byte Value)
        {
            return new AssemblyInstruction("LDY", $"#${Value.ToString("X2")}", 2, 2);
        }

		/// <summary>
		/// No-op [Implied] (2 cycles)
		/// </summary>
		public static AssemblyInstruction NOP()
		{
			return new AssemblyInstruction("NOP", 2, 1);
		}

        /// <summary>
        /// Push Accumulator (3 cycles)
        /// </summary>
        public static AssemblyInstruction PHA()
        {
            return new AssemblyInstruction("PHA", 3, 1);
        }

        /// <summary>
        /// Pull Accumulator (4 cycles)
        /// </summary>
        public static AssemblyInstruction PLA()
        {
            return new AssemblyInstruction("PLA", 4, 1);
        }

        /// <summary>
        /// Return from Subroutine [Implied] (6 cycles)
        /// </summary>
        public static AssemblyInstruction RTS()
        {
            return new AssemblyInstruction("RTS", 6, 1);
        }

		/// <summary>
		/// Subtract with Carry [Zero-page] (3 cycles)
		/// </summary>
		/// <returns></returns>
	    public static AssemblyInstruction SBC(string label)
	    {
		    return new AssemblyInstruction("SBC", label, 3, 2);
	    }

        /// <summary>
        /// Subtract with Carry [Zero-page indexed] (4 cycles)
        /// </summary>
        //@TODO - Can only be zp-indexed with X register. But if we remove that
        // parameter it'll conflict with the future immediate mode one.
        public static AssemblyInstruction SBC(byte Offset, Index IndexRegister)
        {
            return new AssemblyInstruction("SBC", $"${Offset.ToString("X2")},{IndexRegister}", 4, 2);
        }

        /// <summary>
        /// Set Carry Flag (2 cycles)
        /// </summary>
        public static AssemblyInstruction SEC()
        {
            return new AssemblyInstruction("SEC", 2, 1);
        }

        /// <summary>
        /// Set Interrupt Disable (2 cycles)
        /// </summary>
        public static AssemblyInstruction SEI()
        {
            return new AssemblyInstruction("SEI", 2, 1);
        }

        /// <summary>
        /// Store Accumulator [Zero-page] (3 cycles)
        /// </summary>
        public static AssemblyInstruction STA(byte Address)
        {
            return new AssemblyInstruction("STA", $"${Address.ToString("X2")}", 3, 2);
        }

		/// <summary>
		/// Store Accumulator [Zero-page] (3 cycles)
		/// </summary>
		public static AssemblyInstruction STA(string Label, int Offset = 0)
		{
			var Argument = Offset == 0 ? Label : $"{Label}+{Offset}";
			return new AssemblyInstruction("STA", Argument, 3, 2);
		}

		/// <summary>
		/// Store Accumulator [Zero-page] (3 cycles)
		/// </summary>
		public static AssemblyInstruction STA(Symbol Symbol, int Offset = 0)
		{
			var Argument = Offset == 0 ? Symbol.Name : $"{Symbol.Name}+{Offset}";
			return new AssemblyInstruction("STA", Argument, 3, 2);
		}

        /// <summary>
        /// Store Accumulator [Zero-page indexed] (4 cycles)
        /// </summary>
        public static AssemblyInstruction STA(byte Offset, Index IndexRegister)
        {
			if (IndexRegister != Index.X)
			{
				throw new ArgumentException("STA zero-page indexed must use X register", nameof(IndexRegister));
			}
            return new AssemblyInstruction("STA", $"${Offset.ToString("X2")},{IndexRegister}", 4, 2);
        }

		/// <summary>
		/// Store X Register [Zero-page indexed] (4 cycles)
		/// </summary>
		public static AssemblyInstruction STX(byte Offset, Index IndexRegister)
		{
			if (IndexRegister != Index.Y)
			{
				throw new ArgumentException("STX zero-page indexed must use Y register", nameof(IndexRegister));
			}

			return new AssemblyInstruction("STX", $"${Offset.ToString("X2")},{IndexRegister}", 4, 2);
		}

        /// <summary>
        /// Transfer Accumulator to X Register (2 cycles)
        /// </summary>
        public static AssemblyInstruction TAX()
        {
            return new AssemblyInstruction("TAX", 2, 1);
        }

		/// <summary>
		/// Transfer Accumulator to Y Register (2 cycles)
		/// </summary>
		public static AssemblyInstruction TAY()
		{
			return new AssemblyInstruction("TAY", 2, 1);
		}

		/// <summary>
		/// Transfer Stack Pointer to X Register (2 cycles)
		/// </summary>
		public static AssemblyInstruction TSX()
        {
            return new AssemblyInstruction("TSX", 2, 1);
        }

        /// <summary>
        /// Transfer X Register to Accumulator (2 cycles)
        /// </summary>
        public static AssemblyInstruction TXA()
        {
            return new AssemblyInstruction("TXA", 2, 1);
        }

        /// <summary>
        /// Transfer X Register to Stack Pointer (2 cycles)
        /// </summary>
        public static AssemblyInstruction TXS()
        {
            return new AssemblyInstruction("TXS", 2, 1);
        }
        #endregion
    }
}
