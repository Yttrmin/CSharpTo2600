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
            for(byte i = 0xFF; i >= 0x80; i--)
            {
                Lines.Add(AssemblyFactory.LDA(i));
                Lines.Add(AssemblyFactory.PHA());
            }
            RunProgramFromFragment(Lines, true);
            for(var i = 0xFF; i >= 0x80; i--)
            {
                Assert.AreEqual(i, CPU.Memory.ReadValue(i));
            }
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
