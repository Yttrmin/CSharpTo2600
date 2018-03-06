using NUnit.Framework;
using VCSCompiler;
using static VCSTests.TestUtil;

namespace VCSTests
{
	[TestFixture]
	public class ClassTests
	{
		[Test]
		public void SimplestWorkingCase()
		{
			var source =
				@"
public static class Program
{
	public static void Main()
	{
	}
}";
			Assert.DoesNotThrowAsync(async () => await CompileFromText(source));
		}

		[Test]
		public void ClassMustBeStatic()
		{
			var source =
				@"
public class Program
{
	public static void Main()
	{
	}
}";
			Assert.ThrowsAsync<FatalCompilationException>(async () => await CompileFromText(source));
		}

		[Test]
		public void ClassCanBeNonPublic()
		{
			var source =
				@"
internal static class Program
{
	public static void Main()
	{
	}
}";
			Assert.DoesNotThrowAsync(async () => await CompileFromText(source));
		}

		[Test]
		public void ClassCanBeUnsafe()
		{
			var source =
				@"
internal static class Program
{
	public static unsafe void Main()
	{
	}
}";
			Assert.DoesNotThrowAsync(async () => await CompileFromText(source));
		}
	}
}
