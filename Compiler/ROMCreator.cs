using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CSharpTo2600.Framework;
using CSharpTo2600.Framework.Assembly;
using Microsoft.CodeAnalysis;
using static CSharpTo2600.Framework.Assembly.AssemblyFactory;
using static CSharpTo2600.Framework.Assembly.ReservedSymbols;

namespace CSharpTo2600.Compiler
{
    internal static class ROMCreator
    {
        private static readonly Symbol StartLabel = Label("__StartProgram");
        private static readonly Symbol MainLoopLabel = Label("__MainLoop");
        private static readonly Symbol OverscanLabel = Label("__Overscan");
        private const string AssemblyFileName = "out.asm";
        private const string BinaryFileName = "out.bin";
        private const string SymbolsFileName = "out.sym";
        private const string ListFileName = "out.lst";
        private const string DASMPath = "./Dependencies/DASM/";
        // OutputDirectory has to come first since static variables are initialized in the order they appear.
        private static readonly string OutputDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly string BinaryFilePath = Path.Combine(OutputDirectory, BinaryFileName);
        private static readonly string SymbolsFilePath = Path.Combine(OutputDirectory, SymbolsFileName);
        private static readonly string ListFilePath = Path.Combine(OutputDirectory, ListFileName);

        public static CompilationResult CreateROM(CompilationState State)
        {
            var Lines = new List<AssemblyLine>();
            Lines.AddRange(CreateHeader());
            Lines.AddRange(CreateSymbolDefinitions(State));
            Lines.AddRange(CreateEntryPoint(State));
            Lines.AddRange(CreateMainLoop(State));
            Lines.AddRange(CreateKernel(State));
            Lines.AddRange(CreateOverscan(State));
            Lines.AddRange(CreateTypeData(State));
            Lines.AddRange(CreateInterruptVectors());

            using (var Writer = new StreamWriter(AssemblyFileName))
            {
                foreach (var Line in Lines)
                {
                    Writer.WriteLine(Line.ToString());
                }
            }

            var DASMSuccess = AssembleOutput();

            return new CompilationResult(Path.GetFullPath(BinaryFilePath), Path.GetFullPath(AssemblyFileName), 
                Path.GetFullPath(SymbolsFilePath), Path.GetFullPath(ListFilePath), State, Lines, DASMSuccess);
        }

        public static CompilationResult CreateRawROM(IEnumerable<AssemblyLine> Lines)
        {
            using (var Writer = new StreamWriter(AssemblyFileName))
            {
                foreach (var Line in Lines)
                {
                    Writer.WriteLine(Line.ToString());
                }
            }

            var DASMSuccess = AssembleOutput();

            return new CompilationResult(Path.GetFullPath(BinaryFilePath), Path.GetFullPath(AssemblyFileName),
                Path.GetFullPath(SymbolsFilePath), Path.GetFullPath(ListFilePath), null, Lines, DASMSuccess);
        }

        private static void WriteLines(IEnumerable<AssemblyLine> Lines, StringBuilder Builder)
        {
            foreach (var Line in Lines)
            {
                Builder.AppendLine(Line.ToString());
            }
        }

        private static IEnumerable<AssemblyLine> CreateHeader()
        {
            yield return Comment("Beginning of compiler-generated source file.", 0);
            yield return Processor();
            yield return Include("vcs.h");
            yield return Org(0xF000);
            yield return BlankLine();
        }

        private static IEnumerable<AssemblyLine> CreateSymbolDefinitions(CompilationState CompilationState)
        {
            // Special globals
            // See Compiler.MethodCompiler.ReturnValue;
            yield return DefineSymbol("_ReturnValue", 0x80);

            foreach (var Global in CompilationState.AllGlobals)
            {
                if (CompilationState.AllGlobals.Any(v => v != Global && v.Name == Global.Name))
                {
                    //@TODO - Prepend class name? Namespace and class name? Arbitrary text?
                    throw new NotImplementedException("Same name globals in different types not supported yet.");
                }
                yield return Global.AssemblySymbol.WithComment($"{Global.Type} - {Global.Size} bytes");
            }
            yield return BlankLine();
        }

        private static IEnumerable<AssemblyLine> CreateEntryPoint(CompilationState CompilationState)
        {
            yield return Subroutine(StartLabel);
            foreach (var Line in Fragments.ClearSystem())
            {
                yield return Line;
            }
            var UserInitializer = GetSpecialSubroutines(CompilationState, MethodType.Initialize).SingleOrDefault();
            if (UserInitializer != null)
            {
                yield return Comment("Beginning of user initialization code.");
                foreach (var Line in Fragments.InlineInvoke(UserInitializer))
                {
                    yield return Line;
                }
                yield return Comment("End of user code.");
            }
        }

        private static IEnumerable<AssemblyLine> CreateMainLoop(CompilationState CompilationState)
        {
            yield return Subroutine(MainLoopLabel);
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
            var UserLoop = GetSpecialSubroutines(CompilationState, MethodType.MainLoop).SingleOrDefault();
            if (UserLoop != null)
            {
                yield return Comment("Beginning of user main loop code.");
                foreach (var Line in Fragments.InlineInvoke(UserLoop))
                {
                    yield return Line;
                }
                yield return Comment("End of user code.");
            }
            var WaitLabel = Label(".WaitForVBlankEnd");
            yield return WaitLabel;
            yield return LDA(INTIM);
            yield return BNE(WaitLabel);
            yield return STA(WSYNC);
            yield return STA(VBLANK);
        }

        private static IEnumerable<AssemblyLine> CreateKernel(CompilationState State)
        {
            var GameClassSubroutines = State.GetSubroutineInfosFromType(State.GetGameClass());
            var Kernel = GameClassSubroutines.SingleOrDefault(s => s.Type == MethodType.Kernel);
            if (Kernel != null)
            {
                var KernelTechnique = Kernel.KernelTechnique;
                switch (KernelTechnique)
                {
                    case KernelTechnique.CallEveryScanline:
                        return CreateKernelEveryScanline(Kernel);
                    default:
                        throw new FatalCompilationException($"Kernel technique not supported: {KernelTechnique}");
                }
            }
            else
            {
                return CreateEmptyKernel();
            }
            
        }

        private static IEnumerable<AssemblyLine> CreateEmptyKernel()
        {
            yield return Comment("Beginning of kernel code generation.");
            yield return Comment("No kernel method found in user code. Just emitting 192 WSYNCs.", 0);
            yield return Repeat(192);
            yield return STA(WSYNC);
            yield return Repend();
        }

        private static IEnumerable<AssemblyLine> CreateKernelEveryScanline(SubroutineInfo UserCode)
        {
            //@TODO - Check UserCode for too many cycles.
            //@TODO - Pick REPEAT vs loop dependent on cycles.
            //@TODO - Check cycle count to see if we can skip WSYNC or use faster instruction.
            yield return Comment("Beginning of kernel code generation.");
            yield return Repeat(192);
            yield return Comment("Begin user code.");
            foreach (var Line in Fragments.InlineInvoke(UserCode))
            {
                yield return Line;
            }
            yield return STA(WSYNC).WithComment("Compiler-generated");
            yield return Comment("End user code.");
            yield return Repend();
        }

        private static IEnumerable<AssemblyLine> CreateOverscan(CompilationState CompilationState)
        {
            yield return Subroutine(OverscanLabel);
            yield return LDA(35);
            yield return STA(TIM64T);
            yield return LDA(2);
            yield return STA(VBLANK);
            var UserSubroutine = GetSpecialSubroutines(CompilationState, MethodType.Overscan).SingleOrDefault();
            if (UserSubroutine != null)
            {
                yield return Comment("Beginning of user code.");
                foreach (var Line in Fragments.InlineInvoke(UserSubroutine))
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
        
        private static IEnumerable<AssemblyLine> CreateTypeData(CompilationState CompilationState)
        {
            //@TODO - Include static fields here as well so all type information is in one place?

            // Emits all of a type's user-defined methods.
            foreach(var Type in CompilationState.AllTypes)
            {
                var UserSubroutines = CompilationState.GetSubroutineInfosFromType(Type).Where(s => s.Type == MethodType.UserDefined);
                if(UserSubroutines.Count() != 0)
                {
                    yield return Comment($"Begin Type: {Type.Name}", 0);
                    foreach(var UserSubroutine in UserSubroutines)
                    {
                        yield return Comment($"Begin: {UserSubroutine.ToString()}", 0);
                        yield return UserSubroutine.Label;
                        foreach(var Line in UserSubroutine.Body)
                        {
                            yield return Line;
                        }
                        yield return Comment($"End: {UserSubroutine.ToString()}", 0);
                        if (UserSubroutine != UserSubroutines.Last())
                        {
                            yield return BlankLine();
                        }
                    }
                    yield return Comment($"End Type: {Type.Name}", 0);
                    yield return BlankLine();
                }
            }
        }

        private static IEnumerable<AssemblyLine> CreateInterruptVectors()
        {
            yield return Comment("Interrupt vectors", 0);
            yield return Org(0xFFFC);
            yield return Word(StartLabel);
            // BRK vector remains if we want to use it.
        }

        private static IEnumerable<SubroutineInfo> GetSpecialSubroutines(CompilationState CompilationState, 
            MethodType MethodType)
        {
            return CompilationState.GetSubroutineInfosFromType(CompilationState.GetGameClass()).Where(s => s.Type == MethodType);
        }

        private static bool AssembleOutput()
        {
            var DASM = new Process();
            var FullDASMPath = Path.Combine(DASMPath, "dasm.exe");
            DASM.StartInfo.FileName = FullDASMPath;
            if (!File.Exists(FullDASMPath))
            {
                throw new FileNotFoundException($"DASM executable not found at: {FullDASMPath}");
            }
            DASM.StartInfo.UseShellExecute = false;
            DASM.StartInfo.RedirectStandardOutput = true;
            DASM.StartInfo.WorkingDirectory = DASMPath;
            DASM.StartInfo.Arguments = $"\"{Path.GetFullPath(AssemblyFileName)}\" -f3 -o{BinaryFilePath} -s{SymbolsFilePath} -l{ListFilePath}";
            DASM.StartInfo.CreateNoWindow = true;

            DASM.Start();
            DASM.WaitForExit();
            var Output = DASM.StandardOutput.ReadToEnd();
            // DASM documentation says this returns 0 on success and 1 otherwise. This is not
            // true since it returned 0 when the ASM was missing the 'processor' op, causing a
            // lot of errors and spit out a 0 byte BIN. Hopefully nothing else returns 0 on failure.
            var Success = DASM.ExitCode == 0;
            Console.WriteLine(Output);
            return Success;
        }
    }
}
