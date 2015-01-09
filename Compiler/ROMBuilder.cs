﻿using CSharpTo2600.Framework;
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
        private int NextGlobalStart;

        public ROMBuilder()
        {
            Subroutines = new List<Subroutine>();
            Globals = new List<GlobalInfo>();
            ReserveGlobals();
            NextGlobalStart = RAMRange.Start;
        }

        private void ReserveGlobals()
        {
            // Reserve symbols used in VCS.h to ensure no conflicts.
            // DASM symbols are case-sensitive. So someone can name their variable
            // "VSyNC" if they really want to.
            // Offsets are important for if we ever support cartridges with bank switching.
            // The addresses aren't particularly important, but we have the field in
            // GlobalInfo so we might as well use it correctly.

            // TIA Write
            AddReservedGlobals(0, "VSYNC", "VBLANK", "WSYNC", "RSYNC", "NUSIZ0", "NUSIZ1",
                "COLUP0", "COLUP1", "COLUPF", "COLUBK", "CTRLPF", "REFP0", "REFP1", "PF0", 
                "PF1", "PF2", "RESP0", "RESP1", "RESM0", "RESM1", "RESBL", "AUDC0", "AUDC1", 
                "AUDF0", "AUDF1", "AUDV0", "AUDV1", "GRP0", "GRP1", "ENAM0", "ENAM1", "ENABL", 
                "HMP0", "HMP1", "HMM0", "HMM1", "HMBL", "VDELP0", "VDELP1", "VDELBL", "RESMP0",
                "RESMP1", "HMOVE", "HMCLR", "CXCLR");
            // TIA Read. Yes, they overlap TIA Write.
            AddReservedGlobals(0, "CXM0P", "CXM1P", "CXP0FB", "CXP1FB", "CXM0FB", "CXM1FB",
                "CXBLPF", "CXPPMM", "INPT0", "INPT1", "INPT2", "INPT3", "INPT4", "INPT5");
            // RIOT
            AddReservedGlobals(0x280, "SWCHA", "SWACNT", "SWCHB", "SWBCNT", "INTIM", "TIMINT");
            AddReservedGlobals(0x294, "TIM1T", "TIM8T", "TIM64T", "T1024T");
        }

        private void AddReservedGlobals(int Offset, params string[] Names)
        {
            for(var i = 0; i < Names.Length; i++)
            {
                var Address = new Range(Offset + i, Offset + i);
                AddGlobalVariable(typeof(byte), Names[i], Address, false, false);
            }
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

            var AddressRange = new Range(NextGlobalStart, NextGlobalStart + Marshal.SizeOf(Type) - 1);
            if(!RAMRange.Contains(AddressRange))
            {
                throw new FatalCompilationException("Ran out of RAM trying to add new global [\{Type} \{Name}]");
            }
            AddGlobalVariable(Type, Name, AddressRange, true, true);
        }

        private void AddGlobalVariable(Type Type, string Name, Range Address, bool Emit, bool ConflictCheck)
        {
            var NewGlobal = new GlobalInfo(Type, Name, Address, Emit);
            if(ConflictCheck)
            {
                if(Globals.Any(g => g.ConflictsWith(NewGlobal)))
                {
                    throw new FatalCompilationException("Attempted to add a new global [\{NewGlobal}] that conflicts with an existing global.");
                }
            }
            NextGlobalStart = Address.End + 1;
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
            //@TODO - TIA_BASE_ADDRESS
            Writer.WriteLine("\tinclude vcs.h");
            Writer.WriteLine("\torg $F000");
            Writer.WriteLine();
        }

        private void WriteGlobals(StreamWriter Writer)
        {
            Writer.WriteLine(";Globals");
            foreach(var Global in Globals.Where(g => g.Emit))
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
                Writer.WriteLine("\t\{Instruction.Text}");
            }
            Writer.WriteLine();
        }
    }

    internal struct GlobalInfo
    {
        public readonly Type Type;
        public readonly string Name;
        public readonly Range Address;
        public readonly bool Emit;
        public int Size { get { return Marshal.SizeOf(Type); } }

        public GlobalInfo(Type Type, string Name, Range Address, bool Emit)
        {
            this.Type = Type;
            this.Name = Name;
            this.Address = Address;
            this.Emit = Emit;
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
