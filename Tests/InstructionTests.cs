using System.Collections.Generic;
using CSharpTo2600.Framework.Assembly;
using NUnit.Framework;

namespace CSharpTo2600.UnitTests
{
    class InstructionTests : AssemblyTests
    {
        // Arbitrary magic number. DASM will error if we accidentally pass it as decimal.
        private const byte TestValue = 0x9A;

        [Test]
        public void ADCImmediateUnsigned(
            [Values(0, 0xFF)] byte a,
            [Values(0, 0x80, 0xFF)] byte b)
        {
            RunProgramFromFragment(false, AssemblyFactory.LDA(a), AssemblyFactory.CLC(), AssemblyFactory.ADC(b));
            var ExpectedResult = (byte)(a + b);
            Assert.AreEqual(ExpectedResult, CPU.Accumulator);
            Assert.AreEqual(a + b > byte.MaxValue, CPU.CarryFlag);
        }

        [Test]
        public void ADCZeroPageIndexedUnsigned(
            [Values(0, 0xFF)] byte a,
            [Values(0, 0x80, 0xFF)] byte b,
            [Values(0, 0xFF)] byte Offset)
        {
            const byte Address = 0xA0;
            var XValue = (byte)(Address - Offset);
            // Stores b at some arbitrary zero-page address.
            // X value is loaded with whatever added to Offset would equal the address.
            // a is loaded into accumulator, carry cleared, perform add.
            RunProgramFromFragment(true, AssemblyFactory.LDA(b), AssemblyFactory.STA(Address),
                AssemblyFactory.LDX(XValue),
                AssemblyFactory.LDA(a), AssemblyFactory.CLC(), AssemblyFactory.ADC(Offset, AssemblyFactory.Index.X));
            var ExpectedResult = (byte)(a + b);
            Assert.AreEqual(ExpectedResult, CPU.Accumulator);
            Assert.AreEqual(a + b > byte.MaxValue, CPU.CarryFlag);
        }

        [Test]
        public void BNE(
            [Values(0, 0xFF)] byte Value)
        {
            // if (Value != 0)
            //   EndProgram
            // else
            //   DEX
            var BranchTarget = AssemblyFactory.Label("Target");
            RunProgramFromFragment(true, AssemblyFactory.LDX(Value), AssemblyFactory.BNE(BranchTarget), AssemblyFactory.DEX(), BranchTarget);
            byte ExpectedValue;
            if (Value != 0)
                ExpectedValue = Value;
            else
                ExpectedValue = (byte)(Value - 1);

            Assert.AreEqual(ExpectedValue, CPU.XRegister);
        }

        [Test]
        public void CLC()
        {
            RunProgramFromFragment(false, AssemblyFactory.CLC());
            Assert.False(CPU.CarryFlag);
        }

        [Test]
        public void CLD()
        {
            RunProgramFromFragment(false, AssemblyFactory.CLD());
            Assert.False(CPU.DecimalFlag);
        }

        [Test]
        public void CPXImmediate(
            [Values(0, 0xFF)] byte a,
            [Values(0, 0xFF)] byte b)
        {
            RunProgramFromFragment(true, AssemblyFactory.LDX(a), AssemblyFactory.CPX(b));
            var SubtractResult = a - b;

            Assert.AreEqual(SubtractResult == 0, CPU.ZeroFlag);
            Assert.AreEqual(a >= b, CPU.CarryFlag);
        }

        [Test]
        public void DEX(
            [Values(0, 0xFF)] byte StartValue)
        {
            RunProgramFromFragment(true, AssemblyFactory.LDX(StartValue), AssemblyFactory.DEX());
            var ExpectedValue = (byte)unchecked(StartValue - 1);
            Assert.AreEqual(ExpectedValue, CPU.XRegister);
        }

        [Test]
        // JMP is relied on by literally every test. I'm pretty certain it works.
        public void JMPAbsolute()
        {
            RunProgramFromFragment(false);
            Assert.AreEqual(ProgramEnd.Value.Value, CPU.ProgramCounter);
        }

        [Test]
        public void LDAImmediate(
            // Always make sure some nibbles >=A are included to ensure DASM is
            // treating it all as hex and not decimal.
            [Values(0, 0x80, 0xFF)] byte Value)
        {
            RunProgramFromFragment(false, AssemblyFactory.LDA(Value));
            Assert.AreEqual(Value, CPU.Accumulator);
        }

        [Test]
        public void LDXImmediate(
            [Values(0, 0x80, 0xFF)] byte Value)
        {
            RunProgramFromFragment(false, AssemblyFactory.LDX(Value));
            Assert.AreEqual(Value, CPU.XRegister);
        }

        [Test]
        public void LDYImmediate(
            [Values(0, 0x80, 0xFF)] byte Value)
        {
            RunProgramFromFragment(false, AssemblyFactory.LDY(Value));
            Assert.AreEqual(Value, CPU.YRegister);
        }

        [Test]
        public void PHA()
        {
            // Fill up RIOT RAM with values equaling the (zero page) address.
            // Read it back from the simulator and check it was all pushed correctly.
            var Lines = new List<AssemblyLine>();
            for (byte i = 0xFF; i >= 0x80; i--)
            {
                Lines.Add(AssemblyFactory.LDA(i));
                Lines.Add(AssemblyFactory.PHA());
            }
            RunProgramFromFragment(Lines, true);
            for (var i = 0xFF; i >= 0x80; i--)
            {
                Assert.AreEqual(i, CPU.Memory.ReadValue(i));
            }
        }

        [Test]
        public void PLA()
        {
            RunProgramFromFragment(true, AssemblyFactory.LDA(TestValue), AssemblyFactory.PHA(),
                AssemblyFactory.LDA(0), AssemblyFactory.PLA());
            Assert.AreEqual(TestValue, CPU.Accumulator);
            Assert.AreEqual(0xFF, CPU.StackPointer);
        }

        [Test]
        public void SBCZeroPageIndexedUnsigned(
            [Values(0, 0xFF)] byte a,
            [Values(0, 0x80, 0xFF)] byte b,
            [Values(0, 0xFF)] byte Offset)
        {
            const byte Address = 0xA0;
            var XValue = (byte)(Address - Offset);
            // Stores b at some arbitrary zero-page address.
            // X value is loaded with whatever added to Offset would equal the address.
            // a is loaded into accumulator, carry set, perform subtract.
            RunProgramFromFragment(true, AssemblyFactory.LDA(b), AssemblyFactory.STA(Address),
                AssemblyFactory.LDX(XValue),
                AssemblyFactory.LDA(a), AssemblyFactory.SEC(), AssemblyFactory.SBC(Offset, AssemblyFactory.Index.X));
            var ExpectedResult = (byte)(a - b);
            Assert.AreEqual(ExpectedResult, CPU.Accumulator);
            Assert.AreEqual(a - b < 0, !CPU.CarryFlag);
        }

        [Test]
        public void STAZeroPage(
            [Values(0, 0x80, 0xFF)] byte Address)
        {
            RunProgramFromFragment(false, AssemblyFactory.LDA(TestValue), AssemblyFactory.STA(Address));
            Assert.AreEqual(TestValue, CPU.Memory.ReadValue(Address));
        }

        [Test]
        public void SEC()
        {
            RunProgramFromFragment(false, AssemblyFactory.SEC());
            Assert.True(CPU.CarryFlag);
        }

        [Test]
        public void SEI()
        {
            RunProgramFromFragment(false, AssemblyFactory.SEI());
            Assert.True(CPU.DisableInterruptFlag);
        }

        [Test]
        public void TAX()
        {
            RunProgramFromFragment(false, AssemblyFactory.LDA(TestValue), AssemblyFactory.TAX());
            Assert.AreEqual(CPU.Accumulator, CPU.XRegister);
        }

        [Test]
        public void TSX()
        {
            RunProgramFromFragment(true, AssemblyFactory.TSX());
            Assert.AreEqual(CPU.StackPointer, CPU.XRegister);
        }

        [Test]
        public void TXA()
        {
            RunProgramFromFragment(false, AssemblyFactory.LDX(TestValue), AssemblyFactory.TXA());
            Assert.AreEqual(CPU.XRegister, CPU.Accumulator);
        }

        [Test]
        public void TXS()
        {
            RunProgramFromFragment(false, AssemblyFactory.LDX(TestValue), AssemblyFactory.TXS());
            Assert.AreEqual(CPU.XRegister, CPU.StackPointer);
        }
    }
}
