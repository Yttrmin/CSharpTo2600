using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCSFramework.V2
{
    /// <summary>
    /// Collection of advanced utilities that directly interact with the asembler or assembly code.
    /// Use at your own risk.
    /// </summary>
    public static class AssemblyUtilities
    {
        /// <summary>
        /// Emits <paramref name="assembly"/> directly into the assembly file at the call site.
        /// </summary>
        /// <param name="assembly">6502.NET-compatible assembly code. MUST be a compile time constant.</param>
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
