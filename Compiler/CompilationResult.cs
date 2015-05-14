using System.Collections.Generic;
using System.IO;
using CSharpTo2600.Framework.Assembly;

namespace CSharpTo2600.Compiler
{
    /// <summary>
    /// Contains information about the result of the compilation.
    /// </summary>
    public sealed class CompilationResult
    {
        /// <summary>
        /// Path of the assembled binary file. Null if DASM assembly failed.
        /// </summary>
        public string ROMPath { get; }
        /// <summary>
        /// Path of the assembly code file.
        /// </summary>
        public string AssemblyPath { get; }
        /// <summary>
        /// Path of the symbols file. Null if DASM assembly failed.
        /// </summary>
        public string SymbolsPath { get; }
        /// <summary>
        /// Path of the list file. Null if DASM assembly failed.
        /// </summary>
        public string ListPath { get; }
        /// <summary>
        /// The final CompilationInfo used to attempt to create a ROM.
        /// </summary>
        public CompilationState CompilationState { get; }
        /// <summary>
        /// The AssemblyLines used to create the ASM file.
        /// </summary>
        public IEnumerable<AssemblyLine> AssemblyLinesSource { get; }
        /// <summary>
        /// The machine code ultimately generated from the CompilationInfo. Null if DASM assembly failed.
        /// </summary>
        public byte[] ROM { get; }
        /// <summary>
        /// True if DASM produced a binary file with no errors.
        /// </summary>
        public bool DASMSuccess { get { return ROM != null; } }

        internal CompilationResult(string ROMPath, string AssemblyPath, string SymbolsPath, string ListPath,
            CompilationState CompilationState, IEnumerable<AssemblyLine> AssemblyLinesSource, bool Success)
        {
            this.AssemblyPath = AssemblyPath;
            this.CompilationState = CompilationState;
            this.AssemblyLinesSource = AssemblyLinesSource;
            if (Success)
            {
                this.SymbolsPath = SymbolsPath;
                this.ListPath = ListPath;
                this.ROMPath = ROMPath;
                ROM = File.ReadAllBytes(ROMPath);
            }
        }
    }
}
