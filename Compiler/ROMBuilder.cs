using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using CSharpTo2600.Framework;
using static CSharpTo2600.Framework.Assembly.AssemblyFactory;
using CSharpTo2600.Framework.Assembly;

namespace CSharpTo2600.Compiler
{
    internal class ROMBuilder
    {
        private static readonly Range RAMRange = new Range(0x80, 0xFF);
        public GlobalVariableManager VariableManager { get; private set; }
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
                VariableManager = VariableManager.AddVariable(Names[i], typeof(byte), Address, false);
            }
        }

        public void AddSubroutine(Subroutine Subroutine)
        {
            Subroutines.Add(Subroutine);
        }

        public void WriteToFile(string Path)
        {
            var Lines = new List<AssemblyLine>();
            Lines.AddRange(WriteHeader());
            Lines.AddRange(WriteGlobals());
            var MemoryInitializer = Subroutines.Single(s => s.Name == "InitializeCPU");
            WriteSubroutine(MemoryInitializer);
            var InitializeMethod = Subroutines.Where(s => s.Type == MethodType.Initialize).SingleOrDefault();
            if (InitializeMethod != null)
            {
                WriteSubroutine(InitializeMethod);
            }
            using (var Writer = new StreamWriter(Path))
            {
                foreach(var Line in Lines)
                {
                    Writer.WriteLine(Line.ToString());
                }
            }
        }

        public void AddGlobalVariable(Type Type, string Name)
        {
            VariableManager = VariableManager.AddVariable(Name, Type);
        }

        private IEnumerable<AssemblyLine> WriteHeader()
        {
            //@TODO
            yield return Comment("Beginning of compiler-generated source file.");
            yield return Processor();
            yield return Include("vcs.h");
            yield return Org(0xF000);
            yield return BlankLine();
        }

        private IEnumerable<AssemblyLine> WriteGlobals()
        {
            yield return Comment("Globals:");
            foreach (var Global in VariableManager.GetLocalScopeVariables().Cast<GlobalVariable>()
                .Where(v => v.EmitToFile).OrderBy(v => v.Address.Start))
            {
                //@TODO - Add comment on type and size in bytes.
                yield return DefineSymbol(Global.Name, Global.Address.Start);
            }
        }

        private IEnumerable<AssemblyLine> WriteSubroutine(Subroutine Subroutine)
        {
            yield return Label(Subroutine.Name);
            yield return BlankLine();
            throw new NotImplementedException();
        }
    }
}
