#nullable enable
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using VCSFramework.V2;

namespace VCSCompiler.V2
{
    internal static class LabelGenerator
    {
        public static SizeLabel Size(TypeReference typeReference) 
            => new(typeReference);

        public static SizeLabel ByteSize
            => new(TypeData.Byte.Type);

        public static TypeLabel ByteType
            => new(TypeData.Byte.Type);

        public static ConstantLabel Constant(byte value) => new(value);

        public static GlobalLabel Global(FieldReference fieldReference)
            => new($"GLOBAL_{fieldReference.DeclaringType.NamespaceAndName()}_{fieldReference.Name}");

        public static InstructionLabel Instruction(Instruction instruction)
            => new($"IL_{instruction.Offset:x4}");

        public static FunctionLabel Function(MethodDefinition method)
            => new($"FUNC_{method.DeclaringType.NamespaceAndName()}_{method.Name}");

        public static TypeLabel Type(TypeReference type)
            => new(type);

        public static FunctionLabel Start => new("START");
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
