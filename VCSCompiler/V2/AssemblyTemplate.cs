#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
            var builder = new StringBuilder();
            foreach (var entry in program)
            {
                foreach (var str in GetStringFromEntry(entry, annotations))
                    builder.AppendLine(str);
            }
            return builder.ToString();
        }

        private static IEnumerable<string> GetStringFromEntry(IAssemblyEntry entry, SourceAnnotation annotations) => entry switch
        {
            IMacroCall mc => GetStringFromMacro(mc, annotations),
            MultilineComment mc => mc.Text,
            InlineFunction => Enumerable.Empty<string>(),
            _ => Enumerable.Repeat(entry switch
            {
                Blank => "",
                Comment c => $"// {c.Text}",
                ArrayLetOp a => $".let {a.VariableName} = [{string.Join(", ", a.Elements.Select(GetStringFromExpression))}]",
                IPsuedoOp p => p switch
                {
                    BeginBlock => ".block",
                    EndBlock => ".endblock",
                    AssignLabel al => $"{GetStringFromEntry(al.Label, annotations).Single()} = {al.Value}",
                    IncludeOp io => $@".include ""{io.Filename}""",
                    CpuOp co => $@".cpu ""{co.Architecture}""",
                    WordOp wo => $".word {GetStringFromEntry(wo.Label, annotations).Single()}",
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

        private static IEnumerable<string> GetStringFromMacro(IMacroCall macroCall, SourceAnnotation annotations)
        {
            if (annotations.HasFlag(SourceAnnotation.CSharp))
            {

            }
            if (annotations.HasFlag(SourceAnnotation.CIL))
            {

            }
            yield return $".{macroCall.Name} {string.Join(", ", macroCall.Parameters.Select(GetStringFromExpression))}";
            if (macroCall is StackMutatingMacroCall stackMutatingMacroCall)
            {
                foreach (var str in GetStringFromEntry(stackMutatingMacroCall.StackOperation.TypeOp, annotations))
                    yield return str;
                foreach (var str in GetStringFromEntry(stackMutatingMacroCall.StackOperation.SizeOp, annotations))
                    yield return str;
            }
        }
    }
}
