using System;
using System.Runtime.InteropServices;

namespace CSharpTo2600.Compiler
{
    internal abstract class VariableInfo
    {
        public readonly string Name;
        public readonly Type Type;
        public readonly Range Address;
        public int Size { get { return Marshal.SizeOf(Type); } }
        public abstract bool AddressIsAbsolute { get; }
        public abstract bool AddressIsFrameRelative { get; }

        public VariableInfo(string Name, Type Type, Range Address)
        {
            this.Name = Name;
            this.Type = Type;
            this.Address = Address;
        }

        public bool ConflictsWith(VariableInfo Other)
        {
            if (this.Name == Other.Name)
            {
                return true;
            }
            if (this.Address.Overlaps(Other.Address))
            {
                return true;
            }
            return false;
        }

        public override string ToString()
        {
            return $"{Type} {Name} ({Address})";
        }
    }

    internal class LocalVariable : VariableInfo
    {
        public override bool AddressIsAbsolute { get { return false; } }
        public override bool AddressIsFrameRelative { get { return true; } }

        public LocalVariable(string Name, Type Type, Range Address) 
            : base(Name, Type, Address)
        {
            
        }
    }

    internal class GlobalVariable : VariableInfo
    {
        public readonly bool EmitToFile;
        public override bool AddressIsAbsolute { get { return true; } }
        public override bool AddressIsFrameRelative { get { return false; } }

        public GlobalVariable(string Name, Type Type, Range Address, bool EmitToFile)
            : base(Name, Type, Address)
        {
            this.EmitToFile = EmitToFile;
        }
    }
}
