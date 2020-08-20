#nullable enable

namespace VCSCompiler.V2
{
    public sealed class CompilerOptions
    {
        public string? OutputPath { get; init; }
        public string FrameworkPath { get; init; } = "./VCSFramework.dll";
        public string? EmulatorPath { get; init; }
        public string? TextEditorPath { get; init; }
        public bool DisableOptimizations { get; init; }
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
