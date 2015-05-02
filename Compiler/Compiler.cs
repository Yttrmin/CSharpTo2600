using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using System.IO;
using CSharpTo2600.Framework;
using System.Reflection;

namespace CSharpTo2600.Compiler
{
    public sealed partial class GameCompiler
    {
        private readonly CSharpCompilation Compilation;
        private readonly Assembly CompiledAssembly;
        private readonly Assembly FrameworkAssembly;
        private CompileOptions Options = CompileOptions.Default;
        public const string DASMPath = "./Dependencies/DASM/";

        static void Main(string[] args)
        {
            GameCompiler.Compile(args, CompileOptions.Default);
            Console.ReadLine();
        }
        
        private GameCompiler(CSharpCompilation Compilation, CompileOptions Options)
        {
            if (!EndianHelper.EndiannessIsSet)
            {
                EndianHelper.Endianness = Options.Endianness;
            }

            this.Options = Options;
            FrameworkAssembly = typeof(Atari2600Game).Assembly;

            this.Compilation = Compilation;

            using (var Stream = new MemoryStream())
            {
                this.Compilation.Emit(Stream);
                CompiledAssembly = Assembly.Load(Stream.ToArray());
            }
        }

        public static ROMInfo Compile(string SourceText)
        {
            return Compile(SourceText, CompileOptions.Default);
        }

        public static ROMInfo Compile(string SourceText, CompileOptions CompileOptions)
        {
            return Compile(new CompilerWorkspace(SourceText), CompileOptions);
        }

        public static ROMInfo Compile(IEnumerable<string> FilePaths, CompileOptions Options)
        {
            return Compile(new CompilerWorkspace(FilePaths), Options);
        }

        private static ROMInfo Compile(CompilerWorkspace Workspace, CompileOptions Options)
        {
            var Compiler = new GameCompiler(Workspace.Compilation, Options);
            var CompilationInfo = new CompilationInfo();
            // First stage is to parse the types without compiling any methods. This gets us the
            // type's fields and subroutine signatures.
            foreach (var Type in Compiler.CompiledAssembly.DefinedTypes)
            {
                CompilationInfo = CompilationInfo.WithType(TypeParser.ParseType(Type, Workspace.Compilation));
            }
            // All fields have been explored, so we have enough information to layout globals
            // in memory.
            CompilationInfo = MemoryManager.Analyze(CompilationInfo);
            // Now we can compile methods, knowing any field accesses or method calls should work
            // since we explored them in the parsing stage.
            foreach (var Type in CompilationInfo.AllTypes)
            {
                CompilationInfo = CompilationInfo.WithReplacedType(TypeCompiler.CompileType(Type, CompilationInfo, Compiler));
            }
            var ROMInfo = ROMCreator.CreateROM(CompilationInfo);
            if (ROMInfo.Success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Compilation successful.");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Compilation failed.");
            }
            return ROMInfo;
        }
    }

    public struct CompileOptions
    {
        public readonly bool Optimize;
        public readonly Endianness Endianness;
        public static readonly CompileOptions Default =
            new CompileOptions(Optimize: true, Endianness: Endianness.Big);

        public CompileOptions(bool Optimize, Endianness Endianness)
        {
            this.Optimize = Optimize;
            this.Endianness = Endianness;
        }
    }
}
