using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
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
/// This is the default. <see cref="Frb2CodeGenerationLoadTests"/> covers the same project once it opts
/// into typed accessors, where the .csproj must still be untouched and FRB1's generators must still
/// not run.
/// </remarks>
[Collection("Frb2ProjectLoad")]
public class Frb2ProjectLoadTests
{
    const string ProjectName = Frb2ProjectFixture.ProjectName;

    static string WriteFrb2Project(string root) => Frb2ProjectFixture.Write(root);

    static string WriteTemplateShapedFrb2Project(string root) => Frb2ProjectFixture.WriteTemplateShaped(root);

    [StaFact]
    public async Task LoadingATemplateCreatedFrb2Project_IsRecognisedAndWritesNoCode()
    {
        // `dotnet new frb2-desktop` takes the engine from nuget rather than by source, which the
        // detector's original single rule did not match - so a template-created game fell through to
        // the DefineConstants cascade and could not be opened at all.
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2Template_");
        var commonCsproj = WriteTemplateShapedFrb2Project(temp.Root);

        await GoldProject.LoadInGlueAsync(commonCsproj);

        Assert.IsType<Frb2Project>(GlueState.Self.CurrentMainProject);
        Assert.True(File.Exists(Path.Combine(
                Path.GetDirectoryName(commonCsproj)!, "Content", "FrbEditor", ProjectName + ".gluj")),
            "The .gluj should land under the Common project's Content/FrbEditor/, named after the " +
            "game rather than carrying the Common project's own \".Common\" suffix.");

        // Same contract as a source-referencing FRB2 game: no generated code anywhere.
        Assert.Empty(Directory.GetFiles(temp.Root, "*.cs", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(temp.Root, f).Replace('\\', '/'))
            .Where(f => f != ProjectName + ".Common/Game1.cs"));
    }

    [StaFact]
    public async Task OpeningTheDesktopLauncher_LoadsTheCommonProjectInstead()
    {
        // Measured before the redirect existed: ProjectCreator returned null for the launcher and asked
        // "FlatRedBall could not determine the project type", after which ProjectLoader skipped loading
        // entirely. Picking the runnable project of the two is not a user error worth failing on.
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2Launcher_");
        WriteTemplateShapedFrb2Project(temp.Root);
        var desktopCsproj = Path.Combine(
            temp.Root, ProjectName + ".Desktop", ProjectName + ".Desktop.csproj");

        await GoldProject.LoadInGlueAsync(desktopCsproj);

        Assert.IsType<Frb2Project>(GlueState.Self.CurrentMainProject);
        Assert.Equal(
            ProjectName + ".Common.csproj",
            Path.GetFileName(GlueState.Self.CurrentMainProject.FullFileName.FullPath));
        Assert.Empty(GlueTestBootstrap.RecordedDialogMessages);
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

    [StaFact]
    public async Task AddScreen_OnAnFrb2ProjectWithoutCodeGeneration_DoesNotWarnAboutAFileTheTemplateShipped()
    {
        // Reproduces a wizard-created FRB2 platformer project: the FRB2 template ships its own
        // Screens/GameScreen.cs on disk before Glue ever adds the screen. With code generation off,
        // Glue never reads or writes that file (CodeWritePolicy.GeneratesFrb2Code is false), so its
        // mere presence on disk is not Glue's concern and must not prompt the "already exists" warning.
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2TemplateShippedScreen_");
        await GoldProject.LoadInGlueAsync(WriteFrb2Project(temp.Root));

        var screenFile = Path.Combine(temp.Root, "Screens", "GameScreen.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(screenFile)!);
        File.WriteAllText(screenFile, "// shipped by the FRB2 template\n");

        await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("GameScreen");

        Assert.Empty(GlueTestBootstrap.RecordedDialogMessages);
    }

    // A real 1x1 PNG, so anything that reads image headers gets a valid file rather than a stub.
    const string OnePixelPngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";

    [StaFact]
    public async Task CurrentGlueProjectDirectory_OnAnFrb2Project_IsWhereTheGlujActuallyIs()
    {
        // "View in Explorer" on an entity opened the desktop instead of the folder, because it builds
        // its path as CurrentGlueProjectDirectory + element.Name + extension and that property was
        // derived from the .csproj rather than the .gluj. Identical for a project that keeps the two
        // together, which is every FRB1 project - hence nobody noticing.
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2GlueDir_");
        await GoldProject.LoadInGlueAsync(WriteFrb2Project(temp.Root));
        await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("NewScreen");

        // Composed the same way every caller composes it, so the assertion fails for the same reason
        // the menu item did.
        var screenFile = new FilePath(
            GlueState.Self.CurrentGlueProjectDirectory + @"Screens\NewScreen.glsj");

        Assert.True(screenFile.Exists(),
            $"Expected the screen's .glsj at {screenFile.FullPath}, which is where every " +
            "CurrentGlueProjectDirectory-based path points.");
    }

    [StaFact]
    public async Task AddingAContentFile_ToAnFrb2Screen_PutsItInsideTheEditorFolder()
    {
        // Dropping a PNG on a screen put it in Content/Screens/NewScreen/, outside the folder that is
        // supposed to hold everything Glue authored - so deleting Content/FrbEditor would have left the
        // referenced art orphaned in the game's own content tree.
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2ContentFolder_");
        await GoldProject.LoadInGlueAsync(WriteFrb2Project(temp.Root));
        await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("NewScreen");

        Assert.Equal(
            new FilePath(Path.Combine(temp.Root, "Content", "FrbEditor") + "/"),
            new FilePath(GlueState.Self.ContentDirectory));
    }

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

        using var dialogs = new RecordedChoiceDialogs();

        // Load again, now that the .gluj has a screen in it.
        await GoldProject.LoadInGlueAsync(csprojPath);

        Assert.Empty(dialogs.Messages);
        Assert.Empty(GlueTestBootstrap.RecordedDialogMessages);
    }

    [StaFact]
    public async Task LoadingAnFrb2Project_WithATopDownEntity_WritesNoTopDownCode()
    {
        // Opening a project with a top-down entity threw DirectoryNotFoundException on
        // Content/FrbEditor/TopDown/TopDownAnimationControllerGenerator.Generated.cs: the generator
        // asked CreateAndAddCodeFile for the file (suppressed for FRB2, so its Directory.CreateDirectory
        // never ran) and then wrote with System.IO.File.WriteAllText, which consults nothing.
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2TopDown_");
        var csprojPath = WriteFrb2Project(temp.Root);
        await GoldProject.LoadInGlueAsync(csprojPath);

        var entity = await GlueCommands.Self.GluxCommands.EntityCommands.AddEntityAsync(
            new AddEntityViewModel { Name = "PlayerEntity" });
        entity.Properties.SetValue("IsTopDown", true);
        GlueCommands.Self.GluxCommands.SaveProjectAndElements();

        // Loaded again rather than asserting on the first load: the top-down generators run from
        // EntityInputMovementPlugin's ReactToLoadedGlux, which only fires them for a project that
        // already has a top-down entity in it - which is the state the user's project opens in.
        await GoldProject.LoadInGlueAsync(csprojPath);

        Assert.Empty(ErrorRecordingPlugin.Errors);
        Assert.Empty(Directory.GetFiles(temp.Root, "*.cs", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(temp.Root, f).Replace('\\', '/'))
            .Where(f => f != "Game1.cs"));
    }

    [StaFact]
    public async Task SettingTheStartupScreen_OnAnFrb2Project_SavesItWithoutWarningAboutTheGameClass()
    {
        // The setter writes StartUpScreen to the .gluj and saves it, and only then looks for the Game
        // class to regenerate. An FRB2 project has no Game class for Glue to find and does not want one
        // touched - it reads StartUpScreen out of the .gluj at runtime - so the "could not set the
        // startup screen" popup fired on a change that had in fact already been made.
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2Startup_");
        await GoldProject.LoadInGlueAsync(WriteFrb2Project(temp.Root));
        await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("NewScreen");

        GlueTestBootstrap.RecordedDialogMessages.Clear();

        GlueCommands.Self.GluxCommands.StartUpScreenName = "Screens\\NewScreen";

        Assert.Empty(GlueTestBootstrap.RecordedDialogMessages);
        Assert.Equal("Screens\\NewScreen", GlueState.Self.CurrentGlueProject.StartUpScreen);
    }

    [StaFact]
    public async Task RenamingAScreen_OnAnFrb2Project_RenamesTheGlsj_WithoutWarningAboutCode()
    {
        // Rename moves the element's .cs and .Generated.cs alongside its JSON. An FRB2 project has
        // neither, so this is the same shape as the startup-screen popup: a code-shaped step failing on
        // a project that legitimately has no code.
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2Rename_");
        await GoldProject.LoadInGlueAsync(WriteFrb2Project(temp.Root));
        var screen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("NewScreen");

        GlueTestBootstrap.RecordedDialogMessages.Clear();

        await GlueCommands.Self.GluxCommands.ElementCommands.RenameElement(
            screen, "Screens\\RenamedScreen", showRenameWindow: false);

        Assert.True(File.Exists(Path.Combine(temp.Root, "Content", "FrbEditor", "Screens", "RenamedScreen.glsj")),
            "The renamed screen's .glsj should exist under its new name.");
        Assert.Empty(GlueTestBootstrap.RecordedDialogMessages);
    }

    [StaFact]
    public async Task RemoveEntityAsync_WithNoFileList_RemovesTheEntityInsteadOfThrowing()
    {
        // filesThatCouldBeRemoved is an output accumulator that the caller shows the user - the method
        // does not delete anything itself. It defaults to null and was then added to unconditionally, so
        // the documented no-list overload threw at the first Add for any project type. RemoveScreen has
        // always coalesced it; this one did not.
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2Remove_");
        await GoldProject.LoadInGlueAsync(WriteFrb2Project(temp.Root));
        var entity = await GlueCommands.Self.GluxCommands.EntityCommands.AddEntityAsync(
            new AddEntityViewModel { Name = "DoomedEntity" });

        GlueTestBootstrap.RecordedDialogMessages.Clear();

        await GlueCommands.Self.GluxCommands.RemoveEntityAsync(entity);

        Assert.DoesNotContain(GlueState.Self.CurrentGlueProject.Entities, item => item.Name == entity.Name);
        Assert.Empty(GlueTestBootstrap.RecordedDialogMessages);
    }

    [StaFact]
    public async Task LoadingAnFrb2ProjectForTheFirstTime_RewritesGame1ToLoadTheGluj()
    {
        // FRB2 generates no code, so the template's Game1.cs hardcodes FlatRedBallService.Default
        // .Initialize<GameScreen>(this) - it has to be swapped for a call that loads Glue's .gluj
        // instead, and the only safe moment to do that automatically is the very first load, before
        // any .gluj exists and while Game1.cs is still whatever the template shipped.
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2Game1Rewrite_");
        var csprojPath = WriteFrb2Project(temp.Root);

        await GoldProject.LoadInGlueAsync(csprojPath);

        var game1Contents = File.ReadAllText(Path.Combine(temp.Root, "Game1.cs"));
        Assert.DoesNotContain(Frb2ProjectFixture.DefaultInitializeCall, game1Contents);
        Assert.Contains(
            $"FlatRedBallService.Default.Initialize(this, \"Content/FrbEditor/{ProjectName}.gluj\");",
            game1Contents);
    }

    [StaFact]
    public async Task ReloadingAnFrb2Project_DoesNotTouchGame1Again()
    {
        // The rewrite only fires the moment the .gluj is created. A user who reverts the Initialize
        // call back to Initialize<GameScreen>(this) after that point has made a deliberate choice Glue
        // must not silently overwrite on every later load.
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2Game1NoReRewrite_");
        var csprojPath = WriteFrb2Project(temp.Root);
        await GoldProject.LoadInGlueAsync(csprojPath);

        var game1Path = Path.Combine(temp.Root, "Game1.cs");
        File.WriteAllText(game1Path, Frb2ProjectFixture.DefaultInitializeCall);

        await GoldProject.LoadInGlueAsync(csprojPath);

        Assert.Equal(Frb2ProjectFixture.DefaultInitializeCall, File.ReadAllText(game1Path));
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
