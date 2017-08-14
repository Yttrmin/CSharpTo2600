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
		byte backgroundColor = 0;
	MainLoop:
		// VSYNC
		VSync = 0b10;
		WSync();
		WSync();
		WSync();
		Tim64T = 43;
		VSync = 0;
		ColuBk = 0x56;

		// Wait for VBlank end.
		while (InTim != 0) ;

		WSync();
		VBlank = 0;

		/*var lines = 191;
		while (lines != 0)
		{
			lines--;
			WSync();
		}*/
		WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync();

		WSync();
		VBlank = 0b10;

		/*lines = 30;
		while (lines != 0)
		{
			lines--;
			WSync();
		}*/
		WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync(); WSync();

		goto MainLoop;
	}

	private static void Foo(byte q, byte z)
	{
		byte c = z;
		ColuBk = c;
	}
}