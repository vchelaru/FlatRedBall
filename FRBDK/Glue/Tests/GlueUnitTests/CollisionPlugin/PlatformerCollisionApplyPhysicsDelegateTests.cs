using System;
using System.IO;
using System.Threading.Tasks;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.TestSupport;
using GlueUnitTests.Tasks;
using OfficialPlugins.CollisionPlugin;
using OfficialPlugins.CollisionPlugin.ViewModels;
using OfficialPlugins.Wizard.Managers;
using OfficialPlugins.Wizard.Models;
using Shouldly;

namespace GlueUnitTests.CollisionPlugin;

// GitHub issue #942: platformer collision relationships (PlatformerSolidCollision/
// PlatformerCloudCollision) always apply physics whenever a collision is detected - there was no way
// for game code to say "this collision happened, but don't apply physics this time". The fix adds
// ApplyPhysics (a Func<First,Second,bool>) to the engine's Delegate*Relationship classes used by
// platformer relationships; CollisionCodeGenerator.GenerateInitializeCodeFor now wires the generated
// CollisionFunction to consult it when set, falling back to always applying physics (the pre-#942
// behavior, and what every project below the new GluxVersions gate still gets) when it's left null.
//
// This pins CollisionCodeGenerator.GenerateInitializeCodeFor - the same static method used both as
// AssetTypeInfoManager's ConstructorFunc and directly here - against a real PlatformerSolidCollision
// NamedObjectSave whose FirstCollisionName/SecondCollisionName resolve against real NamedObjectSaves
// in a real GameScreen (produced by WizardProjectLogic.HandleAddGameScreen, same real production path
// FixNamedObjectCollisionTypeTests/CollisionAssetTypeRegistrationTests use), asserting on the
// generated code text rather than on runtime behavior (which needs a compiled/running game, out of
// reach for a Glue-side unit test).
[Collection(nameof(TaskManagerSequentialCollection))]
public class PlatformerCollisionApplyPhysicsDelegateTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly string _originalRelativeDirectory;
    private readonly bool _originalSynchronousMode;
    private readonly IUiThreadMarshaller _originalMarshaller;
    private readonly string _tempProjectDirectory;

    public PlatformerCollisionApplyPhysicsDelegateTests()
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

    private async Task<(FlatRedBall.Glue.SaveClasses.ScreenSave gameScreen, NamedObjectSave relationshipNos)> CreatePlatformerSolidCollisionRelationship()
    {
        var vm = new WizardViewModel { AddGameScreen = true, AddSolidCollision = true };
        var (gameScreen, solidCollisionNos, _) = await WizardProjectLogic.HandleAddGameScreen(vm);

        // Stand-in for the player entity: any object type with a resolvable SourceClassType works here
        // since we're only pinning generated *text*, not compiling/running the result.
        var playerNos = new NamedObjectSave();
        playerNos.SetDefaults();
        playerNos.InstanceName = "Player";
        playerNos.SourceType = SourceType.FlatRedBallType;
        playerNos.SourceClassType = "FlatRedBall.Sprite";
        gameScreen.NamedObjects.Add(playerNos);

        var relationshipNos = new NamedObjectSave();
        relationshipNos.SetDefaults();
        relationshipNos.InstanceName = "PlayerVsSolidCollision";
        relationshipNos.SourceType = SourceType.FlatRedBallType;
        relationshipNos.Properties.SetValue(nameof(CollisionRelationshipViewModel.FirstCollisionName), playerNos.InstanceName);
        relationshipNos.Properties.SetValue(nameof(CollisionRelationshipViewModel.SecondCollisionName), solidCollisionNos.InstanceName);
        relationshipNos.Properties.SetValue(nameof(CollisionRelationshipViewModel.CollisionType), (int)OfficialPlugins.CollisionPlugin.ViewModels.CollisionType.PlatformerSolidCollision);

        return (gameScreen, relationshipNos);
    }

    [Fact]
    public async Task GenerateInitializeCodeFor_ShouldReferenceApplyPhysicsDelegate_WhenFileVersionSupportsIt()
    {
        var (gameScreen, relationshipNos) = await CreatePlatformerSolidCollisionRelationship();

        ObjectFinder.Self.GlueProject.FileVersion = (int)GlueProjectSave.GluxVersions.PlatformerCollisionSupportsApplyPhysicsDelegate;

        var codeBlock = new CodeBlockBase();
        CollisionCodeGenerator.GenerateInitializeCodeFor(gameScreen, relationshipNos, codeBlock);
        var generatedCode = codeBlock.ToString();

        generatedCode.ShouldContain("temp.ArePhysicsAppliedAutomatically");
        generatedCode.ShouldContain("temp.ApplyPhysics");
    }

    // The "Automatically Apply Physics" checkbox (ArePhysicsAppliedAutomatically) previously did nothing
    // for platformer relationships: DelegateCollisionRelationship's DoCollisions() never calls
    // DoCollisionPhysicsInner, the only place that property was consulted. Projects on this version (but
    // below the newer ApplyPhysics-delegate gate) now get the checkbox honored, without needing the
    // delegate feature from #942.
    [Fact]
    public async Task GenerateInitializeCodeFor_ShouldHonorAutomaticallyApplyPhysicsCheckbox_WhenFileVersionSupportsManualPhysicsButPredatesTheDelegate()
    {
        var (gameScreen, relationshipNos) = await CreatePlatformerSolidCollisionRelationship();

        ObjectFinder.Self.GlueProject.FileVersion = (int)GlueProjectSave.GluxVersions.PlatformerCollisionSupportsApplyPhysicsDelegate - 1;

        var codeBlock = new CodeBlockBase();
        CollisionCodeGenerator.GenerateInitializeCodeFor(gameScreen, relationshipNos, codeBlock);
        var generatedCode = codeBlock.ToString();

        generatedCode.ShouldContain("temp.ArePhysicsAppliedAutomatically");
        generatedCode.ShouldNotContain("ApplyPhysics");
    }

    [Fact]
    public async Task GenerateInitializeCodeFor_ShouldFallBackToAlwaysApplyingPhysics_WhenFileVersionPredatesManualPhysics()
    {
        var (gameScreen, relationshipNos) = await CreatePlatformerSolidCollisionRelationship();

        ObjectFinder.Self.GlueProject.FileVersion = (int)GlueProjectSave.GluxVersions.CollisionRelationshipManualPhysics - 1;

        var codeBlock = new CodeBlockBase();
        CollisionCodeGenerator.GenerateInitializeCodeFor(gameScreen, relationshipNos, codeBlock);
        var generatedCode = codeBlock.ToString();

        generatedCode.ShouldNotContain("ArePhysicsAppliedAutomatically");
        generatedCode.ShouldNotContain("ApplyPhysics");
        generatedCode.ShouldContain("first.CollideAgainst(second, false);");
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
