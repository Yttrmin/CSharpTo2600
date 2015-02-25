using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CSharpTo2600.Framework;
using CSharpTo2600.Framework.Assembly;
using static CSharpTo2600.Framework.Assembly.AssemblyFactory;
using static CSharpTo2600.Framework.Assembly.Symbols;
using System.Diagnostics;
using System.Reflection;

namespace CSharpTo2600.Compiler
{
    internal class ROMBuilder
    {
        private static readonly Range RAMRange = new Range(0x80, 0xFF);
        public GlobalVariableManager VariableManager { get; private set; }
        private readonly List<Subroutine> Subroutines;
        private int NextGlobalStart;
        private readonly Symbol StartLabel, MainLoop;
        private delegate IEnumerable<AssemblyLine> KernelGenerator(Subroutine UserCode);

        public ROMBuilder()
        {
            Subroutines = new List<Subroutine>();
            VariableManager = new GlobalVariableManager(RAMRange);
            ReserveGlobals();
            NextGlobalStart = RAMRange.Start;
            StartLabel = Label("__StartProgram");
            MainLoop = Label("__MainLoop");
        }

        public void AddSubroutine(Subroutine Subroutine)
        {
            Subroutines.Add(Subroutine);
        }

        public string WriteToFile(string FileName)
        {
            var Lines = new List<AssemblyLine>();
            Lines.AddRange(WriteHeader());
            Lines.AddRange(WriteGlobals());
            Lines.AddRange(GenerateInitializer());
            Lines.AddRange(GenerateMainLoop());
            Lines.AddRange(GenerateKernel());
            Lines.AddRange(GenerateOverscan());
            Lines.AddRange(GenerateInterruptVectors());

            using (var Writer = new StreamWriter(FileName))
            {
                foreach (var Line in Lines)
                {
                    Writer.WriteLine(Line.ToString());
                }
            }

            return Path.GetFullPath(FileName);
        }

        public void AddGlobalVariable(Type Type, string Name)
        {
            VariableManager = VariableManager.AddVariable(Name, Type);
        }

        /// <summary>
        /// Assembles only the given AssemblyLines, no other lines are inserted
        /// by the builder.
        /// </summary>
        /// <returns>The bytes of the assembled binary.</returns>
        internal static byte[] BuildRawROM(IEnumerable<AssemblyLine> Lines)
        {
            const string ASMFileName = "tempOut.asm";
            using (var Writer = new StreamWriter(ASMFileName))
            {
                foreach(var Line in Lines)
                {
                    Writer.WriteLine(Line.ToString());
                }
            }

            var Success = Compiler.AssembleOutput($"\"{Path.GetFullPath(ASMFileName)}\"");

            if(Success)
            {
                var Data = File.ReadAllBytes("output.bin");
                return Data;
            }
            else
            {
                throw new FatalCompilationException("DASM compilation failed.");
            }
        }

        private void ReserveGlobals()
        {
            // Reserve symbols used in VCS.h to ensure no conflicts.
            // DASM symbols are case-sensitive. So someone can name their variable
            // "VSyNC" if they really want to.
            // Offsets are important for if we ever support cartridges with bank switching.
            // The addresses aren't particularly important, but we have the field in
            // GlobalInfo so we might as well use it correctly.

            // Not using the const strings in Symbols.cs since we might make those
            // into symbols.
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
            for (var i = 0; i < Names.Length; i++)
            {
                var Address = new Range(Offset + i, Offset + i);
                VariableManager = VariableManager.AddVariable(Names[i], typeof(byte), Address, false);
            }
        }

        private IEnumerable<AssemblyLine> GenerateInitializer()
        {
            yield return Subroutine(StartLabel);
            foreach(var Line in Fragments.ClearSystem())
            {
                yield return Line;
            }
            var UserInitializer = GetSpecialSubroutine(MethodType.Initialize);
            if (UserInitializer != null)
            {
                yield return Comment("Beginning of user code.");
                foreach (var Line in UserInitializer.Body)
                {
                    yield return Line;
                }
                yield return Comment("End of user code.");
            }
        }

        private IEnumerable<AssemblyLine> GenerateMainLoop()
        {
            yield return Subroutine(MainLoop);
            yield return LDA(2);
            yield return STA(VSYNC);
            yield return STA(WSYNC);
            yield return STA(WSYNC);
            yield return STA(WSYNC);
            // If there was no branching in the user code we could trivially count
            // the cycles and not need a timer. That's pretty unlikely though.
            yield return LDA(43);
            yield return STA(TIM64T);
            yield return LDA(0);
            yield return STA(VSYNC);
            yield return Comment("Beginning of user code.");
            var UserSubroutine = GetSpecialSubroutine(MethodType.MainLoop);
            if (UserSubroutine != null)
            {
                foreach (var Line in UserSubroutine.Body)
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
        }

        private IEnumerable<AssemblyLine> GenerateKernel()
        {
            var KernelLabel = Label("__Kernel");
            yield return Subroutine(KernelLabel);
            KernelGenerator Generator;
            var KernelSubroutine = GetSpecialSubroutine(MethodType.Kernel);

            if (KernelSubroutine != null)
            {
                var Technique = KernelSubroutine.OriginalMethod.GetCustomAttributes(false).OfType<KernelAttribute>().Single().Technique;
                switch (Technique)
                {
                    case KernelTechnique.None:
                        throw new FatalCompilationException("Can't use KernelTechnique.None");
                    case KernelTechnique.CallEveryScanline:
                        Generator = GenerateKernelEveryScanline;
                        break;
                    default:
                        throw new NotImplementedException($"{Technique} not supported yet.");
                }
            }
            else
            {
                Generator = GenerateEmptyKernel;
            }

            foreach(var Line in Generator(KernelSubroutine))
            {
                yield return Line;
            }
        }

        private IEnumerable<AssemblyLine> GenerateEmptyKernel(Subroutine UserCode)
        {
            yield return Comment("No kernel method found in user code. Just emitting 192 WSYNCs.", 0);
            yield return Repeat(192);
            yield return STA(WSYNC);
            yield return Repend();
        }

        private IEnumerable<AssemblyLine> GenerateKernelEveryScanline(Subroutine UserCode)
        {
            yield return Comment("Generating single kernel, called 192 times.", 0);
            //@TODO - Do this during VBlank, save precious cycles.
            yield return LDX(192);
            var LoopLabel = Label(".__ScanLoop");
            yield return LoopLabel;
            yield return DEX();
            yield return Comment("Beginning of user code.");
            foreach(var Line in UserCode.Body)
            {
                yield return Line;
            }
            yield return Comment("End of user code.");
            yield return CPX(0);
            yield return STA(WSYNC);
            yield return BNE(LoopLabel);
            //@TODO - Make sure we have enough cycles. Every cycle counts and someone could
            // be using up all the cycles for the whole scanline. Or not leave enough
            // cycles for a STA.
            //@TODO - Make sure UserCode doesn't exceed the cycle count for a scanline.
        }

        private IEnumerable<AssemblyLine> GenerateOverscan()
        {
            var OverscanLabel = Label("__Overscan");
            yield return Subroutine(OverscanLabel);
            yield return LDA(35);
            yield return STA(TIM64T);
            yield return LDA(2);
            yield return STA(VBLANK);
            var UserSubroutine = GetSpecialSubroutine(MethodType.Overscan);
            if(UserSubroutine != null)
            {
                yield return Comment("Beginning of user code.");
                foreach (var Line in UserSubroutine.Body)
                {
                    yield return Line;
                }
                yield return Comment("End of user code.");
            }
            var LoopLabel = Label(".__WaitForOverscanEnd");
            yield return LoopLabel;
            yield return LDA(INTIM);
            yield return BNE(LoopLabel);
            yield return JMP(MainLoop);
            yield return BlankLine();
        }

        private IEnumerable<AssemblyLine> GenerateInterruptVectors()
        {
            yield return Comment("Interrupt vectors", 0);
            yield return Org(0xFFFC);
            yield return Word(StartLabel);
            // BRK vector remains if we want to use it.
        }

        /// <summary>
        /// Returns the subroutine matching the given Type, or null if none exist.
        /// </summary>
        private Subroutine GetSpecialSubroutine(MethodType Type)
        {
            return Subroutines.SingleOrDefault(s => s.Type == Type);
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
