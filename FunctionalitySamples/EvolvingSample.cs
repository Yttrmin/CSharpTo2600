using static VCSFramework.Registers;
using static VCSFramework.Assembly.AssemblyFactory;

static class Evolving
{
	static byte a;

	public static void Main()
	{
		SEI();
		CLD();
	loopStart:
		a = 0x4C;
		BackgroundColor = a;
		goto loopStart;
	}
}