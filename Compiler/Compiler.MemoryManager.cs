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
            private readonly CompilationInfo Info;
            private int NextAddress = GlobalsStart;
            private const int RAMAmount = 128;
            private const int GlobalsStart = 0x80;
            private const int StackStart = 0xFF;
            //@TODO
            // Reserve a completely arbitrary amount of memory for globals.
            private const int GlobalUsageLimit = 100;

            private MemoryManager(CompilationInfo Info)
            {
                this.Info = Info;
            }

            public static CompilationInfo Analyze(CompilationInfo Info)
            {
                // Note types won't be compiled at this point.
                var Manager = new MemoryManager(Info);
                foreach (var NewType in Manager.LayoutGlobals())
                {
                    Info = Info.WithReplacedType(NewType);
                }
                return Info;
            }

            private IEnumerable<ProcessedType> LayoutGlobals()
            {
                var MemoryUsage = Info.AllGlobals.Sum(v => v.Size);
                if (MemoryUsage > GlobalUsageLimit)
                {
                    throw new FatalCompilationException($"Too many globals, {MemoryUsage} bytes needed, but only {GlobalUsageLimit} bytes available.");
                }

                foreach (var Type in Info.AllTypes)
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
                var NewGlobals = new Dictionary<IFieldSymbol, VariableInfo>();
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
