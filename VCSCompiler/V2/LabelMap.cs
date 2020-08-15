﻿using Mono.Cecil;
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
        public ImmutableDictionary<TypeLabel, string> TypeToString { get; }
        public ImmutableDictionary<SizeLabel, string> SizeToValue { get; }

        public LabelMap(
            IEnumerable<ImmutableArray<AssemblyEntry>> functions,
            ImmutableArray<AssemblyDefinition> assemblies)
        {
            var globalToAddress = new Dictionary<GlobalLabel, string>();
            var localToAddress = new Dictionary<LocalLabel, string>();
            var constantToValue = new Dictionary<ConstantLabel, string>();
            var typeToString = new Dictionary<TypeLabel, string>();
            var sizeToValue = new Dictionary<SizeLabel, string>();

            var allLabelParams = functions
                .SelectMany(it => it)
                .OfType<Macro>()
                .SelectMany(it => it.Params)
                .Where(it => !(it is InstructionLabel))
                .Where(it => !(it is StackSizeLabel))
                .Where(it => !(it is StackTypeLabel))
                .Distinct()
                .ToImmutableArray();

            foreach (var typeLabel in allLabelParams.OfType<TypeLabel>())
            {
                typeToString[typeLabel] = @$"""{typeLabel.Type.NamespaceAndName()}""";
            }

            foreach (var sizeLabel in allLabelParams.OfType<SizeLabel>())
            {
                sizeToValue[sizeLabel] = $"{TypeData.Of(sizeLabel.Type, assemblies).Size}";
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

            GlobalToAddress = globalToAddress.ToImmutableDictionary();
            LocalToAddress = localToAddress.ToImmutableDictionary();
            ConstantToValue = constantToValue.ToImmutableDictionary();
            TypeToString = typeToString.ToImmutableDictionary();
            SizeToValue = sizeToValue.ToImmutableDictionary();
        }
    }
}
