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

    public sealed class RomDataAttribute : Attribute
    {
        public byte[] Data { get; }

        public RomDataAttribute(byte[] data)
        {
            Data = data;
        }
    }
}
