using System;
using Microsoft.CodeAnalysis;

namespace CSharpTo2600.Compiler
{
    internal sealed class CompilationInfo
    {
        public CompiledType GetTypeFromSymbol(INamedTypeSymbol TypeSymbol)
        {
            throw new NotImplementedException();
        }

        public Subroutine GetSubroutineFromSymbol(IMethodSymbol MethodSymbol)
        {
            throw new NotImplementedException();
        }
    }
}
