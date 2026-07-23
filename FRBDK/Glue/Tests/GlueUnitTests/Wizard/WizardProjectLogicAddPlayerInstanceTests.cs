using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FlatRedBall.Glue.Managers;
using Glue;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;
using GlueUnitTests.TestSupport;
using GlueUnitTests.Tasks;
using OfficialPlugins.CollisionPlugin.ViewModels;
using OfficialPlugins.Wizard.Managers;
using OfficialPlugins.Wizard.Models;
using Shouldly;

namespace GlueUnitTests.Wizard;

// Follow-up to GitHub issue #1894: after Collision (#1901) / Gum (#1902) / Tiled (#1903) / Entity Input
// Movement (#1904) plugin coverage, the one remaining item was the PluginManager.TabControlViewModel /
// mMenuStrip UI coupling flagged in #1901's investigation, which blocked the real
// WizardProjectLogic.HandleAddPlayerInstance path (collision-relationship creation between the player
// entity and level geometry) from being driven end-to-end.
//
// Two things were actually needed to unblock it (see PluginManager.cs and GlueTestBootstrap.cs):
//  - PluginManager.TabControlViewModel is now given a real (not fake) TabControlViewModel by
//    GlueTestBootstrap.EnsureInitialized. It's a plain MVVM view model - ObservableCollection-backed tab
//    containers, no live WPF Dispatcher/message loop needed to construct one - only ever set in
//    production by MainPanelControl.xaml.cs's real WPF startup. DialogCommands.FocusTab (called at the
//    end of collision-relationship creation) just finds no matching tab in this always-empty instance and
//    returns, which is not an error case for that code.
//  - PluginManager.ReactToCreateCollisionRelationshipsBetween (what HandleAddPlayerInstance actually
//    calls) is a static event with no subscriber unless a real MainCollisionPlugin has run StartUp - true
//    in this test host, since PluginManager.LoadPlugins' reflection/directory-scan plugin discovery never
//    runs here. GlueTestBootstrap.EnsureCollisionPluginRegisteredWithPluginManager constructs a real
//    MainCollisionPlugin and registers it via the new PluginManager.RegisterPluginForTesting - the same
//    dispatch-table registration StartupPlugin does for a discovered plugin, just without the discovery
//    step in front of it. That, in turn, surfaced one more real gap: PluginManager's synchronous plugin
//    dispatch (used by PluginManager.CallPluginMethod, e.g. "FixNamedObjectCollisionType") unconditionally
//    read the private static PluginManager.mMenuStrip - null outside a live MainGlueWindow - fixed with a
//    null-guard (same shape as the earlier MainGlueWindow.Self.HasErrorOccurred fix).
//
// With those in place, this drives the real, full WizardProjectLogic.Apply(vm) - the exact
// HandleAddPlayerInstance path #1901 stopped at - and asserts real production side effects: both
// collision-relationship NamedObjectSaves exist, wired to the real player list and collision objects, with
// SourceClassType recomputed by the real (now actually-dispatched) FixNamedObjectCollisionType - not just
// "Apply() didn't throw."
[Collection(nameof(TaskManagerSequentialCollection))]
public class WizardProjectLogicAddPlayerInstanceTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly string _originalRelativeDirectory;
    private readonly bool _originalSynchronousMode;
    private readonly IUiThreadMarshaller _originalMarshaller;
    private readonly string _tempProjectDirectory;

    public WizardProjectLogicAddPlayerInstanceTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        GlueTestBootstrap.EnsureCollisionPluginRegisteredWithPluginManager();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = ObjectFinder.Self.GlueProject;
        _originalRelativeDirectory = FlatRedBall.IO.FileManager.RelativeDirectory;
        _originalSynchronousMode = TaskManager.SynchronousMode;
        _originalMarshaller = TaskManager.UiThreadMarshaller;

        var vsProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);

        GlueState.Self.CurrentMainProject = vsProject;
        ObjectFinder.Self.GlueProject = new GlueProjectSave();
        FlatRedBall.IO.FileManager.RelativeDirectory = _tempProjectDirectory + "\\";

        TaskManager.SynchronousMode = true;
        TaskManager.UiThreadMarshaller = new InlineUiThreadMarshaller();

        // PluginManager.PluginCommand normally catches exceptions thrown by real plugin dispatch and just
        // logs them (production correctly doesn't want one misbehaving plugin to crash Glue.exe) - but that
        // means a real bug in the collision-relationship creation path this test drives would silently
        // produce a wrong/incomplete NamedObjectSave instead of failing the test. Disabled here so any real
        // exception surfaces directly as a test failure.
        FlatRedBall.Glue.Plugins.PluginManager.HandleExceptions = false;
    }

    public void Dispose()
    {
        GlueState.Self.CurrentMainProject = _originalMainProject;
        ObjectFinder.Self.GlueProject = _originalGlueProject;
        FlatRedBall.IO.FileManager.RelativeDirectory = _originalRelativeDirectory;
        TaskManager.SynchronousMode = _originalSynchronousMode;
        TaskManager.UiThreadMarshaller = _originalMarshaller;
        FlatRedBall.Glue.Plugins.PluginManager.HandleExceptions = true;

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
    public async Task Apply_ShouldCreateRealCollisionRelationships_ForPlayerVsSolidAndCloudCollision()
    {
        var vm = new WizardViewModel
        {
            AddGameScreen = true,
            AddSolidCollision = true,
            AddCloudCollision = true,
            AddPlayerEntity = true,
            PlayerCreationType = PlayerCreationType.SelectOptions,
            PlayerControlType = GameType.TopDown,
            PlayerCollisionType = OfficialPlugins.Wizard.Models.CollisionType.Rectangle,
            AddPlayerListToGameScreen = true,
            AddPlayerToList = true,
            CollideAgainstSolidCollision = true,
            CollideAgainstCloudCollision = true,
            // Skip sprite/animation setup entirely - irrelevant to collision-relationship creation, and
            // avoids embedded-resource copying that has nothing to do with what this test covers.
            AddPlayerSprite = false,
        };

        await WizardProjectLogic.Self.Apply(vm);

        var glueProject = ObjectFinder.Self.GlueProject;
        var gameScreen = glueProject.Screens.FirstOrDefault(item => item.Name == @"Screens\GameScreen");
        gameScreen.ShouldNotBeNull();

        // Real side effect #1: HandleAddPlayerEntity's real CreatePlayerEntityFromOptions ran and produced
        // a real Player entity, and AddEntityViewModel.IncludeListsInScreens (set from
        // vm.AddPlayerListToGameScreen) produced a real player list NamedObjectSave in GameScreen.
        var playerEntity = glueProject.Entities.FirstOrDefault(item => item.GetStrippedName() == "Player");
        playerEntity.ShouldNotBeNull();

        var playerList = gameScreen.NamedObjects.FirstOrDefault(
            item => item.IsList && item.SourceClassGenericType == playerEntity.Name);
        playerList.ShouldNotBeNull();

        var solidCollisionNos = gameScreen.NamedObjects.FirstOrDefault(item => item.InstanceName == "SolidCollision");
        solidCollisionNos.ShouldNotBeNull();
        var cloudCollisionNos = gameScreen.NamedObjects.FirstOrDefault(item => item.InstanceName == "CloudCollision");
        cloudCollisionNos.ShouldNotBeNull();

        // Real side effect #2: PluginManager.ReactToCreateCollisionRelationshipsBetween found the real,
        // now-registered MainCollisionPlugin subscriber and ran
        // CollidableNamedObjectController.CreateCollisionRelationshipBetweenObjects for real - these
        // relationship NamedObjectSaves only exist if that production logic actually executed (previously
        // this event had no subscriber and silently produced nothing).
        var solidRelationship = gameScreen.NamedObjects.FirstOrDefault(item =>
            item.InstanceName == "PlayerVsSolidCollision" || item.InstanceName == "PlayerListVsSolidCollision");
        solidRelationship.ShouldNotBeNull();
        solidRelationship.Properties.GetValue<string>(nameof(CollisionRelationshipViewModel.FirstCollisionName))
            .ShouldBe(playerList.InstanceName);
        solidRelationship.Properties.GetValue<string>(nameof(CollisionRelationshipViewModel.SecondCollisionName))
            .ShouldBe(solidCollisionNos.InstanceName);

        var cloudRelationship = gameScreen.NamedObjects.FirstOrDefault(item =>
            item.InstanceName == "PlayerVsCloudCollision" || item.InstanceName == "PlayerListVsCloudCollision");
        cloudRelationship.ShouldNotBeNull();
        cloudRelationship.Properties.GetValue<string>(nameof(CollisionRelationshipViewModel.FirstCollisionName))
            .ShouldBe(playerList.InstanceName);
        cloudRelationship.Properties.GetValue<string>(nameof(CollisionRelationshipViewModel.SecondCollisionName))
            .ShouldBe(cloudCollisionNos.InstanceName);

        // Real side effect #3: PluginManager.CallPluginMethod("Collision Plugin",
        // "FixNamedObjectCollisionType", ...) now actually finds and dispatches to the real
        // MainCollisionPlugin (previously silently no-op'd, since no plugin was registered) - proven by
        // SourceClassType having been recomputed to a real collision-relationship runtime type, not left
        // at whatever CreateCollisionRelationshipBetweenObjects's own initial computation produced.
        solidRelationship.SourceClassType.ShouldStartWith("FlatRedBall.Math.Collision.");
        cloudRelationship.SourceClassType.ShouldStartWith("FlatRedBall.Math.Collision.");

        // Both relationships are real, distinct NamedObjectSaves wired into the screen.
        gameScreen.NamedObjects.ShouldContain(solidRelationship);
        gameScreen.NamedObjects.ShouldContain(cloudRelationship);
        solidRelationship.ShouldNotBe(cloudRelationship);

        // Real code generation ran for the containing screen after both relationships were added.
        var generatedCodeFile = Path.Combine(_tempProjectDirectory, "Screens", "GameScreen.Generated.cs");
        File.Exists(generatedCodeFile).ShouldBeTrue();
    }

    private class InlineUiThreadMarshaller : IUiThreadMarshaller
    {
        public void Invoke(Action action) => action();
        public T Invoke<T>(Func<T> func) => func();
        public Task Invoke(Func<Task> func) => func();
        public Task<T> Invoke<T>(Func<Task<T>> func) => func();
        public void BeginInvoke(Action action) => action();
    }
}
