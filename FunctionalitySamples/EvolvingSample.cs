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
		/*byte d = 0xD8;
		Foo(a, d);
		d = InTim;
		WSync();
		Tim64T = a;*/
	MainLoop:
		// VSYNC
		VSync = 0b10;
		WSync();
		WSync();
		WSync();
		Tim64T = 43;
		VSync = 0;

		while (InTim != 0) ;
		goto MainLoop;
	}

	private static void Foo(byte q, byte z)
	{
		byte c = z;
		ColuBk = c;
	}
}