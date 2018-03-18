using System;
using System.Collections.Generic;
using System.Text;
using VCSFramework;

namespace Samples
{
    public static class MethodCompilationOrderTesting
    {
		private static byte TestByte;

		// AlwaysInline exacerbates the issue since since it requires the method to already be compiled by the
		// time it finds any call sites for it.
		[AlwaysInline]
		public static void Foo()
		{
			TestByte = 12;
		}

		public static void Main()
		{
			Foo();
			TestByte++;
			Bar();
		}

		[AlwaysInline]
		public static void Bar()
		{
			TestByte = 0;
		}
    }
}
