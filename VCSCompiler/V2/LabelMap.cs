#nullable enable
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using VCSFramework.V2;

namespace VCSCompiler.V2
{
    /*
    internal class LabelMap
    {
        public ImmutableDictionary<string, GlobalFieldLabel> AliasToGlobal { get; }
        public ImmutableDictionary<GlobalFieldLabel, string> GlobalToAddress { get; }
        public ImmutableDictionary<LocalLabel, string> LocalToAddress { get; }
        //public ImmutableDictionary<ConstantLabel, string> ConstantToValue { get; }
        public ImmutableDictionary<ITypeLabel, string> TypeToString { get; }
        public ImmutableDictionary<ISizeLabel, string> SizeToValue { get; }
        public ImmutableDictionary<TypeLabel, ISizeLabel> TypeToSize { get; }
        public ImmutableDictionary<MethodDefinition, ImmutableArray<IAssemblyEntry>> FunctionToBody { get; }

        public LabelMap(
            IEnumerable<ImmutableArray<IAssemblyEntry>> functions,
            AssemblyDefinition userAssembly)
        {
            var aliasToGlobal = new Dictionary<string, GlobalFieldLabel>();
            var globalToAddress = new Dictionary<GlobalFieldLabel, string>();
            var localToAddress = new Dictionary<LocalLabel, string>();
            //var constantToValue = new Dictionary<ConstantLabel, string>();
            var typeToString = new Dictionary<ITypeLabel, string>();
            var sizeToValue = new Dictionary<ISizeLabel, string>();
            var typeToSize = new Dictionary<TypeLabel, ISizeLabel>();
            var functionToBody = new Dictionary<MethodDefinition, ImmutableArray<IAssemblyEntry>>();

            var allLabelParams = functions
                .SelectMany(it => it)
                .OfType<IMacroCall>()
                .SelectMany(it => it.Parameters)
                .Concat(BuiltInDefinitions.Types.SelectMany(t => new ILabel[] { new TypeLabel(t), new TypeSizeLabel(t) })) // Force built-in types since there are VIL checks that rely on them.
                .Prepend(new PredefinedGlobalLabel("INTERNAL_RESERVED_0")) // @TODO - Find a better way
                .Where(it => !(it is InstructionLabel))
                .Distinct()
                .ToImmutableArray();

            var typeNumber = 100;
            // @TODO - An interface for labels with TypeReferences may be more appropriate.
            var allTypes = allLabelParams
                .OfType<TypeLabel>()
                .Select(l => l.Type)
                .Concat(
                    allLabelParams
                    .OfType<PointerTypeLabel>()
                    .Select(l => l.ReferentType)
                    .Concat(
                        allLabelParams
                        .OfType<ISizeLabel>()
                        .Select(l => l.Type)))
                .Distinct(new TypeReferenceEqualityComparer());
            foreach (var typeRef in allTypes)
            {
                // Resolving pointers returns the type they point to.
                var type = typeRef.Resolve();
                // For every type, we give it a type number, give its pointer a type number, and give it a size.
                // This is to avoid cases where we e.g. generate a pointer type, but not its non-pointer type,
                // and some function goes to fetch the non-pointer type and fails.
                var thisTypeNumber = typeNumber;
                typeNumber += 2;

                typeToString[new TypeLabel(type)] = thisTypeNumber.ToString();
                // Pointer types use the same number as the type they're pointing to, but with the LSB set to 1.
                typeToString[new PointerTypeLabel(type)] = (thisTypeNumber | 0x1).ToString();
                sizeToValue[new TypeSizeLabel(type)] = $"{TypeData.Of(type, userAssembly).Size}";
                typeToSize[new TypeLabel(type)] = new TypeSizeLabel(type);
            }

            // @TODO - 
            var aliasedFields = userAssembly.CompilableTypes().SelectMany(t => t.Fields).Where(f => f.CustomAttributes.Any(a => a.AttributeType.FullName == typeof(InlineAssemblyAliasAttribute).FullName));
            foreach (var field in aliasedFields)
            {
                if (!field.IsStatic)
                    throw new InvalidOperationException($"[{nameof(InlineAssemblyAliasAttribute)}] can only be used on static fields. '{field.FullName}' is not static.");
                var aliases = field.CustomAttributes.Where(a => a.AttributeType.FullName == typeof(InlineAssemblyAliasAttribute).FullName).Select(a => (string)a.ConstructorArguments[0].Value);
                foreach (var alias in aliases)
                {
                    if (!alias.StartsWith(AssemblyUtilities.AliasPrefix))
                        throw new InvalidOperationException($"Alias '{alias}' must begin with '{AssemblyUtilities.AliasPrefix}'");
                    if (aliasToGlobal.ContainsKey(alias))
                        throw new InvalidOperationException($"Alias '{alias}' is already being aliased to '{aliasToGlobal[alias].Field.Name}', can't alias to '{field.FullName}' too.");
                    aliasToGlobal[alias] = new(field);
                }
            }

            // @TODO - Need to reserve some for VIL.
            var ramStart = 0x80;
            foreach (var globalLabel in allLabelParams.OfType<GlobalFieldLabel>())
            {
                globalToAddress[globalLabel] = $"${ramStart:X2}";
                ramStart += TypeData.Of(globalLabel.Field.Type, userAssembly).Size;
            }
            foreach (var localLabel in allLabelParams.OfType<LocalLabel>())
            {
                localToAddress[localLabel] = $"${ramStart++:X2}";
                ramStart += TypeData.Of(localLabel.Method.Body.Variables[localLabel.Index].VariableType, userAssembly).Size;
            }

            foreach (var method in allLabelParams.OfType<MethodLabel>().Select(l => l.Method).Distinct())
            {
                var compiler = new CilInstructionCompiler(method, userAssembly);
                functionToBody[method] = MethodCompiler.Compile(method, userAssembly, false);
            }

            AliasToGlobal = aliasToGlobal.ToImmutableDictionary();
            GlobalToAddress = globalToAddress.ToImmutableDictionary();
            LocalToAddress = localToAddress.ToImmutableDictionary();
            //ConstantToValue = constantToValue.ToImmutableDictionary();
            TypeToString = typeToString.ToImmutableDictionary();
            SizeToValue = sizeToValue.ToImmutableDictionary();
            TypeToSize = typeToSize.ToImmutableDictionary();
            FunctionToBody = functionToBody.ToImmutableDictionary();
        }

        private class TypeReferenceEqualityComparer : EqualityComparer<TypeReference>
        {
            public override bool Equals(TypeReference? x, TypeReference? y) => x != null && TrimSymbols(x) == TrimSymbols(y);

            public override int GetHashCode(TypeReference obj) => TrimSymbols(obj).GetHashCode();

            [return: NotNullIfNotNull("obj")]
            private string? TrimSymbols(TypeReference? obj) => obj?.FullName?.Replace("*", "")?.Replace("&", "");
        }
    }*/
}
