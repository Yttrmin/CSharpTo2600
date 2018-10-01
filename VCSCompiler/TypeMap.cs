using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

namespace VCSCompiler
{
    internal class TypeMap
    {
        private readonly IDictionary<string, ProcessedType> Types;
        
        protected TypeMap(IDictionary<string, ProcessedType> source)
        {
            Types = source.ToImmutableDictionary();
        }

        public TypeMap()
        {
            Types = new Dictionary<string, ProcessedType>();
            var system = AssemblyDefinition.ReadAssembly(typeof(object).GetTypeInfo().Assembly.Location);
            var supportedTypes = new[] { "Object", "ValueType", "Void", "Byte", "Boolean" };
            var types = system.Modules[0].Types.Where(td => supportedTypes.Contains(td.Name)).ToImmutableArray();

            var objectType = types.Single(x => x.Name == "Object");
            var objectCompiled = new CompiledType(new ProcessedType(objectType, null, Enumerable.Empty<ProcessedField>(), ImmutableDictionary<ProcessedField, byte>.Empty, ImmutableList<ProcessedSubroutine>.Empty, 0), ImmutableList<CompiledSubroutine>.Empty);
            this[objectType] = objectCompiled;

            var valueType = types.Single(x => x.Name == "ValueType");
            var valueTypeCompiled = new CompiledType(new ProcessedType(valueType, objectCompiled, Enumerable.Empty<ProcessedField>(), ImmutableDictionary<ProcessedField, byte>.Empty, ImmutableList<ProcessedSubroutine>.Empty, 0), ImmutableList<CompiledSubroutine>.Empty);
            this[valueType] = valueTypeCompiled;

            var voidType = types.Single(x => x.Name == "Void");
            var voidCompiled = new CompiledType(new ProcessedType(voidType, valueTypeCompiled, Enumerable.Empty<ProcessedField>(), ImmutableDictionary<ProcessedField, byte>.Empty, ImmutableList<ProcessedSubroutine>.Empty, 0), ImmutableList<CompiledSubroutine>.Empty);
            this[voidType] = voidCompiled;

            var byteType = types.Single(x => x.Name == "Byte");
            var byteCompiled = new CompiledType(new ProcessedType(byteType, valueTypeCompiled, Enumerable.Empty<ProcessedField>(), ImmutableDictionary<ProcessedField, byte>.Empty, ImmutableList<ProcessedSubroutine>.Empty, 1), ImmutableList<CompiledSubroutine>.Empty);
            this[byteType] = byteCompiled;

            var boolType = types.Single(x => x.Name == "Boolean");
            var boolCompiled = new CompiledType(new ProcessedType(boolType, valueTypeCompiled, Enumerable.Empty<ProcessedField>(), ImmutableDictionary<ProcessedField, byte>.Empty, ImmutableList<ProcessedSubroutine>.Empty, 1), ImmutableList<CompiledSubroutine>.Empty);
            this[boolType] = boolCompiled;
        }

        public ProcessedType this[ProcessedType type]
        {
            get => Types[type.FullName];
            set => SetValue(type.FullName, value);
        }

        public ProcessedType this[TypeReference type]
        {
            get => Types[type.FullName];
            set => SetValue(type.FullName, value);
        }

        public IEnumerable<ProcessedType> ProcessedTypes => Types.Values.OfType<ProcessedType>().Where(t => t.GetType() == typeof(ProcessedType));

        public bool Contains(TypeReference typeReference) => Types.ContainsKey(typeReference.FullName);

        public ImmutableTypeMap ToImmutableTypeMap()
        {
            if (this is ImmutableTypeMap immutableThis)
            {
                return immutableThis;
            }

            return new ImmutableTypeMap(Types);
        }

        public bool TryGetType(TypeReference typeReference, out ProcessedType type)
        {
            if (Types.TryGetValue(typeReference.FullName, out type))
            {
                return true;
            }

            return false;
        }

        private void SetValue(string key, ProcessedType value)
        {
            Types[key] = value;
        }
    }

    internal sealed class ImmutableTypeMap : TypeMap
    {
        public ImmutableTypeMap(IDictionary<string, ProcessedType> source)
            : base(source)
        {
        }
    }
}
