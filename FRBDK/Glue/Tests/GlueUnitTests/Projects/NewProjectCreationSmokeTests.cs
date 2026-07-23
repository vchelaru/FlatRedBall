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

// End-to-end smoke test for new-project creation: drives the real ProjectCreationHelper.MakeNewProject
// against a checked-in template (via LocalSourceFile, so it's network-free for the create step), then
// runs `dotnet build` on the created project. This is what actually catches "creating a project is
// broken" (bad template, missed namespace, non-compiling output) — see GitHub issue #1892.
//
// Slow (copies a template + restores/builds), so it's tagged Category=BuildSmoke and excluded from the
// fast unit run. `dotnet build` still needs network for the first NuGet restore.
[Trait("Category", "BuildSmoke")]
public class NewProjectCreationSmokeTests
{
    // Templates known to build on a plain Windows .NET install. The others in EmptyTemplates need extra
    // toolchains and are intentionally not covered here yet: Android (android workload), iOS (macOS),
    // Web/Kni (wasm workload), FNA (net7 targeting pack) — add them as their build prerequisites are
    // confirmed available in CI.
    [Theory]
    [InlineData("FlatRedBallDesktopGlMonoGameTemplate")]
    public async Task CreateFromTemplate_ThenBuild_ShouldSucceed(string templateName)
    {
        var templateDir = Path.Combine(FindTemplatesRoot(), templateName);
        Directory.Exists(templateDir).ShouldBeTrue($"Template not found at {templateDir}");

        using var temp = new TempDir();
        const string newProjectName = "SmokeTestGame";

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
                    FriendlyName = templateName,
                    Namespace = templateName,          // the string that gets replaced by the project name
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
            if (Directory.Exists(Path.Combine(candidate, "FlatRedBallDesktopGlMonoGameTemplate")))
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
            Root = Path.Combine(Path.GetTempPath(), "FrbNpcSmoke_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public void Dispose()
        {
            try { Directory.Delete(Root, recursive: true); }
            catch { /* best-effort cleanup */ }
        }
    }
}
