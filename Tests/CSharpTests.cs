using System.Collections.Generic;
using System.Linq;
using CSharpTo2600.Compiler;
using CSharpTo2600.Framework.Assembly;
using static CSharpTo2600.Compiler.Fragments;
using NUnit.Framework;

namespace CSharpTo2600.UnitTests
{
    public class CSharpTests
    {
        // Make sure optimizations are turned off, otherwise our expected fragments won't
        // match up with the subroutine's body.
        private static readonly CompileOptions CompileOptions =
            new CompileOptions(Optimize: false, Endianness: Endianness.Big);

        [Test]
        public void GameClassMustBeMarkedWithAttribute()
        {
            var Source = "static class Test { }";
            Assert.Throws<GameClassNotFoundException>(() => GameCompiler.CompileFromTexts(Source));
        }

        [Test]
        public void NonStaticGameClassThrows()
        {
            var NonStaticSource = @"
using CSharpTo2600.Framework;
[Atari2600Game]class Test { }";

            Assert.Throws<GameClassNotStaticException>(() => GameCompiler.CompileFromTexts(NonStaticSource));
        }

        [Test]
        public void StaticGameClassCompiles()
        {
            var StaticSource = @"
using CSharpTo2600.Framework;
[Atari2600Game]static class Test { }";

            Assert.DoesNotThrow(() => GameCompiler.CompileFromTexts(StaticSource));
        }

        [Test]
        public void PrimitiveGlobalsHaveCorrectTypeAndSize()
        {
            var ROMInfo = GameCompiler.CompileFromTexts(@"
using CSharpTo2600.Framework;
[Atari2600Game]static class Test {
	static byte ByteTest;
	static int IntTest; }");
            var Info = ROMInfo.CompilationInfo;

            // Do not make assumptions about things like the order variables
            // are defined or what specific addresses they occupy. 

            var ByteVar = Info.AllGlobals.Single(v => v.Name == "ByteTest");
            Assert.AreEqual(typeof(byte), ByteVar.Type);
            Assert.AreEqual(sizeof(byte), ByteVar.Size);

            var IntVar = Info.AllGlobals.Single(v => v.Name == "IntTest");
            Assert.AreEqual(typeof(int), IntVar.Type);
            Assert.AreEqual(sizeof(int), IntVar.Size);
        }
        
        [Test]
        public void GlobalNamesConflictCaseSensitively()
        {
            var Source = @"
using CSharpTo2600.Framework;
[Atari2600Game]static class Test {
	static byte VSYNC; }";
            Assert.Throws<VariableNameReservedException>(() => GameCompiler.CompileFromTexts(Source));
        }

        [Test]
        public void GlobalNamesDontConflictCaseInsensitively()
        {
            var Source = @"
using CSharpTo2600.Framework;
[Atari2600Game]static class Test {
	static byte VSyNC; }";
            Assert.DoesNotThrow(() => GameCompiler.CompileFromTexts(Source));
        }

        [Test]
        public void ThrowsOnGlobalsOverflowingRAM()
        {
            // You don't neccessarily need >= 128 bytes to overflow. Some amount may be
            // reserved for stack space, etc. But this many longs will guarantee an overflow.
            var Source = @"
using CSharpTo2600.Framework;
[Atari2600Game]static class Test {
	static long l1,l2,l3,l4,l5,l6,l7,l8,l9,l10,l11,l12,l13,l14,l15,l16,l17; }";
            Assert.Throws<GlobalMemoryOverflowException>(() => GameCompiler.CompileFromTexts(Source));
        }

        [Test]
        public void LiteralAssignmentToGlobal()
        {
            ROMInfo ROMInfo;
            var Subroutine = CompileStaticMethod("static int a;", "a = 0x7EAD;", out ROMInfo);
            var GlobalVar = ROMInfo.CompilationInfo.AllGlobals.Single(v => v.Name == "a");
            var ExpectedCode = ConcatenateFragments(PushLiteral((int)0x7EAD), StoreVariable(GlobalVar, typeof(int)));

            Assert.True(Subroutine.Body.Where(l => !(l is Trivia)).SequenceEqual(ExpectedCode));
        }

        [Test]
        public void GameClassCanAccessOtherTypeStaticFields()
        {
            var Source = @"
using CSharpTo2600.Framework;
[Atari2600Game]static class Test { static void TestMethod() { Other.Var++; } }
static class Other { public static byte Var; }";
            Assert.DoesNotThrow(() => GameCompiler.CompileFromTexts(Source));
        }

        // Explicitly test this despite already testing static fields in
        // other classes, because we could screw up parsing the syntax tree
        // of a nested class if we use DescendentNodes() or something.
        [Test]
        public void GameClassCanAccessNestedTypeStaticFields()
        {
            var Source = @"
using CSharpTo2600.Framework;
[Atari2600Game]static class Test { static void TestMethod() { Nested.Var++; } 
static class Nested { public static byte Var; } }";
            Assert.DoesNotThrow(() => GameCompiler.CompileFromTexts(Source));
        }

        // Partial methods result in 2 symbols: One with the body, and one without.
        // If the one without a body is picked, we end up with a subroutine with no code.
        [Test]
        public void CompileImplementedPartialMethods()
        {
            var Source = @"
using CSharpTo2600.Framework;
[Atari2600Game]static partial class Test { static byte Var;
static partial void TestMethod();
static partial void TestMethod() {Var++;Var++;Var++;}}";
            var Info = GameCompiler.CompileFromTexts(Source);
            var Subroutine = Info.CompilationInfo.AllSubroutines.Single(s => s.Name == "TestMethod");
            Assert.Greater(Subroutine.InstructionCount, 0);
        }

        [Test]
        public void SupportMultipleFileCompilation()
        {
            var Source1 = @"
using CSharpTo2600.Framework;
[Atari2600Game]static class GameClass { static void TestMethod() { DataClass.Var++; } }";
            var Source2 = @"
static class DataClass { public static byte Var; }";

            CompilationInfo Info = null;
            Assert.DoesNotThrow(() => Info = GameCompiler.CompileFromTexts(Source1, Source2).CompilationInfo);
            Assert.IsTrue(Info.AllTypes.Any(t => t.Name == "GameClass"));
            Assert.IsTrue(Info.AllTypes.Any(t => t.Name == "DataClass"));
        }

        private Subroutine CompileStaticMethod(string Globals, string Source)
        {
            ROMInfo ThrowAway;
            return CompileStaticMethod(Globals, Source, out ThrowAway);
        }

        private Subroutine CompileStaticMethod(string Globals, string Source, out ROMInfo ROMInfo)
        {
            var Code = $"using CSharpTo2600.Framework; [Atari2600Game]static class Test {{ {Globals} static void TestMethod() {{ {Source} }} }}";
            ROMInfo = GameCompiler.CompileFromTexts(Code);
            return ROMInfo.CompilationInfo.AllSubroutines.Single(s => s.Name == "TestMethod");
        }

        private IEnumerable<AssemblyLine> ConcatenateFragments(params IEnumerable<AssemblyLine>[] Lines)
        {
            var AllLines = new List<AssemblyLine>();
            foreach (var Enum in Lines)
            {
                AllLines.AddRange(Enum);
            }
            return AllLines;
        }
    }
}
