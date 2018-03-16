using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

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

	[DoNotCompile]
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class CompilerImplementedAttribute : Attribute
	{

	}

	/// <summary>
	/// Instructs the compiler to execute the method provided at <c>ImplementationName</c>
	/// and replace the call site with the results of that execution.
	/// The implementing method must return IEnumerable<AssemblyLine>.
	/// The implementing method can take an arbitrary number of constant byte arguments.
	/// </summary>
	[DoNotCompile]
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public sealed class CompileTimeExecutedMethodAttribute : Attribute
	{
		public string ImplementationName { get; }

		public CompileTimeExecutedMethodAttribute(string implementationName)
		{
			ImplementationName = implementationName;
		}
	}

	/// <summary>
	/// Instructs compiler not to compile the CIL body of this method.
	/// Generally used in combination with another attribute to provide an implementation.
	/// </summary>
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
	/// Instructs compiler to replace this method's body with that of another method.
	/// </summary>
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
	
	/// <summary>
	/// Instructs compiler to completely ignore this type or method.
	/// </summary>
	[DoNotCompile]
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
	public sealed class DoNotCompileAttribute : Attribute
	{

	}
}
