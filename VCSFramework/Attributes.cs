using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("VCSCompiler")]

namespace VCSFramework
{
	[DoNotCompile]
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public sealed class AlwaysInlineAttribute : Attribute
	{

	}

	[DoNotCompile]
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class CompilerImplementedAttribute : Attribute
	{

	}

	[DoNotCompile]
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class IgnoreImplementationAttribute : Attribute
	{
		
	}

	[DoNotCompile]
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class OverrideWithLoadFromSymbolAttribute : Attribute
	{
		public string Symbol { get; }

		public OverrideWithLoadFromSymbolAttribute(string symbol)
		{
			Symbol = symbol;
		}
	}

	[DoNotCompile]
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public sealed class OverrideWithStoreToSymbolAttribute : Attribute
	{
		public string Symbol { get; }

		public OverrideWithStoreToSymbolAttribute(string symbol)
		{
			Symbol = symbol;
		}
	}

	[DoNotCompile]
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public sealed class OverrideWithLoadToRegisterAttribute : Attribute
	{
		public string Register { get; }

		public OverrideWithLoadToRegisterAttribute(string register)
		{
			Register = register;
		}
	}

	[DoNotCompile]
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public sealed class UseProvidedImplementationAttribute : Attribute
	{
		public string ImplementationName { get; }

		public UseProvidedImplementationAttribute(string implementatioName)
		{
			ImplementationName = implementatioName;
		}
	}
	
	[DoNotCompile]
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
	public sealed class DoNotCompileAttribute : Attribute
	{

	}
}
