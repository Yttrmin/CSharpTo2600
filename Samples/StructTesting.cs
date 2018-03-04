using System;
using System.Collections.Generic;
using System.Text;

namespace Samples
{
	struct SingleByteStruct
	{
		public byte Value;
	}

	struct MultiByteStruct
	{
		public byte ValueA;
		public byte ValueB;
		public byte ValueC;
	}

	static class StructTesting
    {
		private static SingleByteStruct Foo;
		private static MultiByteStruct Bar;

		public static void Main()
		{
			Foo.Value = 24;

			Bar.ValueC = 48;
		}
    }
}
