using System;
using System.Collections.Generic;
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
    internal sealed class ROMCreator
    {
        private readonly CompilationInfo CompilationInfo;
        private readonly Symbol StartLabel = Label("__StartProgram");
        private readonly Symbol MainLoopLabel = Label("__MainLoop");
        private readonly Symbol OverscanLabel = Label("__Overscan");

        private ROMCreator(CompilationInfo Info)
        {
            this.CompilationInfo = Info;
        }

        public static string CreateASMFile(CompilationInfo Info)
        {
            var Creator = new ROMCreator(Info);
            var Builder = new StringBuilder();
            var Lines = new List<AssemblyLine>();
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
                if (CompilationInfo.AllGlobals.Any(v => v != Global && v.Name == Global.Name))
                {
                    //@TODO - Prepend class name? Namespace and class name? Arbitrary text?
                    throw new NotImplementedException("Same name globals in different types not supported yet.");
                }
                yield return Global.AssemblySymbol.WithComment($"{Global.Type} - {Global.Size} bytes");
            }
            yield return BlankLine();
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
