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
using GumPlugin.Managers;
using OfficialPlugins.CollisionPlugin.ViewModels;
using OfficialPlugins.Wizard.Managers;
using OfficialPlugins.Wizard.Models;
using Shouldly;

namespace GlueUnitTests.Wizard;

// Follow-up to GitHub issue #1894: every existing Wizard test (WizardProjectLogicAddGameScreenTests,
// WizardProjectLogicAddPlayerInstanceTests, and the per-plugin Collision/Gum/Tiled/Entity-Input-Movement
// test files) drives WizardProjectLogic.Apply/HandleAddGameScreen/HandleAddPlayerInstance with a SINGLE
// feature enabled at a time. This file drives one real Apply(vm) call with several WizardViewModel options
// enabled together - the combination an actual new-project user would pick via WizardViewModel.ApplyDefaults()
// - to catch the class of bug isolated tests structurally cannot: shared-state/ordering/naming collisions
// between steps.
//
// Combo driven here (a narrowed version of ApplyDefaults()): AddGameScreen + AddTiledMap + AddSolidCollision
// + AddCloudCollision + AddHudLayer + AddGum(no forms) + AddPlayerEntity/AddPlayerListToGameScreen/
// AddPlayerToList + CollideAgainstSolidCollision/CollideAgainstCloudCollision + AddCameraController
// (FollowPlayersWithCamera + KeepCameraInMap). Two things ApplyDefaults() also sets were deliberately
// dropped, both for reasons distinct from (and in the Gum case, deeper than) previously-documented blockers:
//
//  - AddFlatRedBallForms: the already-documented blocker (REFACTORING.md's "Real CreateGumProjectWithForms
//    coverage" entry, and every Gum test since) - MainGumPlugin.HandleBuildMissingFonts spawns an external
//    process at a Glue.exe-relative path. Not revisited here; this test uses the "no forms" branch, as
//    every other Wizard test touching Gum has.
//  - AddGameScreenGumToHudLayer (and by extension real dispatch of the "Add Gum" task itself): a NEW
//    blocker found while building this test, deeper than the Forms one. WizardProjectLogic.HandleAddGum
//    dispatches through PluginManager.CallPluginMethodAsync("Gum Plugin", "CreateGumProjectNoForms", ...),
//    which silently no-ops unless a real MainGumPlugin is registered with PluginManager (the same
//    "PluginManager.CallPluginMethod no-ops without a registered plugin" gap #1901/#1902/#1903/#1904 fixed
//    per-plugin). Unlike Collision Plugin (registered successfully in #1905's
//    EnsureCollisionPluginRegisteredWithPluginManager), MainGumPlugin.StartUp unconditionally calls
//    AddMenuItemTo("New Gum Project", ..., Localization.MenuIds.ContentId) -> PluginBase.GetItem(container),
//    which reads `FlatRedBall.Glue.AutomatedGlue.GlueGui.MenuStrip.Items` - a static ONLY ever initialized
//    by a live MainGlueWindow building out the real WinForms menu tree (including the "Content" parent menu
//    GetItem searches for by name). GlueTestBootstrap's FakeMainGlueWindow/PluginManager.mMenuStrip
//    null-guard don't cover this - GlueGui.MenuStrip is a separate, unrelated static, and a bare
//    `new MenuStrip()` doesn't have the "Content" item GetItem needs, so `Initialize`ing one still NREs one
//    level down at `itemToAddTo.DropDownItems.Add`. Reproducing Glue's real menu tree in a test host is a
//    materially bigger seam than this test-coverage pass justifies, so: the real Gum project is pre-seeded
//    directly via NewGumProjectCreationLogic.CreateGumProjectInternal (the same helper
//    GumPluginTests.GumProjectCreationTests already uses/proves for the "no forms" branch), and
//    AddGameScreenGumToHudLayer is left false so WizardProjectLogic.HandleAddGumScreenToLayer (which
//    references a Gum-generated "GameScreenGum.gusx" that only gets created via a live Gum Plugin's
//    NewScreenCreated handler) is never invoked. vm.AddGum is still set true - the real, unregistered "Add
//    Gum" task in Apply() still runs and still silently no-ops (proven harmless, not skipped) alongside
//    everything else.
//
// What this DOES prove for real, not pre-seeded: the collision-relationship NamedObjectSaves
// (PlayerVsSolidCollision/PlayerVsCloudCollision or PlayerListVs- equivalents) reference the SolidCollision/
// CloudCollision TileShapeCollection NamedObjectSaves added earlier in the SAME Apply() call (not fixture
// data) - the ordering/wiring bug class isolated tests can't catch. And that the pre-seeded Gum project's
// ReferencedFileSave and generated GumIdb.Generated.cs survive Apply()'s own "Regenerate All Code"/"Saving
// Project" steps running afterward, coexisting with every NamedObjectSave Apply() itself adds to GameScreen
// (HudLayer, Map, SolidCollision, CloudCollision, the player list/instance, both collision relationships,
// the camera controller) with no name collisions or lost state.
[Collection(nameof(TaskManagerSequentialCollection))]
public class WizardProjectLogicCombinedScenarioTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly string _originalRelativeDirectory;
    private readonly bool _originalSynchronousMode;
    private readonly IUiThreadMarshaller _originalMarshaller;
    private readonly Gum.DataTypes.GumProjectSave? _originalGumProjectSave;
    private readonly string _tempProjectDirectory;

    public WizardProjectLogicCombinedScenarioTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        GlueTestBootstrap.EnsureCollisionPluginRegisteredWithPluginManager();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = ObjectFinder.Self.GlueProject;
        _originalRelativeDirectory = FlatRedBall.IO.FileManager.RelativeDirectory;
        _originalSynchronousMode = TaskManager.SynchronousMode;
        _originalMarshaller = TaskManager.UiThreadMarshaller;
        _originalGumProjectSave = Gum.Managers.ObjectFinder.Self.GumProjectSave;

        var vsProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);

        GlueState.Self.CurrentMainProject = vsProject;
        ObjectFinder.Self.GlueProject = new GlueProjectSave();
        // A real project (loaded from disk, or created via the New Project Creator) always has a non-null
        // DisplaySettings by the time the Wizard runs - WizardProjectLogic.ApplyMainCameraSettings assumes
        // this and NREs otherwise. No prior Wizard test reached this (none set AddCameraController = true),
        // so this is the first test needing it - completing the minimal fixture, not masking a real bug.
        ObjectFinder.Self.GlueProject.DisplaySettings = new FlatRedBall.Glue.SaveClasses.DisplaySettings();
        FlatRedBall.IO.FileManager.RelativeDirectory = _tempProjectDirectory + "\\";

        TaskManager.SynchronousMode = true;
        TaskManager.UiThreadMarshaller = new InlineUiThreadMarshaller();
        Gum.Managers.ObjectFinder.Self.GumProjectSave = null;

        // See REFACTORING.md's "HandleAddPlayerInstance" entry: PluginManager.PluginCommand normally
        // catches and logs exceptions from real plugin dispatch so one misbehaving plugin can't crash a
        // live Glue.exe. Disabled here so a real bug anywhere in this combined path surfaces as a test
        // failure instead of being silently swallowed.
        FlatRedBall.Glue.Plugins.PluginManager.HandleExceptions = false;
    }

    public void Dispose()
    {
        GlueState.Self.CurrentMainProject = _originalMainProject;
        ObjectFinder.Self.GlueProject = _originalGlueProject;
        FlatRedBall.IO.FileManager.RelativeDirectory = _originalRelativeDirectory;
        TaskManager.SynchronousMode = _originalSynchronousMode;
        TaskManager.UiThreadMarshaller = _originalMarshaller;
        Gum.Managers.ObjectFinder.Self.GumProjectSave = _originalGumProjectSave;
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
    public async Task Apply_ShouldWireGameScreenTiledMapCollisionPlayerGumAndCamera_WhenAllEnabledTogether()
    {
        // Pre-seed a real Gum project the same way GumPluginTests.GumProjectCreationTests does - see the
        // class comment above for why vm.AddGum's own dispatch through Apply() can't reach
        // MainGumPlugin/CreateGumProjectNoForms for real in this test host.
        var gumCreationLogic = new NewGumProjectCreationLogic(new GumxPropertiesManager());
        await gumCreationLogic.CreateGumProjectInternal(shouldAlsoAddForms: false, askToOverwrite: false);
        var gumRfsBeforeApply = GumProjectManager.Self.GetRfsForGumProject();
        gumRfsBeforeApply.ShouldNotBeNull();

        var vm = new WizardViewModel
        {
            AddGameScreen = true,
            AddTiledMap = true,
            AddSolidCollision = true,
            AddCloudCollision = true,
            AddHudLayer = true,

            AddGum = true,
            AddFlatRedBallForms = false,
            AddGameScreenGumToHudLayer = false,

            AddPlayerEntity = true,
            PlayerCreationType = PlayerCreationType.SelectOptions,
            PlayerControlType = GameType.TopDown,
            PlayerCollisionType = OfficialPlugins.Wizard.Models.CollisionType.Rectangle,
            AddPlayerListToGameScreen = true,
            AddPlayerToList = true,
            CollideAgainstSolidCollision = true,
            CollideAgainstCloudCollision = true,
            // Skip sprite/animation setup - orthogonal to what this test covers, and avoids embedded-
            // resource copying that has nothing to do with cross-feature wiring.
            AddPlayerSprite = false,

            // Skip levels: WizardProjectLogic.CreateLevel drives the Tiled Plugin through
            // PluginManager.CallPluginMethod, the same "needs real plugin registration" gap as Gum - out of
            // scope for this combo (Tiled/Entity-Input-Movement already have their own dedicated,
            // registration-free coverage - see TiledPluginTests/EntityInputMovementPluginTests).
            CreateLevels = false,

            AddCameraController = true,
            FollowPlayersWithCamera = true,
            KeepCameraInMap = true,
            SelectedCameraResolution = CameraResolution._480x360,
            ScalePercent = 200,
        };

        await WizardProjectLogic.Self.Apply(vm);

        var glueProject = ObjectFinder.Self.GlueProject;
        var gameScreen = glueProject.Screens.FirstOrDefault(item => item.Name == @"Screens\GameScreen");
        gameScreen.ShouldNotBeNull();
        glueProject.StartUpScreen.ShouldBe(gameScreen.Name);

        // ---- Real side effects from each individual feature, all landing in the SAME GameScreen ----

        var hudLayerNos = gameScreen.NamedObjects.FirstOrDefault(item => item.InstanceName == "HudLayer");
        hudLayerNos.ShouldNotBeNull();

        var mapNos = gameScreen.NamedObjects.FirstOrDefault(item =>
            item.InstanceName == "Map" && item.SourceClassType == "FlatRedBall.TileGraphics.LayeredTileMap");
        mapNos.ShouldNotBeNull();

        var solidCollisionNos = gameScreen.NamedObjects.FirstOrDefault(item => item.InstanceName == "SolidCollision");
        solidCollisionNos.ShouldNotBeNull();
        solidCollisionNos.SourceClassType.ShouldBe("FlatRedBall.TileCollisions.TileShapeCollection");

        var cloudCollisionNos = gameScreen.NamedObjects.FirstOrDefault(item => item.InstanceName == "CloudCollision");
        cloudCollisionNos.ShouldNotBeNull();

        var playerEntity = glueProject.Entities.FirstOrDefault(item => item.GetStrippedName() == "Player");
        playerEntity.ShouldNotBeNull();

        var playerList = gameScreen.NamedObjects.FirstOrDefault(
            item => item.IsList && item.SourceClassGenericType == playerEntity.Name);
        playerList.ShouldNotBeNull();

        var playerInstanceNos = gameScreen.NamedObjects
            .Concat(playerList.ContainedObjects)
            .FirstOrDefault(item => item.InstanceName == "Player1");
        playerInstanceNos.ShouldNotBeNull();

        var cameraNos = gameScreen.NamedObjects.FirstOrDefault(item => item.InstanceName == "CameraControllingEntityInstance");
        cameraNos.ShouldNotBeNull();
        cameraNos.SourceClassType.ShouldBe("FlatRedBall.Entities.CameraControllingEntity");
        // SetVariableOnAsync writes an instance-variable assignment (InstructionSaves), not a Properties
        // entry - Properties is for ATI/meta properties like CollisionType.
        ((string)cameraNos.InstructionSaves.First(i => i.Member == nameof(FlatRedBall.Entities.CameraControllingEntity.Targets)).Value)
            .ShouldBe(playerList.InstanceName);
        ((string)cameraNos.InstructionSaves.First(i => i.Member == nameof(FlatRedBall.Entities.CameraControllingEntity.Map)).Value)
            .ShouldBe(mapNos.InstanceName);

        // ---- Cross-feature assertion #1: collision relationships reference the TileShapeCollections added
        // earlier in this SAME Apply() call - not pre-seeded fixtures. This is the ordering/wiring bug class
        // isolated single-feature tests structurally can't catch. ----
        var solidRelationship = gameScreen.NamedObjects.FirstOrDefault(item =>
            item.InstanceName == "PlayerVsSolidCollision" || item.InstanceName == "PlayerListVsSolidCollision");
        solidRelationship.ShouldNotBeNull();
        solidRelationship.Properties.GetValue<string>(nameof(CollisionRelationshipViewModel.FirstCollisionName))
            .ShouldBe(playerList.InstanceName);
        solidRelationship.Properties.GetValue<string>(nameof(CollisionRelationshipViewModel.SecondCollisionName))
            .ShouldBe(solidCollisionNos.InstanceName);
        solidRelationship.SourceClassType.ShouldStartWith("FlatRedBall.Math.Collision.");

        var cloudRelationship = gameScreen.NamedObjects.FirstOrDefault(item =>
            item.InstanceName == "PlayerVsCloudCollision" || item.InstanceName == "PlayerListVsCloudCollision");
        cloudRelationship.ShouldNotBeNull();
        cloudRelationship.Properties.GetValue<string>(nameof(CollisionRelationshipViewModel.FirstCollisionName))
            .ShouldBe(playerList.InstanceName);
        cloudRelationship.Properties.GetValue<string>(nameof(CollisionRelationshipViewModel.SecondCollisionName))
            .ShouldBe(cloudCollisionNos.InstanceName);
        cloudRelationship.SourceClassType.ShouldStartWith("FlatRedBall.Math.Collision.");

        solidRelationship.ShouldNotBe(cloudRelationship);

        // ---- Cross-feature assertion #2: every feature's NamedObjectSave coexists simultaneously in the
        // same screen - nothing overwritten/lost by a later step in the same Apply() call. ----
        var expectedInstanceNames = new[]
        {
            "HudLayer", "Map", "SolidCollision", "CloudCollision", playerList.InstanceName,
            solidRelationship.InstanceName, cloudRelationship.InstanceName, "CameraControllingEntityInstance",
        };
        foreach (var expectedName in expectedInstanceNames)
        {
            gameScreen.NamedObjects.Count(item => item.InstanceName == expectedName).ShouldBe(1,
                $"expected exactly one '{expectedName}' NamedObjectSave in {gameScreen.Name}, proving it wasn't duplicated or overwritten by a later Apply() step");
        }

        // ---- Cross-feature assertion #3: the Gum project pre-seeded before Apply() survives Apply()'s own
        // "Regenerate All Code"/"Saving Project" steps untouched - proving Gum's RFS/generated code coexist
        // with everything the rest of Apply() added, not clobbered by it. ----
        var gumRfsAfterApply = GumProjectManager.Self.GetRfsForGumProject();
        gumRfsAfterApply.ShouldNotBeNull();
        gumRfsAfterApply.ShouldBe(gumRfsBeforeApply);
        glueProject.GlobalFiles.ShouldContain(gumRfsAfterApply);
        Directory.GetFiles(_tempProjectDirectory, "GumIdb.Generated.cs", SearchOption.AllDirectories)
            .ShouldNotBeEmpty();

        // ---- Cross-feature assertion #4: generated code for GameScreen contains real members from every
        // included feature, not just the last one added. ----
        var generatedCodeFile = Path.Combine(_tempProjectDirectory, "Screens", "GameScreen.Generated.cs");
        File.Exists(generatedCodeFile).ShouldBeTrue();
        var generatedCode = File.ReadAllText(generatedCodeFile);
        generatedCode.ShouldContain("HudLayer");
        generatedCode.ShouldContain("SolidCollision");
        generatedCode.ShouldContain("CloudCollision");
        generatedCode.ShouldContain(playerList.InstanceName);
        generatedCode.ShouldContain("CameraControllingEntityInstance");
        generatedCode.ShouldContain(solidRelationship.InstanceName);
        generatedCode.ShouldContain(cloudRelationship.InstanceName);

        // ---- Camera settings from ApplyMainCameraSettings/GetDisplaySettingsFor also applied in this same
        // Apply() call. ----
        glueProject.DisplaySettings.ResolutionWidth.ShouldBe(480);
        glueProject.DisplaySettings.ResolutionHeight.ShouldBe(360);
        glueProject.DisplaySettings.Scale.ShouldBe(200);

        // "Saving Project" ran at the end of this same Apply() call.
        File.Exists(GlueState.Self.GlueProjectFileName.FullPath).ShouldBeTrue();
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
