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

            private MemoryManager(CompilationState State)
            {
                this.State = State;
            }

            public static CompilationState Analyze(CompilationState State)
            {
                // Note types won't be compiled at this point.
                var Manager = new MemoryManager(State);
                foreach (var NewType in Manager.LayoutGlobals())
                {
                    State = State.WithReplacedType(NewType);
                }
                return State;
            }

            private IEnumerable<ProcessedType> LayoutGlobals()
            {
                var MemoryUsage = State.AllGlobals.Sum(v => v.Size);
                if (MemoryUsage > GlobalUsageLimit)
                {
                    throw new GlobalMemoryOverflowException(MemoryUsage, GlobalUsageLimit);
                }

                foreach (var Type in State.AllTypes)
                {
                    if (Type.Globals.Any())
                    {
                        yield return AssignGlobalsAddresses(Type);
                    }
                    else
                    {
                        yield return Type;
                    }
                }
            }

            private ProcessedType AssignGlobalsAddresses(ProcessedType Type)
            {
                var NewGlobals = new Dictionary<IFieldSymbol, IVariableInfo>();
                foreach (var Global in Type.Globals)
                {
                    var Symbol = Global.Key;
                    var NewVariable = VariableInfo.CreateDirectlyAddressableVariable(Global.Key,
                        Global.Value.Type, NextAddress);
                    NewGlobals.Add(Symbol, NewVariable);
                    NextAddress += NewVariable.Size;
                }
                return new ProcessedType(Type, Globals: NewGlobals.ToImmutableDictionary());
            }
        }
    }
}
