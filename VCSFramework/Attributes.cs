using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("VCSCompiler")]

namespace VCSFramework
{
    /// <summary>
    /// Instructs compiler to replace all invocations of this method with its body.
    /// </summary>
    [DoNotCompile]
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public sealed class AlwaysInlineAttribute : Attribute
	{

	}

	/// <summary>
	/// Instructs compiler not to compile the CIL body of this method.
	/// Generally used in combination with another attribute to provide an implementation.
	/// </summary>
	[Obsolete("Don't think this is needed now that we only compile on-demand.")]
	[DoNotCompile]
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class IgnoreImplementationAttribute : Attribute
	{
		
	}

	/// <summary>
	/// Instructs compiler to replace a non-void 0-parameter method invocation with an LDA instruction.
	/// </summary>
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

	/// <summary>
	/// Instructs compiler to replace a void 1-parameter method invocation with an STA instruction.
	/// </summary>
	[DoNotCompile]
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public sealed class OverrideWithStoreToSymbolAttribute : Attribute
	{
		public string Symbol { get; }
		public bool Strobe { get; }

		public OverrideWithStoreToSymbolAttribute(string symbol, bool strobe = false)
		{
			Symbol = symbol;
			Strobe = strobe;
		}
	}

	/// <summary>
	/// Instructs compiler to replace a void 1-parameter method invocation with an LDA/X/Y instruction.
	/// </summary>
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
	
	/// <summary>
	/// Instructs compiler to completely ignore this type or method.
	/// </summary>
	[Obsolete("Don't think this is needed now that we only compile on-demand.")]
	[DoNotCompile]
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
	public sealed class DoNotCompileAttribute : Attribute
	{

	}
}
