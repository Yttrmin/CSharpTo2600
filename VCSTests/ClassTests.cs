using NUnit.Framework;
using VCSCompiler;

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
			Assert.DoesNotThrowAsync(
				async () => await Compiler.CompileFromText(source, "./VCSFramework.dll", "../../../../Dependencies/DASM/dasm.exe"));
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
			Assert.ThrowsAsync<FatalCompilationException>(
				async () => await Compiler.CompileFromText(source, "./VCSFramework.dll", "../../../../Dependencies/DASM/dasm.exe"));
		}
	}
}
