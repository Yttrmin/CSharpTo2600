#nullable enable
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using VCSFramework.V2;

namespace VCSCompiler.V2
{
    internal static class LabelGenerator
    {
        public static SizeLabel ByteSize(TypeReference typeReference) 
            => new($"SIZE_{typeReference.NamespaceAndName()}");

        public static SizeLabel ByteSize(Type type)
            => new("SIZE_System_Byte");

        public static ConstantLabel Constant(byte value) => new($"CONST_{value}");

        public static GlobalLabel Global(FieldReference fieldReference)
            => new($"GLOBAL_{fieldReference.DeclaringType.NamespaceAndName()}_{fieldReference.Name}");

        public static InstructionLabel Instruction(Instruction instruction)
            => new($"IL_{instruction.Offset:x4}");

        public static FunctionLabel Function(MethodDefinition method)
            => new($"FUNC_{method.DeclaringType.NamespaceAndName()}_{method.Name}");

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
