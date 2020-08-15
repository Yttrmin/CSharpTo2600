#nullable enable
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCSFramework.V2;

namespace VCSCompiler.V2
{
    internal class AssemblyWriter
    {
        private readonly ImmutableDictionary<MethodDefinition, ImmutableArray<AssemblyEntry>> CompiledMethods;
        private readonly Lazy<string> AssemblyText;
        private static readonly string Indent = "    ";
        private static readonly string StackIndex = "  ";

        public AssemblyWriter(Dictionary<MethodDefinition, ImmutableArray<AssemblyEntry>> compiledMethods)
        {
            CompiledMethods = compiledMethods.ToImmutableDictionary();
            AssemblyText = new(BuildAssemblyText, false);
        }

        public void WriteToFile(string path)
        {
            throw new NotImplementedException();
        }

        public void WriteToConsole()
        {
            Console.WriteLine(AssemblyText.Value);
        }

        private string BuildAssemblyText()
        {
            var builder = new StringBuilder();
            builder.AppendLine($"// Generated on {DateTime.Now:R}");
            builder.AppendLine();
            builder.AppendLine(@".cpu ""6502""");
            builder.AppendLine(@".format ""flat""");
            builder.AppendLine("* = $F000");
            builder.AppendLine();

            builder.AppendLine(new Comment("Begin function definitions."));
            builder.AppendLine(LabelGenerator.Start);
            // Entry point needs to come first so we execute it after init/clear.
            var entryPointPair = CompiledMethods.Single(it => it.Value.OfType<EntryPoint>().Any());
            AppendMethodText(entryPointPair, builder);
            foreach (var pair in CompiledMethods.Where(it => !it.Equals(entryPointPair)))
            {
                builder.AppendLine();
                AppendMethodText(pair, builder);
            }

            builder.AppendLine(new Comment("End function definitions."));
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
                var indent = entry is InstructionLabel ? "" : Indent;
                builder.AppendLine($"{indent}{entry}");
                if (entry is Macro macro)
                {
                    foreach (var stackLet in macro.StackLets)
                    {
                        builder.AppendLine($"{StackIndex}{stackLet}");
                    }
                }
            }
            builder.AppendLine(new Comment($"End {methodPair.Key.FullName}"));
        }
    }
}
