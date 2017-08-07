using static VCSFramework.Registers;
using static VCSFramework.Assembly.AssemblyFactory;
using static VCSFramework.Memory;

static class Evolving
{
	static byte a;

	public static void Main()
	{
		/*SEI();
		CLD();
		X = 0xFF;
		TXS();
		// ClearMemory() will infinitely loop until inlining works.
		ClearMemory();
		byte b = 0;
	loopStart:
		a++;
		a = 0x4C;
		Foo(27);
		goto loopStart;*/
		ClearMemory();
		byte q = 1;
		Foo(1);
	}

	private static void Foo(byte q)
	{
		BackgroundColor = a;
	}
}