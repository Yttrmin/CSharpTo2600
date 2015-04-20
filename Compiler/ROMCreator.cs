using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using CSharpTo2600.Framework;
using CSharpTo2600.Framework.Assembly;
using Microsoft.CodeAnalysis;
using static CSharpTo2600.Framework.Assembly.AssemblyFactory;
using static CSharpTo2600.Framework.Assembly.ReservedSymbols;

namespace CSharpTo2600.Compiler
{
    internal abstract class ROMCreator
    {
        protected CompilationInfo CompilationInfo { get; private set; }
        protected readonly Symbol StartLabel = Label("__StartProgram");
        protected readonly Symbol MainLoopLabel = Label("__MainLoop");
        protected readonly Symbol OverscanLabel = Label("__Overscan");

        public ROMCreator(CompilationInfo CompilationInfo)
        {
            this.CompilationInfo = CompilationInfo;
        }

        public static void CreateRawROM()
        {
            throw new NotImplementedException();
        }
    }

    class MemoryManager
    {
        private readonly CompilationInfo Info;
        private int NextAddress = GlobalsStart;
        private const int RAMAmount = 128;
        private const int GlobalsStart = 0x80;
        private const int StackStart = 0xFF;
        //@TODO
        // Reserve a completely arbitrary amount of memory for globals.
        private const int GlobalUsageLimit = 100;

        private MemoryManager(CompilationInfo Info)
        {
            this.Info = Info;
        }

        public static CompilationInfo Analyze(CompilationInfo Info)
        {
            // Note types won't be compiled at this point.
            var Manager = new MemoryManager(Info);
            foreach (var NewType in Manager.LayoutGlobals())
            {
                Info = Info.ReplaceType(NewType);
            }
            return Info;
        }

        private IEnumerable<ProcessedType> LayoutGlobals()
        {
            var MemoryUsage = Info.AllGlobals.Sum(v => v.Size);
            if (MemoryUsage > GlobalUsageLimit)
            {
                throw new FatalCompilationException($"Too many globals, {MemoryUsage} bytes needed, but only {GlobalUsageLimit} bytes available.");
            }

            foreach (var Type in Info.AllTypes)
            {
                if (Type.Globals.Any())
                {
                    yield return AssignGlobalsAddresses(Type);
                }
                else
                {
                    yield return Type;
                }
            }
        }

        private ProcessedType AssignGlobalsAddresses(ProcessedType Type)
        {
            var NewGlobals = new Dictionary<IFieldSymbol, VariableInfo>();
            foreach (var Global in Type.Globals)
            {
                var Symbol = Global.Key;
                var NewVariable = VariableInfo.CreateDirectlyAddressableVariable(Global.Key,
                    Global.Value.Type, NextAddress);
                NewGlobals.Add(Symbol, NewVariable);
                NextAddress += NewVariable.Size;
            }
            return new ProcessedType(Type, Globals: NewGlobals.ToImmutableDictionary());
        }
    }

    internal sealed class ROMStandard4K : ROMCreator
    {
        private ROMStandard4K(CompilationInfo Info)
            : base(Info)
        { }

        public static string CreateASMFile(CompilationInfo Info)
        {
            var Creator = new ROMStandard4K(Info);
            var Builder = new StringBuilder();
            WriteLines(Creator.CreateHeader(), Builder);
            WriteLines(Creator.CreateSymbolDefinitions(), Builder);
            WriteLines(Creator.CreateEntryPoint(), Builder);
            WriteLines(Creator.CreateMainLoop(), Builder);
            //@TODO
            WriteLines(Creator.CreateEmptyKernel(), Builder);
            WriteLines(Creator.CreateOverscan(), Builder);
            WriteLines(Creator.CreateInterruptVectors(), Builder);

            using (var Writer = new StreamWriter("out.asm"))
            {
                Writer.WriteLine(Builder.ToString());
            }

            return Path.GetFullPath("out.asm");
        }

        private static void WriteLines(IEnumerable<AssemblyLine> Lines, StringBuilder Builder)
        {
            foreach (var Line in Lines)
            {
                Builder.AppendLine(Line.ToString());
            }
        }

        private IEnumerable<AssemblyLine> CreateHeader()
        {
            yield return Comment("Beginning of compiler-generated source file.", 0);
            yield return Processor();
            yield return Include("vcs.h");
            yield return Org(0xF000);
            yield return BlankLine();
        }

        private IEnumerable<AssemblyLine> CreateSymbolDefinitions()
        {
            foreach (var Global in CompilationInfo.AllGlobals)
            {
                yield return Global.AssemblySymbol;
            }
        }

        private IEnumerable<AssemblyLine> CreateEntryPoint()
        {
            yield return Subroutine(StartLabel);
            foreach (var Line in Fragments.ClearSystem())
            {
                yield return Line;
            }
            var UserInitializer = GetSpecialSubroutines(MethodType.Initialize).SingleOrDefault();
            if (UserInitializer != null)
            {
                yield return Comment("Beginning of user initialization code.");
                foreach (var Line in UserInitializer.Body)
                {
                    yield return Line;
                }
                yield return Comment("End of user code.");
            }
        }

        private IEnumerable<AssemblyLine> CreateMainLoop()
        {
            yield return Subroutine(MainLoopLabel);
            var UserLoop = GetSpecialSubroutines(MethodType.MainLoop).SingleOrDefault();
            if (UserLoop != null)
            {
                yield return Comment("Beginning of user main loop code.");
                foreach (var Line in UserLoop.Body)
                {
                    yield return Line;
                }
                yield return Comment("End of user code.");
            }
        }

        private IEnumerable<AssemblyLine> CreateEmptyKernel()
        {
            yield return Comment("No kernel method found in user code. Just emitting 192 WSYNCs.", 0);
            yield return Repeat(192);
            yield return STA(WSYNC);
            yield return Repend();
        }

        private IEnumerable<AssemblyLine> CreateOverscan()
        {
            yield return Subroutine(OverscanLabel);
            yield return LDA(35);
            yield return STA(TIM64T);
            yield return LDA(2);
            yield return STA(VBLANK);
            var UserSubroutine = GetSpecialSubroutines(MethodType.Overscan).SingleOrDefault();
            if (UserSubroutine != null)
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
            yield return JMP(MainLoopLabel);
            yield return BlankLine();
        }

        private IEnumerable<AssemblyLine> CreateInterruptVectors()
        {
            yield return Comment("Interrupt vectors", 0);
            yield return Org(0xFFFC);
            yield return Word(StartLabel);
            // BRK vector remains if we want to use it.
        }

        private IEnumerable<Subroutine> GetSpecialSubroutines(MethodType MethodType)
        {
            return CompilationInfo.AllSubroutines.Where(s => s.Type == MethodType);
        }
    }
}
