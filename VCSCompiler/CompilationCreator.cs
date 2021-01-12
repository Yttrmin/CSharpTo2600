#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace VCSCompiler
{
	internal static class CompilationCreator
	{
		// netstandard 1.5 needed for Assembly.Location
		private static readonly MetadataReference RuntimeReference = MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("System.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")).Location);
		private static readonly MetadataReference CoreLibReference = MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location);
		private static readonly MetadataReference MsCorLibReference = MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e")).Location);
		private static readonly MetadataReference FrameworkReference = MetadataReference.CreateFromFile(typeof(VCSFramework.NByte).GetTypeInfo().Assembly.Location);
		private static readonly MetadataReference[] MetadataReferences = new[] { RuntimeReference, CoreLibReference, MsCorLibReference, FrameworkReference };

		public static CSharpCompilation CreateFromFilePaths(IEnumerable<string> filePaths, string? mainTypeName)
		{
			var syntaxTrees = filePaths.Select(Parse);

			var outputType = mainTypeName != null ? OutputKind.ConsoleApplication : OutputKind.DynamicallyLinkedLibrary;
			var options = new CSharpCompilationOptions(outputType, allowUnsafe: true, optimizationLevel: OptimizationLevel.Release, mainTypeName: mainTypeName);
			var compilation = CSharpCompilation.Create("UserProgram", syntaxTrees, MetadataReferences, options);
			var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error);
			var auditor = AuditorManager.Instance.GetAuditor("Roslyn", AuditTag.Compiler);
			if (errors.Any())
			{
				var stringBuilder = new StringBuilder();
				stringBuilder.AppendLine("Roslyn compilation failed! Errors:");
				stringBuilder.AppendLine(string.Join(Environment.NewLine, errors));
				auditor.RecordEntry(stringBuilder.ToString());

				stringBuilder.Clear();
				stringBuilder.AppendLine("All other messages:");
				var remaining = compilation.GetDiagnostics().Where(d => d.Severity != DiagnosticSeverity.Error).OrderByDescending(d => d.Severity);
				stringBuilder.AppendLine(string.Join(Environment.NewLine, remaining));
				auditor.RecordEntry(stringBuilder.ToString());
				throw new FatalCompilationException("Roslyn compilation must succeed in order to compile for VCS.");
			}

			auditor.RecordEntry("Roslyn compilation successful!");
			return compilation;
		}

		private static SyntaxTree Parse(string filename)
		{
			var fileText = File.ReadAllText(filename);
			var sourceText = SourceText.From(fileText, Encoding.UTF8); // Need to specify encoding in order to embed PDB.
			return SyntaxFactory.ParseSyntaxTree(sourceText, new CSharpParseOptions(LanguageVersion.Latest), filename);
		}
	}
}
