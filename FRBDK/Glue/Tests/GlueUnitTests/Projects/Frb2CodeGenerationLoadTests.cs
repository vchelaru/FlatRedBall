using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers.Projects;
using GlueFormsCore.ViewModels;
using GlueUnitTests.TestSupport;
using Xunit;

namespace GlueUnitTests.Projects;

/// <summary>
/// What an FRB2 project opting into code generation (<see cref="GlueProjectSave.GenerateCode"/>) gets,
/// driven through the same <c>ProjectLoader.LoadProject</c> Glue.exe runs on File &gt; Load Project.
/// </summary>
/// <remarks>
/// The sibling <see cref="Frb2ProjectLoadTests"/> covers the default - opted out, no code at all. Opting
/// in must change exactly one thing: the Screen/Entity accessor pair appears beside the JSON. It must
/// not turn on FRB1's generators or hand Glue ownership of the .csproj, which is what
/// <c>ProjectBase.IsMaintainedByGlue</c> would have done had the setting been routed through it - that
/// one flag also gates csproj saving and every plugin generator reached through
/// <c>CodeWritePolicy</c>/<c>CodeBuildItemAdder</c>, none of which have an FRB2 equivalent.
/// </remarks>
[Collection("Frb2ProjectLoad")]
public class Frb2CodeGenerationLoadTests
{
    const string ProjectName = Frb2ProjectFixture.ProjectName;

    /// <summary>
    /// Turns the setting on the way a user does - edit the .gluj, reopen the project - and returns the
    /// .csproj path. Generation runs on load, so the reload is the point rather than an artifact.
    /// </summary>
    static async Task<string> LoadOptedIn(string root, bool withTopDownEntity = false)
    {
        var csprojPath = Frb2ProjectFixture.Write(root);
        await GoldProject.LoadInGlueAsync(csprojPath);

        await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("GameScreen");
        var entity = await GlueCommands.Self.GluxCommands.EntityCommands.AddEntityAsync(
            new AddEntityViewModel { Name = "PlayerEntity" });

        if (withTopDownEntity)
        {
            entity.Properties.SetValue("IsTopDown", true);
        }

        GlueState.Self.CurrentGlueProject.GenerateCode = true;
        GlueCommands.Self.GluxCommands.SaveProjectAndElements();

        await GoldProject.LoadInGlueAsync(csprojPath);
        return csprojPath;
    }

    static IReadOnlyList<string> CodeFilesUnder(string root) =>
        Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(root, f).Replace('\\', '/'))
            .Where(f => f != Frb2ProjectFixture.HandWrittenCodeFile)
            .OrderBy(f => f, System.StringComparer.OrdinalIgnoreCase)
            .ToList();

    [StaFact]
    public async Task OptingIn_WritesTheGeneratedAndCustomFilePair_BesideTheJson()
    {
        // The two halves of a partial class have to land in one place. They were rooted at different
        // directories - the generated file at FileManager.RelativeDirectory (the .csproj's folder) and
        // the custom file at CurrentGlueProjectDirectory (the .gluj's folder) - which for FRB1 are the
        // same directory and for FRB2 are three levels apart.
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2GenPair_");
        using var dialogs = new RecordedChoiceDialogs();

        await LoadOptedIn(temp.Root);

        Assert.Equal(
            new[]
            {
                "Content/FrbEditor/Entities/PlayerEntity.Generated.cs",
                "Content/FrbEditor/Entities/PlayerEntity.cs",
                "Content/FrbEditor/Screens/GameScreen.Generated.cs",
                "Content/FrbEditor/Screens/GameScreen.cs",
            }.OrderBy(f => f, System.StringComparer.OrdinalIgnoreCase),
            CodeFilesUnder(temp.Root));
    }

    [StaFact]
    public async Task AddScreen_WhenCodeGenerationIsAlreadyOn_StillWarnsAboutAPreexistingCustomCodeFile()
    {
        // The sibling test in Frb2ProjectLoadTests pins the opted-out default: no warning, because
        // Glue never touches this file. Once opted in, Glue's generated accessors pair with this exact
        // file, so a pre-existing one still deserves the same warning FRB1 gives - losing it would
        // silently accept whatever the user already had there.
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2PreexistingScreenGenOn_");
        await GoldProject.LoadInGlueAsync(Frb2ProjectFixture.Write(temp.Root));
        GlueState.Self.CurrentGlueProject.GenerateCode = true;

        var screenFile = Path.Combine(temp.Root, "Screens", "GameScreen.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(screenFile)!);
        File.WriteAllText(screenFile, "// hand-written customization\n");

        await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("GameScreen");

        Assert.Single(GlueTestBootstrap.RecordedDialogMessages);
        Assert.Contains("GameScreen.cs", GlueTestBootstrap.RecordedDialogMessages[0]);
    }

    [StaFact]
    public async Task OptingIn_DoesNotMakeGlueOwnTheCsproj()
    {
        // Opting into generated accessors says nothing about who maintains the project file. FRB2's
        // .csproj globs its own sources; Glue adding Compile/None items and a Newtonsoft.Json
        // PackageReference to it is the FRB1 contract leaking through.
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2GenCsproj_");
        using var dialogs = new RecordedChoiceDialogs();

        var csprojPath = Frb2ProjectFixture.Write(temp.Root);
        var csprojBefore = File.ReadAllText(csprojPath);

        await LoadOptedIn(temp.Root);

        Assert.Equal(csprojBefore, File.ReadAllText(csprojPath));
    }

    [StaFact]
    public async Task OptingIn_DoesNotRunFrb1sOwnGenerators()
    {
        // CameraSetup.Generated.cs, FileAliases.Generated.cs and the Performance/ embedded resources are
        // written on load by CodeBuildItemAdder/CameraSetupCodeGenerator/AliasCodeGenerator, none of
        // which go through IGenerateCodeCommands - so routing the interface to an FRB2 implementation
        // does not stop them. They compile against FRB1 APIs that do not exist in FRB2.
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2GenNoFrb1_");
        using var dialogs = new RecordedChoiceDialogs();

        await LoadOptedIn(temp.Root);

        Assert.DoesNotContain(CodeFilesUnder(temp.Root), f =>
            f.Contains("CameraSetup") ||
            f.Contains("FileAliases") ||
            f.Contains("/Performance/") ||
            f.Contains("/TileGraphics/") ||
            f.Contains("/TileCollisions/") ||
            f.Contains("/TileEntities/"));
    }

    [StaFact]
    public async Task OptingIn_WithATopDownEntity_StillWritesNoTopDownCode()
    {
        // EntityInputMovementPlugin's generators fire from ReactToLoadedGlux for any project holding a
        // top-down entity, outside IGenerateCodeCommands entirely.
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2GenTopDown_");
        using var dialogs = new RecordedChoiceDialogs();

        await LoadOptedIn(temp.Root, withTopDownEntity: true);

        Assert.Empty(ErrorRecordingPlugin.Errors);
        Assert.DoesNotContain(CodeFilesUnder(temp.Root), f => f.Contains("/TopDown/"));
    }

    [StaFact]
    public async Task ReloadingAnOptedInProject_DoesNotPromptAboutMissingCustomCodeFiles()
    {
        // The load-time "this file is missing, want me to re-create it?" check builds its path the FRB1
        // way, so it looked for the custom file in the .csproj's folder while the generator had just
        // written it beside the .gluj. Answering the prompt wrote the file where it already was, so it
        // fired again for every screen and entity on every subsequent load.
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2GenReprompt_");
        using var dialogs = new RecordedChoiceDialogs();

        var csprojPath = await LoadOptedIn(temp.Root);
        dialogs.Messages.Clear();

        await GoldProject.LoadInGlueAsync(csprojPath);

        Assert.Empty(dialogs.Messages);
        Assert.Empty(GlueTestBootstrap.RecordedDialogMessages);
    }

    [StaFact]
    public async Task NotOptingIn_LeavesTheProjectWithNoCodeAtAll()
    {
        // The default has to stay exactly what #2021 shipped - this is the same project and the same
        // steps as the opted-in tests, differing only in the setting.
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2GenOptedOut_");
        using var dialogs = new RecordedChoiceDialogs();

        var csprojPath = Frb2ProjectFixture.Write(temp.Root);
        await GoldProject.LoadInGlueAsync(csprojPath);
        await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("GameScreen");
        GlueCommands.Self.GluxCommands.SaveProjectAndElements();
        await GoldProject.LoadInGlueAsync(csprojPath);

        Assert.IsType<Frb2Project>(GlueState.Self.CurrentMainProject);
        Assert.Empty(CodeFilesUnder(temp.Root));
        Assert.Empty(dialogs.Messages);
    }
}
