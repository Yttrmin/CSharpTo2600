using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace CSharpTo2600.Compiler
{
    public sealed class CompilationInfo
    {
        private readonly ImmutableDictionary<INamedTypeSymbol, ProcessedType> Types;
        private readonly SemanticModel Model;

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

        public CompilationInfo(SemanticModel Model)
        {
            Types = ImmutableDictionary<INamedTypeSymbol, ProcessedType>.Empty;
            this.Model = Model;
        }

        private CompilationInfo(CompilationInfo OldInfo, INamedTypeSymbol Symbol, ProcessedType Type)
        {
            // Either adding a key/value that didn't exist before or overwriting an existing value.
            Types = OldInfo.Types.SetItem(Symbol, Type);
            Model = OldInfo.Model;
        }

        private CompilationInfo(CompilationInfo OldInfo, ProcessedType NewType)
        {
            throw new NotImplementedException();
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
