using System;
using System.Collections.Generic;
using System.Linq;
using CSharpTo2600.Compiler;
using CSharpTo2600.Framework.Assembly;
using static CSharpTo2600.Framework.Assembly.AssemblyFactory;
using NUnit.Framework;

namespace CSharpTo2600.UnitTests
{
    public abstract class AssemblyTests
    {
        protected Processor.Processor CPU { get; }
        protected Symbol ProgramEnd { get; }
        private readonly IList<byte> ZeroPage;
        private readonly byte[] OldZeroPage;
        private readonly IList<byte> StackPage;
        private readonly byte[] OldStackPage;

        public AssemblyTests()
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
        }

        [SetUp]
        public void SetupTest()
        {
            CPU.Reset();
            // Fill memory with garbage so we know we're
            // actually clearing it.
            // Garbages up 0x00-0xFF. RIOT RAM is only 0x80-0xFF but we want to clear the
            // TIA registers too.
            for (var i = 0; i < 0x100; i++)
            {
                CPU.Memory.WriteValue(i, 123);
                // Page 0 and page 1 are mirrored during 6502 execution (see UpdateMemoryMirror),
                // but we need to make sure they start out identical too.
                CPU.Memory.WriteValue(0x100 + i, 123);
            }
            Array.Copy(ZeroPage.ToArray(), OldZeroPage, ZeroPage.Count);
            Array.Copy(StackPage.ToArray(), OldStackPage, StackPage.Count);
        }

        [Explicit("Takes 6-9 seconds to run, mirroring code and LDA/STA unlikely to change.")]
        //@FIXME - Test runs regardless of Explicit attribute. Possibly just an issue on my end.
        //[Test]
        // Makes sure UpdateMemoryMirror actually works. Crucial for other tests.
        // This test is pretty slow. Probably from running 256 individual 6502 programs that all
        // clear RIOT RAM and TIA registers.
        public void Page0AndPage1AreMirrored()
        {
            const byte TestValue = 0xCD;

            // Could make this a parameterized test, but we really don't need 256 individual tests.
            for (var Address = 0; Address <= 0xFF; Address++)
            {
                RunProgramFromFragment(
                    new[]
                {
                    LDA(TestValue),
                    STA((byte)Address)
                });

                Assert.AreEqual(TestValue, ZeroPage.ElementAt(Address), $"Address [{Address.ToString("X2")}] not mirrored.");
                Assert.AreEqual(TestValue, StackPage.ElementAt(Address), $"Address [{Address.ToString("X2")}] not mirrored.");
                Assert.True(ZeroPage.ElementAt(Address) == StackPage.ElementAt(Address));
                Assert.True(CPU.Memory.DumpMemory()[Address] == CPU.Memory.DumpMemory()[0x100 + Address]);
            }
        }

        protected void RunProgramFromFragment(params IEnumerable<AssemblyLine>[] FragmentLines)
        {
            var Lines = new List<AssemblyLine>();
            foreach (var LineEnmuerable in FragmentLines)
            {
                Lines.AddRange(LineEnmuerable);
            }
            RunProgramFromFragment(Lines.ToArray(), true);
        }

        protected void RunProgramFromFragment(bool InsertClearSystemCode = true, params AssemblyLine[] FragmentLines)
        {
            RunProgramFromFragment(FragmentLines, InsertClearSystemCode);
        }

        protected void RunProgramFromFragment(IEnumerable<AssemblyLine> FragmentLines, bool InsertClearSystemCode = true)
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

            CPU.LoadProgram(0xF000, ROMCreator.CreateRawROM(Lines).ROM, 0xF000);
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
            for (var i = 0; i <= 0xFF; i++)
            {
                if (OldStackPage[i] != StackPage[i])
                {
                    // Stack page change!
                    ZeroPage[i] = StackPage[i];
                    OldStackPage[i] = StackPage[i];
                }
                if (OldZeroPage[i] != ZeroPage[i])
                {
                    // Zero page change!
                    StackPage[i] = ZeroPage[i];
                    OldZeroPage[i] = ZeroPage[i];
                }
            }
            // Stack and zero page should always be identical.
            Assert.True(StackPage.SequenceEqual(ZeroPage));
        }
    }
}
