using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CSharpTo2600.Framework;
using Microsoft.CodeAnalysis;

namespace CSharpTo2600.Compiler
{
    /// <summary>
    /// Immutable representation of the state of compilation.
    /// </summary>
    public sealed class CompilationState
    {
        //@TODO - Move elsewhere
        private readonly ImmutableDictionary<IMethodSymbol, Subroutine> Subroutines;
        private readonly ImmutableDictionary<INamedTypeSymbol, ProcessedType> Types;
        private readonly MemoryMap MemoryMap;
        public MethodCallHierarchy MethodCallHierarchy { get; }
        public BuiltInTypes BuiltIn { get; }

        public IEnumerable<ProcessedType> AllTypes { get { return Types.Values; } }
        public IEnumerable<IVariableInfo> AllGlobals
        {
            get
            {
                return MemoryMap.AllStaticFields;
            }
        }

        public IEnumerable<SubroutineInfo> AllSubroutineInfos
        {
            get
            {
                return Subroutines.Values.Cast<SubroutineInfo>();
            }
        }

        public IEnumerable<Subroutine> AllSubroutines
        {
            get
            {
                return Subroutines.Values;
            }
        }

        public CompilationState()
            : this(ImmutableDictionary<INamedTypeSymbol, ProcessedType>.Empty, MemoryMap.Empty, null, MethodCallHierarchy.Empty,
                  ImmutableDictionary<IMethodSymbol, Subroutine>.Empty)
        {
        }

        private CompilationState(ImmutableDictionary<INamedTypeSymbol, ProcessedType> Types,
            MemoryMap MemoryMap, BuiltInTypes BuiltIn, MethodCallHierarchy MethodCallHierarchy,
            ImmutableDictionary<IMethodSymbol, Subroutine> Subroutines)
        {
            this.Types = Types;
            this.MethodCallHierarchy = MethodCallHierarchy;
            this.BuiltIn = BuiltIn;
            this.MemoryMap = MemoryMap;
            this.Subroutines = Subroutines;
        }

        public ProcessedType GetGameClass()
        {
            //@TODO - Compare with symbol instead of string.
            var Class = AllTypes.SingleOrDefault(t => t.Symbol.GetAttributes().SingleOrDefault(a => a.AttributeClass.Name == nameof(Atari2600Game)) != null);
            if (Class == null)
            {
                throw new GameClassNotFoundException();
            }
            else if (!Class.IsStatic)
            {
                throw new GameClassNotStaticException(Class.Symbol);
            }
            else
            {
                return Class;
            }
        }

        public ProcessedType GetTypeFromSymbol(INamedTypeSymbol TypeSymbol)
        {
            return Types[TypeSymbol];
        }

        public SubroutineInfo GetSubroutineFromSymbol(IMethodSymbol MethodSymbol)
        {
            return (SubroutineInfo)Subroutines[MethodSymbol];
        }

        public Subroutine GetSubroutineInfoFromSymbol(IMethodSymbol MethodSymbol)
        {
            return Subroutines[MethodSymbol];
        }

        public IVariableInfo GetVariableFromField(IFieldSymbol FieldSymbol)
        {
            return MemoryMap[FieldSymbol];
        }

        public IEnumerable<SubroutineInfo> GetSubroutineInfosFromType(ProcessedType Type)
        {
            return GetSubroutinesFromType(Type).Cast<SubroutineInfo>();
        }

        public IEnumerable<Subroutine> GetSubroutinesFromType(ProcessedType Type)
        {
            foreach (var Symbol in Type.Subroutines)
            {
                var Value = Subroutines.GetValueOrDefault(Symbol);
                // Some subroutines are never made for builtin types. Like void's constructor.
                // So just skip them if they come up.
                if (Value != null)
                {
                    yield return Subroutines[Symbol];
                }
            }
        }

        internal CompilationState WithType(ProcessedType Type)
        {
            var NewDictionary = Types.Add(Type.Symbol, Type);
            return new CompilationState(NewDictionary, MemoryMap, BuiltIn, MethodCallHierarchy, Subroutines);
        }

        internal CompilationState WithBuiltInTypes(IEnumerable<Tuple<Type, ProcessedType>> BuiltInTypes)
        {
            if(BuiltIn != null)
            {
                throw new InvalidOperationException("Attempted to set state's BuiltIn more than once.");
            }
            var KeyValuePairs = BuiltInTypes.Select(tuple => new KeyValuePair<INamedTypeSymbol, ProcessedType>(tuple.Item2.Symbol, tuple.Item2));
            var NewDictionary = Types.AddRange(KeyValuePairs);
            return new CompilationState(NewDictionary, MemoryMap, new BuiltInTypes(BuiltInTypes), MethodCallHierarchy, Subroutines);
        }

        internal CompilationState WithMethodCallHierarchy(MethodCallHierarchy NewHierarchy)
        {
            if(MethodCallHierarchy != null)
            {
                throw new InvalidOperationException("Attempted to set the state's MethodCallHierarchy more than once.");
            }
            return new CompilationState(Types, MemoryMap, BuiltIn, NewHierarchy, Subroutines);
        }

        internal CompilationState WithSubroutines(ImmutableDictionary<IMethodSymbol, Subroutine> NewSubroutines)
        {
            if(Subroutines != ImmutableDictionary<IMethodSymbol, Subroutine>.Empty)
            {
                throw new InvalidOperationException($"Attempted to set the state's {nameof(Subroutines)} more than once.");
            }
            return new CompilationState(Types, MemoryMap, BuiltIn, MethodCallHierarchy, NewSubroutines);
        }

        internal CompilationState WithSubroutineInfos(ImmutableDictionary<IMethodSymbol, SubroutineInfo> NewSubroutineInfos)
        {
            if (this.Subroutines.Any((KeyValuePair<IMethodSymbol, Subroutine> pair) => pair.Value is SubroutineInfo))
            {
                throw new InvalidOperationException($"Attempted to set the state's {nameof(CompilationState.Subroutines)} more than once.");
            }
            ImmutableDictionary<IMethodSymbol, Subroutine> Subroutines = 
                NewSubroutineInfos.ToDictionary(pair => pair.Key, pair => (Subroutine)pair.Value).ToImmutableDictionary();
            return new CompilationState(Types, MemoryMap, BuiltIn, MethodCallHierarchy, Subroutines);
        }

        internal CompilationState WithMemoryMap(MemoryMap NewMap)
        {
            if (MemoryMap != MemoryMap.Empty)
            {
                throw new InvalidOperationException("Attempted to set the state's MemoryMap more than once.");
            }
            return new CompilationState(Types, NewMap, BuiltIn, MethodCallHierarchy, Subroutines);
        }

        /// <summary>
        /// Provides a convenient way to access the builtin ProcessedTypes.
        /// </summary>
        public class BuiltInTypes
        {
            private ImmutableDictionary<Type, ProcessedType> TypeMap;
            public ProcessedType Void { get; }
            public ProcessedType Byte { get; }

            public BuiltInTypes(IEnumerable<Tuple<Type, ProcessedType>> BuiltInTypes)
            {
                foreach(var Pair in BuiltInTypes)
                {
                    if(Pair.Item1 == typeof(void))
                    {
                        Void = Pair.Item2;
                    }
                    else if(Pair.Item1 == typeof(byte))
                    {
                        Byte = Pair.Item2;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unsupported built-in type: {Pair.Item1}.");
                    }
                }
                TypeMap = new Dictionary<Type, ProcessedType>
                {
                    [typeof(void)] = Void,
                    [typeof(byte)] = Byte
                }.ToImmutableDictionary();
            }

            public Type CLRTypeFromType(ProcessedType Type)
            {
                return TypeMap.Single(pair => pair.Value == Type).Key;
            }

            public bool IsBuiltIn(ProcessedType Type)
            {
                return TypeMap.Any(pair => pair.Value == Type);
            }
        }
    }
}
