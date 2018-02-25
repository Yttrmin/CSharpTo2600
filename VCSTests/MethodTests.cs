using NUnit.Framework;
using VCSCompiler;
using static VCSTests.TestUtil;

namespace VCSTests
{
	[TestFixture]
    public class MethodTests
    {
		[Test]
		public void MethodsMustNotBeInstance()
		{
			var source =
				@"
public static class Program
{
	public static void Main()
	{
	}

	public void Foo()
	{
	}
}";
			Assert.ThrowsAsync<FatalCompilationException>(async () => await CompileFromText(source));
		}

		[Test]
		public void MethodsMayBeStatic()
		{
			var source =
				@"
public static class Program
{
	public static void Main()
	{
	}
	
	public static void Foo()
	{
	}
}";
			Assert.DoesNotThrowAsync(async () => await CompileFromText(source));
		}

		[Test]
		public void MethodsMustNotHaveNonVoidReturnType()
		{
			var source =
				@"
public static class Program
{
	public static void Main()
	{
	}

	public static byte Foo()
	{
		return default(byte);
	}
}";

			Assert.ThrowsAsync<FatalCompilationException>(async () => await CompileFromText(source));
		}

		[Test]
		public void MethodsMayTakeParameters()
		{
			var source =
				@"
public static class Program
{
	public static void Main()
	{
	}

	public static void Foo(byte a)
	{
	}
}";

			Assert.DoesNotThrowAsync(async () => await CompileFromText(source));
		}

		[Test]
		public void MethodsMayNotTakeRefParameters()
		{
			var source =
				@"
public static class Program
{
	public static void Main()
	{
	}

	public static void Foo(ref byte a)
	{
	}
}";

			Assert.ThrowsAsync<FatalCompilationException>(async () => await CompileFromText(source));
		}

		[Test]
		public void MethodsMayNotTakeOutParameters()
		{
			var source =
				@"
public static class Program
{
	public static void Main()
	{
	}

	public static void Foo(out byte a)
	{
	}
}";

			Assert.ThrowsAsync<FatalCompilationException>(async () => await CompileFromText(source));
		}
	}
}
