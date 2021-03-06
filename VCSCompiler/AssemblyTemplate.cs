﻿#nullable enable
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using VCSFramework;

namespace VCSCompiler
{
    internal static class AssemblyTemplate
    {
        private sealed record IndentInfo(int DeltaIndent = 0, int? AbsoluteIndent = null);
        private static readonly ILabel StartLabel = new BranchTargetLabel("START");

        public static IEnumerable<IAssemblyEntry> GenerateProgram(
            Function entryPoint,
            ImmutableArray<Function> nonInlineFunctions,
            ImmutableArray<LabelAssign> labelAssignments,
            ImmutableArray<(RomDataGlobalLabel, ImmutableArray<byte>)> allRomData)
        {
            yield return new Comment($"Generated on {DateTime.Now:R}");
            yield return new Blank();
            yield return new CpuOp("6502");
            yield return new ProgramCounterAssign(0xF000);
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
            foreach (var (label, data) in allRomData)
            {
                yield return label;
                foreach (var dataByte in data)
                    yield return new ByteOp(ImmutableArray.Create(dataByte));
            }
            yield return new Blank();
            yield return new ProgramCounterAssign(0xFFFC);
            yield return new WordOp(StartLabel);
            yield return new WordOp(StartLabel);
        }

        public static string ProgramToString(IEnumerable<IAssemblyEntry> program, SourceAnnotation annotations)
        {
            const string IndentString = "\t";
            var currentMethodStack = new Stack<MethodDefinition>();
            var builder = new StringBuilder();
            var indentLevel = 0;
            foreach (var entry in program)
            {
                var indentInfo = GetIndentInfo(entry, indentLevel);
                foreach (var str in GetStringFromEntry(entry, currentMethodStack.Count == 0 ? null : currentMethodStack.Peek(), annotations))
                    builder.AppendLine($"{string.Join(string.Empty, Enumerable.Repeat(IndentString, indentInfo.AbsoluteIndent ?? indentLevel))}{str}");
                indentLevel += indentInfo.DeltaIndent;

                switch (entry)
                {
                    case FunctionLabel m:
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

            static IndentInfo GetIndentInfo(IAssemblyEntry entry, int indentLevel) => entry switch
            {
                InstructionLabel => new(AbsoluteIndent: 0),
                InlineFunction or FunctionLabel => new(DeltaIndent: 1),
                EndFunction => new(DeltaIndent: -1),
                _ => new()
            };
        }

        private static IEnumerable<string> GetStringFromEntry(IAssemblyEntry entry, MethodDefinition? method, SourceAnnotation annotations) => entry switch
        {
            IMacroCall mc => GetStringFromMacro(mc, method, annotations),
            InlineAssembly ia => ia.Assembly,
            InlineFunction or EndFunction => Enumerable.Empty<string>(),
            // @TODO - Check if comments contain "/*" or "*/" already and fallback to "//" ?
            MultilineComment mc => mc.Text.Prepend("/*").Append("*/"),
            IPreprocessedEntry => throw new InvalidOperationException($"An {nameof(IPreprocessedEntry)} made it to assembly emitting, this should've been caught during optimization."),
            _ => Enumerable.Repeat(entry switch
            {
                ArrayLetOp a => $".let {a.VariableName} = [{string.Join(", ", a.Elements.Select(e => GetStringFromEntry(e, method, annotations).Single()))}]",
                Blank => "",
                Comment c => $"// {c.Text}",
                LabelAssign al => $"{GetStringFromEntry(al.Label, method, annotations).Single()} = {GetStringFromEntry(al.Value, method, annotations).Single()}",
                ProgramCounterAssign pc => $"* = ${pc.Address:X4}",
                IPseudoOp p => p switch
                {
                    BeginBlock => ".block",
                    ByteOp bo => $".byte {string.Join(",", bo.Bytes.Select(b => $"${b:X2}"))}",
                    CpuOp co => $@".cpu ""{co.Architecture}""",
                    EndBlock => ".endblock",
                    IncludeOp io => $@".include ""{io.Filename}""",
                    WordOp wo => $".word {GetStringFromEntry(wo.Label, method, annotations).Single()}",
                    _ => throw new ArgumentException($"PsuedoOp {entry} is not mapped to a string.")
                },
                IExpression expression => expression switch
                {
                    ArrayAccess aao => $"{aao.VariableName}[{aao.Index}]",
                    Constant c => c.Value switch
                    {
                        bool b => Convert.ToString(b),
                        byte b => Convert.ToString(b),
                        FormattedByte fb => fb.ToString(),
                        int i => Convert.ToString(i),
                        _ => throw new ArgumentException($"No support for constant of type {c.Value.GetType()}")
                    },
                    IFunctionCall fc => $"{fc.Name}({string.Join(", ", fc.Parameters.Select(e => GetStringFromEntry(e, method, annotations).Single()))})",
                    ILabel label => label switch
                    {
                        ArgumentGlobalLabel a => $"ARG_{a.Method.DeclaringType.NamespaceAndName()}_{a.Method.SafeName()}_{a.Index}",
                        BranchTargetLabel b => b.Name,
                        FunctionLabel m => $"FUNCTION_{m.Method.DeclaringType.NamespaceAndName()}_{m.Method.SafeName()}",
                        GlobalFieldLabel g => $"GLOBAL_{g.Field.DeclaringType.NamespaceAndName()}_{g.Field.Field.Name}",
                        InstructionLabel i => $"IL_{i.Instruction.Instruction.Offset:X4}",
                        LocalGlobalLabel l => $"LOCAL_{l.Method.DeclaringType.NamespaceAndName()}_{l.Method.SafeName()}_{l.Index}",
                        PointerGlobalSizeLabel pg => $"PTR_GLOBAL_SIZE_{GetStringFromEntry(pg.Global, method, annotations).Single()}",
                        PointerSizeLabel ps => ps.ZeroPage ? "SIZE_SHORT_POINTER" : "SIZE_LONG_POINTER",
                        PointerTypeLabel p => $"TYPE_{p.ReferentType.NamespaceAndName()}_PTR",
                        PredefinedGlobalLabel pg => pg.Name,
                        ReservedGlobalLabel rg => $"INTERNAL_RESERVED_{rg.Index}",
                        ReturnValueGlobalLabel rv => $"RETVAL_{rv.Method.DeclaringType.NamespaceAndName()}_{rv.Method.SafeName()}",
                        RomDataGlobalLabel rdgl => $"ROMDATA_{rdgl.GeneratorMethod.DeclaringType.NamespaceAndName()}_{rdgl.GeneratorMethod.SafeName()}",
                        ThisPointerGlobalLabel t => $"THIS_PTR_{t.Method.DeclaringType.NamespaceAndName()}_{t.Method.SafeName()}",
                        TypeLabel t => $"TYPE_{t.Type.NamespaceAndName()}",
                        TypeSizeLabel ts => $"SIZE_{ts.Type.NamespaceAndName()}",
                        _ => throw new ArgumentException($"Label {label} does not map to a string.")
                    },
                    _ => throw new ArgumentException($"Expression {expression} does not map to a string.")
                },
                _ => throw new ArgumentException($"Entry {entry} does not map to a string.")
            }, 1)
        };

        private static IEnumerable<string> GetStringFromMacro(IMacroCall macroCall, MethodDefinition? method, SourceAnnotation annotations)
        {
            if (method == null)
                throw new ArgumentException($"{nameof(method)} shouldn't be null when processing a macro call");
            foreach (Instruction instruction in macroCall.Instructions)
            {
                if (instruction.OpCode == CilInstructionCompiler.NopInst.OpCode && instruction.Operand == CilInstructionCompiler.NopInst.Operand)
                    continue;
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
            yield return $".{macroCall.Name} {string.Join(", ", macroCall.Parameters.Select(e => GetStringFromEntry(e, method, annotations).Single()))}";
            if (macroCall is StackMutatingMacroCall stackMutatingMacroCall)
            {
                var typeFirst = !stackMutatingMacroCall.MacroCall.GetType().GetCustomAttributes(false).Any(a => a.GetType().FullName == typeof(SizeFirstAttribute).FullName);
                var typeAssignment = GetStringFromEntry(stackMutatingMacroCall.StackOperation.TypeOp, method, annotations);
                var sizeAssignment = GetStringFromEntry(stackMutatingMacroCall.StackOperation.SizeOp, method, annotations);
                var assignments = typeFirst ? typeAssignment.Concat(sizeAssignment) : sizeAssignment.Concat(typeAssignment);
                foreach (var assignment in assignments)
                    yield return assignment;
            }

            static ImmutableArray<string> ReadSource(
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
}
