using System;
using System.Runtime.InteropServices;
using CSharpTo2600.Framework.Assembly;
using Microsoft.CodeAnalysis;

namespace CSharpTo2600.Compiler
{
    internal abstract class VariableInfo
    {
        private readonly ISymbol CompilerSymbol;
        public readonly Symbol AssemblySymbol;
        public readonly Type Type;
        public string Name { get { return CompilerSymbol.Name; } }
        public int Size { get { return Marshal.SizeOf(Type); } }
        public abstract bool IsDirectlyAddressable { get; }
        public abstract bool IsStackRelative { get; }

        private VariableInfo(ISymbol CompilerSymbol, Symbol AssemblySymbol, Type Type)
        {
            this.CompilerSymbol = CompilerSymbol;
            this.AssemblySymbol = AssemblySymbol;
            this.Type = Type;
        }

        public static VariableInfo CreateDirectlyAddressableVariable(ISymbol Symbol, Type Type, int StartAddress)
        {
            return new DirectlyAddressableVariable(Symbol, Type, new Range(StartAddress, StartAddress + Marshal.SizeOf(Type)));
        }

        public static VariableInfo CreateStackVariable(ISymbol Symbol)
        {
            throw new NotImplementedException();
        }

        public static VariableInfo CreatePlaceholderVariable(ISymbol Symbol, Type Type)
        {
            return new UnknownVariable(Symbol, Type);
        }

        /// <summary>
        /// Represents a variable that exists at a known, constant address.
        /// No assumptions are made about its lifetime or uniqueness of its address.
        /// </summary>
        private sealed class DirectlyAddressableVariable : VariableInfo
        {
            private readonly Range Address;
            public override bool IsDirectlyAddressable { get { return true; } }
            public override bool IsStackRelative { get { return false; } }

            public DirectlyAddressableVariable(ISymbol CompilerSymbol, Type Type, Range Address)
                : base(CompilerSymbol, AssemblyFactory.DefineSymbol(CompilerSymbol.Name, Address.Start), Type)
            {
                this.Address = Address;
            }
        }

        /// <summary>
        /// Represents a variable that exists at a constant offset from the stack frame.
        /// </summary>
        private sealed class StackRelativeVariable : VariableInfo
        {
            public override bool IsDirectlyAddressable { get { return false; } }
            public override bool IsStackRelative { get { return true; } }

            public StackRelativeVariable(ISymbol CompilerSymbol, Type Type, Range Offset)
                : base(CompilerSymbol, AssemblyFactory.DefineSymbol($".{CompilerSymbol.Name}", Offset.Start), Type)
            {

            }
        }

        private sealed class UnknownVariable : VariableInfo
        {
            public override bool IsDirectlyAddressable { get { return false; } }
            public override bool IsStackRelative { get { return false; } }

            public UnknownVariable(ISymbol CompilerSymbol, Type Type)
                : base(CompilerSymbol, null, Type)
            {

            }
        }
    }
}
