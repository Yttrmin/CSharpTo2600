static class Evolving
{
	static byte a;

	public static void Main()
	{
		mutateA();
	}

	private static void mutateA()
	{
		a = 0;
	}
}