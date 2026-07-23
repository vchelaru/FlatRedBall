using System;
using System.IO;
using System.Threading.Tasks;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.TestSupport;
using GlueUnitTests.Tasks;
using OfficialPlugins.CollisionPlugin.Controllers;
using OfficialPlugins.CollisionPlugin.ViewModels;
using OfficialPlugins.Wizard.Managers;
using OfficialPlugins.Wizard.Models;
using Shouldly;

namespace GlueUnitTests.CollisionPlugin;

// Follow-up to GitHub issue #1894 (reopened): WizardProjectLogic.HandleAddPlayerInstance's two
// FixNamedObjectCollisionType call sites (~line 996/1033) go through
// PluginManager.CallPluginMethod("Collision Plugin", "FixNamedObjectCollisionType", nos), which silently
// no-ops in a test host (no plugin registered with PluginManager) - exactly the false-coverage risk the
// issue was reopened for: a naive test could pass without the real fixup ever running.
//
// MainCollisionPlugin.FixNamedObjectCollisionType is already a one-line forwarder to the public static
// CollisionRelationshipViewModelController.TryFixSourceClassType (same "thin forwarder to a directly-
// callable static method" shape as MainCollisionPlugin.RegisterAssetTypes) - no extraction was needed,
// it was already directly callable. This test calls that real method directly (bypassing
// PluginManager.CallPluginMethod, per option (b) in the #1894 follow-up plan - see REFACTORING.md) on a
// NamedObjectSave whose FirstCollisionName/SecondCollisionName point at real NamedObjectSaves produced by
// the real WizardProjectLogic.HandleAddGameScreen path, and asserts the real recomputed SourceClassType -
// not just that the call didn't throw.
//
// Registering "Collision Plugin" with PluginManager so CallPluginMethod itself resolves it for real
// (option (a)) was investigated and rejected here: PluginManager.CallMethodOnPlugin always routes calls
// through PluginCommand(doOnUiThread: true), which reads the private static PluginManager.mMenuStrip -
// only ever set by MainGlueWindow's real startup (PluginManager.ShareMenuStripReference), so a registered
// plugin would still NRE there. And even if that were faked, the real call sites' surrounding method
// (HandleAddPlayerInstance) is blocked further upstream: PluginManager.ReactToCreateCollisionRelationshipsBetween
// needs a live MainCollisionPlugin subscriber (only wired by a fully-instantiated, StartUp'd plugin), and
// its handler (CollidableNamedObjectController.CreateCollisionRelationshipBetweenObjects) then calls
// DialogCommands.FocusTab, which reads PluginManager.TabControlViewModel - only ever set by a live
// WinForms MainGlueWindow. That's a PluginManager-wide UI coupling (not Collision-Plugin-specific),
// already flagged as "deliberately not attempted" in REFACTORING.md's "AvailableAssetTypes.Self.Initialize
// / Collision Plugin asset-type test seam" entry - out of scope for a Collision-Plugin-only pass. See
// REFACTORING.md for the full writeup and the precise blocker chain.
[Collection(nameof(TaskManagerSequentialCollection))]
public class FixNamedObjectCollisionTypeTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly string _originalRelativeDirectory;
    private readonly bool _originalSynchronousMode;
    private readonly IUiThreadMarshaller _originalMarshaller;
    private readonly string _tempProjectDirectory;

    public FixNamedObjectCollisionTypeTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        GlueTestBootstrap.EnsureCollisionPluginAssetTypesRegistered();

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
    }

    public void Dispose()
    {
        GlueState.Self.CurrentMainProject = _originalMainProject;
        ObjectFinder.Self.GlueProject = _originalGlueProject;
        FlatRedBall.IO.FileManager.RelativeDirectory = _originalRelativeDirectory;
        TaskManager.SynchronousMode = _originalSynchronousMode;
        TaskManager.UiThreadMarshaller = _originalMarshaller;

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
    public async Task TryFixSourceClassType_ShouldRecomputeSourceClassType_WhenCollisionTypePropertyChanges()
    {
        // Real Wizard path: produces real SolidCollision/CloudCollision TileShapeCollection NamedObjectSaves,
        // the same objects WizardProjectLogic.HandleAddPlayerInstance passes into
        // PluginManager.ReactToCreateCollisionRelationshipsBetween / FixNamedObjectCollisionType.
        var vm = new WizardViewModel { AddGameScreen = true, AddSolidCollision = true, AddCloudCollision = true };
        var (gameScreen, solidCollisionNos, cloudCollisionNos) = await WizardProjectLogic.HandleAddGameScreen(vm);

        // Mirrors the shape of the real "PlayerVsSolidCollision"/"PlayerVsCloudCollision" NamedObjectSave
        // that CollidableNamedObjectController.CreateCollisionRelationshipBetweenObjects would produce:
        // a collision-relationship NamedObjectSave whose SourceClassType is derived from its
        // FirstCollisionName/SecondCollisionName/CollisionType properties (see
        // AssetTypeInfoManager.GetCollisionRelationshipSourceClassType), constructed directly here since
        // driving the real creation path is blocked by the PluginManager.TabControlViewModel issue
        // described above.
        var relationshipNos = new NamedObjectSave();
        relationshipNos.SetDefaults();
        relationshipNos.InstanceName = "PlayerVsSolidCollision";
        relationshipNos.SourceType = SourceType.FlatRedBallType;
        relationshipNos.Properties.SetValue(nameof(CollisionRelationshipViewModel.FirstCollisionName), solidCollisionNos.InstanceName);
        relationshipNos.Properties.SetValue(nameof(CollisionRelationshipViewModel.SecondCollisionName), cloudCollisionNos.InstanceName);
        relationshipNos.Properties.SetValue(nameof(CollisionRelationshipViewModel.CollisionType), (int)OfficialPlugins.CollisionPlugin.ViewModels.CollisionType.BounceCollision);
        // Stale placeholder value, as if this NamedObjectSave was just created - still recognized as a
        // collision relationship by IsCollisionRelationship() (used as the entry gate by TryFixSourceClassType).
        relationshipNos.SourceClassType = "FlatRedBall.Math.Collision.CollisionRelationship";
        gameScreen.NamedObjects.Add(relationshipNos);

        // The seam under test: this is exactly what MainCollisionPlugin.FixNamedObjectCollisionType does
        // (a one-line forward), and exactly what
        // PluginManager.CallPluginMethod("Collision Plugin", "FixNamedObjectCollisionType", nos) invokes
        // via reflection in production - real production logic, not a fake.
        var changedForBounce = CollisionRelationshipViewModelController.TryFixSourceClassType(relationshipNos);

        changedForBounce.ShouldBeTrue();
        var bounceSourceClassType = relationshipNos.SourceClassType;
        // Non-list-vs-TileShapeCollection, non-platformer CollisionType -> CollidableVsTileShapeCollectionRelationship<T>.
        bounceSourceClassType.ShouldStartWith("FlatRedBall.Math.Collision.CollidableVsTileShapeCollectionRelationship<");
        bounceSourceClassType.ShouldContain("TileShapeCollection");

        // Mirrors WizardProjectLogic.cs's isPlatformer branch (~line 1020): the CollisionType property is
        // changed, then FixNamedObjectCollisionType is called again to recompute SourceClassType to match.
        relationshipNos.Properties.SetValue(nameof(CollisionRelationshipViewModel.CollisionType), (int)OfficialPlugins.CollisionPlugin.ViewModels.CollisionType.PlatformerSolidCollision);
        var changedForPlatformer = CollisionRelationshipViewModelController.TryFixSourceClassType(relationshipNos);

        changedForPlatformer.ShouldBeTrue();
        // Platformer CollisionType -> DelegateCollisionRelationship<T1, T2>, a different runtime type -
        // proves the recompute is driven by the real CollisionType property, not cached/stale.
        relationshipNos.SourceClassType.ShouldStartWith("FlatRedBall.Math.Collision.DelegateCollisionRelationship<");
        relationshipNos.SourceClassType.ShouldNotBe(bounceSourceClassType);
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
