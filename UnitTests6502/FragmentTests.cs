using CSharpTo2600.Compiler;
using System.Collections.Generic;
using Xunit;
using CSharpTo2600.Framework.Assembly;
using static CSharpTo2600.Framework.Assembly.AssemblyFactory;

namespace CSharpTo2600.UnitTests
{
    public class FragmentTests
    {
        private readonly Processor.Processor CPU;
        private readonly Symbol ProgramEnd;

        public FragmentTests()
        {
            // Completely arbitrary magic number. We end all tests in a JMP to here so we
            // know when it's over. Can't just count instructions since some tests might
            // involve branches.
            ProgramEnd = DefineSymbol("__END", 0xABCD);
            CPU = new Processor.Processor();
            // Fill memory with garbage so we know we're
            // actually clearing it.
            for(var i = 0; i < 128; i++)
            {
                CPU.Memory.WriteValue(i, 123);
            }
            // LoadProgram resets the CPU so don't bother here.
        }

        [Fact]
        public void SystemIsCleared()
        {
            var ClearCode = new List<AssemblyLine>();
            ClearCode.Add(AssemblyFactory.Processor());
            ClearCode.Add(Include("vcs.h"));
            ClearCode.Add(Org(0xF000));
            ClearCode.Add(ProgramEnd);
            ClearCode.AddRange(Fragments.ClearSystem());
            ClearCode.Add(JMP(ProgramEnd));
            CPU.LoadProgram(0xF000, ROMBuilder.BuildRawROM(ClearCode), 0xF000);
            while(CPU.ProgramCounter != ProgramEnd.Value.Value)
            {
                CPU.NextStep();
            }

            // Make sure memory was reset to 0.
            for(var i = 0; i < 128; i++)
            {
                Assert.Equal(0, CPU.Memory.ReadValue(i));
            }

            // Make sure interrupts are disabled.
            Assert.True(CPU.DisableInterruptFlag);
            // Make sure decimal mode is cleared.
            Assert.False(CPU.DecimalFlag);
            // Make sure stack pointer = 0xFF.
            Assert.Equal(0xFF, CPU.StackPointer);
        }
    }
}
