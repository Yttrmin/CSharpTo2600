using System;

namespace VCSFramework.V2
{
    public abstract class MacroEffectAttribute : Attribute
    {
    }

    public sealed class PushStackAttribute : MacroEffectAttribute
    {
        public int Count { get; init; }
    }

    public sealed class PopStackAttribute : MacroEffectAttribute
    {
        public int Count { get; init; }
    }

    public sealed class ReservedBytesAttribute : MacroEffectAttribute
    {
        public int Count { get; init; }
    }
}
