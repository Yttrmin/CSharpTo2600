static class Evolving
{
	static byte a;

	public static void Main()
	{
		mutateA(a);
	}

	private static void mutateA(byte b)
	{
		a = 0;
	}
}