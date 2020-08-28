#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace VILMacroGenerator
{
    [Generator]
    public class MacroGenerator : ISourceGenerator
    {
        private SourceGeneratorContext Context;

        public void Execute(SourceGeneratorContext context)
        {
            Context = context;
            InfoDianostic(context, "HELLO!??!?!?!", "hi");
            var vilLines = context.AdditionalFiles.Single(f => f.Path.Contains("vil.h")).GetText()!.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToImmutableArray();
            foreach (var tuple in FetchMacrosToGenerate(vilLines))
            {
                context.AddSource(tuple.Name, SourceText.From(tuple.Source, Encoding.UTF8));
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("VIL0010", "Macro Generated", tuple.Name, "VIL", DiagnosticSeverity.Warning, true), null));
            }
            HelloWorld(context);
            context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("VI0011", "Macro Generator Finished", "See previous diagnostics for results.", "VIL", DiagnosticSeverity.Warning, true), null));
        }

        public void Initialize(InitializationContext context)
        {
        }

        private IEnumerable<(string Name, string Source)> FetchMacrosToGenerate(ImmutableArray<string> lines)
        {
            var generateNextMacro = false;
            foreach (var line in lines)
            {
                if (line.Contains("@GENERATE"))
                {
                    generateNextMacro = true;
                }

                if (generateNextMacro && line.Contains(".macro"))
                {
                    var macroName = line.Split(' ').FirstOrDefault();
                    var source = GenerateMacro(line);
                    if (source != null)
                    {
                        yield return (macroName, source);
                    }
                    generateNextMacro = false;
                }
            }
        }

        private string? GenerateMacro(string source)
        {
            var parts = source.Replace(",", "").Split(' ');
            var macroName = parts.FirstOrDefault();
            if (macroName == null)
                return null;
            var csharpName = macroName.Capitalize();
            var variables = parts.Skip(2).ToImmutableArray();
            var paramTypes = variables.Select(TypeNameFromHungarian).ToImmutableArray();
            InfoDianostic(Context, "README", string.Join(" ", variables));
            var typesWithNames = paramTypes.Zip(variables, (Type, Name) => (Type, Name)).ToImmutableArray();

            var constructorParamText = string.Join(", ", typesWithNames.Select(p => $"{p.Type} {p.Name}"));
            var baseConstructorParamText = string.Join(", ", variables);
            var publicProperties = string.Join(Environment.NewLine,
                typesWithNames.Zip(Enumerable.Range(0, typesWithNames.Length), (pair, index) =>
            {
                return $"\t\tpublic {pair.Type} {pair.Name.Capitalize()} => ({pair.Type})Params[{index}];";
            }));

            var deconstructParamText = string.Join(", ", typesWithNames.Select(p => $"out {p.Type} {p.Name}"));

            var deconstructAssignments = string.Join(Environment.NewLine, variables.Select(v => $"\t\t\t{v} = {v.Capitalize()};"));

            return $@"
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Immutable;
using VCSFramework.V2;

namespace VCSFramework.V2
{{
    public sealed partial record {csharpName} : Macro
    {{
        public {csharpName}(Instruction instruction{constructorParamText.DelimitIfAny()}{constructorParamText})
            : base(instruction, new MacroLabel(""{macroName}""){baseConstructorParamText.DelimitIfAny()}{baseConstructorParamText}) {{}}

{publicProperties}

        public void Deconstruct(out ImmutableArray<Instruction> instructions{deconstructParamText.DelimitIfAny()}{deconstructParamText})
        {{
            instructions = Instructions;
{deconstructAssignments}
        }}
    }}
}}";
        }

        private string TypeNameFromHungarian(string variableName)
        {
            if (variableName.EndsWith("Type", StringComparison.CurrentCultureIgnoreCase))
                return "TypeLabel";
            else if (variableName.EndsWith("Size", StringComparison.CurrentCultureIgnoreCase))
                return "SizeLabel";
            else if (variableName.EndsWith("Global", StringComparison.CurrentCultureIgnoreCase))
                return "GlobalLabel";
            else if (variableName.EndsWith("Constant", StringComparison.CurrentCultureIgnoreCase))
                return "ConstantLabel";
            else
                throw new ArgumentException($"Could not determine type of: {variableName}");
        }

        private void InfoDianostic(SourceGeneratorContext context, string title, string message)
        {
            context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("VI0012", title, message, "VIL", DiagnosticSeverity.Warning, true), null));
        }

        private void HelloWorld(SourceGeneratorContext context)
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
