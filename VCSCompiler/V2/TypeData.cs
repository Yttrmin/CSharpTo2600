#nullable enable
using Mono.Cecil;
using System;
using System.Linq;
using VCSFramework.V2;

namespace VCSCompiler.V2
{
    // @TODO - Unless we find a need for other data this can probably
    // just be a util that returns size.
    internal sealed record TypeData(TypeDefinition? Type, int Size)
    {
        public static TypeData Byte { get; } = new(GetBuiltInTypeDef<byte>(), 1);

        public static TypeData Bool { get; } = new(GetBuiltInTypeDef<bool>(), 1);

        public static TypeData Nothing { get; } = new(GetBuiltInTypeDef<Nothing>(), 0);

        public static TypeData Of(TypeDefinition type, AssemblyDefinition userAssembly)
        {
            var size = GetSize(type, userAssembly);
            return new(type, size);
        }

        public static TypeData Of(TypeReference type, AssemblyDefinition userAssembly)
        {
            if (type.IsPointer || type.IsPinned)
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
                return new(null, 1);
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
            if (type.Namespace.StartsWith("System"))
            {
                return GetSystemTypeData(type).Size;
            }

            return type.Fields.Sum(f => Of(f.FieldType, userAssembly).Size);
        }

        private static TypeData GetSystemTypeData(TypeDefinition type)
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
