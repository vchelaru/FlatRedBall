using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins.Frb2SettingsPlugin;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers.Projects;
using GlueFormsCore.ViewModels;
using GlueUnitTests.TestSupport;
using Xunit;

namespace GlueUnitTests.Frb2SettingsPlugin;

/// <summary>
/// GitHub issue #2058: <see cref="MainFrb2SettingsPlugin.ApplyGenerateCodeSetting"/> is the seam the new
/// "FRB2 Code Generation" checkbox drives - it flips <c>GlueProjectSave.GenerateCode</c>, saves the
/// .gluj, and (unlike hand-editing the .gluj, which only took effect on the next project load per
/// <see cref="Frb2CodeGenerationLoadTests"/>) generates code immediately so the checkbox is useful
/// without asking the user to reload.
/// </summary>
/// <remarks>
/// See <see cref="GlueUnitTests.Projects.Frb2CodeGenerationLoadTests"/> for the "hand-edit the .gluj,
/// reload" path this plugin is meant to shortcut.
/// </remarks>
[Collection("Frb2ProjectLoad")]
public class MainFrb2SettingsPluginTests : IDisposable
{
    readonly VisualStudioProject _previousMainProject;
    readonly GlueProjectSave _previousGlueProject;
    readonly string _previousRelativeDirectory;

    public MainFrb2SettingsPluginTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        _previousMainProject = GlueState.Self.CurrentMainProject;
        _previousGlueProject = GlueState.Self.CurrentGlueProject;
        _previousRelativeDirectory = FlatRedBall.IO.FileManager.RelativeDirectory;
    }

    public void Dispose()
    {
        // GenerateCode is a setting nothing else in this process turns on, so leaving it set on a
        // shared GlueProjectSave would change what a later, unrelated test (e.g.
        // Frb2CodeGenerationSuppressionTests, which deliberately loads no GlueProjectSave of its own)
        // sees as ambient state - the assembly disables test parallelization, but every test still
        // shares this one process.
        GlueState.Self.CurrentMainProject = _previousMainProject;
        ObjectFinder.Self.GlueProject = _previousGlueProject;
        FlatRedBall.IO.FileManager.RelativeDirectory = _previousRelativeDirectory;
    }

    static IReadOnlyList<string> CodeFilesUnder(string root) =>
        Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(root, f).Replace('\\', '/'))
            .Where(f => f != Frb2ProjectFixture.HandWrittenCodeFile)
            .OrderBy(f => f, System.StringComparer.OrdinalIgnoreCase)
            .ToList();

    [StaFact]
    public async Task TurningOn_GeneratesCodeImmediately_WithoutRequiringAReload()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2SettingsOn_");
        using var dialogs = new RecordedChoiceDialogs();

        var csprojPath = Frb2ProjectFixture.Write(temp.Root);
        await GoldProject.LoadInGlueAsync(csprojPath);

        await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("GameScreen");
        var entity = await GlueCommands.Self.GluxCommands.EntityCommands.AddEntityAsync(
            new AddEntityViewModel { Name = "PlayerEntity" });

        var wasSynchronous = TaskManager.SynchronousMode;
        TaskManager.SynchronousMode = true;
        try
        {
            MainFrb2SettingsPlugin.ApplyGenerateCodeSetting(GlueState.Self.CurrentGlueProject, generateCode: true);
        }
        finally
        {
            TaskManager.SynchronousMode = wasSynchronous;
        }

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
    public async Task TurningOn_PersistsAcrossReload()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2SettingsPersist_");
        using var dialogs = new RecordedChoiceDialogs();

        var csprojPath = Frb2ProjectFixture.Write(temp.Root);
        await GoldProject.LoadInGlueAsync(csprojPath);

        var wasSynchronous = TaskManager.SynchronousMode;
        TaskManager.SynchronousMode = true;
        try
        {
            MainFrb2SettingsPlugin.ApplyGenerateCodeSetting(GlueState.Self.CurrentGlueProject, generateCode: true);
        }
        finally
        {
            TaskManager.SynchronousMode = wasSynchronous;
        }

        await GoldProject.LoadInGlueAsync(csprojPath);

        Assert.True(GlueState.Self.CurrentGlueProject.GenerateCode);
    }
}
