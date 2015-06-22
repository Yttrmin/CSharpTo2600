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
        public IEnumerable<Subroutine> AllSubroutines
        {
            get
            {
                foreach (var Type in AllTypes)
                {
                    foreach (var Subroutine in Type.Subroutines.Values)
                    {
                        yield return Subroutine;
                    }
                }
            }
        }

        public CompilationState()
            : this(ImmutableDictionary<INamedTypeSymbol, ProcessedType>.Empty, MemoryMap.Empty, null, MethodCallHierarchy.Empty)
        {
        }

        private CompilationState(ImmutableDictionary<INamedTypeSymbol, ProcessedType> Types,
            MemoryMap MemoryMap, BuiltInTypes BuiltIn, MethodCallHierarchy MethodCallHierarchy)
        {
            this.Types = Types;
            this.MethodCallHierarchy = MethodCallHierarchy;
            this.BuiltIn = BuiltIn;
            this.MemoryMap = MemoryMap;
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

        public Subroutine GetSubroutineFromSymbol(IMethodSymbol MethodSymbol)
        {
            var Type = Types[MethodSymbol.ContainingType];
            return Type.Subroutines[MethodSymbol];
        }

        public IVariableInfo GetVariableFromField(IFieldSymbol FieldSymbol)
        {
            return MemoryMap[FieldSymbol];
        }

        internal CompilationState WithType(ProcessedType Type)
        {
            var NewDictionary = Types.Add(Type.Symbol, Type);
            return new CompilationState(NewDictionary, MemoryMap, BuiltIn, MethodCallHierarchy);
        }

        internal CompilationState WithBuiltInTypes(IEnumerable<Tuple<Type, ProcessedType>> BuiltInTypes)
        {
            if(BuiltIn != null)
            {
                throw new InvalidOperationException("Attempted to set state's BuiltIn more than once.");
            }
            var KeyValuePairs = BuiltInTypes.Select(tuple => new KeyValuePair<INamedTypeSymbol, ProcessedType>(tuple.Item2.Symbol, tuple.Item2));
            var NewDictionary = Types.AddRange(KeyValuePairs);
            return new CompilationState(NewDictionary, MemoryMap, new BuiltInTypes(BuiltInTypes), MethodCallHierarchy);
        }

        internal CompilationState WithReplacedType(ProcessedType Type)
        {
            if(!Types.ContainsKey(Type.Symbol))
            {
                throw new ArgumentException($"Type was not previously parsed: {Type}", nameof(Type));
            }
            var NewDictionary = Types.SetItem(Type.Symbol, Type);
            return new CompilationState(NewDictionary, MemoryMap, BuiltIn, MethodCallHierarchy);
        }

        internal CompilationState WithMethodCallHierarchy(MethodCallHierarchy NewHierarchy)
        {
            if(MethodCallHierarchy != null)
            {
                throw new InvalidOperationException("Attempted to set the state's MethodCallHierarchy more than once.");
            }
            return new CompilationState(Types, MemoryMap, BuiltIn, NewHierarchy);
        }

        internal CompilationState WithMemoryMap(MemoryMap NewMap)
        {
            if (MemoryMap != MemoryMap.Empty)
            {
                throw new InvalidOperationException("Attempted to set the state's MemoryMap more than once.");
            }
            return new CompilationState(Types, NewMap, BuiltIn, MethodCallHierarchy);
        }

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

            public ProcessedType TypeFromCLRType(Type Type)
            {
                return TypeMap[Type];
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
