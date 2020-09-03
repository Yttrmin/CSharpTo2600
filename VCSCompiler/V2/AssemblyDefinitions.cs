#nullable enable
using Mono.Cecil;
using System;
using System.Collections.Immutable;
using System.Reflection;
using VCSFramework.V2;

namespace VCSCompiler.V2
{
    [Obsolete]
    internal static class AssemblyDefinitions
    {
        public static readonly AssemblyDefinition System
            = AssemblyDefinition.ReadAssembly(typeof(object).GetTypeInfo().Assembly.Location);

        public static readonly AssemblyDefinition Framework
            = AssemblyDefinition.ReadAssembly(typeof(Macro).GetTypeInfo().Assembly.Location);

        public static readonly ImmutableArray<AssemblyDefinition> BuiltIn
            = new[] { System, Framework }.ToImmutableArray();
    }
}
