#nullable enable
using Mono.Cecil;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace VCSCompiler.V2
{
    internal sealed record TypeData(TypeDefinition Type, int Size)
    {
        private static readonly ImmutableArray<TypeDefinition> SystemTypes
            = AssemblyDefinition.ReadAssembly(typeof(object).GetTypeInfo().Assembly.Location).MainModule.Types.ToImmutableArray();

        private static TypeDefinition GetSystemTypeDef<T>()
            => SystemTypes.Single(it => it.FullName == typeof(T).FullName);

        public static TypeData Byte { get; } = new(GetSystemTypeDef<byte>(), 1);
    }
}
