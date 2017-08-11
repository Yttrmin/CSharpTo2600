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
		byte d = 0xD8;
		Foo(a, d);
	loop:
		goto loop;
	}

	private static void Foo(byte q, byte z)
	{
		byte c = z;
		ColuBk = c;
	}
}