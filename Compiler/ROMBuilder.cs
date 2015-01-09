using CSharpTo2600.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace CSharpTo2600.Compiler
{
    internal class ROMBuilder
    {
        private static readonly Range RAMRange = new Range(0x80, 0xFF);
        private readonly List<GlobalInfo> Globals;
        private readonly List<Subroutine> Subroutines;

        public ROMBuilder()
        {
            Subroutines = new List<Subroutine>();
            Globals = new List<GlobalInfo>();
        }

        public void AddSubroutine(Subroutine Subroutine)
        {
            Subroutines.Add(Subroutine);
        }

        public void WriteToFile(string Path)
        {
            using (var Writer = new StreamWriter(Path))
            {
                WriteHead(Writer);
                WriteGlobals(Writer);
                WriteInitializeSystem(Writer);
                var InitializeMethod = Subroutines.Where(s => s.Type == MethodType.Initialize).SingleOrDefault();
                if(InitializeMethod != null)
                {
                    WriteSubroutine(Writer, InitializeMethod);
                }
            }
        }

        public void AddGlobalVariable(Type Type, string Name)
        {
            if(!Type.IsValueType)
            {
                throw new ArgumentException("No reference types are supported yet.");
            }
            if(Globals.Exists(g => g.Name == Name))
            {
                throw new ArgumentException("Variable of name \"\{Name}\" already decalred. Should be caught by compiler?");
            }

            var VariableStart = 0;
            if(Globals.Count == 0)
            {
                VariableStart = RAMRange.Start;
            }
            else
            {
                VariableStart = Globals.Last().Address.End + 1;
            }
            var AddressRange = new Range(VariableStart, VariableStart + Marshal.SizeOf(Type) - 1);
            AddGlobalVariable(Type, Name, AddressRange);
        }

        private void AddGlobalVariable(Type Type, string Name, Range Address)
        {
            var NewGlobal = new GlobalInfo(Type, Name, Address);
            foreach(var Global in Globals)
            {
                if(NewGlobal.ConflictsWith(Global))
                {
                    throw new FatalCompilationException("Attempted to add a new global [\{NewGlobal}] that conflicts with an existing global [\{Global}]");
                }
            }
            Globals.Add(NewGlobal);
        }

        public GlobalInfo GetGlobal(string Name)
        {
            return Globals.Single(g => g.Name == Name);
        }

        private void WriteHead(StreamWriter Writer)
        {
            Writer.WriteLine("; Beginning of compiler-generated source file.");
            Writer.WriteLine("\tprocessor 6502");
            Writer.WriteLine("\tinclude vcs.h");
            Writer.WriteLine("\torg $F000");
            Writer.WriteLine();
        }

        private void WriteGlobals(StreamWriter Writer)
        {
            Writer.WriteLine(";Globals");
            foreach(var Global in Globals)
            {
                Writer.WriteLine("\{Global.Name} = $\{Global.Address.Start.ToString("X")} ; \{Global.Type} (\{Global.Size} bytes)");
            }
            Writer.WriteLine();
        }

        private void WriteInitializeSystem(StreamWriter Writer)
        {
            //@TODO - Use Instructions.
            Writer.WriteLine(
@"Start
	SEI ; Disable interrupts.
	CLD ; Clear BCD math bit.
	LDX #$FF ; Reset stack pointer to FF.
	TXS
	LDA #0
ClearMem
	; Clear all memory from $FF to $00 with 0s.
	STA 0,X
	DEX
	BNE ClearMem");
            Writer.WriteLine();
        }

        private void WriteSubroutine(StreamWriter Writer, Subroutine Subroutine)
        {
            Writer.WriteLine(Subroutine.Name);
            foreach(var Instruction in Subroutine.Instructions)
            {
                Writer.WriteLine("\t\{Instruction.Text} ; \{Instruction.Cycles} cycles");
            }
            Writer.WriteLine();
        }
    }

    internal struct GlobalInfo
    {
        public readonly Type Type;
        public readonly string Name;
        public readonly Range Address;
        public int Size { get { return Marshal.SizeOf(Type); } }

        public GlobalInfo(Type Type, string Name, Range Address)
        {
            this.Type = Type;
            this.Name = Name;
            this.Address = Address;
        }

        public bool ConflictsWith(GlobalInfo Other)
        {
            if(this.Name == Other.Name)
            {
                return true;
            }
            if(this.Address.Overlaps(Other.Address))
            {
                return true;
            }
            return false;
        }

        public override string ToString()
        {
            return "\{Type} \{Name} (\{Address})";
        }
    }
}
