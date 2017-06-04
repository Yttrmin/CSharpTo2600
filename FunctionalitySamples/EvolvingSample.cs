using static VCSFramework.Registers;

static class Evolving
{
	static byte a;

	public static void Main()
	{
		a = 0;
		BackgroundColor = a;
	}
}