using System;

namespace CSharpTo2600.Framework
{
    public class Atari2600Game : Attribute
    {

    }

    public class SpecialMethodAttribute : Attribute
    {
        public readonly MethodType GameMethod;

        public SpecialMethodAttribute(MethodType GameMethod)
        {
            this.GameMethod = GameMethod;
        }
    }

    public enum MethodType
    {
        None,
        UserDefined,
        Initialize,
        Kernel,
        Tick,
        DuringVSync,
    }
}
