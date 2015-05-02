using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Linq;
using CSharpTo2600.Framework;
using Microsoft.CodeAnalysis;

namespace CSharpTo2600.Compiler
{
    public sealed class CompilationInfo
    {
        private readonly ImmutableDictionary<INamedTypeSymbol, ProcessedType> Types;
        
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

        public CompilationInfo()
        {
            Types = ImmutableDictionary<INamedTypeSymbol, ProcessedType>.Empty;
        }

        private CompilationInfo(CompilationInfo OldInfo, INamedTypeSymbol Symbol, ProcessedType Type)
        {
            // Either adding a key/value that didn't exist before or overwriting an existing value.
            Types = OldInfo.Types.SetItem(Symbol, Type);
        }

        private CompilationInfo(CompilationInfo OldInfo, ProcessedType NewType)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public IVariableInfo GetVariableFromField(IFieldSymbol FieldSymbol)
        {
            var Type = Types[FieldSymbol.ContainingType];
            return Type.Globals[FieldSymbol];
        }

        public CompilationInfo WithType(ProcessedType Type)
        {
            return new CompilationInfo(this, Type.Symbol, Type);
        }

        public CompilationInfo WithReplacedType(ProcessedType Type)
        {
            if (!Types.ContainsKey(Type.Symbol))
            {
                throw new ArgumentException($"Type was not previously parsed: {Type}", nameof(Type));
            }
            return new CompilationInfo(this, Type.Symbol, Type);
        }
    }
}
