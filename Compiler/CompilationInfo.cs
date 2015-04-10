using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpTo2600.Compiler
{
    internal sealed class CompilationInfo
    {
        public readonly ImmutableDictionary<INamedTypeSymbol, ProcessedType> Types;
        private readonly SemanticModel Model;

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
            throw new NotImplementedException();
        }

        public Subroutine GetSubroutineFromSymbol(IMethodSymbol MethodSymbol)
        {
            throw new NotImplementedException();
        }

        public VariableInfo GetVariableFromFieldAccess(MemberAccessExpressionSyntax Node)
        {
            var TypeSymbol = (INamedTypeSymbol)Model.GetSymbolInfo(Node.Expression).Symbol;
            //@TODO - What if type isn't compiled/parsed yet?
            var Type = Types[TypeSymbol];
            var FieldSymbol = (IFieldSymbol)Model.GetSymbolInfo(Node.Name).Symbol;
            var VarInfo = Type.Globals[FieldSymbol];
            return VarInfo;
        }

        public CompilationInfo WithParsedType(ProcessedType Type)
        {
            return new CompilationInfo(this, Type.Symbol, Type);
        }

        public CompilationInfo WithCompiledType(ProcessedType Type)
        {
            if (!Types.ContainsKey(Type.Symbol))
            {
                throw new ArgumentException($"Type was not previously parsed: {Type}", nameof(Type));
            }
            return new CompilationInfo(this, Type.Symbol, Type);
        }
    }
}
