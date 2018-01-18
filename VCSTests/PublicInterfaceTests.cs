using NUnit.Framework;
using VCSCompiler;
using System;
using System.Linq;

namespace VCSTests
{
	[TestFixture]
	public class PublicInterfaceTests
	{
		[Test]
		public void ThrowsOnNullSource()
		{
			Assert.ThrowsAsync<ArgumentNullException>(async () => await Compiler.CompileFromText(
				null,
				TestContext.CurrentContext.Random.GetString(32),
				TestContext.CurrentContext.Random.GetString(32)));
		}

		[Test]
		public void ThrowsOnNullFilePaths()
		{
			Assert.ThrowsAsync<ArgumentNullException>(async () => await Compiler.CompileFromFiles(
				null,
				TestContext.CurrentContext.Random.GetString(32),
				TestContext.CurrentContext.Random.GetString(32)));
		}

		[Test]
		public void ThrowsOnEmptyFilePaths()
		{
			Assert.ThrowsAsync<ArgumentException>(async () => await Compiler.CompileFromFiles(
				Enumerable.Empty<string>(),
				TestContext.CurrentContext.Random.GetString(32),
				TestContext.CurrentContext.Random.GetString(32)));
		}

		[Test]
		public void ThrowsOnNullFrameworkPath()
		{
			Assert.ThrowsAsync<ArgumentNullException>(async () => await Compiler.CompileFromText(
				TestContext.CurrentContext.Random.GetString(32),
				null,
				TestContext.CurrentContext.Random.GetString(32)));
		}

		[Test]
		public void ThrowsOnEmptyFrameworkPath()
		{
			Assert.ThrowsAsync<ArgumentException>(async () => await Compiler.CompileFromText(
				TestContext.CurrentContext.Random.GetString(32),
				string.Empty,
				TestContext.CurrentContext.Random.GetString(32)));
		}
	}
}
