#nullable enable
using Mono.Cecil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using VCSFramework.V2;

namespace VCSCompiler.V2
{
    // @TODO - Unless we find a need for other data this can probably
    // just be a util that returns size.
    internal sealed record TypeData(TypeDefinition? Type, int Size, ImmutableArray<TypeData.FieldData> Fields)
    {
        public sealed record FieldData(FieldDefinition Field, byte Offset);

        public static TypeData Byte { get; } = new(GetBuiltInTypeDef<byte>(), 1, ImmutableArray<TypeData.FieldData>.Empty);

        public static TypeData Bool { get; } = new(GetBuiltInTypeDef<bool>(), 1, ImmutableArray<TypeData.FieldData>.Empty);

        public static TypeData Nothing { get; } = new(GetBuiltInTypeDef<Nothing>(), 0, ImmutableArray<TypeData.FieldData>.Empty);

        public static TypeData Of(TypeDefinition type, AssemblyDefinition userAssembly)
        {
            if (type.Namespace.StartsWith("System"))
            {
                return GetSystemTypeData(type);
            }

            var size = GetSize(type, userAssembly);
            return new(type, size, GetFieldData(type, userAssembly).ToImmutableArray());
        }

        public static TypeData Of(TypeReference type, AssemblyDefinition userAssembly)
        {
            if (type.IsPointer || type.IsPinned || type.IsByReference)
            {
                // IsPinned catches e.g. System.Byte&

                // Pointer types have no TypeDefinition. Attempting to Resolve() them just
                // produces their non-pointer type.
                // @TODO - We assume these are always zero-page pointers. Will have to figure
                // something out for pointers to ROM (use arrays instead?)
                // @TODO - Zero-page pointers also won't work for cartridge types that map
                // extra RAM outside of the zero-page.
                // @TODO - Depending on how plausible optimizing is, we could always use
                // 16-bit pointers and hope enough of it is optimized away...
                return new(null, 1, ImmutableArray<TypeData.FieldData>.Empty);
            }

            if (type.Namespace.StartsWith("System"))
            {
                return GetSystemTypeData(type);
            }

            var typeDef = BuiltInDefinitions.Types
                .Concat(userAssembly.CompilableTypes())
                .Single(t => t.FullName == type.FullName);
            return Of(typeDef, userAssembly);
        }

        private static TypeDefinition GetBuiltInTypeDef<T>()
            => BuiltInDefinitions.Types.Single(it => it.FullName == typeof(T).FullName);

        private static int GetSize(TypeDefinition type, AssemblyDefinition userAssembly)
        {
            // @TODO - Throw if Pack!=0/Size!=0 for sequential
            if (type.IsAutoLayout || type.IsSequentialLayout)
            {
                return type.InstanceFields().Sum(f => Of(f.FieldType, userAssembly).Size);
            }
            else
            {
                // Size of explicit struct is determined by largest FieldOffset+FieldSize struct.
                // A struct with a single byte at offset 30 would be 31 bytes large.
                return type.InstanceFields().Select(f =>
                {
                    var offset = f.Offset;
                    var size = TypeData.Of(f.FieldType, userAssembly).Size;
                    return offset + size;
                }).Max();
            }
        }

        private static IEnumerable<FieldData> GetFieldData(TypeDefinition type, AssemblyDefinition userAssembly)
        {
            if (type.IsExplicitLayout)
            {
                foreach (var field in type.InstanceFields())
                {
                    yield return new FieldData(field, (byte)field.Offset);
                }
            }
            else
            {
                byte offset = 0;
                foreach (var field in type.InstanceFields())
                {
                    yield return new FieldData(field, offset);
                    offset += (byte)GetSize(field.FieldType.Resolve(), userAssembly);
                }
            }
        }

        private static TypeData GetSystemTypeData(TypeReference type)
        {
            if (type.FullName == typeof(byte).FullName)
                return Byte;
            else if (type.FullName == typeof(bool).FullName)
                return Bool;
            else
                throw new ArgumentException($"No support for System type: '{type.FullName}'");
        }
    }
}
