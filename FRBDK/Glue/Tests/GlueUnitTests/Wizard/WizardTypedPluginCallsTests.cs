using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.Plugins;
using GlueUnitTests.Tasks;
using GlueUnitTests.TestSupport;
using OfficialPlugins.Wizard.Managers;
using OfficialPlugins.Wizard.Models;
using Shouldly;

namespace GlueUnitTests.Wizard;

/// <summary>
/// The wizard's calls to the Gum and Tiled plugins, through <see cref="IGumCommands"/> and
/// <see cref="ITiledCommands"/>.
/// </summary>
[Collection(nameof(TaskManagerSequentialCollection))]
public class WizardTypedPluginCallsTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly string _originalRelativeDirectory;
    private readonly bool _originalSynchronousMode;
    private readonly IUiThreadMarshaller _originalMarshaller;
    private IGumCommands _originalGumCommands;
    private ITiledCommands _originalTiledCommands;
    private readonly string _tempProjectDirectory;

    public WizardTypedPluginCallsTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = ObjectFinder.Self.GlueProject;
        _originalRelativeDirectory = FlatRedBall.IO.FileManager.RelativeDirectory;
        _originalSynchronousMode = TaskManager.SynchronousMode;
        _originalMarshaller = TaskManager.UiThreadMarshaller;
        _originalGumCommands = GlueCommands.Self.GumCommands;
        _originalTiledCommands = GlueCommands.Self.TiledCommands;

        GlueState.Self.CurrentMainProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);
        ObjectFinder.Self.GlueProject = new GlueProjectSave();
        FlatRedBall.IO.FileManager.RelativeDirectory = _tempProjectDirectory + "\\";

        TaskManager.SynchronousMode = true;
        TaskManager.UiThreadMarshaller = new InlineUiThreadMarshaller();
    }

    public void Dispose()
    {
        GlueState.Self.CurrentMainProject = _originalMainProject;
        ObjectFinder.Self.GlueProject = _originalGlueProject;
        FlatRedBall.IO.FileManager.RelativeDirectory = _originalRelativeDirectory;
        TaskManager.SynchronousMode = _originalSynchronousMode;
        TaskManager.UiThreadMarshaller = _originalMarshaller;
        GlueCommands.Self.GumCommands = _originalGumCommands;
        GlueCommands.Self.TiledCommands = _originalTiledCommands;

        try { Directory.Delete(_tempProjectDirectory, recursive: true); } catch { }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HandleAddGum_CreatesTheGumProject_WithFormsAsChosen(bool addForms)
    {
        var gumCommands = new TypedPluginCommandsTests.FakeGumCommands(hasGum: false);
        GlueCommands.Self.GumCommands = gumCommands;

        await WizardProjectLogic.HandleAddGum(new WizardViewModel { AddGum = true, AddFlatRedBallForms = addForms });

        gumCommands.CreatedProjects.ShouldBe(new[] { (addForms, false) });
    }

    [Fact]
    public async Task HandleAddGum_DoesNothing_WhenGumPluginIsNotLoaded()
    {
        GlueCommands.Self.GumCommands = null;

        await WizardProjectLogic.HandleAddGum(new WizardViewModel { AddGum = true });
    }

    /// <summary>
    /// Registers the game project plugins for the TMX and LayeredTileMap asset types the level needs, then swaps in
    /// a fake for the Tiled Plugin's commands.
    /// </summary>
    [StaFact]
    public async Task CreateLevel_AddsTheChosenTiledContent_ToTheLevelsTmx()
    {
        GlueTestBootstrap.EnsureHeadlessProjectLoadReady();
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();
        // Registering can assign these for the rest of the run, so Dispose restores the registered ones.
        _originalGumCommands = GlueCommands.Self.GumCommands;
        _originalTiledCommands = GlueCommands.Self.TiledCommands;
        var tiledCommands = new FakeTiledCommands();
        GlueCommands.Self.TiledCommands = tiledCommands;
        var vm = new WizardViewModel
        {
            AddGameScreen = true,
            AddTiledMap = true,
            WithVisualType = WithVisualType.NoVisuals,
            IncludStandardTilesetInLevels = true,
            IncludeGameplayLayerInLevels = false,
            IncludeCollisionBorderInLevels = true,
        };
        var (gameScreen, _, _) = await WizardProjectLogic.HandleAddGameScreen(vm);

        await WizardProjectLogic.CreateLevel(vm, gameScreen, levelIndex0Based: 0, "Level1");

        var level = ObjectFinder.Self.GetScreenSave(@"Screens\Level1");
        var tmx = level.ReferencedFiles.Single(item => item.GetAssetTypeInfo()?.Extension == "tmx");
        tiledCommands.Calls.ShouldBe(new[] { $"AddStandardTileset {tmx.Name}", $"AddCollisionBorder {tmx.Name}" });
    }

    private class InlineUiThreadMarshaller : IUiThreadMarshaller
    {
        public void Invoke(Action action) => action();
        public T Invoke<T>(Func<T> func) => func();
        public Task Invoke(Func<Task> func) => func();
        public Task<T> Invoke<T>(Func<Task<T>> func) => func();
        public void BeginInvoke(Action action) => action();
    }

    private class FakeTiledCommands : ITiledCommands
    {
        public List<string> Calls { get; } = new();

        public void SaveTmxWithVisuals(ReferencedFileSave tmxFile, string levelName) =>
            Calls.Add($"SaveTmxWithVisuals {tmxFile.Name} {levelName}");

        public void SaveTilesetFilesToDisk() => Calls.Add("SaveTilesetFilesToDisk");
        public void AddStandardTileset(ReferencedFileSave tmxFile) => Calls.Add($"AddStandardTileset {tmxFile?.Name}");
        public void AddGameplayLayer(ReferencedFileSave tmxFile) => Calls.Add($"AddGameplayLayer {tmxFile?.Name}");
        public void AddCollisionBorder(ReferencedFileSave tmxFile) => Calls.Add($"AddCollisionBorder {tmxFile?.Name}");
    }
}
