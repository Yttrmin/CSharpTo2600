#nullable enable
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCSFramework.V2;

namespace VCSCompiler.V2
{
    internal static class AssemblyTemplate
    {
        private static readonly ILabel StartLabel = new BranchTargetLabel("START");

        public static IEnumerable<IAssemblyEntry> Foo(
            Function entryPoint,
            ImmutableArray<Function> nonInlineFunctions,
            ImmutableArray<AssignLabel> labelAssignments)
        {
            yield return new Comment($"Generated on {DateTime.Now:R}");
            yield return new Blank();
            yield return new CpuOp("6502");
            yield return new ProgramCounterAssignOp(0xF000);
            yield return new Blank();
            yield return new IncludeOp("vcs.h");
            yield return new IncludeOp("vil.h");
            yield return new Blank();
            
            foreach (var assignment in labelAssignments)
                yield return assignment;
            yield return new Blank();
            yield return StartLabel;
            foreach (var entry in entryPoint.Body)
                yield return entry;
            
            foreach (var func in nonInlineFunctions)
            {
                yield return new Blank();
                foreach (var entry in func.Body)
                    yield return entry;
            }
            yield return new Blank();
            yield return new ProgramCounterAssignOp(0xFFFC);
            yield return new WordOp(StartLabel);
            yield return new WordOp(StartLabel);
        }

        public static string FooToString(IEnumerable<IAssemblyEntry> program, SourceAnnotation annotations)
        {
            var currentMethodStack = new Stack<MethodDefinition>();
            var builder = new StringBuilder();
            foreach (var entry in program)
            {
                foreach (var str in GetStringFromEntry(entry, currentMethodStack.Count == 0 ? null : currentMethodStack.Peek(), annotations))
                    builder.AppendLine(str);
                switch (entry)
                {
                    case MethodLabel m:
                        currentMethodStack.Push(m.Method);
                        break;
                    case InlineFunction f:
                        currentMethodStack.Push(f.Definition);
                        break;
                    case EndFunction:
                        currentMethodStack.Pop();
                        break;
                }
            }
            return builder.ToString();
        }

        private static IEnumerable<string> GetStringFromEntry(IAssemblyEntry entry, MethodDefinition? method, SourceAnnotation annotations) => entry switch
        {
            IMacroCall mc => GetStringFromMacro(mc, method, annotations),
            MultilineComment mc => mc.Text,
            InlineFunction or EndFunction => Enumerable.Empty<string>(),
            _ => Enumerable.Repeat(entry switch
            {
                Blank => "",
                Comment c => $"// {c.Text}",
                ArrayLetOp a => $".let {a.VariableName} = [{string.Join(", ", a.Elements.Select(GetStringFromExpression))}]",
                IPsuedoOp p => p switch
                {
                    BeginBlock => ".block",
                    EndBlock => ".endblock",
                    AssignLabel al => $"{GetStringFromEntry(al.Label, method, annotations).Single()} = {al.Value}",
                    IncludeOp io => $@".include ""{io.Filename}""",
                    CpuOp co => $@".cpu ""{co.Architecture}""",
                    WordOp wo => $".word {GetStringFromEntry(wo.Label, method, annotations).Single()}",
                    ProgramCounterAssignOp pc => $"* = ${pc.Address:X4}",
                    _ => throw new ArgumentException($"PsuedoOp {entry} is not mapped to a string.")
                },
                IExpression e => GetStringFromExpression(e),
                _ => throw new ArgumentException($"{entry} does not map to a string.")
            }, 1)
        };

        private static string GetStringFromExpression(IExpression expression) => expression switch
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

        private static string GetStringFromLabel(ILabel label) => label switch
        {
            PredefinedGlobalLabel pg => pg.Name,
            ReservedGlobalLabel rg => $"INTERNAL_RESERVED_{rg.Index}",
            GlobalFieldLabel g => $"GLOBAL_{g.Field.Field.DeclaringType.NamespaceAndName()}_{g.Field.Field.Name}",
            LiftedLocalLabel ll => $"LOCAL_{ll.Method}_{ll.Index}",
            LocalLabel l => throw new NotImplementedException(),
            TypeSizeLabel ts => $"SIZE_{ts.Type.Type.NamespaceAndName()}",
            PointerSizeLabel ps => ps.ZeroPage ? "SIZE_SHORT_POINTER" : "SIZE_LONG_POINTER",
            TypeLabel t => $"TYPE_{t.Type.Type.NamespaceAndName()}",
            PointerTypeLabel p => $"TYPE_{p.ReferentType.Type.NamespaceAndName()}_PTR",
            MethodLabel m => $"METHOD_{m.Method.Method.DeclaringType.NamespaceAndName()}_{m.Method.Method.Name}",
            InstructionLabel i => $"IL_{i.Instruction.Instruction.Offset:X4}",
            BranchTargetLabel b => b.Name,
            _ => throw new ArgumentException($"{label} does not map to a string.")
        };

        private static IEnumerable<string> GetStringFromMacro(IMacroCall macroCall, MethodDefinition? method, SourceAnnotation annotations)
        {
            if (method == null)
                throw new ArgumentException($"{nameof(method)} shouldn't be null when processing a macro call");
            foreach (Instruction instruction in macroCall.Instructions)
            {
                if (annotations.HasFlag(SourceAnnotation.CSharp))
                {
                    var point = method.DebugInformation.GetSequencePoint(instruction);
                    if (point != null && !point.IsHidden)
                    {
                        var sourceFile = File.ReadAllLines(point.Document.Url).ToImmutableArray();
                        var source = ReadSource(sourceFile, point.StartLine, point.StartColumn, point.EndLine, point.EndColumn);
                        if (source.Count() == 1)
                            yield return GetStringFromEntry(new Comment(source.Single()), method, annotations).Single();
                        else
                            foreach (var str in GetStringFromEntry(new MultilineComment(source), method, annotations))
                                yield return str;
                    }
                }
                if (annotations.HasFlag(SourceAnnotation.CIL))
                {
                    yield return $"// {instruction}";
                }
            }
            yield return $".{macroCall.Name} {string.Join(", ", macroCall.Parameters.Select(GetStringFromExpression))}";
            if (macroCall is StackMutatingMacroCall stackMutatingMacroCall)
            {
                foreach (var str in GetStringFromEntry(stackMutatingMacroCall.StackOperation.TypeOp, method, annotations))
                    yield return str;
                foreach (var str in GetStringFromEntry(stackMutatingMacroCall.StackOperation.SizeOp, method, annotations))
                    yield return str;
            }
        }

        private static ImmutableArray<string> ReadSource(
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
                return allLines.ToImmutableArray();
            }
            var line = sourceText[lineStart - 1];
            var subLine = line[(columnStart - 1)..(columnEnd - 1)];
            return new[] { subLine }.ToImmutableArray();
        }
    }
}
