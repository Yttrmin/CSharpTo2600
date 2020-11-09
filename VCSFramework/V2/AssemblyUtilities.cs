using System;

namespace VCSFramework.V2
{
    /// <summary>
    /// Collection of advanced utilities that directly interact with the asembler or assembly code.
    /// Use at your own risk.
    /// </summary>
    public static class AssemblyUtilities
    {
        /// <summary>
        /// A string that MUST be prefixed onto all strings used for <see cref="InlineAssemblyAliasAttribute"/>.
        /// Used to guarantee that a user's alias does not conflict with compiler-generated names.
        /// </summary>
        public const string AliasPrefix = "ALIAS_";

        /// <summary>
        /// Emits <paramref name="assembly"/> directly into the assembly file at the call site.
        /// </summary>
        /// <param name="assembly">6502.NET-compatible assembly code. MUST be a compile time constant.</param>
        // @TODO - Do we need this attribute, or can we just look for CallVoid in the optimization?
        [ReplaceWithMacro(typeof(InlineAssembly))]
        public static void InlineAssembly(string assembly)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Instructs the assembler to repeat the code between here and the corresponding <see cref="EndRepeat"/> call <paramref name="count"/> times.
        /// </summary>
        /// <param name="count">Number of times to repeat enclosed code. MUST be a compile time constant.</param>
        public static void BeginRepeat(int count)
        {

        }

        public static void EndRepeat()
        {

        }
    }
}
