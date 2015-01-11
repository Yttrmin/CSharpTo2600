using CSharpTo2600.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CSharpTo2600.Compiler
{
    partial class Compiler
    {
        [Obsolete("Use MethodCompiler", true)]
        private class SubroutineBuilder
        {
            private readonly List<InstructionInfo> Instructions;
            private readonly Compiler Compiler;
            private SemanticModel Model { get { return Compiler.Model; } }
            private readonly string Name;
            private readonly MethodType Type;

            public SubroutineBuilder(Compiler Compiler, string Name, MethodType Type)
            {
                if (Type == MethodType.None)
                {
                    throw new ArgumentException("MethodType can't be None.");
                }
                this.Compiler = Compiler;
                this.Name = Name;
                this.Type = Type;
                Instructions = new List<InstructionInfo>();
            }

            public Subroutine ToSubroutine()
            {
                return new Subroutine(Name, Instructions.ToImmutableArray(), Type);
            }

            public void Append(InstructionInfo Instruction)
            {
                Instructions.Add(Instruction);
            }

            public void Append(AssignmentExpressionSyntax e)
            {
                if(!e.IsKind(SyntaxKind.SimpleAssignmentExpression))
                {
                    throw new FatalCompilationException("Only simple assignments supported.", e);
                }

                // First, get info on the left side of the expression. What variable we storing to?
                //@TODO - Property setters are also a possibility here.
                var VarInfo = Model.GetSymbolInfo(e.Left).Symbol;
                var VarType = e.Left.ToType(Model);

                if (VarInfo.Kind == SymbolKind.Local)
                {
                    throw new FatalCompilationException("Locals not supported yet.", e);
                }
                else if (VarInfo.Kind == SymbolKind.Field)
                {
                    var Global = Compiler.ROMBuilder.GetGlobal(VarInfo.Name);
                }
                else if(VarInfo.Kind == SymbolKind.Property)
                {
                    throw new FatalCompilationException("Property assignment not supported yet.", e);
                }
                else
                {
                    throw new FatalCompilationException("Unknown variable kind.", e);
                }

                // Right side. Get the value to assign. Could be literal, field, getter, method.
                // Start with the easiest, literals.
                var RightSideLiteral = Compiler.Model.GetConstantValue(e.Right);
                if(RightSideLiteral.HasValue)
                {
                    Instructions.AddRange(Fragments.LoadIntoVariable(VarInfo.Name, RightSideLiteral.Value, VarType));
                }
                else
                {
                    throw new FatalCompilationException("Only literals supported in assignment", e);
                }
            }

            public void Append(InvocationExpressionSyntax e)
            {
                throw new NotImplementedException();
            }

            public void Append(PostfixUnaryExpressionSyntax e)
            {
                throw new NotImplementedException();
            }
        }
    }

    internal class Subroutine
    {
        public readonly string Name;
        public readonly ImmutableArray<InstructionInfo> Instructions;
        public readonly MethodType Type;

        public Subroutine(string Name, ImmutableArray<InstructionInfo> Instructions, MethodType Type)
        {
            this.Name = Name;
            this.Instructions = Instructions;
            this.Type = Type;
        }
    }
}
