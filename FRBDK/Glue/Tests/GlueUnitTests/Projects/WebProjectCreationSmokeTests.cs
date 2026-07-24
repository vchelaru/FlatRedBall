using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Npc;
using Npc.ViewModels;
using Shouldly;
using ToolsUtilities;

namespace GlueUnitTests.Projects;

// End-to-end smoke test for new-project creation of the Web (KNI/MonoGame BlazorGL) template: drives the
// real ProjectCreationHelper.MakeNewProject against a checked-in template (via LocalSourceFile, so it's
// network-free for the create step), then runs `dotnet build` on the created project. Mirrors
// NewProjectCreationSmokeTests.CreateFromTemplate_ThenBuild_ShouldSucceed for
// FlatRedBallDesktopGlMonoGameTemplate — see that file/GitHub issue #1892 for background. Kept in its own
// file (rather than added as another [InlineData] there) to avoid merge conflicts with sibling agents
// adding coverage for other templates in parallel — see issue #1894.
//
// Slow (copies a template + restores/builds), so it's tagged Category=BuildSmoke and excluded from the
// fast unit run. `dotnet build` still needs network for the first NuGet restore, and for the Web/BlazorGL
// template specifically also needs the `wasm-tools` workload installed (`dotnet workload install
// wasm-tools`) since the .csproj is Microsoft.NET.Sdk.BlazorWebAssembly targeting net8.0.
[Trait("Category", "BuildSmoke")]
public class WebProjectCreationSmokeTests
{
    private const string TemplateName = "FlatRedBallWebTemplate";

    [Fact]
    public async Task CreateFromTemplate_ThenBuild_ShouldSucceed()
    {
        var templateDir = Path.Combine(FindTemplatesRoot(), TemplateName);
        Directory.Exists(templateDir).ShouldBeTrue($"Template not found at {templateDir}");

        using var temp = new TempDir();
        const string newProjectName = "SmokeTestWebGame";

        var recordingNotifier = new RecordingNotifier();
        var originalNotifier = ProjectCreationHelper.Notifier;
        ProjectCreationHelper.Notifier = recordingNotifier;
        try
        {
            var viewModel = new NewProjectViewModel
            {
                ProjectName = newProjectName,
                ProjectDestinationLocation = temp.Root,
                IsCreateProjectDirectoryChecked = true,
                UseLocalCopy = true,
                OpenSlnFolderAfterCreation = false,
                IsOpenNewProjectWizardChecked = false,
                SelectedProject = new PlatformProjectInfo
                {
                    FriendlyName = TemplateName,
                    Namespace = TemplateName,          // the string that gets replaced by the project name
                    LocalSourceFile = new FilePath(templateDir),
                    SupportedInGlue = true,
                },
            };

            var succeeded = await ProjectCreationHelper.MakeNewProject(viewModel);

            succeeded.ShouldBeTrue();
            // No message boxes should have fired — any message here means creation hit an error path.
            recordingNotifier.Messages.ShouldBeEmpty();

            var createdSln = Path.Combine(viewModel.FinalDirectory, newProjectName + ".sln");
            File.Exists(createdSln).ShouldBeTrue($"Expected renamed solution at {createdSln}");

            var (exitCode, output) = RunDotnetBuild(createdSln);
            exitCode.ShouldBe(0, $"dotnet build failed for the created project:\n{output}");
        }
        finally
        {
            ProjectCreationHelper.Notifier = originalNotifier;
        }
    }

    private static (int exitCode, string output) RunDotnetBuild(string solutionPath)
    {
        var startInfo = new ProcessStartInfo("dotnet", $"build \"{solutionPath}\" -c Debug")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, output);
    }

    // Walks up from the test assembly to the repo's Templates folder.
    private static string FindTemplatesRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Templates");
            if (Directory.Exists(Path.Combine(candidate, TemplateName)))
            {
                return candidate;
            }
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate the repo 'Templates' folder above " + AppContext.BaseDirectory);
    }

    private sealed class RecordingNotifier : IProjectCreationNotifier
    {
        public List<string> Messages { get; } = new();
        public void ShowMessage(string message) => Messages.Add(message);
    }

    private sealed class TempDir : IDisposable
    {
        public string Root { get; }

        public TempDir()
        {
            Root = Path.Combine(Path.GetTempPath(), "FrbNpcWebSmoke_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public void Dispose()
        {
            try { Directory.Delete(Root, recursive: true); }
            catch { /* best-effort cleanup */ }
        }
    }
}
