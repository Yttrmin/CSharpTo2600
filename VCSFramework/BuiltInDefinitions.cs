using Mono.Cecil;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace VCSFramework
{
    internal static class BuiltInDefinitions
    {
        public static readonly AssemblyDefinition System
            = AssemblyDefinition.ReadAssembly(typeof(object).GetTypeInfo().Assembly.Location);

        public static readonly AssemblyDefinition Framework
            = AssemblyDefinition.ReadAssembly(typeof(IAssemblyEntry).GetTypeInfo().Assembly.Location);

        public static readonly ImmutableArray<AssemblyDefinition> Assemblies
            = new[] { System, Framework }.ToImmutableArray();

        private static IEnumerable<TypeDefinition> AllTypeDefinitions
            => Assemblies.SelectMany(a => a.MainModule.Types);

        public static IEnumerable<TypeDefinition> Types
            => new[] { Byte, Bool, Nothing };

        public static readonly TypeDefinition Byte = AllTypeDefinitions.Single(t => t.FullName == typeof(byte).FullName);

        public static readonly TypeDefinition Bool = AllTypeDefinitions.Single(t => t.FullName == typeof(bool).FullName);

        public static readonly TypeDefinition IEnumerable = AllTypeDefinitions.Single(t => t.Name == "IEnumerable`1");

        public static readonly TypeDefinition Nothing = AllTypeDefinitions.Single(t => t.FullName == typeof(Nothing).FullName);
    }
}
