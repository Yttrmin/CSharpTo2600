﻿#nullable enable

namespace VCSCompiler
{
    public sealed record RomInfo
    {
        public bool IsSuccessful { get; init; }
        public string? RomPath { get; init; }
        public string? AssemblyPath { get; init; }
        public string? ListPath { get; init; }
    }
}
