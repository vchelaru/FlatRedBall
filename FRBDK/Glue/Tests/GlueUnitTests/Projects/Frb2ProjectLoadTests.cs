using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using GlueFormsCore.ViewModels;
using GlueUnitTests.TestSupport;
using Xunit;

namespace GlueUnitTests.Projects;

/// <summary>
/// The whole point of issue #2021, driven through the same <c>ProjectLoader.LoadProject</c> Glue.exe
/// runs on File > Load Project: pointing the editor at an FRB2 game project gets a .gluj and nothing
/// else - no generated code, and the .csproj untouched.
/// </summary>
/// <remarks>
/// The fixture is written here rather than checked in because an FRB2 game's only distinguishing
/// feature is a ProjectReference to FlatRedBall2.csproj, which lives in a different repository. MSBuild
/// evaluation does not require a ProjectReference's target to exist, so a synthetic project is a
/// faithful stand-in for what <see cref="Frb2ProjectDetector"/> and the loader actually look at.
/// </remarks>
[Collection("Frb2ProjectLoad")]
public class Frb2ProjectLoadTests
{
    const string ProjectName = "Frb2LoadTestGame";

    static string WriteFrb2Project(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, "Content"));

        var csprojPath = Path.Combine(root, ProjectName + ".csproj");
        File.WriteAllText(csprojPath, $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>{ProjectName}</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include=""..\..\src\FlatRedBall2.csproj"" />
  </ItemGroup>
</Project>");

        File.WriteAllText(Path.Combine(root, "Game1.cs"),
            "namespace " + ProjectName + ";\npublic class Game1\n{\n}\n");

        // A .slnx, not a .sln, because that is what the real FRB2 sample ships and what a solution
        // created in a recent Visual Studio looks like. ProjectSyncer.LocateSolution throws when it
        // finds no solution at all, and that exception surfaces during load - so getting this wrong
        // means the project does not open.
        File.WriteAllText(Path.Combine(root, ProjectName + ".slnx"),
            $"<Solution>\n  <Project Path=\"{ProjectName}.csproj\" />\n</Solution>\n");
        return csprojPath;
    }

    [StaFact]
    public async Task LoadingAnFrb2Project_CreatesTheGluj_AndWritesNothingElse()
    {
        // The full game-project plugin set, not just the embedded ones: which plugins are registered is
        // process-wide, so registering them here is what makes this assertion mean the same thing run
        // on its own and run in the middle of the suite. It is also the point - a generator that writes
        // outside GenerateCodeCommands only shows up when its plugin is loaded.
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2Load_");
        var csprojPath = WriteFrb2Project(temp.Root);
        var csprojBefore = File.ReadAllText(csprojPath);

        await GoldProject.LoadInGlueAsync(csprojPath);

        Assert.IsType<Frb2Project>(GlueState.Self.CurrentMainProject);

        // Self-contained under Content/FrbEditor/, so the game copies one folder to output with a
        // single rule. A glob rooted at the project directory would also match bin/ and obj/ and copy
        // the previous build's copies in again, compounding every build.
        Assert.True(File.Exists(Path.Combine(temp.Root, "Content", "FrbEditor", ProjectName + ".gluj")),
            "Loading an FRB2 project should have created its .gluj under Content/FrbEditor/.");
        Assert.False(File.Exists(Path.Combine(temp.Root, ProjectName + ".gluj")),
            "The .gluj should not also be left at the project root.");

        // Everything Glue authors goes in that one folder, GlueSettings included, so deleting it
        // removes every trace of the editor from the project. Those settings being copied to output
        // along with it is the accepted cost.
        //
        // Asserted on the resolved path rather than on the folder existing: the settings files are
        // written by plugins that do not run in a headless host, so Directory.Exists here would be
        // testing the test harness rather than where Glue decided to put them.
        // Trailing separator: ProjectSpecificSettingsFolder returns a directory path, and FilePath
        // treats "…/GlueSettings" and "…/GlueSettings/" as different values.
        Assert.Equal(
            new FilePath(Path.Combine(temp.Root, "Content", "FrbEditor", "GlueSettings") + "/"),
            new FilePath(GlueState.Self.ProjectSpecificSettingsFolder));

        // Glue does not own this project's .csproj.
        Assert.Equal(csprojBefore, File.ReadAllText(csprojPath));

        // Nothing generated. Game1.cs is the one file the fixture itself wrote.
        var codeFiles = Directory.GetFiles(temp.Root, "*.cs", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(temp.Root, f).Replace('\\', '/'))
            .ToList();
        Assert.Equal(new[] { "Game1.cs" }, codeFiles);

        // The point of the layout: one folder holds everything Glue authored, so deleting
        // Content/FrbEditor leaves no trace of the editor behind. Anything outside it fails this - a
        // generator's destination folder, a plugin's scaffolding, including one nobody has thought of
        // yet. Content/ itself is the game's, and the fixture created it.
        var directoriesOutsideTheEditorFolder = Directory
            .GetDirectories(temp.Root, "*", SearchOption.AllDirectories)
            .Select(d => Path.GetRelativePath(temp.Root, d).Replace('\\', '/'))
            .Where(d => d != "Content" && !d.StartsWith("Content/FrbEditor"))
            .OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
            .ToList();
        Assert.Empty(directoriesOutsideTheEditorFolder);
    }

    [StaFact]
    public async Task AddScreen_OnAnFrb2Project_WritesTheGlsj_WithoutClaimingACodeFileExists()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2AddScreen_");
        await GoldProject.LoadInGlueAsync(WriteFrb2Project(temp.Root));

        await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("NewScreen");

        // Authoring the JSON is the entire job here, so it has to actually happen.
        // Screens follow the .gluj: every writer derives its directory from that path.
        Assert.True(File.Exists(Path.Combine(temp.Root, "Content", "FrbEditor", "Screens", "NewScreen.glsj")),
            "Adding a screen should have written its .glsj beside the .gluj.");

        // No custom-code or generated-code file, and - the bug this pins - no dialog telling the user
        // an existing NewScreen.cs will be reused. Nothing creates one, so nothing can reuse one:
        // CreateAndAddCodeFile returns null both when the file is already in the project and when the
        // project takes no code files at all, and AddScreen used to read either as the former.
        Assert.Empty(Directory.GetFiles(temp.Root, "*.cs", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(temp.Root, f).Replace('\\', '/'))
            .Where(f => f != "Game1.cs"));
        Assert.Empty(GlueTestBootstrap.RecordedDialogMessages);
    }

    // A real 1x1 PNG, so anything that reads image headers gets a valid file rather than a stub.
    const string OnePixelPngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";

    [StaFact]
    public async Task AddingAContentFile_ToAnFrb2Project_DoesNotAddItToTheCsproj()
    {
        // Dropping a PNG onto the editor reported "Added Screens/NewScreen/Bear.png ... as content".
        // Glue does not maintain this .csproj, so there is no content item to add and nothing to say.
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2AddContent_");
        var csprojPath = WriteFrb2Project(temp.Root);
        await GoldProject.LoadInGlueAsync(csprojPath);
        var screen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("NewScreen");

        var png = Path.Combine(temp.Root, "Content", "Screens", "NewScreen", "Bear.png");
        Directory.CreateDirectory(Path.GetDirectoryName(png)!);
        File.WriteAllBytes(png, Convert.FromBase64String(OnePixelPngBase64));

        var csprojBefore = File.ReadAllText(csprojPath);
        ErrorRecordingPlugin.Output.Clear();

        await GlueCommands.Self.GluxCommands.CreateReferencedFileSaveForExistingFileAsync(
            screen, new FilePath(png));

        Assert.DoesNotContain(ErrorRecordingPlugin.Output, line => line.Contains("as content"));
        Assert.Equal(csprojBefore, File.ReadAllText(csprojPath));
    }

    [StaFact]
    public async Task ReloadingAnFrb2Project_DoesNotPromptAboutMissingCustomCodeFiles()
    {
        // An FRB2 screen has no NewScreen.cs by design, so the load-time "this file is missing, want me
        // to re-create it?" check fires for every screen and entity, on every load.
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2Reload_");
        var csprojPath = WriteFrb2Project(temp.Root);
        await GoldProject.LoadInGlueAsync(csprojPath);
        await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("NewScreen");

        var choicesOffered = new List<string>();
        var previousShowChoice = DialogService.ShowChoiceImpl;
        try
        {
            // Must be stubbed, not just asserted on afterwards: unstubbed, ShowChoice puts a real modal
            // on the developer's desktop and the run blocks on it forever.
            DialogService.ShowChoiceImpl = (message, options) =>
            {
                choicesOffered.Add(message);
                return null;
            };

            // Load again, now that the .gluj has a screen in it.
            await GoldProject.LoadInGlueAsync(csprojPath);
        }
        finally
        {
            DialogService.ShowChoiceImpl = previousShowChoice;
        }

        Assert.Empty(choicesOffered);
        Assert.Empty(GlueTestBootstrap.RecordedDialogMessages);
    }

    [StaFact]
    public async Task AddEntity_OnAnFrb2Project_WritesTheGlej_WithoutClaimingACodeFileExists()
    {
        // AddEntity is a separate method with its own copy of AddScreen's create-code-file logic, so it
        // needs its own coverage rather than being assumed to follow along.
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2AddEntity_");
        await GoldProject.LoadInGlueAsync(WriteFrb2Project(temp.Root));

        await GlueCommands.Self.GluxCommands.EntityCommands.AddEntityAsync(
            new AddEntityViewModel { Name = "NewEntity" });

        Assert.True(File.Exists(Path.Combine(temp.Root, "Content", "FrbEditor", "Entities", "NewEntity.glej")),
            "Adding an entity should have written its .glej beside the .gluj.");
        Assert.Empty(Directory.GetFiles(temp.Root, "*.cs", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(temp.Root, f).Replace('\\', '/'))
            .Where(f => f != "Game1.cs"));
        Assert.Empty(GlueTestBootstrap.RecordedDialogMessages);
    }
}
