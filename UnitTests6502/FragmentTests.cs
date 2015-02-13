using System.Runtime.InteropServices;
using System;
using System.Linq;
using System.Collections.Generic;
using Xunit;
using CSharpTo2600.Compiler;
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
            RunProgramFromFragmentWithoutClearingSystem(Fragments.ClearSystem());

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

        [Theory]
        [InlineData((byte)0xEF)]
        [InlineData((int)0x7FABCDEF)]
        [InlineData((ulong)0xDEADBEEFBAADF00D)]
        public void PushLiteral(object Input)
        {
            RunProgramFromFragment(Fragments.PushLiteral(Input));
            var Size = Marshal.SizeOf(Input.GetType());
            var Ptr = Marshal.AllocHGlobal(Size);
            var ByteArray = new byte[Size];
            Marshal.StructureToPtr(Input, Ptr, false);
            Marshal.Copy(Ptr, ByteArray, 0, ByteArray.Length);
            Marshal.FreeHGlobal(Ptr);
            // ByteArray contains the bytes of the literal in big-endian.
            Array.Reverse(ByteArray);

            // Where the top of the stack should be based on size of literal.
            var ExpectedStackPointer = 0xFF - Size;
            // On a 6502 the stack is hardwired to 0x100-0x1FF, which is a mirror of
            // 0x000-0x0FF on the 2600.
            // Remember SP points to where the next pushed byte would go. Not where
            // the last byte that was pushed is located.
            var ExpectedLiteralStart = ExpectedStackPointer + 1 + 0x100;
            
            // Ensure SP is correct.
            Assert.Equal(ExpectedStackPointer, CPU.StackPointer);
            // Contains the bytes of the literal from the 6502 in big-endian.
            var BigEndianBytes = new byte[Size];
            Array.ConstrainedCopy(CPU.Memory.DumpMemory(), ExpectedLiteralStart, BigEndianBytes, 0, Size);
            // Ensure bytes are pushed onto stack in big-endian.
            Assert.True(ByteArray.SequenceEqual(BigEndianBytes));
        }

        private void RunProgramFromFragment(IEnumerable<AssemblyLine> FragmentLines)
        {
            var Lines = new List<AssemblyLine>();
            Lines.Add(AssemblyFactory.Processor());
            Lines.Add(Include("vcs.h"));
            Lines.Add(Org(0xF000));
            Lines.Add(ProgramEnd);
            // Need to use this to ensure SP = 0xFF and all the other stuff.
            // Confirmed to do its job by another unit test.
            Lines.AddRange(Fragments.ClearSystem());
            Lines.AddRange(FragmentLines);
            Lines.Add(JMP(ProgramEnd));

            CPU.LoadProgram(0xF000, ROMBuilder.BuildRawROM(Lines), 0xF000);
            while (CPU.ProgramCounter != ProgramEnd.Value.Value)
            {
                CPU.NextStep();
            }
        }

        // Use this when testing Fragments.ClearSystem() only.
        private void RunProgramFromFragmentWithoutClearingSystem(IEnumerable<AssemblyLine> FragmentLines)
        {
            var Lines = new List<AssemblyLine>();
            Lines.Add(AssemblyFactory.Processor());
            Lines.Add(Include("vcs.h"));
            Lines.Add(Org(0xF000));
            Lines.Add(ProgramEnd);
            Lines.AddRange(FragmentLines);
            Lines.Add(JMP(ProgramEnd));

            CPU.LoadProgram(0xF000, ROMBuilder.BuildRawROM(Lines), 0xF000);
            while (CPU.ProgramCounter != ProgramEnd.Value.Value)
            {
                CPU.NextStep();
            }
        }
    }
}
