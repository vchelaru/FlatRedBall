using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GlueFormsCore.Plugins.EmbeddedPlugins.AddScreenPlugin;
using GlueFormsCore.ViewModels;
using GlueUnitTests.TestSupport;
using GlueUnitTests.Tasks;
using Shouldly;

namespace GlueUnitTests.AddScreenPlugin;

// GitHub issue #2099: checking "Add SolidCollision ShapeCollection" while leaving "Add LayeredTileMap"
// unchecked in the New Screen dialog generated code referencing a "Map" object that was never added
// (CS0103). MainAddScreenPlugin.AddCollision always defaulted setFromMapObject to true regardless of
// whether the caller actually added a Map object - WizardProjectLogic.HandleAddGameScreen already passes
// setFromMapObject: vm.AddTiledMap correctly (see WizardProjectLogicAddGameScreenTests), but
// MainAddScreenPlugin.ApplyViewModelToScreen (the New Screen dialog's own path) did not mirror that.
//
// ApplyViewModelToScreen was private async void - switched to internal static async Task (same pattern
// WizardProjectLogic.HandleAddGameScreen already uses) so these tests can drive it directly and await
// its NamedObjectSave/codegen side effects instead of firing-and-forgetting into the UI thread.
[Collection(nameof(TaskManagerSequentialCollection))]
public class MainAddScreenPluginTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly string _originalRelativeDirectory;
    private readonly bool _originalSynchronousMode;
    private readonly string _tempProjectDirectory;

    public MainAddScreenPluginTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = ObjectFinder.Self.GlueProject;
        _originalRelativeDirectory = FlatRedBall.IO.FileManager.RelativeDirectory;
        _originalSynchronousMode = TaskManager.SynchronousMode;

        var vsProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);

        GlueState.Self.CurrentMainProject = vsProject;
        ObjectFinder.Self.GlueProject = new GlueProjectSave();
        FlatRedBall.IO.FileManager.RelativeDirectory = _tempProjectDirectory + "\\";

        TaskManager.SynchronousMode = true;
    }

    public void Dispose()
    {
        GlueState.Self.CurrentMainProject = _originalMainProject;
        ObjectFinder.Self.GlueProject = _originalGlueProject;
        FlatRedBall.IO.FileManager.RelativeDirectory = _originalRelativeDirectory;
        TaskManager.SynchronousMode = _originalSynchronousMode;

        try
        {
            Directory.Delete(_tempProjectDirectory, recursive: true);
        }
        catch
        {
            // best-effort cleanup; a stray temp dir isn't worth failing the test over
        }
    }

    [Fact]
    public async Task ApplyViewModelToScreen_SolidCollisionWithoutMap_ShouldNotSourceCollisionFromMap()
    {
        var gameScreen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("GameScreen");

        var viewModel = new AddScreenViewModel
        {
            AddScreenType = AddScreenType.BaseLevelScreen,
            IsAddMapLayeredTileMapChecked = false,
            IsAddSolidCollisionShapeCollectionChecked = true
        };

        await GlueFormsCore.Plugins.EmbeddedPlugins.AddScreenPlugin.MainAddScreenPlugin
            .ApplyViewModelToScreen(gameScreen, viewModel);

        var solidCollisionNos = gameScreen.NamedObjects.FirstOrDefault(item => item.InstanceName == "SolidCollision");
        solidCollisionNos.ShouldNotBeNull();

        // CollisionCreationOptions.FromType == 4 is exactly what TileShapeCollectionCodeGenerator.
        // GenerateFromTileType gates on to emit "Map.Height"/"AddCollisionFromTilesWithType(..., Map, ...)" -
        // the literal CS0103 repro from #2099, referencing a "Map" field that was never declared because
        // "Add LayeredTileMap" was left unchecked. Leaving it unset (0/Empty) is what stops that codegen
        // branch from running at all.
        solidCollisionNos.Properties.GetValue<int>("CollisionCreationOptions").ShouldBe(0);
        solidCollisionNos.Properties.GetValue<string>("SourceTmxName").ShouldBeNull();

        var generatedCodeFile = Path.Combine(_tempProjectDirectory, "Screens", "GameScreen.Generated.cs");
        File.Exists(generatedCodeFile).ShouldBeTrue();
    }

    [Fact]
    public async Task ApplyViewModelToScreen_SolidCollisionWithMap_ShouldSourceCollisionFromMap()
    {
        var gameScreen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("GameScreen");

        var viewModel = new AddScreenViewModel
        {
            AddScreenType = AddScreenType.BaseLevelScreen,
            IsAddMapLayeredTileMapChecked = true,
            IsAddSolidCollisionShapeCollectionChecked = true
        };

        await GlueFormsCore.Plugins.EmbeddedPlugins.AddScreenPlugin.MainAddScreenPlugin
            .ApplyViewModelToScreen(gameScreen, viewModel);

        var solidCollisionNos = gameScreen.NamedObjects.FirstOrDefault(item => item.InstanceName == "SolidCollision");
        solidCollisionNos.ShouldNotBeNull();

        // Regression guard: with the Map object present, SolidCollision should still be sourced from it -
        // the fix must not turn this off unconditionally, only when there's no Map to source it from.
        solidCollisionNos.Properties.GetValue<int>("CollisionCreationOptions").ShouldBe(4); // FromType
        solidCollisionNos.Properties.GetValue<string>("SourceTmxName").ShouldBe("Map");

        var generatedCodeFile = Path.Combine(_tempProjectDirectory, "Screens", "GameScreen.Generated.cs");
        File.Exists(generatedCodeFile).ShouldBeTrue();
    }
}
