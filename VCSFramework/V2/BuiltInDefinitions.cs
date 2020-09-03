﻿using Mono.Cecil;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace VCSFramework.V2
{
    internal static class BuiltInDefinitions
    {
        public static readonly AssemblyDefinition System
            = AssemblyDefinition.ReadAssembly(typeof(object).GetTypeInfo().Assembly.Location);

        public static readonly AssemblyDefinition Framework
            = AssemblyDefinition.ReadAssembly(typeof(Macro).GetTypeInfo().Assembly.Location);

        public static readonly ImmutableArray<AssemblyDefinition> BuiltInAssemblies
            = new[] { System, Framework }.ToImmutableArray();

        private static IEnumerable<TypeDefinition> AllTypeDefinitions
            => BuiltInAssemblies.SelectMany(a => a.MainModule.Types);

        public static readonly TypeDefinition Byte = AllTypeDefinitions.Single(t => t.FullName == typeof(byte).FullName);

        public static readonly TypeDefinition Bool = AllTypeDefinitions.Single(t => t.FullName == typeof(bool).FullName);
    }
}
