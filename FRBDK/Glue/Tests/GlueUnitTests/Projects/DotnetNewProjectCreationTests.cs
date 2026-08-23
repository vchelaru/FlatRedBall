using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GlueUnitTests.TestSupport;
using Npc;
using Npc.Data;
using Npc.Managers;
using Npc.ViewModels;
using Shouldly;
using ToolsUtilities;

namespace GlueUnitTests.Projects;

/// <summary>
/// FlatRedBall 2 ships its templates as a nuget package, so creating one is `dotnet new` rather than
/// the download-zip / unzip / rename / rewrite-guids pipeline every other entry in the New Project
/// window uses. The CLI does the naming and guid work itself, so the point of these is as much what
/// does *not* happen as what does.
/// </summary>
public class DotnetNewProjectCreationTests : IDisposable
{
    readonly string _root;
    readonly IDotnetNewService _originalService;
    readonly IProjectCreationNotifier _originalNotifier;
    readonly RecordingDotnetNewService _service = new();
    readonly RecordingNotifier _notifier = new();

    public DotnetNewProjectCreationTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "DotnetNew_" + Guid.NewGuid());
        Directory.CreateDirectory(_root);

        _originalService = ProjectCreationHelper.DotnetNew;
        _originalNotifier = ProjectCreationHelper.Notifier;
        ProjectCreationHelper.DotnetNew = _service;
        ProjectCreationHelper.Notifier = _notifier;
    }

    public void Dispose()
    {
        ProjectCreationHelper.DotnetNew = _originalService;
        ProjectCreationHelper.Notifier = _originalNotifier;

        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    static DotnetNewProjectInfo Frb2Template() =>
        (DotnetNewProjectInfo)EmptyTemplates.Projects.First(item => item is DotnetNewProjectInfo);

    NewProjectViewModel MakeViewModel(string projectName) => new()
    {
        ProjectName = projectName,
        SelectedProject = Frb2Template(),
        ProjectDestinationLocation = _root + Path.DirectorySeparatorChar,
        IsCreateProjectDirectoryChecked = true,
        OpenSlnFolderAfterCreation = false,
    };

    [Fact]
    public void TheWindowOffersAnFrb2Entry()
    {
        // The whole point: it has to be selectable alongside the zip templates.
        var template = Frb2Template();

        template.TemplatePackageId.ShouldBe("FlatRedBall2.Templates");
        template.TemplateShortName.ShouldBe("frb2-desktop");
        template.SupportedInGlue.ShouldBeTrue();
        template.Url.ShouldBeNullOrEmpty("A dotnet template has no zip to download.");
    }

    [Fact]
    public async Task CreatingAnFrb2Project_InvokesTheTemplateWithTheChosenName()
    {
        var succeeded = await ProjectCreationHelper.MakeNewProject(MakeViewModel("MyFrb2Game"));

        succeeded.ShouldBeTrue(string.Join("\n", _notifier.Messages));
        _service.Calls.Count.ShouldBe(1);
        _service.Calls[0].TemplateShortName.ShouldBe("frb2-desktop");
        _service.Calls[0].ProjectName.ShouldBe("MyFrb2Game");
        _service.Calls[0].OutputDirectory.ShouldContain("MyFrb2Game");
    }

    [Fact]
    public async Task CreatingAnFrb2Project_ReportsTheFailureInsteadOfClaimingSuccess()
    {
        _service.Response = GeneralResponse.UnsuccessfulWith("the SDK is not installed");

        var succeeded = await ProjectCreationHelper.MakeNewProject(MakeViewModel("Doomed"));

        succeeded.ShouldBeFalse();
        _notifier.Messages.ShouldContain(message => message.Contains("the SDK is not installed"));
    }

    [Fact]
    public async Task CreatingAnFrb2Project_DoesNotRenameOrRewriteAnything()
    {
        // The zip path replaces a placeholder name across every file and regenerates guids. Doing that
        // to a dotnet-new project would corrupt a tree the CLI has already named correctly, so nothing
        // may be written beyond what the template produced.
        _service.OnCreate = (name, directory) =>
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, name + ".Common.csproj"), "<Project />");
        };

        await ProjectCreationHelper.MakeNewProject(MakeViewModel("Untouched"));

        var created = Directory.GetFiles(_service.Calls[0].OutputDirectory, "*", SearchOption.AllDirectories)
            .Select(Path.GetFileName)
            .ToList();

        created.ShouldBe(new[] { "Untouched.Common.csproj" });
    }

    [Fact]
    public async Task CreatingAnFrb2Project_RemovesTheTemplatesDefaultGameScreen()
    {
        // The template ships Screens/GameScreen.cs (a "Hello from FlatRedBall 2" starter screen) under
        // the .Common project so a bare `dotnet new` + F5 shows something. Once Glue rewrites Game1.cs
        // to load the .gluj instead, that class is never referenced again - a Glue-made project should
        // not carry it.
        _service.OnCreate = (name, directory) =>
        {
            var commonDirectory = Path.Combine(directory, name + ".Common");
            Directory.CreateDirectory(Path.Combine(commonDirectory, "Screens"));
            File.WriteAllText(Path.Combine(commonDirectory, name + ".Common.csproj"), "<Project />");
            File.WriteAllText(Path.Combine(commonDirectory, "Screens", "GameScreen.cs"), "// starter screen");
            File.WriteAllText(Path.Combine(commonDirectory, "Game1.cs"), "// Game1");
        };

        await ProjectCreationHelper.MakeNewProject(MakeViewModel("StripMe"));

        var commonDirectory = Path.Combine(_service.Calls[0].OutputDirectory, "StripMe.Common");
        File.Exists(Path.Combine(commonDirectory, "Screens", "GameScreen.cs")).ShouldBeFalse();

        // Nothing else the template wrote should be touched.
        File.Exists(Path.Combine(commonDirectory, "Game1.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(commonDirectory, "StripMe.Common.csproj")).ShouldBeTrue();
    }

    [Fact]
    public void GlueIsToldToOpenTheCommonProject_NotTheLauncher()
    {
        // `dotnet new frb2-desktop` produces two projects and only .Common is the game.
        var toOpen = ProjectCreationHelper.GetProjectToOpen(Frb2Template(), "MyFrb2Game", @"C:\Games\MyFrb2Game");

        Path.GetFileName(toOpen).ShouldBe("MyFrb2Game.Common.csproj");
    }

    [Fact]
    public void AZipTemplate_StillOpensTheProjectInsideItsOwnFolder()
    {
        // Unchanged behaviour for every existing entry: a zip unpacks one project into a folder named
        // after it. This used to be hardcoded at the call site in NewProjectHelper.
        var zipTemplate = EmptyTemplates.Projects.First(item => !string.IsNullOrEmpty(item.Url));

        var toOpen = ProjectCreationHelper.GetProjectToOpen(zipTemplate, "Whatever", @"C:\Games");

        toOpen.ShouldBe(Path.Combine(@"C:\Games", "Whatever", "Whatever.csproj"));
    }

    class RecordingDotnetNewService : IDotnetNewService
    {
        public record Call(string TemplatePackageId, string TemplateShortName, string ProjectName, string OutputDirectory);

        public List<Call> Calls { get; } = new();
        public GeneralResponse Response { get; set; } = GeneralResponse.SuccessfulResponse;
        public Action<string, string> OnCreate { get; set; }

        public Task<GeneralResponse> CreateProjectAsync(
            string templatePackageId, string templateShortName, string projectName, string outputDirectory)
        {
            Calls.Add(new Call(templatePackageId, templateShortName, projectName, outputDirectory));
            OnCreate?.Invoke(projectName, outputDirectory);
            return Task.FromResult(Response);
        }
    }

    class RecordingNotifier : IProjectCreationNotifier
    {
        public List<string> Messages { get; } = new();

        public void ShowMessage(string message) => Messages.Add(message);
    }
}
