using static VCSFramework.Registers;
using static VCSFramework.Assembly.AssemblyFactory;
using static VCSFramework.Memory;

static class Evolving
{
	static byte a;
	static bool ShouldLoop;

	public static void Main()
	{
		SEI();
		CLD();
		X = 0xFF;
		TXS();
		ClearMemory();
		byte backgroundColor = 0;
	MainLoop:
		// Vertical blank.
		VSync = 0b10;
		WSync();
		WSync();
		WSync();
		Tim64T = 43;
		VSync = 0;
		ColuBk = 0x56;

		Foo(0,1,2,3,4,5,6);
		ShouldLoop = true;
		// Wait for VBlank end.
		while (InTim != 0) ;

		WSync();
		VBlank = 0;

		// Visible image.
		byte lines = 191;
		while (lines != 0)
		{
			lines--;
			WSync();
		}
		
		WSync();
		VBlank = 0b10;

		// Overscan.
		lines = 30;
		while (ShouldLoop)
		{
			lines--;
			ShouldLoop = lines != 0;
			WSync();
		}
		
		goto MainLoop;
	}

	private static void Foo(byte e, byte f, byte g, byte h, byte i, byte j, byte k)
	{
		k = j;
	}
}