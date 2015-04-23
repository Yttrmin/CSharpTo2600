using System.Collections.Generic;
using System.IO;
using CSharpTo2600.Framework.Assembly;

namespace CSharpTo2600.Compiler
{
    public sealed class ROMInfo
    {
        public readonly string ROMPath;
        public readonly string AssemblyPath;
        public readonly string SymbolsPath;
        public readonly string ListPath;
        public readonly CompilationInfo CompilationInfo;
        public readonly IEnumerable<AssemblyLine> AssemblyLinesSource;
        public readonly byte[] ROM;
        public readonly bool Success;

        internal ROMInfo(string ROMPath, string AssemblyPath, string SymbolsPath, string ListPath,
            CompilationInfo CompilationInfo, IEnumerable<AssemblyLine> AssemblyLinesSource, bool Success)
        {
            this.AssemblyPath = AssemblyPath;
            this.SymbolsPath = SymbolsPath;
            this.ListPath = ListPath;
            this.CompilationInfo = CompilationInfo;
            this.AssemblyLinesSource = AssemblyLinesSource;
            this.Success = Success;
            if (Success)
            {
                this.ROMPath = ROMPath;
                ROM = File.ReadAllBytes(ROMPath);
            }
        }
    }
}
