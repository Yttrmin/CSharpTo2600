#nullable enable
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using VCSFramework.V2;

namespace VCSCompiler.V2
{
    internal static class LabelGenerator
    {
        public static TypeSizeLabel Size(TypeReference typeReference) 
            => new(typeReference);

        public static TypeSizeLabel ByteSize
            => new(BuiltInDefinitions.Byte);

        public static TypeLabel ByteType
            => new(BuiltInDefinitions.Byte);

        public static TypeLabel NothingType
            => new(BuiltInDefinitions.Nothing);

        public static TypeSizeLabel NothingSize
            => new(BuiltInDefinitions.Nothing);

        public static TypeLabel Type(TypeReference type)
            => new(type);
    }

    static class TypeReferenceStringExtensions
    {
        public static string NamespaceAndName(this TypeReference @this)
        {
            var formattedNamespace = @this.Namespace.Replace('.', '_');
            return $"{formattedNamespace}_{@this.Name}";
        }
    }
}
