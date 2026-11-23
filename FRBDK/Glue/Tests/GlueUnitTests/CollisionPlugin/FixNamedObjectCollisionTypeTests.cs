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
using OfficialPlugins.CollisionPlugin.Managers;
using OfficialPlugins.CollisionPlugin.ViewModels;
using OfficialPlugins.Wizard.Managers;
using OfficialPlugins.Wizard.Models;
using Shouldly;

namespace GlueUnitTests.CollisionPlugin;

// WizardProjectLogic.HandleAddPlayerInstance and ElementCommands' rename call
// ICollisionRelationshipCommands.FixNamedObjectCollisionType after changing a relationship's objects or
// collision type. This runs it on a relationship whose FirstCollisionName/SecondCollisionName point at real
// NamedObjectSaves from WizardProjectLogic.HandleAddGameScreen, and asserts the recomputed SourceClassType.
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

        var collisionCommands = new CollisionRelationshipCommands();
        var changedForBounce = collisionCommands.FixNamedObjectCollisionType(relationshipNos);

        changedForBounce.ShouldBeTrue();
        var bounceSourceClassType = relationshipNos.SourceClassType;
        // Non-list-vs-TileShapeCollection, non-platformer CollisionType -> CollidableVsTileShapeCollectionRelationship<T>.
        bounceSourceClassType.ShouldStartWith("FlatRedBall.Math.Collision.CollidableVsTileShapeCollectionRelationship<");
        bounceSourceClassType.ShouldContain("TileShapeCollection");

        // Mirrors WizardProjectLogic.cs's isPlatformer branch (~line 1020): the CollisionType property is
        // changed, then FixNamedObjectCollisionType is called again to recompute SourceClassType to match.
        relationshipNos.Properties.SetValue(nameof(CollisionRelationshipViewModel.CollisionType), (int)OfficialPlugins.CollisionPlugin.ViewModels.CollisionType.PlatformerSolidCollision);
        var changedForPlatformer = collisionCommands.FixNamedObjectCollisionType(relationshipNos);

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
