using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("VCSCompiler")]

namespace VCSFramework
{
	[DoNotCompile]
	[AttributeUsage(AttributeTargets.Method)]
    public sealed class CompilerImplementedAttribute : Attribute
	{

	}

	[DoNotCompile]
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class OverrideWithStoreToSymbolAttribute : Attribute
	{
		public string Symbol { get; }

		public OverrideWithStoreToSymbolAttribute(string symbol)
		{
			Symbol = symbol;
		}
	}

	[DoNotCompile]
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class DoNotCompileAttribute : Attribute
	{

	}
}
