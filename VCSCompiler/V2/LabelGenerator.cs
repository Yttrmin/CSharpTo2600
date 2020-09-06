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
            => new(BuiltInDefinitions.Byte);

        public static TypeLabel ByteType
            => new(BuiltInDefinitions.Byte);

        public static TypeLabel NothingType
            => new(BuiltInDefinitions.Nothing);

        public static SizeLabel NothingSize
            => new(BuiltInDefinitions.Nothing);

        public static ConstantLabel Constant(byte value) => new(value);

        public static GlobalLabel Global(FieldReference fieldReference)
            => new($"GLOBAL_{fieldReference.DeclaringType.NamespaceAndName()}_{fieldReference.Name}", fieldReference.FieldType);

        public static InstructionLabel Instruction(Instruction instruction)
            => new($"IL_{instruction.Offset:x4}");

        public static MethodLabel Function(MethodDefinition method)
            => new(method, false);

        [Obsolete]
        public static MethodLabel InlineFunction(MethodDefinition method)
            => new(method, true);

        public static BaseSizeLabel FieldSize(FieldReference field)
        {
            // @TODO - IsByRef?
            if (field.FieldType.IsPointer)
            {
                return new PointerSizeLabel(true);
            }
            return new SizeLabel(field.FieldType);
        }

        public static BaseSizeLabel LocalSize(VariableDefinition variable)
        {
            var type = variable.VariableType;
            if (type.IsPointer || type.IsPinned || type.IsByReference)
            {
                return new PointerSizeLabel(true);
            }
            return new SizeLabel(type);
        }

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
