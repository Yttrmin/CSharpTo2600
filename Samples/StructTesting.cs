﻿using System;
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
		private static CompositeStruct CompositeStruct;

		public static void Main()
		{
			SingleByteStruct.Value = 24;

			MultiByteStruct1.ValueC = 48;
			
			MultiByteStruct1.ValueA = MultiByteStruct1.ValueC;
			
			CompositeStruct.MultiByteStruct2.ValueA = 76;

			SingleByteStruct = default;

			CompositeStruct = default;

			// This will cause an error due to multi-byte loads/stores.
			// MultiByteStruct1 = MultiByteStruct2;
		}
    }
}