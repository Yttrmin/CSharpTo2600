using System;
using System.Runtime.InteropServices;
using System.Reflection;
using CSharpTo2600.Framework.Assembly;
using Microsoft.CodeAnalysis;

namespace CSharpTo2600.Compiler
{
    internal interface IVariableInfo
    {
        string Name { get; }
        Type Type { get; }
        int Size { get; }
        Symbol AssemblySymbol { get; }
        bool IsDirectlyAddressable { get; }
        bool IsStackRelative { get; }
    }

    internal abstract class VariableInfo : IVariableInfo
    {
        private readonly ISymbol CompilerSymbol;
        public Symbol AssemblySymbol { get; }
        public Type Type { get; }
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

        public static IVariableInfo CreateDirectlyAddressableVariable(ISymbol Symbol, Type Type, int StartAddress)
        {
            return new DirectlyAddressableVariable(Symbol, Type, new Range(StartAddress, StartAddress + Marshal.SizeOf(Type)));
        }

        public static IVariableInfo CreateStackVariable(ISymbol Symbol)
        {
            throw new NotImplementedException();
        }

        public static IVariableInfo CreateRegisterVariable(Symbol AssemblySymbol)
        {
            var a = typeof(ReservedSymbols).GetTypeInfo().GetDeclaredField(AssemblySymbol.Name);
            if (a == null)
            {
                throw new FatalCompilationException($"Symbol must refer to a TIA/RIOT register: {AssemblySymbol}");
            }
            return new RegisterVariable(AssemblySymbol);
        }

        public static IVariableInfo CreatePlaceholderVariable(ISymbol Symbol, Type Type)
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

        private sealed class RegisterVariable : IVariableInfo
        {
            public string Name { get { return AssemblySymbol.Name; } }
            public Type Type { get { return typeof(byte); } }
            public int Size { get { return sizeof(byte); } }
            public Symbol AssemblySymbol { get; }
            public bool IsDirectlyAddressable { get { return true; } }
            public bool IsStackRelative { get { return false; } }

            public RegisterVariable(Symbol AssemblySymbol)
            {
                this.AssemblySymbol = AssemblySymbol;
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
