using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace CSharpTo2600.Compiler
{
    internal sealed class CompilationInfo
    {
        private readonly ImmutableDictionary<INamedTypeSymbol, ProcessedType> Types;

        public CompilationInfo()
        {
            Types = ImmutableDictionary<INamedTypeSymbol, ProcessedType>.Empty;
        }

        private CompilationInfo(CompilationInfo OldInfo, INamedTypeSymbol Symbol, ProcessedType Type)
        {
            Types = OldInfo.Types.Add(Symbol, Type);
        }

        private CompilationInfo(CompilationInfo OldInfo, ProcessedType NewType)
        {
            throw new NotImplementedException();
        }

        public ProcessedType GetTypeFromSymbol(INamedTypeSymbol TypeSymbol)
        {
            throw new NotImplementedException();
        }

        public Subroutine GetSubroutineFromSymbol(IMethodSymbol MethodSymbol)
        {
            throw new NotImplementedException();
        }

        public CompilationInfo WithCompiledType(ProcessedType Type)
        {
            return new CompilationInfo(this, Type.Symbol, Type);
        }
    }
}
