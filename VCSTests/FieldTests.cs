using NUnit.Framework;
using VCSCompiler;
using static VCSTests.TestUtil;

namespace VCSTests
{
	[TestFixture]
    public class FieldTests
    {
		[Test]
		public void StaticFieldsAreAllowed()
		{
			var source =
				@"
public static class Program
{
	public static byte field;

	public static void Main()
	{
	}
}";
			Assert.DoesNotThrowAsync(async () => await CompileFromText(source));
		}

		[Test]
		public void InstanceFieldsAreNotAllowed()
		{
			var source =
				@"
public static class Program
{
	public byte field;

	public static void Main()
	{
	}
}";
			Assert.ThrowsAsync<FatalCompilationException>(async () => await CompileFromText(source));
		}

		[Test]
		public void StaticFieldInitializersAreNotAllowed()
		{
			var source =
				@"
public static class Program
{
	public static byte field = 0;

	public static void Main()
	{
	}
}";
			Assert.ThrowsAsync<FatalCompilationException>(async () => await CompileFromText(source));
		}
	}
}
