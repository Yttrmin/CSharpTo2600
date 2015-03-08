using System;
using CSharpTo2600.Framework.Assembly;
using BindingFlags = System.Reflection.BindingFlags;

namespace CSharpTo2600.Framework
{
    public class Atari2600Game : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Method)]
    public class SpecialMethodAttribute : Attribute
    {
        public readonly MethodType GameMethod;

        public SpecialMethodAttribute(MethodType GameMethod)
        {
            this.GameMethod = GameMethod;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class KernelAttribute : Attribute
    {
        public readonly KernelTechnique Technique;

        public KernelAttribute(KernelTechnique Technique)
        {
            this.Technique = Technique;
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
        public readonly Symbol GlobalSymbol;

        public CompilerIntrinsicGlobalAttribute(string Name)
        {
            GlobalSymbol = typeof(ReservedSymbols).GetField(Name, BindingFlags.Static | BindingFlags.Public)
                .GetValue(null) as Symbol;
            if (GlobalSymbol == null)
            {
                throw new ArgumentException($"There is no reserved symbol called {Name}", nameof(Name));
            }
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

    public enum KernelTechnique
    {
        None,
        /// <summary>
        /// This method is invoked during the horizontal blank period of every scanline (192 times in NTSC).
        /// The framework will handle synchronization.
        /// </summary>
        CallEveryScanline,
        /// <summary>
        /// This method is invoked during the horizontal blank period of every other scanline (96 times in NTSC).
        /// The framework will handle synchronization.
        /// </summary>
        CallEveryOtherScanline,
        /// <summary>
        /// This method is called only once after the vertical blank period. The user is responsible for
        /// synchronization and ensuring all 192 scanlines have occured before returning.
        /// This should be used by advanced users only.
        /// </summary>
        Manual
    }
}
