using System;
using System.Collections.Generic;
using System.Text;
using VCSFramework;

namespace Samples
{
	public static class Foo
	{
		[AlwaysInline]
		public static void Run()
		{

		}
	}

	// TODO - This is not supported yet.
	public static class TypeCompilationOrderTesting
    {
		private static byte TestByte;

		// AlwaysInline exacerbates the issue since since it requires the method to already be compiled by the
		// time it finds any call sites for it.
		public static void Main()
		{
			Foo.Run();
			TestByte++;
			Bar.Run();
		}
	}

	public static class Bar
	{
		[AlwaysInline]
		public static void Run()
		{

		}
	}
}
