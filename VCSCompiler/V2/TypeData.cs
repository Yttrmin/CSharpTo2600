#nullable enable
using Mono.Cecil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace VCSCompiler.V2
{
    internal sealed record TypeData(TypeDefinition Type, int Size)
    {
        private static readonly ImmutableArray<TypeDefinition> SystemTypes
            = AssemblyDefinition.ReadAssembly(typeof(object).GetTypeInfo().Assembly.Location).MainModule.Types.ToImmutableArray();

        public static TypeData Byte { get; } = new(GetSystemTypeDef<byte>(), 1);

        public static TypeData Of(TypeDefinition type, IEnumerable<AssemblyDefinition> assemblies)
        {
            var size = GetSize(type, assemblies);
            return new(type, size);
        }

        public static TypeData Of(TypeReference type, IEnumerable<AssemblyDefinition> assemblies)
        {
            var typeDef = SystemTypes
                .Concat(assemblies.SelectMany(a => a.CompilableTypes()))
                .Single(t => t.FullName == type.FullName);
            return Of(typeDef, assemblies);
        }

        private static TypeDefinition GetSystemTypeDef<T>()
            => SystemTypes.Single(it => it.FullName == typeof(T).FullName);

        private static int GetSize(TypeDefinition type, IEnumerable<AssemblyDefinition> assemblies)
        {
            if (type.Namespace.StartsWith("System"))
            {
                return GetSystemTypeData(type).Size;
            }

            return type.Fields.Sum(f => Of(f.FieldType, assemblies).Size);
        }

        private static TypeData GetSystemTypeData(TypeDefinition type)
        {
            switch (type.Name)
            {
                case var b when b == typeof(byte).Name:
                    return Byte;
                default:
                    throw new ArgumentException($"No support for System type: '{type.FullName}'");
            }
        }
    }
}
