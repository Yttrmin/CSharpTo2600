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

	struct CompositeStruct
	{
		public MultiByteStruct MultiByteStruct1;
		public SingleByteStruct SingleByteStruct1;
		public MultiByteStruct MultiByteStruct2;
	}

	static class StructTesting
    {
		private static SingleByteStruct SingleByteStruct;
		private static MultiByteStruct MultiByteStruct1;
		private static MultiByteStruct MultiByteStruct2;

		public static void Main()
		{
			SingleByteStruct.Value = 24;

			MultiByteStruct1.ValueC = 48;

			// This will cause an error due to ldfld not being implemented.
			// MultiByteStruct1.ValueA = MultiByteStruct1.ValueC;

			// This will cause an error due to multi-byte loads/stores.
			// MultiByteStruct1 = MultiByteStruct2;
		}
    }
}
