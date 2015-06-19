using System;
using System.Runtime.InteropServices;
using System.Reflection;
using CSharpTo2600.Framework.Assembly;
using Microsoft.CodeAnalysis;

namespace CSharpTo2600.Compiler
{
    public interface IVariableInfo
    {
        string Name { get; }
        ProcessedType Type { get; }
        int Size { get; }
        Symbol AssemblySymbol { get; }
        bool IsDirectlyAddressable { get; }
        bool IsStackRelative { get; }
    }

    internal abstract class VariableInfo : IVariableInfo
    {
        private readonly ISymbol CompilerSymbol;
        public Symbol AssemblySymbol { get; }
        public ProcessedType Type { get; }
        public string Name { get { return CompilerSymbol.Name; } }
        public int Size { get { return Type.InstanceSize; } }
        public abstract bool IsDirectlyAddressable { get; }
        public abstract bool IsStackRelative { get; }

        private VariableInfo(ISymbol CompilerSymbol, Symbol AssemblySymbol, ProcessedType Type)
        {
            this.CompilerSymbol = CompilerSymbol;
            this.AssemblySymbol = AssemblySymbol;
            this.Type = Type;
        }

        public static IVariableInfo CreateDirectlyAddressableVariable(ISymbol Symbol, ProcessedType Type, int StartAddress)
        {
            return new DirectlyAddressableVariable(Symbol, Type, new Range(StartAddress, StartAddress + Type.InstanceSize));
        }

        public static IVariableInfo CreateDirectlyAddressableCustomVariable(string Name, ProcessedType Type, int StartAddress)
        {
            return new DirectlyAddressableCustomVariable(Name, Type, StartAddress);
        }

        public static IVariableInfo CreateStackVariable(ISymbol Symbol)
        {
            throw new NotImplementedException();
        }

        public static IVariableInfo CreateRegisterVariable(Symbol AssemblySymbol, CompilationState State)
        {
            var SymbolField = typeof(ReservedSymbols).GetTypeInfo().GetDeclaredField(AssemblySymbol.Name);
            if (SymbolField == null)
            {
                throw new FatalCompilationException($"Symbol must refer to a TIA/RIOT register: {AssemblySymbol}");
            }
            var ByteType = State.GetTypeFromName("Byte");
            return new RegisterVariable(AssemblySymbol, ByteType);
        }

        public static IVariableInfo CreatePlaceholderVariable(ISymbol Symbol, ProcessedType Type)
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

            public DirectlyAddressableVariable(ISymbol CompilerSymbol, ProcessedType Type, Range Address)
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

            public StackRelativeVariable(ISymbol CompilerSymbol, ProcessedType Type, Range Offset)
                : base(CompilerSymbol, AssemblyFactory.DefineSymbol($".{CompilerSymbol.Name}", Offset.Start), Type)
            {

            }
        }

        /// <summary>
        /// Represents a variable that directly maps to a TIA/RIOT register.
        /// These variables consume no RAM.
        /// </summary>
        private sealed class RegisterVariable : IVariableInfo
        {
            public string Name { get { return AssemblySymbol.Name; } }
            public ProcessedType Type { get; }
            public int Size { get { return sizeof(byte); } }
            public Symbol AssemblySymbol { get; }
            public bool IsDirectlyAddressable { get { return true; } }
            public bool IsStackRelative { get { return false; } }

            public RegisterVariable(Symbol AssemblySymbol, ProcessedType ByteType)
            {
                this.AssemblySymbol = AssemblySymbol;
                Type = ByteType;
            }
        }

        private sealed class DirectlyAddressableCustomVariable : IVariableInfo
        {
            public Symbol AssemblySymbol { get; }
            public bool IsDirectlyAddressable { get { return true; } }
            public bool IsStackRelative { get { return false; } }
            public string Name { get; }
            public int Size { get { return Type.InstanceSize; } }
            public ProcessedType Type { get; }

            public DirectlyAddressableCustomVariable(string Name, ProcessedType Type, int StartAddress)
            {
                AssemblySymbol = AssemblyFactory.DefineSymbol(Name, StartAddress);
                this.Name = Name;
                this.Type = Type;
            }
        }

        /// <summary>
        /// Represents a variable that has not yet had its storage location type decided.
        /// Used as a placeholder during the initial parsing of types until the MemoryManager
        /// decides where each variable goes.
        /// </summary>
        private sealed class UnknownVariable : VariableInfo
        {
            public override bool IsDirectlyAddressable { get { return false; } }
            public override bool IsStackRelative { get { return false; } }

            public UnknownVariable(ISymbol CompilerSymbol, ProcessedType Type)
                : base(CompilerSymbol, null, Type)
            {

            }
        }
    }
}
