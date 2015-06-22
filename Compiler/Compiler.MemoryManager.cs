using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace CSharpTo2600.Compiler
{
    public sealed partial class GameCompiler
    {
        private class MemoryManager
        {
            private readonly CompilationState State;
            private int NextAddress = GlobalsStart;
            private const int RAMAmount = 128;
            // 0x80 reserved for return value.
            // See Compiler.MethodCompiler.ReturnValue;
            private const int GlobalsStart = 0x81;
            private const int StackStart = 0xFF;
            //@TODO
            // Reserve a completely arbitrary amount of memory for globals.
            private const int GlobalUsageLimit = 100;
            private int MemoryConsumption { get { return NextAddress - GlobalsStart; } }

            private MemoryManager(CompilationState State)
            {
                this.State = State;
            }

            public static MemoryMap Analyze(CompilationState State)
            {
                // Note types won't be compiled at this point.
                var Manager = new MemoryManager(State);
                return Manager.LayoutGlobals();
            }

            private MemoryMap LayoutGlobals()
            {
                var Map = MemoryMap.Empty;
                foreach (var Type in State.AllTypes)
                {
                    if (Type.StaticFields.Any())
                    {
                        Map = Map.WithMerge(AssignGlobalsAddresses(Type));
                    }
                }
                // Do the check here so we can report how much total memory we need.
                if (MemoryConsumption > GlobalUsageLimit)
                {
                    throw new GlobalMemoryOverflowException(MemoryConsumption, GlobalUsageLimit);
                }
                return Map;
            }

            private MemoryMap AssignGlobalsAddresses(ProcessedType Type)
            {
                var NewGlobals = new Dictionary<IFieldSymbol, IVariableInfo>();
                foreach (var Symbol in Type.StaticFields)
                {
                    var FieldType = State.GetTypeFromSymbol((INamedTypeSymbol)Symbol.Type);
                    if(Symbol.IsConst)
                    {
                        continue;
                    }
                    var NewVariable = VariableInfo.CreateDirectlyAddressableVariable(Symbol,
                        FieldType, NextAddress);
                    NewGlobals.Add(Symbol, NewVariable);
                    NextAddress += NewVariable.Size;
                }
                return new MemoryMap(NewGlobals.ToImmutableDictionary());
            }
        }
    }

    public class MemoryMap
    {
        private ImmutableDictionary<IFieldSymbol, IVariableInfo> StaticFieldToVariable;
        public readonly static MemoryMap Empty = new MemoryMap(ImmutableDictionary<IFieldSymbol, IVariableInfo>.Empty);

        public IVariableInfo this[IFieldSymbol Field]
        {
            get
            {
                return StaticFieldToVariable[Field];
            }
        }

        public IEnumerable<IVariableInfo> AllStaticFields { get { return StaticFieldToVariable.Values; } }

        internal MemoryMap(ImmutableDictionary<IFieldSymbol, IVariableInfo> StaticFieldToVariable)
        {
            this.StaticFieldToVariable = StaticFieldToVariable;
        }

        internal MemoryMap WithMerge(MemoryMap Other)
        {
            var NewPairs = Other.StaticFieldToVariable.Where(pair => !this.StaticFieldToVariable.ContainsKey(pair.Key));
            var NewDictionary = new Dictionary<IFieldSymbol, IVariableInfo>(this.StaticFieldToVariable);
            foreach(var Pair in NewPairs)
            {
                NewDictionary.Add(Pair.Key, Pair.Value);
            }
            return new MemoryMap(NewDictionary.ToImmutableDictionary());
        }
    }
}
