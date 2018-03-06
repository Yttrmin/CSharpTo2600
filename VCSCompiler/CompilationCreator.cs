﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;

namespace VCSCompiler
{
	internal static class CompilationCreator
	{
		// netstandard 1.5 needed for Assembly.Location
		private static readonly MetadataReference RuntimeReference = MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("System.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")).Location);
		private static readonly MetadataReference CoreLibReference = MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location);
		private static readonly MetadataReference MsCorLibReference = MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e")).Location);
		private static readonly MetadataReference FrameworkReference = MetadataReference.CreateFromFile(typeof(VCSFramework.NByte).GetTypeInfo().Assembly.Location);
		private static readonly CompilationOptions Options = new CSharpCompilationOptions(OutputKind.ConsoleApplication, allowUnsafe: true);

		private static async Task<DocumentInfo> CreateDocumentInfo(FileInfo file, ProjectId projectId)
		{
			var textAndVersion = TextAndVersion.Create(SourceText.From(await file.OpenText().ReadToEndAsync()), VersionStamp.Default);
			var documentId = DocumentId.CreateNewId(projectId, "file.Name");
			return DocumentInfo.Create(documentId, file.Name, loader: TextLoader.From(textAndVersion));
		}

		private static async Task<ProjectInfo> CreateProjectInfo(IEnumerable<FileInfo> files)
		{
			var projectId = ProjectId.CreateNewId();
			var documentTasks = new List<Task<DocumentInfo>>();
			foreach (var file in files)
			{
				documentTasks.Add(CreateDocumentInfo(file, projectId));
			}

			var allDocumentInfo = Task.WhenAll(documentTasks);

			var info = ProjectInfo.Create(projectId, VersionStamp.Default, "UserProject", "UserAssembly", "C#",
				metadataReferences: new[] { CoreLibReference, RuntimeReference, MsCorLibReference, FrameworkReference },
				compilationOptions: Options,
				documents: await allDocumentInfo, 
				parseOptions: new CSharpParseOptions(LanguageVersion.Latest));

			return info;
		}

		private static async Task<CSharpCompilation> CompileProject(Project project)
		{
			var compilation = (CSharpCompilation)await project.GetCompilationAsync();
			var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error);
			if (errors.Any())
			{
				Console.WriteLine("Roslyn compilation failed! Errors:");
				foreach(var error in errors)
				{
					Console.WriteLine(error);
				}
				Console.WriteLine("All other messages:");
				var remaining = compilation.GetDiagnostics().Where(d => d.Severity != DiagnosticSeverity.Error).OrderByDescending(d => d.Severity);
				foreach(var message in remaining)
				{
					Console.WriteLine(message);
				}
				throw new FatalCompilationException("Roslyn compilation must succeed in order to compile for VCS.");
			}
			return compilation;
		}

		public static async Task<CSharpCompilation> CreateFromFilePaths(IEnumerable<string> filePaths)
		{
			var allFileInfo = filePaths.Select(path => new FileInfo(path));
			var projectInfo = CreateProjectInfo(allFileInfo);

			var workspace = new AdhocWorkspace();
			if (!workspace.Services.IsSupported("C#"))
			{
				throw new InvalidOperationException("C# not supported. Make sure to include Microsoft.CodeAnalysis.CSharp.Workspaces.");
			}
			var project = workspace.AddProject(await projectInfo);

			return await CompileProject(project);
		}
    }
}
