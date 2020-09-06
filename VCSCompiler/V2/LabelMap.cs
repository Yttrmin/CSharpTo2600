using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using VCSFramework.V2;

namespace VCSCompiler.V2
{
    internal class LabelMap
    {
        public ImmutableDictionary<GlobalLabel, string> GlobalToAddress { get; }
        public ImmutableDictionary<LocalLabel, string> LocalToAddress { get; }
        public ImmutableDictionary<ConstantLabel, string> ConstantToValue { get; }
        public ImmutableDictionary<BaseTypeLabel, string> TypeToString { get; }
        public ImmutableDictionary<SizeLabel, string> SizeToValue { get; }
        public ImmutableDictionary<TypeLabel, SizeLabel> TypeToSize { get; }
        public ImmutableDictionary<PointerTypeLabel, TypeLabel> PointerToType { get; }
        public ImmutableDictionary<MethodDefinition, ImmutableArray<AssemblyEntry>> FunctionToBody { get; }

        public LabelMap(
            IEnumerable<ImmutableArray<AssemblyEntry>> functions,
            AssemblyDefinition userAssembly)
        {
            var globalToAddress = new Dictionary<GlobalLabel, string>();
            var localToAddress = new Dictionary<LocalLabel, string>();
            var constantToValue = new Dictionary<ConstantLabel, string>();
            var typeToString = new Dictionary<BaseTypeLabel, string>();
            var sizeToValue = new Dictionary<SizeLabel, string>();
            var typeToSize = new Dictionary<TypeLabel, SizeLabel>();
            var pointerToType = new Dictionary<PointerTypeLabel, TypeLabel>();
            var functionToBody = new Dictionary<MethodDefinition, ImmutableArray<AssemblyEntry>>();

            var allLabelParams = functions
                .SelectMany(it => it)
                .OfType<Macro>()
                .SelectMany(it => it.Params)
                .Concat(BuiltInDefinitions.Types.SelectMany(t => new Label[] { new TypeLabel(t), new SizeLabel(t) })) // Force built-in types since there are VIL checks that rely on them.
                .Prepend(new GlobalLabel("INTERNAL_RESERVED_0")) // @TODO - Find a better way
                .Where(it => !(it is InstructionLabel))
                .Where(it => !(it is StackSizeArrayLabel))
                .Where(it => !(it is StackTypeArrayLabel))
                .Distinct()
                .ToImmutableArray();

            var typeNumber = 100;
            foreach (var foo in allLabelParams.OfType<TypeLabel>().Select(l => l.Type).Concat(allLabelParams.OfType<PointerTypeLabel>().Select(l => l.Type).Concat(allLabelParams.OfType<SizeLabel>().Select(l => l.Type))))
            {
                var thisTypeNumber = typeNumber;
                typeNumber += 2;

                typeToString[new TypeLabel(foo)] = thisTypeNumber.ToString();
                typeToString[new PointerTypeLabel(foo)] = (thisTypeNumber | 0x1).ToString();
                sizeToValue[new SizeLabel(foo)] = $"{TypeData.Of(foo, userAssembly).Size}";
                typeToSize[new TypeLabel(foo)] = new SizeLabel(foo);
            }
            /*foreach (var typeLabel in allLabelParams.OfType<BaseTypeLabel>())
            {
                TypeReference type;
                if (typeLabel is PointerTypeLabel pointerType)
                    type = pointerType.Type;
                else
                    type = ((TypeLabel)typeLabel).Type;
                typeToString[new TypeLabel(type)] = typeNumber++.ToString();
                typeToString[new PointerTypeLabel(type)] = typeNumber++.ToString();
                sizeToValue[new SizeLabel(type)] = $"{TypeData.Of(type, userAssembly).Size}";
                typeToSize[new TypeLabel(type)] = new SizeLabel(type);
            }

            foreach (var sizeLabel in allLabelParams.OfType<SizeLabel>())
            {
                sizeToValue[sizeLabel] = $"{TypeData.Of(sizeLabel.Type, userAssembly).Size}";
            }*/

            foreach (var constantLabel in allLabelParams.OfType<ConstantLabel>())
            {
                constantToValue[constantLabel] = constantLabel.Value.ToString();
            }

            // @TODO - Need to reserve some for VIL.
            var ramStart = 0x80;
            foreach (var globalLabel in allLabelParams.OfType<GlobalLabel>().Where(l => !l.Predefined))
            {
                globalToAddress[globalLabel] = $"${ramStart++:X2}";
            }
            foreach (var localLabel in allLabelParams.OfType<LocalLabel>())
            {
                localToAddress[localLabel] = $"${ramStart++:X2}";
            }

            foreach (var pointerType in allLabelParams.OfType<PointerTypeLabel>())
            {
                pointerToType[pointerType] = new TypeLabel(pointerType.Type);
            }

            foreach (var method in allLabelParams.OfType<MethodLabel>().Select(l => l.Method).Distinct())
            {
                var compiler = new CilInstructionCompiler(method, userAssembly);
                functionToBody[method] = MethodCompiler.Compile(method, userAssembly, false);
            }

            GlobalToAddress = globalToAddress.ToImmutableDictionary();
            LocalToAddress = localToAddress.ToImmutableDictionary();
            ConstantToValue = constantToValue.ToImmutableDictionary();
            TypeToString = typeToString.ToImmutableDictionary();
            SizeToValue = sizeToValue.ToImmutableDictionary();
            TypeToSize = typeToSize.ToImmutableDictionary();
            PointerToType = pointerToType.ToImmutableDictionary();
            FunctionToBody = functionToBody.ToImmutableDictionary();
        }
    }
}
