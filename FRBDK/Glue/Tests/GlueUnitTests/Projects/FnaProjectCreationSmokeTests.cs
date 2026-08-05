using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GlueUnitTests.TestSupport;
using Npc;
using Npc.ViewModels;
using Shouldly;
using ToolsUtilities;

namespace GlueUnitTests.Projects;

// End-to-end smoke test for new-project creation with the FNA template, mirroring
// NewProjectCreationSmokeTests' Desktop GL MonoGame coverage (see GitHub issue #1894/#1892).
// Drives the real ProjectCreationHelper.MakeNewProject against a checked-in template (via
// LocalSourceFile, so it's network-free for the create step), then runs `dotnet build` on the
// created project.
//
// Contrary to the older comment in NewProjectCreationSmokeTests claiming FNA "needs extra
// toolchains (net7 targeting pack)": the template's csproj actually targets plain net8.0-windows,
// and its FlatRedBall.FNA/FNA/etc references are checked-in DLLs via HintPath (no submodule, no
// exotic SDK workload). It does run `dotnet tool restore` (dotnet-mgcb) before Restore, so the
// first build still needs network, same as the MonoGame template.
//
// Slow (copies a template + restores/builds), so it's tagged Category=BuildSmoke and excluded from
// the fast unit run.
[Trait("Category", "BuildSmoke")]
public class FnaProjectCreationSmokeTests
{
    [Fact]
    public async Task CreateFromTemplate_ThenBuild_ShouldSucceed()
    {
        const string templateName = "FlatRedBallDesktopFnaTemplate";
        var templateDir = Path.Combine(FindTemplatesRoot(), templateName);
        Directory.Exists(templateDir).ShouldBeTrue($"Template not found at {templateDir}");

        using var temp = new TempDir();
        const string newProjectName = "SmokeTestFnaGame";

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

    private static (int exitCode, string output) RunDotnetBuild(string solutionPath) =>
        NestedDotnetCli.Run($"build \"{solutionPath}\" -c Debug");

    // Walks up from the test assembly to the repo's Templates folder.
    private static string FindTemplatesRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Templates");
            if (Directory.Exists(Path.Combine(candidate, "FlatRedBallDesktopFnaTemplate")))
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
            Root = Path.Combine(Path.GetTempPath(), "FrbNpcFnaSmoke_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public void Dispose()
        {
            try { Directory.Delete(Root, recursive: true); }
            catch { /* best-effort cleanup */ }
        }
    }
}
