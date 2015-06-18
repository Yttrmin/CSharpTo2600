using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
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
        public MethodCallHierarchy MethodCallHierarchy { get; }

        public IEnumerable<ProcessedType> AllTypes { get { return Types.Values; } }
        public IEnumerable<IVariableInfo> AllGlobals
        {
            get
            {
                foreach (var Type in AllTypes)
                {
                    foreach (var Global in Type.Globals.Values)
                    {
                        yield return Global;
                    }
                }
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
            : this(ImmutableDictionary<INamedTypeSymbol, ProcessedType>.Empty, MethodCallHierarchy.Empty)
        {
        }

        private CompilationState(ImmutableDictionary<INamedTypeSymbol, ProcessedType> Types,
            MethodCallHierarchy MethodCallHierarchy)
        {
            this.Types = Types;
            this.MethodCallHierarchy = MethodCallHierarchy;
        }

        public ProcessedType GetGameClass()
        {
            var Class = AllTypes.SingleOrDefault(t => t.CLRType.GetTypeInfo().GetCustomAttribute<Atari2600Game>() != null);
            if (Class == null)
            {
                throw new GameClassNotFoundException();
            }
            else if (!Class.IsStatic)
            {
                throw new GameClassNotStaticException(Class.CLRType);
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
            var Type = Types[FieldSymbol.ContainingType];
            return Type.Globals[FieldSymbol];
        }

        internal CompilationState WithType(ProcessedType Type)
        {
            var NewDictionary = Types.Add(Type.Symbol, Type);
            return new CompilationState(NewDictionary, MethodCallHierarchy);
        }

        internal CompilationState WithReplacedType(ProcessedType Type)
        {
            if(!Types.ContainsKey(Type.Symbol))
            {
                throw new ArgumentException($"Type was not previously parsed: {Type}", nameof(Type));
            }
            var NewDictionary = Types.SetItem(Type.Symbol, Type);
            return new CompilationState(NewDictionary, MethodCallHierarchy);
        }

        internal CompilationState WithMethodCallHierarchy(MethodCallHierarchy NewHierarchy)
        {
            if(MethodCallHierarchy != null)
            {
                throw new InvalidOperationException("Attempted to set the state's MethodCallHierarchy more than once.");
            }
            return new CompilationState(Types, NewHierarchy);
        }
    }
}
