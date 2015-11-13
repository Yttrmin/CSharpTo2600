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
        private static readonly CompileOptions CompileOptionsNoOptimize =
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
	static byte ByteTest; }");
            var Info = ROMInfo.CompilationState;

            // Do not make assumptions about things like the order variables
            // are defined or what specific addresses they occupy. 

            var ByteVar = Info.AllGlobals.Single(v => v.Name == "ByteTest");
            Assert.AreEqual(sizeof(byte), ByteVar.Size);
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
	static byte b1,b2,b3,b4,b5,b6,b7,b8,b9,b10,b11,b12,b13,b14,b15,b16,b17,b18,b19,b20,b21,b22,b23,b24,b25,b26,b27,b28,b29,b30,b31,b32,b33,b34,b35,b36,b37,b38,b39,b40,b41,b42,b43,b44,b45,b46,b47,b48,b49,b50,b51,b52,b53,b54,b55,b56,b57,b58,b59,b60,b61,b62,b63,b64,b65,b66,b67,b68,b69,b70,b71,b72,b73,b74,b75,b76,b77,b78,b79,b80,b81,b82,b83,b84,b85,b86,b87,b88,b89,b90,b91,b92,b93,b94,b95,b96,b97,b98,b99,b100,b101,b102,b103,b104,b105,b106,b107,b108,b109,b110,b111,b112,b113,b114,b115,b116,b117,b118,b119,b120,b121,b122,b123,b124,b125,b126,b127,b128,b129; }";
            Assert.Throws<GlobalMemoryOverflowException>(() => GameCompiler.CompileFromTexts(Source));
        }

        [Test]
        public void LiteralAssignmentToGlobal()
        {
            CompilationResult ROMInfo;
            var Subroutine = CompileStaticMethod("static byte a;", "void", "a = 0x7E;", out ROMInfo, CompileOptionsNoOptimize);
            var GlobalVar = ROMInfo.CompilationState.AllGlobals.Single(v => v.Name == "a");
            var ExpectedCode = ConcatenateFragments(PushLiteral((byte)0x7E), StoreVariable(GlobalVar, ROMInfo.CompilationState.BuiltIn.Byte));

            Assert.True(Subroutine.Body.StripForInlining().StripTrivia().SequenceEqual(ExpectedCode));
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

        [Test]
        public void SupportMethodInvocation()
        {
            var Source = @"
using CSharpTo2600.Framework;
[Atari2600Game]static class Test { static byte Var;
[SpecialMethod(MethodType.Initialize)]static void Initializer() { TestMethod(); } 
[SpecialMethod(MethodType.UserDefined)]static void TestMethod() { Var++; } }";
            Assert.DoesNotThrow(() => GameCompiler.CompileFromTexts(Source));
        }

        [Test]
        public void NonAttributedMethodsAreImplicitlyUserDefined()
        {
            var Source = @"
using CSharpTo2600.Framework;
[Atari2600Game]static class Test { static byte Var;
[SpecialMethod(MethodType.Initialize)]static void Initializer() { TestMethod(); } 
static void TestMethod() { Var++; } }";
            CompilationState State = null;
            Assert.DoesNotThrow(() => State = GameCompiler.CompileFromTexts(Source).CompilationState);
            var TestSubroutine = State.AllSubroutineInfos.Where(n => n.Name == "TestMethod").Single();
            Assert.AreEqual(Framework.MethodType.UserDefined, TestSubroutine.Type);
        }

        [Test]
        public void NonUserDefinedMethodsCanNotBeInvoked()
        {
            var Source = @"
using CSharpTo2600.Framework;
[Atari2600Game]static class Test { static byte Var;
[SpecialMethod(MethodType.Initialize)]static void Initializer() { TestMethod(); } 
[SpecialMethod(MethodType.MainLoop)]static void TestMethod() { Var++; } }";
            Assert.Throws<AttemptedToInvokeSpecialMethodException>(() => GameCompiler.CompileFromTexts(Source));
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
            var Subroutine = Info.CompilationState.AllSubroutineInfos.Single(s => s.Name == "TestMethod");
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

            CompilationState State = null;
            Assert.DoesNotThrow(() => State = GameCompiler.CompileFromTexts(Source1, Source2).CompilationState);
            Assert.IsTrue(State.AllTypes.Any(t => t.Name == "GameClass"));
            Assert.IsTrue(State.AllTypes.Any(t => t.Name == "DataClass"));
        }

        //@TODO I would test more about non-void returns but it would largely require 
        // examining the emulator's memory, which we aren't currently capable of doing at this level.
        [Test]
        public void ByteReturnCompiles()
        {
            SubroutineInfo TestSubroutine = null;
            Assert.DoesNotThrow(() => TestSubroutine = CompileStaticMethod(null, "byte", "return 21;"));
        }

        private SubroutineInfo CompileStaticMethod(string Globals, string ReturnType, string Source)
        {
            CompilationResult ThrowAway;
            return CompileStaticMethod(Globals, ReturnType, Source, out ThrowAway);
        }

        private SubroutineInfo CompileStaticMethod(string Globals, string ReturnType, string Source, 
            out CompilationResult ROMInfo, CompileOptions CompileOptions)
        {
            var Code = $"using CSharpTo2600.Framework; [Atari2600Game]static class Test {{ {Globals} static {ReturnType} TestMethod() {{ {Source} }} }}";
            ROMInfo = GameCompiler.CompileFromTexts(CompileOptions, Code);
            return ROMInfo.CompilationState.AllSubroutineInfos.Single(s => s.Name == "TestMethod");
        }

        private SubroutineInfo CompileStaticMethod(string Globals, string ReturnType, string Source, out CompilationResult ROMInfo)
        {
            return CompileStaticMethod(Globals, ReturnType, Source, out ROMInfo, CompileOptions.Default);
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
