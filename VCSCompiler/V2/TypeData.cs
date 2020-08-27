#nullable enable
using Mono.Cecil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using VCSFramework.V2;

namespace VCSCompiler.V2
{
    internal sealed record TypeData(TypeDefinition Type, int Size)
    {
        private static readonly ImmutableArray<TypeDefinition> BuiltInTypes
            = AssemblyDefinitions.BuiltIn.Select(a => a.MainModule).SelectMany(m => m.Types).ToImmutableArray();

        private static readonly ImmutableArray<TypeDefinition> SystemTypes
            = AssemblyDefinition.ReadAssembly(typeof(object).GetTypeInfo().Assembly.Location).MainModule.Types.ToImmutableArray();

        public static TypeData Byte { get; } = new(GetBuiltInTypeDef<byte>(), 1);

        // @TODO - Give hardcode typeNum so VIL can check against it
        public static TypeData Nothing { get; } = new(GetBuiltInTypeDef<Nothing>(), 0);

        public static TypeData Of(TypeDefinition type, AssemblyDefinition userAssembly)
        {
            var size = GetSize(type, userAssembly);
            return new(type, size);
        }

        public static TypeData Of(TypeReference type, AssemblyDefinition userAssembly)
        {
            var typeDef = BuiltInTypes
                .Concat(userAssembly.CompilableTypes())
                .Single(t => t.FullName == type.FullName);
            return Of(typeDef, userAssembly);
        }

        private static TypeDefinition GetBuiltInTypeDef<T>()
            => BuiltInTypes.Single(it => it.FullName == typeof(T).FullName);

        private static int GetSize(TypeDefinition type, AssemblyDefinition userAssembly)
        {
            if (type.Namespace.StartsWith("System"))
            {
                return GetSystemTypeData(type).Size;
            }

            return type.Fields.Sum(f => Of(f.FieldType, userAssembly).Size);
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
