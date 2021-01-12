#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace VILMacroGenerator
{
    [Generator]
    public class MacroGenerator : ISourceGenerator
    {
        public const string VilCategory = "VIL";
        private const string ResultsId = "VIL001";
        private const string GeneratedId = "VIL002";
        private const string HeaderParseFailId = "VIL010";
        private const string GenerateFailId = "VIL011";
        public const string OldPushFormatId = "VIL020";


        private sealed class MacroInfo
        {
            public enum InstructionParamType
            {
                None,
                Single,
                Multiple
            }

            public int PushCount { get; set; }
            public int PopCount { get; set; }
            public InstructionParamType InstructionParam { get; set; } = InstructionParamType.Single;
        }

        public void Execute(GeneratorExecutionContext context)
        {
            InfoDianostic(context, "HELLO!??!?!?!", "hi");
            var vilLines = context.AdditionalFiles.Single(f => f.Path.Contains("vil.h")).GetText()!.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToImmutableArray();
            var generated = FetchMacrosToGenerate(vilLines, context).ToArray();
            foreach (var tuple in generated)
            {
                context.AddSource(tuple.Name, SourceText.From(tuple.Source, Encoding.UTF8));
            }
            context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(ResultsId, "Results", "Generated {0} macros/functions: {1}", VilCategory, DiagnosticSeverity.Warning, true), null, generated.Length, string.Join(", ", generated.Select(p => p.Name).OrderBy(n => n))));
            HelloWorld(context);
            context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("VI0011", "Macro Generator Finished", "See previous diagnostics for results.", "VIL", DiagnosticSeverity.Warning, true), null));
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        private IEnumerable<(string Name, string Source)> FetchMacrosToGenerate(ImmutableArray<string> lines, GeneratorExecutionContext context)
        {
            Header? header = null;
            foreach (var line in lines)
            {
                if (line.Contains("@GENERATE"))
                {
                    try
                    {
                        header = Header.Parse(line, context);
                    }
                    catch (Exception e)
                    {
                        var desc = new DiagnosticDescriptor(HeaderParseFailId, "Header parse failed", "Exception thrown when parsing line \"{0}\": {1}", VilCategory, DiagnosticSeverity.Error, true);
                        context.ReportDiagnostic(Diagnostic.Create(desc, null, line, e.ToString()));
                    }
                }

                var isMacro = line.Contains(".macro");
                var isFunction = line.Contains(".function");
                if (header != null && (isMacro || isFunction))
                {
                    var name = line.Split(' ').FirstOrDefault();
                    string? source;
                    try
                    {
                        source = isMacro ? GenerateMacro(line, header, context)
                            : GenerateFunction(line, header, context);
                    } 
                    catch (Exception e)
                    {
                        var desc = new DiagnosticDescriptor(GenerateFailId, $"{(isMacro ? "Macro" : "Function")} generation failed", "Exception thrown when generating \"{0}\": {1}", VilCategory, DiagnosticSeverity.Error, true);
                        context.ReportDiagnostic(Diagnostic.Create(desc, null, name, e.ToString()));
                        source = null;
                    }
                    if (source != null)
                    {
                        yield return (name, source);
                    }
                    header = null;
                }
            }
        }

        private string? GenerateFunction(string source, Header header, GeneratorExecutionContext context)
        {
            var parts = source.Replace(",", "").Split(' ');
            var functionName = parts.First();
            var csharpName = functionName.Capitalize();

            var variables = parts.Skip(2).ToImmutableArray();
            var paramTypes = variables.Select(TypeNameFromHungarian).ToImmutableArray();
            var typesWithNames = paramTypes.Zip(variables, (Type, Name) => (Type, Name)).ToImmutableArray();

            var constructorParamText = string.Join(", ", typesWithNames.Select(p => $"{p.Type} {p.Name.Capitalize()}").Where(s => !string.IsNullOrEmpty(s)));
            var parametersArrayValues = new StringBuilder();
            foreach (var variable in variables)
                parametersArrayValues.AppendLine($"\t\t\t{variable.Capitalize()},");

            return 
$@"#nullable enable
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace VCSFramework
{{
    public sealed partial record {csharpName}({constructorParamText}) : IFunctionCall
    {{
        string IFunctionCall.Name {{ get; }} = ""{functionName}"";

        ImmutableArray<IExpression> IFunctionCall.Parameters => new IExpression[]
        {{
{parametersArrayValues}
        }}.ToImmutableArray();
    }}
}}";
        }

        private string? GenerateMacro(string source, Header header, GeneratorExecutionContext context)
        {
            var annotationsBuilder = new StringBuilder();
            annotationsBuilder.AppendLine($"\t[PushStack(Count = {(header.TypeParam != null ? 1 : 0)})]");
            annotationsBuilder.AppendLine($"\t[PopStack(Count = {header.PopCount})]");
            annotationsBuilder.AppendLine($"\t[ReservedBytes(Count = {header.ReservedBytes})]");

            var parts = source.Replace(",", "").Split(' ');
            var macroName = parts.First();
            var csharpName = macroName.Capitalize();

            var variables = parts.Skip(2).ToImmutableArray();
            var paramTypes = variables.Select(TypeNameFromHungarian).ToImmutableArray();
            var typesWithNames = paramTypes.Zip(variables, (Type, Name) => (Type, Name)).ToImmutableArray();

            var instructionProperty = header.InstructionParam switch
            {
                Header.InstructionParamType.Single => "Instruction SourceInstruction",
                Header.InstructionParamType.Multiple => "IEnumerable<Instruction> SourceInstructions",
                _ => ""
            };
            var instructionInterfaceProperty = header.InstructionParam switch
            {
                Header.InstructionParamType.Single => "new Inst[] { SourceInstruction }.ToImmutableArray()",
                Header.InstructionParamType.Multiple => "SourceInstructions.Select(i => (Inst)i).ToImmutableArray()",
                _ => "ImmutableArray<Inst>.Empty"
            };

            var constructorParamText = string.Join(", ", typesWithNames.Select(p => $"{p.Type} {p.Name.Capitalize()}").Prepend(instructionProperty).Where(s => !string.IsNullOrEmpty(s)));

            var parametersArrayValues = new StringBuilder();
            foreach (var variable in variables)
                parametersArrayValues.AppendLine($"\t\t\t{variable.Capitalize()},");

            return
$@"#nullable enable
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace VCSFramework
{{
{annotationsBuilder}
    public sealed record {csharpName}({constructorParamText}) : IMacroCall
    {{
        string IMacroCall.Name {{ get; }} = ""{macroName}"";

        ImmutableArray<Inst> IMacroCall.Instructions => {instructionInterfaceProperty};

        ImmutableArray<IExpression> IMacroCall.Parameters => new IExpression[]
        {{
{parametersArrayValues}
        }}.ToImmutableArray();

        void IMacroCall.PerformStackOperation(IStackTracker stackTracker)
        {{
            {$"stackTracker.Pop({header.PopCount});"}
            {(header.TypeParam != null ? $"stackTracker.Push({header.TypeParam.CSharpCode}, {header.SizeParam?.CSharpCode});" : "return;")}
        }}
    }}
}}";
        }

        private string TypeNameFromHungarian(string variableName)
        {
            if (variableName.EndsWith("StackType", StringComparison.CurrentCultureIgnoreCase))
                return "StackTypeArrayAccess";
            else if (variableName.EndsWith("StackSize", StringComparison.CurrentCultureIgnoreCase))
                return "StackSizeArrayAccess";
            else if (variableName.EndsWith("PointerType", StringComparison.CurrentCultureIgnoreCase))
                return "PointerTypeLabel";
            else if (variableName.EndsWith("PointerSize", StringComparison.CurrentCultureIgnoreCase))
                return "PointerSizeLabel";
            else if (variableName.EndsWith("Type", StringComparison.CurrentCultureIgnoreCase))
                return "ITypeLabel";
            else if (variableName.EndsWith("Size", StringComparison.CurrentCultureIgnoreCase))
                return "ISizeLabel";
            else if (variableName.EndsWith("RomDataGlobal", StringComparison.CurrentCultureIgnoreCase))
                return "RomDataGlobalLabel";
            else if (variableName.EndsWith("Global", StringComparison.CurrentCultureIgnoreCase))
                return "IGlobalLabel";
            else if (variableName.EndsWith("Local", StringComparison.CurrentCultureIgnoreCase))
                return "LiftedLocalLabel";
            else if (variableName.EndsWith("Constant", StringComparison.CurrentCultureIgnoreCase))
                return "Constant";
            else if (variableName.EndsWith("BranchTarget", StringComparison.CurrentCultureIgnoreCase))
                return "IBranchTargetLabel";
            else if (variableName.EndsWith("Method", StringComparison.CurrentCultureIgnoreCase))
                return "FunctionLabel";
            else if (variableName.EndsWith("Function", StringComparison.CurrentCultureIgnoreCase))
                return "FunctionLabel";
            else if (variableName.EndsWith("Expression", StringComparison.CurrentCultureIgnoreCase))
                return "IExpression";
            else
                throw new ArgumentException($"Could not determine type of: {variableName}");
        }

        private void InfoDianostic(GeneratorExecutionContext context, string title, string message)
        {
            context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("VI0012", title, message, "VIL", DiagnosticSeverity.Info, true), null));
        }

        private void HelloWorld(GeneratorExecutionContext context)
        {
            // begin creating the source we'll inject into the users compilation
            var sourceBuilder = new StringBuilder(@"
using System;
namespace HelloWorldGenerated
{
    public static class HelloWorld
    {
        public static void SayHello() 
        {
            Console.WriteLine(""Hello from generated code!"");
            Console.WriteLine(""The following syntax trees existed in the compilation that created this program:"");
");

            // using the context, get a list of syntax trees in the users compilation
            var syntaxTrees = context.Compilation.SyntaxTrees;

            // add the filepath of each tree to the class we're building
            foreach (SyntaxTree tree in syntaxTrees)
            {
                sourceBuilder.AppendLine($@"Console.WriteLine(@"" - {tree.FilePath}"");");
            }

            // finish creating the source to inject
            sourceBuilder.Append(@"
        }
    }
}");

            // inject the created source into the users compilation
            context.AddSource("helloWorldGenerator", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
            context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("TT0000", "AAA", "BBB", "TEST", DiagnosticSeverity.Warning, true), null));
        }
    }

    static class Extensions
    {
        public static string Capitalize(this string @this)
            => $"{@this.First().ToString().ToUpper()}{@this.Substring(1)}";

        public static string DelimitIfAny(this string @this, string delimiter = ", ")
            => @this.Any() ? delimiter : string.Empty;
    }
}
