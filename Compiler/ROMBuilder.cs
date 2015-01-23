using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using CSharpTo2600.Framework;
using CSharpTo2600.Framework.Instructions;

namespace CSharpTo2600.Compiler
{
    internal class ROMBuilder
    {
        private static readonly Range RAMRange = new Range(0x80, 0xFF);
        private readonly GlobalVariableManager VariableManager;
        private readonly List<Subroutine> Subroutines;
        private int NextGlobalStart;

        public ROMBuilder()
        {
            Subroutines = new List<Subroutine>();
            VariableManager = new GlobalVariableManager(RAMRange);
            ReserveGlobals();
            NextGlobalStart = RAMRange.Start;
            var Initializer = new InstructionInfo[]
            {
                SEI(),
                CLD(),
                LDX(0xFF),
                TXS(),
                LDA(0),
                Label("ClearMem"),
                STA((byte)0, Index.X),
                DEX(),
                BNE("ClearMem")
            }.ToImmutableArray();
            Subroutines.Add(new Subroutine("InitializeCPU", Initializer, MethodType.None));
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
                VariableManager.AddVariable(Names[i], typeof(byte), Address, false);
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
                var MemoryInitializer = Subroutines.Single(s => s.Name == "InitializeCPU");
                WriteSubroutine(Writer, MemoryInitializer);
                var InitializeMethod = Subroutines.Where(s => s.Type == MethodType.Initialize).SingleOrDefault();
                if(InitializeMethod != null)
                {
                    WriteSubroutine(Writer, InitializeMethod);
                }
            }
        }

        public void AddGlobalVariable(Type Type, string Name)
        {
            VariableManager.AddVariable(Name, Type);
        }

        [Obsolete("get_VariableManager")]
        public VariableInfo GetGlobal(string Name)
        {
            return VariableManager.GetVariable(Name);
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
            foreach(var Global in VariableManager.AllVariables().Cast<GlobalVariable>().Where(v => v.EmitToFile))
            {
                Writer.WriteLine("\{Global.Name} = $\{Global.Address.Start.ToString("X")} ; \{Global.Type} (\{Global.Size} bytes)");
            }
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
}
