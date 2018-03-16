using System;
using System.Collections.Generic;
using System.Text;
using VCSFramework;

namespace VCSCompiler
{
    internal static class AttributeReconstructor
    {
		public static Attribute ReconstructFrom<T>(dynamic source)
			where T : Attribute
		{
			switch (typeof(T).Name)
			{
				case nameof(AlwaysInlineAttribute):
					return new AlwaysInlineAttribute();
				case nameof(CompilerImplementedAttribute):
					return new CompilerImplementedAttribute();
				case nameof(CompileTimeExecutedMethodAttribute):
					return new CompileTimeExecutedMethodAttribute(source.ImplementationName);
				case nameof(IgnoreImplementationAttribute):
					return new IgnoreImplementationAttribute();
				case nameof(OverrideWithLoadFromSymbolAttribute):
					return new OverrideWithLoadFromSymbolAttribute(source.Symbol);
				case nameof(OverrideWithStoreToSymbolAttribute):
					return new OverrideWithStoreToSymbolAttribute(source.Symbol, source.Strobe);
				case nameof(OverrideWithLoadToRegisterAttribute):
					return new OverrideWithLoadToRegisterAttribute(source.Register);
				case nameof(UseProvidedImplementationAttribute):
					return new UseProvidedImplementationAttribute(source.ImplementationName);
				case nameof(DoNotCompileAttribute):
					return new DoNotCompileAttribute();
				default:
					throw new FatalCompilationException($"Attempted to reconstruct unknown attribute: {source}");
			}
		}
    }
}
