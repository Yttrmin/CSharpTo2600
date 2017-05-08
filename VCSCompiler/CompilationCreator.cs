using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;

namespace VCSCompiler
{
	internal static class CompilationCreator
	{
		private static readonly CompilationOptions Options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

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
				metadataReferences: new MetadataReference[] { },
				compilationOptions: Options,
				documents: await allDocumentInfo);

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
			var project = workspace.AddProject(await projectInfo);

			return await CompileProject(project);
		}
    }
}
