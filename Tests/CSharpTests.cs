using System.Linq;
using CSharpTo2600.Compiler;
using NUnit.Framework;

namespace CSharpTo2600.UnitTests
{
	public class CSharpTests
	{
		[Test]
		public void GlobalsHaveCorrectTypeSizePlacement()
		{
			var Code = @"
using CSharpTo2600.Framework;
[Atari2600Game]class Test {
	byte ByteTest;
	int IntTest; }";
			var Compiler = new GameCompiler(Code);
			Compiler.Compile();
			var Info = Compiler.GetROMInfo();

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
	}
}
