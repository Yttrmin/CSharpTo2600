#nullable enable
using System;

namespace VCSFramework
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class TemplatedProgramAttribute : Attribute
    {
        public Type TemplateType { get; }

        public TemplatedProgramAttribute(Type templateType)
        {
            TemplateType = templateType;
        }
    }

    // @TODO - Perhaps we could provide a hint attribute that could be used when calling InlineAssembly. Could hint to the compiler
    // when a var is read/written so we can optimize storage usage like what might be possible for un-attributed fields.
    // Only problem is if a user uses it inconsistency it could cause very broken results...
    /// <summary>
    /// Emits an alias that points to the attributed static field. The primary purpose for this is to enable
    /// access to the field via <see cref="AssemblyUtilities.InlineAssembly(string)"/>.
    /// The same field can be aliased multiple times, but one alias can not be used for multiple fields.
    /// Marking a field with this attribute prevents certain optimizations.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class InlineAssemblyAliasAttribute : Attribute
    {
        public string Alias { get; }

        /// <summary>Aliases this static field.</summary>
        /// <param name="alias">Alias to emit into the assembly file. MUST begin with <see cref="AssemblyUtilities.AliasPrefix"/> in order to prevent name collisions with compiler-generated names.</param>
        public InlineAssemblyAliasAttribute(string alias)
        {
            Alias = alias;
        }
    }

    /// <summary>
    /// Replaces invocations of this method with the provided <see cref="IAssemblyEntry"/>, instead of a macro invocation.
    /// Type must have a constructor that takes a single <see cref="Mono.Cecil.Cil.Instruction"/> as the only parameter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ReplaceWithEntryAttribute : Attribute
    {
        public Type Type { get; }

        public ReplaceWithEntryAttribute(Type macroType)
        {
            Type = macroType;
        }
    }

    [Obsolete]
    public sealed class RomDataAttribute : Attribute
    {
        public byte[] Data { get; }

        public RomDataAttribute(byte[] data)
        {
            Data = data;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class RomDataGeneratorAttribute : Attribute
    {
        public string MethodName { get; }

        public RomDataGeneratorAttribute(string methodName)
        {
            MethodName = methodName;
        }
    }

    /// <summary>
    /// Instructs compiler to replace all invocations of this method with its body.
    /// </summary>
    [DoNotCompile]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class AlwaysInlineAttribute : Attribute
    {

    }

    /// <summary>Instructs the compiler to completely ignore calls to methods marked with this.</summary>
    [DoNotCompile]
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class IgnoreCallAttribute : Attribute
    {

    }

    /// <summary>
    /// Instructs the compiler to always use a short 'this' pointer for the marked 'readonly' method (or method of a 'readonly' type).
    /// This must not be used if you make any calls to this method through a reference returned by <see cref="RomData{T}"/>.
    /// There's no point using this on types that aren't used as a type argument for a <see cref="RomData{T}"/> field.
    /// There's no point using this on non-'readonly' methods, because the compiler will make a defensive copy.
    /// Use at your own risk.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class AlwaysUseShortThisPointerAttribute : Attribute
    {

    }

    /// <summary>
    /// For variables: Tells the compiler this variable will only ever hold short pointers.
    /// For return values: Tells the compiler that the pointer returned will always be short.
    /// </summary>
    [AttributeUsage(AttributeTargets.ReturnValue, AllowMultiple = false)]
    public sealed class ShortPointerAttribute : Attribute
    {

    }

    /// <summary>
    /// For variables: Tells the compiler this variable may hold short or long pointers.
    /// For return values: Tells the compiler that the pointer returned will always be long.
    /// </summary>
    [AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class LongPointerAttribute : Attribute
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
