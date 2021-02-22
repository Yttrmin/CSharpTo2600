#nullable enable
using Mono.Cecil;
using System;
using System.Collections.Immutable;
using System.Linq;
using VCSFramework;
using static VCSCompiler.RomDataUtilities;

namespace VCSCompiler
{
    internal partial class MethodCompiler
    {
        // Can't use a record since we need to change Next, which is a pain when immutable.
        private sealed class LinkedEntry
        {
            public IAssemblyEntry Value { get; }
            public LinkedEntry? Next { get; set; }

            public LinkedEntry(IAssemblyEntry value, LinkedEntry? next)
            {
                Value = value;
                Next = next;
            }

            public void Deconstruct(out IAssemblyEntry value, out LinkedEntry? next)
            {
                value = Value;
                next = Next;
            }
        }

        private delegate LinkedEntry Optimizer(AssemblyPair userPair, LinkedEntry next);

        /// <summary>Optimizations that can't be disabled because the program literally won't assemble.</summary>
        private static ImmutableArray<Optimizer> MandatoryOptimizations = new Optimizer[]
        {
            // Turns an AssemblyUtilities.InlineAssembly() call into an entry that emits the assembly string.
            (_, next) => next switch
            {
                (LoadString(var ldStrInstruction), (InlineAssemblyCall, var trueNext)) => 
                    new(new InlineAssembly(((string)ldStrInstruction.Operand).Split(Environment.NewLine).Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).Prepend("// Begin inline assembly").Append("// End inline assembly").ToImmutableArray()), trueNext),
                _ => next
            },
            // Turns a RomData<T>::Length call into a Constant.
            (userPair, next) => next switch
            {
                (PushAddressOfGlobal(_, GlobalFieldLabel global, _ ,_),
                (RomDataLengthCall(var romDataInstruction), var trueNext)) =>
                    new(new PushConstant(romDataInstruction, 
                        new Constant(LengthOf(userPair.Assembly, (FieldDefinition)global.Field)), new TypeLabel(BuiltInDefinitions.Byte), new TypeSizeLabel(BuiltInDefinitions.Byte)), trueNext),
                _ => next
            },
            (userPair, next) => next switch
            {
                (PushAddressOfGlobal(_, GlobalFieldLabel global, _ ,_),
                (RomDataStrideCall(var romDataInstruction), var trueNext)) =>
                    new(new PushConstant(romDataInstruction,
                        new Constant(StrideOf(userPair.Definition, (FieldDefinition)global.Field)), new TypeLabel(BuiltInDefinitions.Byte), new TypeSizeLabel(BuiltInDefinitions.Byte)), trueNext),
                _ => next
            },
            (userPair, next) => next switch
            {
                (PushAddressOfGlobal(var pushInst, GlobalFieldLabel global, var pointerType,_),
                (RomDataGetPointerCall(var romDataInst), var trueNext)) =>
                    new(new PushAddressOfRomDataElementFromConstant(
                        ArrayOf(pushInst, romDataInst),
                        new RomDataGlobalLabel(GetGeneratorMethod((FieldDefinition)global.Field)),
                        GetRomDataArgType(global.Field),
                        GetRomDataArgSize(global.Field),
                        new Constant(0)), trueNext),
                _ => next
            },
            /*(userPair, next) => next switch
            {
                (PushAddressOfGlobal(_, GlobalFieldLabel global, _, _), 
                (_, 
                (PushFi, 
                (RomDataGetByByteOffsetCall(var inst), var trueNext)))) =>
                    new(
                        new PushAddressOfRomDataElementFromStack(ArrayOf(inst), LabelOf((FieldDefinition)global.Field), GetRomDataArgType(global.Field), GetRomDataArgSize(global.Field)), trueNext),
                _ => next
            },*/
#region RomData<T>_getItem optimizations
            (_, next) => next switch
            {
                (PushAddressOfGlobal(var pushGlobalInst, GlobalFieldLabel global, _ ,_),
                (PushConstant(var pushConstantInst, var constant, _, _),
                (RomDataGetterCall(var getInst), var trueNext))) => 
                    new(new PushAddressOfRomDataElementFromConstant(
                        ArrayOf(pushGlobalInst, pushConstantInst, getInst), 
                        new RomDataGlobalLabel(GetGeneratorMethod((FieldDefinition)global.Field)), 
                        GetRomDataArgType(global.Field), 
                        GetRomDataArgSize(global.Field), 
                        constant), trueNext),
                _ => next
            },
            (_, next) => next switch
            {
                (PushAddressOfGlobal(var pushGlobalInst, GlobalFieldLabel global, _ ,_),
                (PushGlobal pushGlobal,
                (RomDataGetterCall(var getInst), var trueNext))) =>
                    new(pushGlobal,
                        new(new PushAddressOfRomDataElementFromStack(
                        ArrayOf(pushGlobalInst, getInst),
                        new RomDataGlobalLabel(GetGeneratorMethod((FieldDefinition)global.Field)),
                        GetRomDataArgType(global.Field),
                        GetRomDataArgSize(global.Field)), trueNext)),
                _ => next
            },
            // @TODO - Optional optimization of .pushAddressOfRomDataElement + .pushDereferenceFromStack
            /*next => next switch
            {
                (PushAddressOfGlobal(_, GlobalFieldLabel global, _ ,_),
                (PushGlobal,
                (RomDataGetterCall, var trueNext))) => new(new Comment("A"), trueNext),
                _ => next
            }*/
#endregion
        }.ToImmutableArray();

        /// <summary>Optimizations that can be disabled, and will just make the program less efficient (perhaps fatally so).</summary>
        private static ImmutableArray<Optimizer> OptionalOptimizations = new Optimizer[]
        {
            // PushConstant + PopToGlobal = AssignConstantToGlobal
            (_, next) => next switch
            {
                // Pushing an integer and popping to a boolean is valid CIL, so requiring identical types would be incorrect.
                (PushConstant(var instA, var constant, _, var size),
                (PopToGlobal(var instB, var global, _, _, _, _), var trueNext))
                    => new(new AssignConstantToGlobal(instA.Concat(instB), constant, global, size), trueNext),
                _ => next
            },

            // PushGlobal + PopToGlobal = CopyGlobalToGlobal
            (_, next) => next switch
            {
                (PushGlobal(var instA, var global, _, var size),
                (PopToGlobal(var instB, var targetGlobal, _, var targetSize, _, _), var trueNext))
                    => new(new CopyGlobalToGlobal(instA.Concat(instB), global, size, targetGlobal, targetSize), trueNext),
                _ => next
            },

            // Adding a global and constant via the stack can be done in one macro, avoiding putting the constant on the stack.
            (_, next) => next switch
            {
                // PushGlobal+PushConstant or PushConstant+PushGlobal are both fine.
                (PushGlobal(var instA, var global, var globalType, var globalSize),
                (PushConstant(var instB, var constant, var constantType, var constantSize),
                (AddFromStack(var instC, _, _, _, _), var trueNext)))
                    => new(new AddFromGlobalAndConstant(instA.Concat(instB.Concat(instC)), global, globalType, globalSize, constant, constantType, constantSize), trueNext),
                (PushConstant(var instA, var constant, var constantType, var constantSize),
                (PushGlobal(var instB, var global, var globalType, var globalSize),
                (AddFromStack(var instC, _, _, _, _), var trueNext)))
                    => new(new AddFromGlobalAndConstant(instA.Concat(instB.Concat(instC)), global, globalType, globalSize, constant, constantType, constantSize), trueNext),
                _ => next
            },

            // Adding a global and constant and storing to a global, can all be done in one macro off the stack.
            (_, next) => next switch
            {
                (AddFromGlobalAndConstant(var instA, var global, var globalType, var globalSize, var constant, var constantType, var constantSize),
                (PopToGlobal(var instB, var targetGlobal, var targetType, var targetSize, _, _), var trueNext))
                    => new(new AddFromGlobalAndConstantToGlobal(instA.Concat(instB), global, globalType, globalSize, constant, constantType, constantSize, targetGlobal, targetType, targetSize), trueNext),
                _ => next
            },

            // Adding 1 to a global, and storing it in the same global, can be done as a single increment macro.
            (_, next) => next switch
            {
                (AddFromGlobalAndConstantToGlobal(var inst, var sourceGlobal, var globalType, var globalSize, var constant, _, _, var targetGlobal, _, _), var trueNext)
                    when sourceGlobal == targetGlobal && constant.Value is byte b && b == 1
                    => new(new IncrementGlobal(inst, targetGlobal, globalType, globalSize), trueNext),
                _ => next
            },

            // Remove unconditional jumps to the very next instruction.
            (_, next) => next switch
            {
                // This primarily happens when inlining methods that have a single exit point. The 'ret' gets replaced with an
                // unconditional jump to the end of the method, which the 'ret' is already at, so it's completely useless.
                // This removes that unconditonal jump but leaves the label (in case there's other instructions in the
                // method that branch to it).
                (Branch(_, var targetLabel), (InstructionLabel instructionLabel, var trueNext)) when targetLabel == instructionLabel
                    => new(instructionLabel, trueNext),
                _ => next
            },
        }.ToImmutableArray();

        private static MethodDef GetGeneratorMethod(FieldDefinition field)
        {
            // @TODO - Add check that RomData<> type arg is public, or else `dynamic` will fail.
            if (field.TryGetFrameworkAttribute<RomDataGeneratorAttribute>(out var attribute))
            {
                var methodName = attribute.MethodName;
                var expectedArg = ((GenericInstanceType)field.FieldType).GenericArguments.Single();
                var matchingMethods = field.DeclaringType.Methods.Concat(field.DeclaringType.DeclaringType?.Methods ?? Enumerable.Empty<MethodDefinition>())
                    .Where(m => m.IsStatic)
                    .Where(m => m.Name == methodName)
                    .Where(m => !m.Parameters.Any())
                    .Where(m => m.ReturnType is GenericInstanceType)
                    .Where(m => ((GenericInstanceType)m.ReturnType).GenericArguments.SingleOrDefault()?.FullName == expectedArg.FullName)
                    .Where(m => ((GenericInstanceType)m.ReturnType).ElementType.FullName == BuiltInDefinitions.IEnumerable.FullName)
                    .ToImmutableArray();
                var expectedMethod = $"static IEnumerable<{expectedArg.Name}> {methodName}() {{ /** ... */ }}";
                // ReturnType.GenericArguments should equal field's argument
                // ReturnType.ElementType == IEnumerable`1
                return matchingMethods.Length switch
                {
                    0 => throw new InvalidOperationException($"No matching RomData generator method found for field '{field.FullName}'. Expected a method on type '{field.DeclaringType.FullName}' that matches: '{expectedMethod}'"),
                    1 => matchingMethods.Single(),
                    _ => throw new InvalidOperationException($"Multiple RomData generators found for field '{field.FullName}'. There should only be 1 match. Matches:{Environment.NewLine}{string.Join(Environment.NewLine, matchingMethods.Select(m => m.FullName))}")
                };
            }
            else
            {
                throw new InvalidOperationException($"Field '{field.FullName}' must be tagged with a {nameof(RomDataGeneratorAttribute)} in order to be used as a RomData.");
            }
        }

        private static ImmutableArray<T> ArrayOf<T>(params T[] values) => values.ToImmutableArray();

        private static ITypeLabel GetRomDataArgType(FieldReference field)
            => new TypeLabel(((GenericInstanceType)field.FieldType).GenericArguments.Single());

        private static ISizeLabel GetRomDataArgSize(FieldReference field)
            => new TypeSizeLabel(((GenericInstanceType)field.FieldType).GenericArguments.Single());
    }
}
