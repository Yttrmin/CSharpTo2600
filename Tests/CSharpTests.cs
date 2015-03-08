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
			var Compiler = new GameCompiler(Source);
			Assert.Throws<GameClassNotFoundException>(Compiler.Compile);
		}

		[Test]
		public void GameClassMustBeStatic()
		{
			var NonStaticSource = @"
using CSharpTo2600.Framework;
[Atari2600Game]class Test { }";
			var Compiler = new GameCompiler(NonStaticSource);

			Assert.Throws<GameClassNotStaticException>(Compiler.Compile);

			var StaticSource = @"
using CSharpTo2600.Framework;
[Atari2600Game]static class Test { }";
			Compiler = new GameCompiler(StaticSource);

			Assert.DoesNotThrow(Compiler.Compile);
		}

		[Test]
		public void GlobalsHaveCorrectTypeSizePlacement()
		{
			var Info = CompileCode(@"
using CSharpTo2600.Framework;
[Atari2600Game]static class Test {
	static byte ByteTest;
	static int IntTest; }");

			// Do not make assumptions about things like the order variables
			// are defined or what specific addresses they occupy. 

			var ByteVar = Info.GlobalVariables.Single(v => v.Name == "ByteTest");
			Assert.AreEqual(typeof(byte), ByteVar.Type);
			Assert.AreEqual(sizeof(byte), ByteVar.Address.End - ByteVar.Address.Start + 1);
			Assert.AreEqual(sizeof(byte), ByteVar.Size);

			var IntVar = Info.GlobalVariables.Single(v => v.Name == "IntTest");
			Assert.AreEqual(typeof(int), IntVar.Type);
			Assert.AreEqual(sizeof(int), IntVar.Address.End - IntVar.Address.Start + 1);
			Assert.AreEqual(sizeof(int), IntVar.Size);

			Assert.False(ByteVar.Address.Overlaps(IntVar.Address));
		}

		[Test]
		public void GlobalNamesConflictCaseSensitively()
		{
			var Source = @"
using CSharpTo2600.Framework;
[Atari2600Game]static class Test {
	static byte VSYNC; }";
			var Compiler = new GameCompiler(Source);
			Assert.Throws<VariableNameAlreadyUsedException>(Compiler.Compile);
		}

		[Test]
		public void GlobalNamesDontConflictCaseInsensitively()
		{
			var Source = @"
using CSharpTo2600.Framework;
[Atari2600Game]static class Test {
	static byte VSyNC; }";
			var Compiler = new GameCompiler(Source);
			Assert.DoesNotThrow(Compiler.Compile);
		}

		[Test]
		public void ThrowsOnGlobalsOverflowingRAM()
		{
			var Source = @"
using CSharpTo2600.Framework;
[Atari2600Game]static class Test {
	static long l1,l2,l3,l4,l5,l6,l7,l8,l9,l10,l11,l12,l13,l14,l15,l16,l17; }";
			var Compiler = new GameCompiler(Source);
			Assert.Throws<HeapOverflowException>(Compiler.Compile);
		}

		[Test]
		public void LiteralAssignmentToGlobal()
		{
			var Subroutine = CompileStaticMethod("static int a;", "a = 0x7EAD;");
			var GlobalVar = new GlobalVariable("a", typeof(int), new Range(0, 0), true);
			var ExpectedCode = ConcatenateFragments(PushLiteral((int)0x7EAD), StoreVariable(GlobalVar, typeof(int)));

			Assert.True(Subroutine.Body.SequenceEqual(ExpectedCode));
		}

		private ROMBuilder.ROMInfo CompileCode(string Source)
		{
			var Compiler = new GameCompiler(Source, CompileOptions);
			Compiler.Compile();
			return Compiler.GetROMInfo();
		}

		private Subroutine CompileStaticMethod(string Globals, string Source)
		{
			var Code = $"using CSharpTo2600.Framework; [Atari2600Game]static class Test {{ {Globals} static void TestMethod() {{ {Source} }} }}";
			var Info = CompileCode(Code);
			return Info.Subroutines.Single(s => s.Name == "TestMethod");
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
