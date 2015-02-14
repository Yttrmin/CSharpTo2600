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
        private readonly ArraySegment<byte> ZeroPage;
        private readonly byte[] OldZeroPage;
        private readonly ArraySegment<byte> StackPage;
        private readonly byte[] OldStackPage;

        public FragmentTests()
        {
            // Completely arbitrary magic number. We end all tests in a JMP to here so we
            // know when it's over. Can't just count instructions since some tests might
            // involve branches.
            ProgramEnd = DefineSymbol("__END", 0xABCD);
            CPU = new Processor.Processor();
            // DumpMemory returns the backing array, not a copy.
            ZeroPage = new ArraySegment<byte>(CPU.Memory.DumpMemory(), 0, 0x100);
            OldZeroPage = new byte[0x100];
            StackPage = new ArraySegment<byte>(CPU.Memory.DumpMemory(), 0x100, 0x100);
            OldStackPage = new byte[0x100];
            // Fill memory with garbage so we know we're
            // actually clearing it.
            // Garbages up 0x00-0xFF. RIOT RAM is only 0x80-0xFF but we want to clear the
            // TIA registers too.
            for(var i = 0; i < 0x100; i++)
            {
                CPU.Memory.WriteValue(i, 123);
                // Page 0 and page 1 are mirrored during 6502 execution (see UpdateMemoryMirror),
                // but we need to make sure they start out identical too.
                CPU.Memory.WriteValue(0x100 + i, 123);
            }
            Array.Copy(ZeroPage.ToArray(), OldZeroPage, ZeroPage.Count);
            Array.Copy(StackPage.ToArray(), OldStackPage, StackPage.Count);
            // LoadProgram resets the CPU so don't bother here.
        }

        [Fact]
        public void SystemIsCleared()
        {
            RunProgramFromFragment(Fragments.ClearSystem(), false);

            // Make sure memory was reset to 0.
            for(var i = 0; i < 0x100; i++)
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
        public void PushLiteralUsesBigEndianAndCorrectSize(object Input)
        {
            RunProgramFromFragment(Fragments.PushLiteral(Input));
            // Can't just use BitConverter since passing a byte would be
            // treated as a short.
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
            // Remember SP points to where the next pushed byte would go. Not where
            // the last byte that was pushed is located.
            var ExpectedLiteralStart = ExpectedStackPointer + 1;
            
            // Ensure SP is correct.
            Assert.Equal(ExpectedStackPointer, CPU.StackPointer);
            // Contains the bytes of the literal from the 6502 in big-endian.
            var BigEndianBytes = new byte[Size];
            Array.ConstrainedCopy(CPU.Memory.DumpMemory(), ExpectedLiteralStart, BigEndianBytes, 0, Size);
            // Ensure bytes are pushed onto stack in big-endian.
            Assert.True(ByteArray.SequenceEqual(BigEndianBytes));
        }

        [Fact]
        private void Page0AndPage1AreMirrored()
        {
            const byte TestValue = 0xCD;

            RunProgramFromFragment(
                new[]
            {
                LDA(TestValue),
                PHA()
            });

            Assert.Equal(TestValue, ZeroPage.ElementAt(0xFF));
            Assert.Equal(TestValue, StackPage.ElementAt(0xFF));
            Assert.True(ZeroPage.ElementAt(0xFF) == StackPage.ElementAt(0xFF));
            Assert.True(CPU.Memory.DumpMemory()[0xFF] == CPU.Memory.DumpMemory()[0x1FF]);
        }

        private void RunProgramFromFragment(IEnumerable<AssemblyLine> FragmentLines, bool InsertClearSystemCode = true)
        {
            var Lines = new List<AssemblyLine>();
            Lines.Add(AssemblyFactory.Processor());
            Lines.Add(Include("vcs.h"));
            Lines.Add(Org(0xF000));
            Lines.Add(ProgramEnd);
            if (InsertClearSystemCode)
            {
                // Need to use this to ensure SP = 0xFF and all the other stuff.
                // Confirmed to do its job by another unit test.
                Lines.AddRange(Fragments.ClearSystem());
            }
            Lines.AddRange(FragmentLines);
            Lines.Add(JMP(ProgramEnd));

            CPU.LoadProgram(0xF000, ROMBuilder.BuildRawROM(Lines), 0xF000);
            while (CPU.ProgramCounter != ProgramEnd.Value.Value)
            {
                CPU.NextStep();
                UpdateMemoryMirror();
            }
        }

        /// <summary>
        /// Simulates the memory mirror between page 0 and page 1 on the 2600.
        /// </summary>
        /// <remarks>
        /// 6502Net does not simulate the very large number of memory mirrors on the
        /// 2600 (not that it should). So we'll just manually keep the two important
        /// mirrors in sync.
        /// </remarks>
        private void UpdateMemoryMirror()
        {
            for(var i = 0; i <= 0xFF; i++)
            {
                if(OldStackPage[i] != StackPage.ElementAt(i))
                {
                    // Stack page change!
                    ZeroPage.Array[i] = StackPage.ElementAt(i);
                    OldStackPage[i] = StackPage.ElementAt(i);
                }
                if(OldZeroPage[i] != ZeroPage.ElementAt(i))
                {
                    // Zero page change!
                    StackPage.Array[StackPage.Offset + i] = ZeroPage.ElementAt(i);
                    OldZeroPage[i] = ZeroPage.ElementAt(i);
                }
            }
            // Stack and zero page should always be identical.
            Assert.True(StackPage.SequenceEqual(ZeroPage));
        }
    }
}
