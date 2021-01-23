#nullable enable
using VCSFramework;

namespace VCSCompiler
{
    public sealed class CompilerOptions
    {
        public string? OutputPath { get; init; }
        public string? EmulatorPath { get; init; }
        public string? TextEditorPath { get; init; }
        public Region? Region { get; init; } // @TODO
        public bool DisableOptimizations { get; init; }
        public bool FailOnStackOperations { get; init; } // @TODO
        public SourceAnnotation SourceAnnotations { get; init; } = SourceAnnotation.CSharp;
    }

    public enum SourceAnnotation
    {
        None,
        CSharp,
        CIL,
        Both
    }
}
