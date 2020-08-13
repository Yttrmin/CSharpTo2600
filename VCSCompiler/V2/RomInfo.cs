#nullable enable

namespace VCSCompiler.V2
{
    public sealed record RomInfo
    {
        bool IsSuccessful => RomPath != null;
        string? RomPath { get; init; }
        string? AssemblyPath { get; init; }
        string? ListPath { get; init; }
    }
}
