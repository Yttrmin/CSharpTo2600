#nullable enable
using System;

namespace VCSFramework.V2
{
    public enum PointerLength
    {
        Invalid,
        Short,
        Long
    }

    [AttributeUsage(AttributeTargets.ReturnValue, AllowMultiple = false)]
    public sealed class PointerLengthAttribute : Attribute
    {
        public PointerLength Length { get; }

        public PointerLengthAttribute(PointerLength length)
        {
            Length = length;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class TemplatedProgramAttribute : Attribute
    {
        public Type TemplateType { get; }

        public TemplatedProgramAttribute(Type templateType)
        {
            TemplateType = templateType;
        }
    }

    /// <summary>
    /// Forces the compiler to allocate this field at the specified address.
    /// Only use this if you need to know the address ahead of time (e.g. for inline assembly).
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class FixedAddressAttribute : Attribute
    {
        public byte Address { get; }

        // @TODO - Flag to turn off reusing of the address? Or util that lets us hint the compiler that it's used? In the
        // context of inline assembly code that the compiler is blind to.
        public FixedAddressAttribute(byte address)
        {
            Address = address;
        }
    }

    /// <summary>
    /// Replaces invocations of this method with the provided macro, instead of an invocation macro.
    /// Macro must have a constructor that takes a single <see cref="Mono.Cecil.Cil.Instruction"/> as the only parameter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ReplaceWithMacroAttribute : Attribute
    {
        public Type MacroType { get; }

        public ReplaceWithMacroAttribute(Type macroType)
        {
            MacroType = macroType;
        }
    }

    public sealed class RomDataAttribute : Attribute
    {
        public byte[] Data { get; }

        public RomDataAttribute(byte[] data)
        {
            Data = data;
        }
    }
}
