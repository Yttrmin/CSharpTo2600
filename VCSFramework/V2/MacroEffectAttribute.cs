using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
