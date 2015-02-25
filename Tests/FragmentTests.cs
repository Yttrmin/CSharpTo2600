using System.Runtime.InteropServices;
using System;
using System.Linq;
using CSharpTo2600.Compiler;
using NUnit.Framework;

namespace CSharpTo2600.UnitTests
{
    public sealed class FragmentTests : AssemblyTests
    {
        [Test]
        public void SystemIsCleared()
        {
            RunProgramFromFragment(Fragments.ClearSystem(), false);

            // Make sure memory was reset to 0.
            for(var i = 0; i < 0x100; i++)
            {
                Assert.AreEqual(0, CPU.Memory.ReadValue(i));
            }
            // Make sure interrupts are disabled.
            Assert.True(CPU.DisableInterruptFlag);
            // Make sure decimal mode is cleared.
            Assert.False(CPU.DecimalFlag);
            // Make sure stack pointer = 0xFF.
            Assert.AreEqual(0xFF, CPU.StackPointer);
        }

        [Test]
        public void PushLiteralUsesBigEndianAndCorrectSize(
            [Values((byte)0xEF,(int)0x7FABCDEF,(ulong)0xDEADBEEFBAADF00D)] object Input)
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
            Assert.AreEqual(ExpectedStackPointer, CPU.StackPointer);
            // Contains the bytes of the literal from the 6502 in big-endian.
            var BigEndianBytes = new byte[Size];
            Array.ConstrainedCopy(CPU.Memory.DumpMemory(), ExpectedLiteralStart, BigEndianBytes, 0, Size);
            // Ensure bytes are pushed onto stack in big-endian.
            Assert.True(ByteArray.SequenceEqual(BigEndianBytes));
        }
    }
}
