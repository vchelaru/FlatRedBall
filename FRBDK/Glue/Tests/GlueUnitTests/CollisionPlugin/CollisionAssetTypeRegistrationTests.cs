using System;
using System.IO;
using System.Threading.Tasks;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.TestSupport;
using GlueUnitTests.Tasks;
using OfficialPlugins.CollisionPlugin.Managers;
using Shouldly;

namespace GlueUnitTests.CollisionPlugin;

// Follow-up to GitHub issue #1894: pins the seam that unblocks Collision-Plugin-typed NamedObjectSaves
// outside a live Glue.exe. MainCollisionPlugin.RegisterAssetTypes (extracted from StartUp, but still
// called from there unchanged) registers AssetTypeInfoManager.Self.CollisionRelationshipAti with
// AvailableAssetTypes - called here directly via GlueTestBootstrap.EnsureCollisionPluginAssetTypesRegistered,
// bypassing PluginManager.LoadPlugins' reflection/directory-scan entirely (that machinery, and real/
// third-party plugin loading through it, is completely unchanged).
//
// Without that registration, NamedObjectSave.GetAssetTypeInfo() - which resolves via
// AvailableAssetTypes.Self.GetAssetTypeFromRuntimeType keyed on SourceClassType, not object identity -
// can never find CollisionRelationshipAti, so every production consumer that compares against it
// (NamedObjectSaveCodeGenerator's ConstructorFunc dispatch, HandleAddEventsForObject,
// GetEventSignatureAndArgs, CollidableNamedObjectController, ...) silently treats a collision-relationship
// NamedObjectSave as an unrecognized FlatRedBall type instead of routing it through CollisionCodeGenerator.
//
// This does NOT attempt to drive the full CollidableNamedObjectController.CreateCollisionRelationshipBetweenObjects
// (the real handler MainCollisionPlugin wires up to PluginManager.ReactToCreateCollisionRelationshipsBetween)
// end-to-end: that method also calls GlueCommands.Self.DialogCommands.FocusTab, which reads
// PluginManager.TabControlViewModel - only ever set by a live WinForms MainGlueWindow, so it NREs outside
// one. That's a separate, UI-rooted blocker, out of scope here - see REFACTORING.md.
[Collection(nameof(TaskManagerSequentialCollection))]
public class CollisionAssetTypeRegistrationTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly string _originalRelativeDirectory;
    private readonly bool _originalSynchronousMode;
    private readonly IUiThreadMarshaller _originalMarshaller;
    private readonly string _tempProjectDirectory;

    public CollisionAssetTypeRegistrationTests()
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
    public async Task NamedObjectSave_WithCollisionRelationshipSourceClassType_ShouldResolveCollisionRelationshipAti()
    {
        var gameScreen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("GameScreen");

        var collisionAti = AssetTypeInfoManager.Self.CollisionRelationshipAti;

        var nos = new NamedObjectSave();
        nos.SetDefaults();
        nos.InstanceName = "TestCollisionRelationship";
        nos.SourceType = SourceType.FlatRedBallType;
        nos.SourceClassType = collisionAti.QualifiedRuntimeTypeName.QualifiedType;

        // AddNamedObjectToAsync (not the higher-level AddNewNamedObjectToAsync, which always runs with
        // updateUi:true and would NRE on MainGlueWindow.Self.PropertyGrid outside a live app) is the same
        // production add-and-generate path Glue.exe uses; performSaveAndGenerateCode:true drives real
        // code generation for the containing screen.
        await GlueCommands.Self.GluxCommands.AddNamedObjectToAsync(
            nos, gameScreen, listToAddTo: null, selectNewNos: false,
            performSaveAndGenerateCode: true, updateUi: false);

        // The seam under test: before MainCollisionPlugin.RegisterAssetTypes runs, AvailableAssetTypes has
        // no record of CollisionRelationshipAti, so this lookup (keyed on SourceClassType, not identity)
        // returns null instead of resolving back to it.
        nos.GetAssetTypeInfo().ShouldBe(collisionAti);

        // Confirms GenerateElementCode (triggered above) actually ran to completion for the screen -
        // exercising NamedObjectSaveCodeGenerator.GenerateInstantiationOrAssignment's `nosAti?.ConstructorFunc`
        // dispatch for this object, not just the lookup in isolation.
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
