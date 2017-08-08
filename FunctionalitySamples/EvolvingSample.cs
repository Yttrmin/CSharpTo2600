using static VCSFramework.Registers;
using static VCSFramework.Assembly.AssemblyFactory;
using static VCSFramework.Memory;

static class Evolving
{
	static byte a;

	public static void Main()
	{
		SEI();
		CLD();
		X = 0xFF;
		TXS();
		ClearMemory();
		Foo(a, 0xD4);
	loop:
		goto loop;
	}

	private static void Foo(byte q, byte z)
	{
		BackgroundColor = z;
	}
}