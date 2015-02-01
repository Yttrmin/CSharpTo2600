using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CSharpTo2600.Framework;
using CSharpTo2600.Framework.Assembly;
using static CSharpTo2600.Framework.Assembly.AssemblyFactory;
using static CSharpTo2600.Framework.Assembly.Symbols;

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

        private IEnumerable<AssemblyLine> GenerateInitializer()
        {
            yield return Comment("Initializer", 0);
            yield return SEI();
            yield return CLD();
            yield return LDX(0xFF);
            yield return TXS();
            yield return LDA(0);
            var ClearLabel = Label(".ClearMem");
            yield return ClearLabel;
            yield return STA((byte)0, Index.X);
            yield return DEX();
            yield return BNE(ClearLabel);
            yield return Comment("Beginning of user code.");
            var UserInitializer = GetSpecialSubroutine(MethodType.Initialize);
            if(UserInitializer != null)
            {
                foreach(var Line in UserInitializer.Body)
                {
                    yield return Line;
                }
            }
            yield return Comment("End of user code.");
        }

        private IEnumerable<AssemblyLine> GenerateMainLoop()
        {
            yield return Comment("Main loop", 0);
            yield return LDA(2);
            yield return STA(VSYNC);
            yield return STA(WSYNC);
            yield return STA(WSYNC);
            yield return STA(WSYNC);
            yield return LDA(43);
            yield return STA(TIM64T);
            yield return LDA(0);
            yield return STA(VSYNC);
            yield return Comment("Beginning of user code.");
            var UserSubroutine = GetSpecialSubroutine(MethodType.Tick);
            if(UserSubroutine != null)
            {
                foreach(var Line in UserSubroutine.Body)
                {
                    yield return Line;
                }
            }
            yield return Comment("End of user code.");
            var WaitLabel = Label(".WaitForVBlankEnd");
            yield return WaitLabel;
            yield return LDA(INTIM);
            yield return BNE(WaitLabel);
            yield return STA(WSYNC);
            yield return STA(VBLANK);
            yield return LDY(192);
        }

        private Subroutine GetSpecialSubroutine(MethodType Type)
        {
            return Subroutines.SingleOrDefault(s => s.Type == Type);
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
            Lines.AddRange(GenerateInitializer());
            Lines.AddRange(GenerateMainLoop());
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
            yield return Comment("Beginning of compiler-generated source file.", 0);
            yield return Processor();
            yield return Include("vcs.h");
            yield return Org(0xF000);
            yield return BlankLine();
        }

        private IEnumerable<AssemblyLine> WriteGlobals()
        {
            yield return Comment("Globals:", 0);
            foreach (var Global in VariableManager.GetLocalScopeVariables().Cast<GlobalVariable>()
                .Where(v => v.EmitToFile).OrderBy(v => v.Address.Start))
            {
                yield return DefineSymbol(Global.Name, Global.Address.Start).WithComment($"{Global.Type} ({Global.Size} bytes)");
            }
            yield return BlankLine();
        }

        private IEnumerable<AssemblyLine> WriteSubroutine(Subroutine Subroutine)
        {
            yield return Label(Subroutine.Name);
            foreach(var Line in Subroutine.Body)
            {
                yield return Line;
            }
            yield return BlankLine();
        }
    }
}
