using System.Runtime.InteropServices;
using System;
using System.Linq;
using CSharpTo2600.Compiler;
using NUnit.Framework;
using BindingFlags = System.Reflection.BindingFlags;
using CSharpTo2600.Framework.Assembly;

namespace CSharpTo2600.UnitTests
{
    public sealed class FragmentTests : AssemblyTests
    {
        [Test]
        public void SystemIsCleared()
        {
            RunProgramFromFragment(Fragments.ClearSystem(), false);

            // Make sure memory was reset to 0.
            for (var i = 0; i < 0x100; i++)
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
    }

    public sealed class BigEndianFragmentTests : AssemblyTests
    {
        //[OneTimeSetUp]
        [SetUp]
        public void SetUp()
        {
            // For some reason, OneTimeSetup does not seem to work. The method never gets called. [SetUp]
            // does work though. So we're using reflection get around that lack of functionality.
            // I'd rather do that than make Endianness less safe.
            // This might just be broken on my end.

            //EndianHelper.Endianness = Endianness.Big;
            typeof(EndianHelper).GetField("_Endianness", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, Endianness.Big);
        }

        [Test]
        public void PushLiteral(
            [Values((byte)0xEF, (int)0x7FABCDEF, (ulong)0xDEADBEEFBAADF00D)] object Input)
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

        [Test]
        public void StoreVariable()
        {
            var ByteType = ProcessedType.FromBuiltInType(null, sizeof(byte));
            var VarInfo = VariableInfo.CreateDirectlyAddressableCustomVariable("Test", ByteType, 0xA0);
            var Instructions = Fragments.StoreVariable(VarInfo, ByteType);
            var Expected = new[]
            {
                AssemblyFactory.PLA(),
                AssemblyFactory.STA(VarInfo.AssemblySymbol)
            };
            Assert.IsTrue(Instructions.SequenceEqual(Expected));
        }

        [Test]
        public void PushVariable()
        {
            var ByteType = ProcessedType.FromBuiltInType(null, sizeof(byte));
            var VarInfo = VariableInfo.CreateDirectlyAddressableCustomVariable("Test", ByteType, 0xA0);
            var Instructions = Fragments.PushVariable(VarInfo);
            var Expected = new[]
            {
                AssemblyFactory.LDA(VarInfo.AssemblySymbol),
                AssemblyFactory.PHA()
            };
            Assert.IsTrue(Instructions.SequenceEqual(Expected));
        }
    }
}
