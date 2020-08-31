#nullable enable
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using VCSFramework.V2;

namespace VCSCompiler.V2
{
    internal class AssemblyWriter
    {
        private readonly ImmutableDictionary<MethodDefinition, ImmutableArray<AssemblyEntry>> CompiledMethods;
        private readonly LabelMap LabelMap;
        private readonly SourceAnnotation SourceAnnotations;
        private readonly Lazy<string> AssemblyText;
        private static readonly string Indent = "    ";
        private static readonly string SourceIndent = "   ";
        private static readonly string StackIndex = "        ";

        public AssemblyWriter(
            Dictionary<MethodDefinition, ImmutableArray<AssemblyEntry>> compiledMethods,
            LabelMap labelMap,
            SourceAnnotation sourceAnnotations)
        {
            CompiledMethods = compiledMethods.ToImmutableDictionary();
            LabelMap = labelMap;
            SourceAnnotations = sourceAnnotations;
            AssemblyText = new(BuildAssemblyText, false);
        }

        public RomInfo WriteToFile(string path)
        {
            var binPath = path;
            var asmPath = Path.ChangeExtension(path, "asm");
            var listPath = Path.ChangeExtension(path, "lst");

            File.WriteAllText(asmPath, AssemblyText.Value);

            var assemblerArgs = new[]
            {
                asmPath,
                "-o",
                binPath,
                "-L",
                listPath,
                "--format=flat"
            };

            using var stdoutStream = new MemoryStream();
            using var writer = new StreamWriter(stdoutStream) { AutoFlush = true };
            Console.SetOut(writer);
            Console.SetError(writer);

            Core6502DotNet.Core6502DotNet.Main(assemblerArgs);

            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
            stdoutStream.Position = 0;
            using var reader = new StreamReader(stdoutStream);
            var stdoutText = reader.ReadToEnd();
            Console.WriteLine("Assembler output:");
            Console.WriteLine(stdoutText);

            if (!stdoutText.Contains("Assembly completed successfully."))
            {
                Console.WriteLine("Assembly failed, there is probably an internal problem with the code that the compiler is generating.");
                return new RomInfo
                {
                    IsSuccessful = false,
                    AssemblyPath = asmPath
                };
            }

            Console.WriteLine("Assembly was successful.");
            return new RomInfo
            {
                IsSuccessful = true,
                AssemblyPath = asmPath,
                RomPath = binPath,
                ListPath = listPath
            };
        }

        public RomInfo WriteToConsole()
        {
            Console.WriteLine(AssemblyText.Value);
            return new RomInfo
            {
                IsSuccessful = true
            };
        }

        private string BuildAssemblyText()
        {
            var builder = new StringBuilder();
            builder.AppendLine($"// Generated on {DateTime.Now:R}");
            builder.AppendLine();
            builder.AppendLine(@".cpu ""6502""");
            builder.AppendLine("* = $F000");
            builder.AppendLine();

            AppendLabels(builder);
            builder.AppendLine();
            builder.AppendLine(@".include ""vcs.h""");
            builder.AppendLine(@".include ""vil.h""");
            builder.AppendLine();

            builder.AppendLine(new Comment("Begin function definitions"));
            builder.AppendLine(LabelGenerator.Start);
            // Entry point needs to come first so we execute it after init/clear.
            var entryPointPair = CompiledMethods.Single(it => it.Value.OfType<EntryPoint>().Any());
            AppendMethodText(entryPointPair, builder);
            foreach (var pair in CompiledMethods.Where(it => !it.Equals(entryPointPair)))
            {
                builder.AppendLine();
                AppendMethodText(pair, builder);
            }

            builder.AppendLine(new Comment("End function definitions"));
            builder.AppendLine();
            builder.AppendLine("* = $FFFC");
            builder.AppendLine($".word {LabelGenerator.Start}");
            builder.AppendLine($".word {LabelGenerator.Start}");
            return builder.ToString();
        }

        private void AppendMethodText(KeyValuePair<MethodDefinition, ImmutableArray<AssemblyEntry>> methodPair, StringBuilder builder)
        {
            builder.AppendLine(new Comment($"Begin {methodPair.Key.FullName}"));
            builder.AppendLine(LabelGenerator.Function(methodPair.Key));
            foreach (var entry in methodPair.Value)
            {
                var macro = entry as Macro;
                if (macro != null)
                {
                    AppendSource(methodPair.Key, macro, builder);
                }
                var indent = entry is InstructionLabel ? "" : Indent;
                builder.AppendLine($"{indent}{entry}");
                if (macro != null)
                {
                    foreach (var stackLet in macro.StackLets)
                    {
                        builder.AppendLine($"{StackIndex}{stackLet}");
                    }
                }
            }
            builder.AppendLine(new Comment($"End {methodPair.Key.FullName}"));
        }

        private void AppendSource(MethodDefinition method, Macro macro, StringBuilder builder)
        {
            if (SourceAnnotations == SourceAnnotation.None)
            {
                return;
            }

            foreach (var instruction in macro.Instructions)
            {
                if (SourceAnnotations.HasFlag(SourceAnnotation.CSharp))
                {
                    var point = method.DebugInformation.GetSequencePoint(instruction);
                    if (point != null && !point.IsHidden)
                    {
                        var source = ReadSource(
                            ReadSourceFile(point.Document.Url),
                            point.StartLine,
                            point.StartColumn,
                            point.EndLine,
                            point.EndColumn);
                        foreach (var line in source)
                        {
                            builder.AppendLine($"{SourceIndent}{new Comment(line)}");
                        }
                    }
                }
                if (SourceAnnotations.HasFlag(SourceAnnotation.CIL))
                {
                    builder.AppendLine($"{SourceIndent}{new Comment(instruction.ToString())}");
                }
            }
        }

        private ImmutableArray<string> ReadSourceFile(string path)
        {
            return File.ReadAllLines(path).ToImmutableArray();
        }

        private IEnumerable<string> ReadSource(
            ImmutableArray<string> sourceText,
            int lineStart,
            int columnStart,
            int lineEnd,
            int columnEnd)
        {
            if (lineStart != lineEnd)
            {
                throw new ArgumentException("Don't support multi-line source snippets yet.");
            }
            var line = sourceText[lineStart - 1];
            var subLine = line[(columnStart-1)..(columnEnd-1)];
            return new[] { subLine };
        }

        private void AppendLabels(StringBuilder builder)
        {
            if (LabelMap.GlobalToAddress.Any())
            {
                builder.AppendLine(new Comment("Begin Globals"));
                foreach (var globalPair in LabelMap.GlobalToAddress.OrderBy(p => p.Value))
                {
                    builder.AppendLine($"{globalPair.Key} = {globalPair.Value}");
                }
                builder.AppendLine(new Comment("End Globals"));
            }

            if (LabelMap.LocalToAddress.Any())
            {
                builder.AppendLine(new Comment("Begin Locals"));
                foreach (var localPair in LabelMap.LocalToAddress.OrderBy(p => p.Value))
                {
                    builder.AppendLine($"{localPair.Key} = {localPair.Value}");
                }
                builder.AppendLine(new Comment("End Locals"));
            }

            if (LabelMap.ConstantToValue.Any())
            {
                builder.AppendLine(new Comment("Begin Constants"));
                foreach (var constantPair in LabelMap.ConstantToValue.OrderBy(p => Convert.ToInt32(p.Value)))
                {
                    builder.AppendLine($"{constantPair.Key} = {constantPair.Value}");
                }
                builder.AppendLine(new Comment("End Constants"));
            }


            if (LabelMap.TypeToString.Any() || LabelMap.SizeToValue.Any())
            {
                builder.AppendLine(new Comment("Begin Types"));
                foreach (var typePair in LabelMap.TypeToString.OrderBy(p => p.Value))
                {
                    builder.AppendLine($"{typePair.Key} = {typePair.Value}");
                }
                foreach (var sizePair in LabelMap.SizeToValue)
                {
                    builder.AppendLine($"{sizePair.Key} = {sizePair.Value}");
                }
                builder.AppendLine(new Comment("End Types"));
            }
        }
    }
}
