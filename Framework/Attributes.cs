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

    /// <summary>
    /// Tells the compiler to ignore the implementation of a property and
    /// to instead treat it as the specified global.
    /// Has no effect when applied to user code outside this framework.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class CompilerIntrinsicGlobalAttribute : Attribute
    {
        public readonly string GlobalName;

        public CompilerIntrinsicGlobalAttribute(string Name)
        {
            GlobalName = Name;
        }
    }

    public class StrobeAttribute : Attribute
    {

    }

    public enum MethodType
    {
        None,
        /// <summary>
        /// Not called by the skeleton code.
        /// </summary>
        UserDefined,
        /// <summary>
        /// Called once after the console has been initialized.
        /// </summary>
        Initialize,
        /// <summary>
        /// Called during the vertical blank period.
        /// </summary>
        MainLoop,
        // A single "kernel" skeleton is impossible, different games will
        // have different needs. Probably need another attribute specifically
        // for kernels.
        Kernel,
        /// <summary>
        /// Called during the overscan period.
        /// </summary>
        Overscan
    }
}
