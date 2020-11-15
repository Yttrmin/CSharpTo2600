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
    /*internal class AssemblyWriter
    {
        private readonly ImmutableDictionary<MethodDefinition, ImmutableArray<AssemblyEntry>> CompiledMethods;
        private readonly LabelMap LabelMap;
        private readonly SourceAnnotation SourceAnnotations;
        private readonly Lazy<string> AssemblyText;
        private static readonly string Indent = "    ";
        private static readonly string SourceIndent = "   ";
        private static readonly string StackIndex = "        ";

        public AssemblyWriter(
            ImmutableDictionary<MethodDefinition, ImmutableArray<AssemblyEntry>> compiledMethods,
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
                "-l",
                Path.Combine(Path.GetDirectoryName(asmPath)!, "labeltest.asm"),
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
                    // @TODO - Some instructions are multiline (e.g. ldstr, though we wouldn't emit that), so comments will break the asm file.
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
                // Multi-line comment support.
                var allLines = new List<string>();
                var startLine = sourceText[lineStart - 1];
                var subStartLine = startLine[(columnStart - 1)..];
                allLines.Add(subStartLine);
                for (var i = lineStart; i < lineEnd - 1; i++)
                {
                    allLines.Add(sourceText[i]);
                }
                var endLine = sourceText[lineEnd - 1];
                var subEndLine = endLine[..(columnEnd - 1)];
                allLines.Add(subEndLine);
                return allLines;
            }
            var line = sourceText[lineStart - 1];
            var subLine = line[(columnStart-1)..(columnEnd-1)];
            return new[] { subLine };
        }

        private IEnumerable<string> GetStringFromEntry(IAssemblyEntry entry) => entry switch
        {
            IMacroCall mc => GetStringFromMacro(mc),
            MultilineComment mc => mc.Text,
            _ => Enumerable.Repeat(entry switch
            {
                Comment c => $"// {c.Text}",
                ArrayLetOp a => $".let {a.VariableName} = [{string.Join(", ", a.Elements.Select(GetStringFromExpression))}]",
                IExpression e => GetStringFromExpression(e),
                _ => throw new ArgumentException($"{entry} does not map to a string.")
            }, 1)
        };

        private string GetStringFromExpression(IExpression expression) => expression switch
        {
            Constant c => c.Value switch
            {
                byte b => Convert.ToString(b),
                bool b => Convert.ToString(b),
                _ => throw new ArgumentException($"No support for constant of type {c.Value.GetType()}")
            },
            ArrayAccessOp aao => $"{aao.VariableName}[{aao.Index}]",
            IFunctionCall fc => $"{fc.Name}({string.Join(", ", fc.Parameters.Select(GetStringFromExpression))})",
            ILabel l => GetStringFromLabel(l),
            _ => throw new ArgumentException($"{expression} does not map to a string.")
        };

        private string GetStringFromLabel(ILabel label) => label switch
        {
            GlobalFieldLabel g => $"GLOBAL_{g.Field.Field.DeclaringType.NamespaceAndName()}_{g.Field.Field.Name}",
            LiftedLocalLabel ll => $"LOCAL_{ll.Method}_{ll.Index}",
            LocalLabel l => throw new NotImplementedException(),
            TypeSizeLabel ts => $"SIZE_{ts.Type.Type.NamespaceAndName()}",
            PointerSizeLabel ps => ps.ZeroPage ? "SIZE_SHORT_POINTER" : "SIZE_LONG_POINTER",
            TypeLabel t => $"TYPE_{t.Type.Type.NamespaceAndName()}",
            PointerTypeLabel p => $"TYPE_{p.ReferentType.Type.NamespaceAndName()}_PTR",
            MethodLabel m => $"METHOD_{m.Method.Method.DeclaringType.NamespaceAndName()}_{m.Method.Method.Name}",
            InstructionLabel i => $"IL_{i.Instruction.Instruction.Offset:X4}",
            _ => throw new ArgumentException($"{label} does not map to a string.")
        };

        private IEnumerable<string> GetStringFromMacro(IMacroCall macroCall)
        {
            if (SourceAnnotations.HasFlag(SourceAnnotation.CSharp))
            {

            }
            if (SourceAnnotations.HasFlag(SourceAnnotation.CIL))
            {

            }
            yield return $".{macroCall.Name} {string.Join(", ", macroCall.Parameters.Select(GetStringFromExpression))}";
            if (macroCall is StackMutatingMacroCall stackMutatingMacroCall)
            {
                foreach (var str in GetStringFromEntry(stackMutatingMacroCall.StackOperation.TypeOp))
                    yield return str;
                foreach (var str in GetStringFromEntry(stackMutatingMacroCall.StackOperation.SizeOp))
                    yield return str;
            }
        }
    }*/
}
