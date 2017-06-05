using static VCSFramework.Registers;

static class Evolving
{
	static byte a;

	public static void Main()
	{
	loopStart:
		a = 0x4C;
		BackgroundColor = a;
		goto loopStart;
	}
}