using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.TestSupport;
using GlueUnitTests.Tasks;
using OfficialPlugins.CollisionPlugin.Controllers;
using OfficialPlugins.CollisionPlugin.Errors;
using OfficialPlugins.CollisionPlugin.Managers;
using OfficialPlugins.CollisionPlugin.ViewModels;
using OfficialPlugins.ErrorPlugin.Logic;
using Shouldly;

namespace GlueUnitTests.CollisionPlugin;

// GitHub issue #2111: leaving a CollisionRelationship's first/second collidable unset (or clearing a
// previously-set one) generates broken code (CollisionRelationship<>, CS0305) with no warning in the
// editor. CollisionRelationshipErrorViewModel.TryGetErrorMessageFor already detects this - the gap was
// that nothing told the Error tab to re-check after the two moments that put a relationship into this
// state: MainCollisionPlugin.HandleNewObjectListAsync (a relationship is added, starting out empty) and
// CollisionRelationshipViewModelController.HandleViewModelPropertyChanged's FirstCollisionName/
// SecondCollisionName case (an existing relationship's collidable is cleared). Both now call
// GlueCommands.Self.RefreshCommands.RefreshErrors(), which - with TaskManager.SynchronousMode on, as
// GlueTestBootstrap forces - runs synchronously, so the error is observable immediately after the
// production call that triggers it, with no fake/mock standing in for any of this.
[Collection(nameof(TaskManagerSequentialCollection))]
public class CollisionRelationshipErrorRefreshTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly string _originalRelativeDirectory;
    private readonly bool _originalSynchronousMode;
    private readonly IUiThreadMarshaller _originalMarshaller;
    private readonly Action _originalRefreshErrorsAction;
    private readonly string _tempProjectDirectory;

    public CollisionRelationshipErrorRefreshTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        // Full StartUp (not just EnsureCollisionPluginAssetTypesRegistered): this test needs the real
        // AddErrorReporter(new CollisionErrorReporter()) call and the real ReactToNewObjectListAsync/
        // ViewModel.PropertyChanged wiring this issue's fix touches, not just the asset type registered.
        GlueTestBootstrap.EnsureCollisionPluginRegisteredWithPluginManager();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = ObjectFinder.Self.GlueProject;
        _originalRelativeDirectory = FlatRedBall.IO.FileManager.RelativeDirectory;
        _originalSynchronousMode = TaskManager.SynchronousMode;
        _originalMarshaller = TaskManager.UiThreadMarshaller;
        _originalRefreshErrorsAction = RefreshCommands.RefreshErrorsAction;

        var vsProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);

        GlueState.Self.CurrentMainProject = vsProject;
        ObjectFinder.Self.GlueProject = new GlueProjectSave();
        FlatRedBall.IO.FileManager.RelativeDirectory = _tempProjectDirectory + "\\";

        TaskManager.SynchronousMode = true;
        TaskManager.UiThreadMarshaller = new InlineUiThreadMarshaller();

        lock (GlueState.ErrorListSyncLock)
        {
            GlueState.Self.ErrorList.Errors.Clear();
        }

        // The same wiring MainErrorPlugin.StartUp performs (RefreshCommands.cs line 66) - not driven here
        // because MainErrorPlugin's StartUp constructs a WPF ErrorWindow, which needs an STA thread.
        RefreshCommands.RefreshErrorsAction =
            () => RefreshLogic.RefreshAllErrors(GlueState.Self.ErrorList, printOutput: false);
    }

    public void Dispose()
    {
        GlueState.Self.CurrentMainProject = _originalMainProject;
        ObjectFinder.Self.GlueProject = _originalGlueProject;
        FlatRedBall.IO.FileManager.RelativeDirectory = _originalRelativeDirectory;
        TaskManager.SynchronousMode = _originalSynchronousMode;
        TaskManager.UiThreadMarshaller = _originalMarshaller;
        RefreshCommands.RefreshErrorsAction = _originalRefreshErrorsAction;

        lock (GlueState.ErrorListSyncLock)
        {
            GlueState.Self.ErrorList.Errors.Clear();
        }

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
    public async Task AddingCollisionRelationshipWithNoFirstCollidable_ShouldImmediatelyReportError()
    {
        var gameScreen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("GameScreen");

        var relationshipNos = new NamedObjectSave();
        relationshipNos.SetDefaults();
        relationshipNos.InstanceName = "IncompleteRelationship";
        relationshipNos.SourceType = SourceType.FlatRedBallType;
        relationshipNos.SourceClassType = AssetTypeInfoManager.Self.CollisionRelationshipAti.QualifiedRuntimeTypeName.QualifiedType;
        // FirstCollisionName/SecondCollisionName intentionally left unset - the state shown in #2111's
        // screenshot, and what generates a CollisionRelationship<> with empty type arguments (CS0305).

        // Real production add path (MainCollisionPlugin.HandleNewObjectListAsync's trigger):
        await GlueCommands.Self.GluxCommands.AddNamedObjectToAsync(
            relationshipNos, gameScreen, listToAddTo: null, selectNewNos: false,
            performSaveAndGenerateCode: false, updateUi: false);

        GlueState.Self.ErrorList.Errors
            .OfType<CollisionRelationshipErrorViewModel>()
            .ShouldContain(error => error.Details.Contains("empty first collidable"));
    }

    [Fact]
    public async Task ClearingAnExistingRelationshipsFirstCollidable_ShouldImmediatelyReportError()
    {
        var gameScreen = await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("GameScreen");

        var firstCollidable = new NamedObjectSave();
        firstCollidable.SetDefaults();
        firstCollidable.InstanceName = "SomeSprite";
        firstCollidable.SourceType = SourceType.FlatRedBallType;
        firstCollidable.SourceClassType = "Sprite";
        await GlueCommands.Self.GluxCommands.AddNamedObjectToAsync(
            firstCollidable, gameScreen, listToAddTo: null, selectNewNos: false,
            performSaveAndGenerateCode: false, updateUi: false);

        var secondCollidable = new NamedObjectSave();
        secondCollidable.SetDefaults();
        secondCollidable.InstanceName = "SomeOtherSprite";
        secondCollidable.SourceType = SourceType.FlatRedBallType;
        secondCollidable.SourceClassType = "Sprite";
        await GlueCommands.Self.GluxCommands.AddNamedObjectToAsync(
            secondCollidable, gameScreen, listToAddTo: null, selectNewNos: false,
            performSaveAndGenerateCode: false, updateUi: false);

        var relationshipNos = new NamedObjectSave();
        relationshipNos.SetDefaults();
        relationshipNos.InstanceName = "PlayerVsSomething";
        relationshipNos.SourceType = SourceType.FlatRedBallType;
        relationshipNos.SourceClassType = AssetTypeInfoManager.Self.CollisionRelationshipAti.QualifiedRuntimeTypeName.QualifiedType;
        relationshipNos.Properties.SetValue(nameof(CollisionRelationshipViewModel.FirstCollisionName), firstCollidable.InstanceName);
        relationshipNos.Properties.SetValue(nameof(CollisionRelationshipViewModel.SecondCollisionName), secondCollidable.InstanceName);
        await GlueCommands.Self.GluxCommands.AddNamedObjectToAsync(
            relationshipNos, gameScreen, listToAddTo: null, selectNewNos: false,
            performSaveAndGenerateCode: false, updateUi: false);

        // Starts out complete - the "just added" refresh (covered by the other test) should not be
        // reporting an error for this one:
        GlueState.Self.ErrorList.Errors
            .OfType<CollisionRelationshipErrorViewModel>()
            .Any(error => error.Details.Contains(relationshipNos.InstanceName))
            .ShouldBeFalse();

        // GlueState.CurrentElement/CurrentNamedObjectSave's public setters both round-trip through
        // GlueState.Self.Find.TreeNodeByTag, which FakeFindManager deliberately returns null for on
        // anything but a ReferencedFileSave (see FakeFindManager's doc comment - a previous attempt to
        // resolve NamedObjectSave tags too woke up MainCollisionPlugin's real selection handling and hit
        // an unrelated pre-existing NRE constructing its WPF CollisionRelationshipView, outside this
        // issue's scope to fix). Going through those setters here would just silently clear the selection
        // back to null. Writing the backing snapshot directly - the same object both getters already read
        // from - reproduces "the relationship is selected/being edited" without touching CurrentTreeNode
        // or PluginManager.ReactToItemsSelected at all, so none of that dead-code-wakeup risk applies.
        SetCurrentSelectionForTest(gameScreen, relationshipNos);
        CollisionRelationshipViewModelController.RefreshViewModel(relationshipNos);

        // Mirrors what the WPF-bound ComboBox does when the user clears their selection in the Collision
        // tab: it writes through CollisionRelationshipViewModel.FirstCollisionName's SyncedProperty setter,
        // which persists to relationshipNos.Properties and raises PropertyChanged, driving the real
        // CollisionRelationshipViewModelController.HandleViewModelPropertyChanged this issue's fix touches.
        CollisionRelationshipViewModelController.ViewModel.FirstCollisionName = "";

        GlueState.Self.ErrorList.Errors
            .OfType<CollisionRelationshipErrorViewModel>()
            .ShouldContain(error => error.Details.Contains("empty first collidable"));
    }

    static readonly FieldInfo SnapshotField =
        typeof(GlueState).GetField("snapshot", BindingFlags.NonPublic | BindingFlags.Instance)!;

    static void SetCurrentSelectionForTest(GlueElement element, NamedObjectSave namedObject)
    {
        var snapshot = (GlueStateSnapshot)SnapshotField.GetValue(GlueState.Self)!;
        snapshot.CurrentElement = element;
        snapshot.CurrentNamedObjectSave = namedObject;
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
