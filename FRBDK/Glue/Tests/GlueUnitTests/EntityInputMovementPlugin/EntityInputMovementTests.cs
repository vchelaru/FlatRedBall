using System;
using System.IO;
using System.Linq;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.PlatformerPlugin.ViewModels;
using GlueUnitTests.TestSupport;
using GlueUnitTests.Tasks;
using Shouldly;
using TopDownPlugin.ViewModels;

// Namespace is "EntityInputMovementPluginTests", not "EntityInputMovementPlugin": the real plugin
// project's root namespace is the bare "EntityInputMovementPlugin" (see EntityInputMovementPlugin.csproj),
// so nesting a same-named namespace under GlueUnitTests would shadow it for every file under
// GlueUnitTests.* - same reasoning as GlueUnitTests.TiledPluginTests/GumPluginTests.
namespace GlueUnitTests.EntityInputMovementPluginTests;

// Follow-up to GitHub issue #1894 (reopened): WizardProjectLogic.HandleAddPlayerEntity's two call sites
// (~lines 661/677) go through PluginManager.CallPluginMethod("Entity Input Movement Plugin",
// "MakeEntityPlatformer"/"MakeEntityTopDown", playerEntity) - the same silent-no-op false-coverage risk
// PRs #1901/#1902/#1903 fixed for the Collision/Gum/Tiled plugins.
//
// Unlike those three, MainEntityInputMovementPlugin.MakeEntityPlatformer/MakeEntityTopDown are NOT thin
// forwarders to directly-callable logic - they route through the plugin's own `mainViewModel` field, which
// is only populated by CreateMainView() (constructs a real WPF UserControl and calls this.CreateTab(...),
// the exact PluginManager.TabControlViewModel-coupled path already flagged as out-of-scope in
// REFACTORING.md from PR #1901). So this test does NOT call MainEntityInputMovementPlugin at all.
//
// Instead it calls what MakeEntityPlatformer/MakeEntityTopDown themselves delegate real work to:
// FlatRedBall.PlatformerPlugin.Controllers.MainController.Self.GetViewModel() /
// TopDownPlugin.Controllers.MainController.Self.GetViewModel() - the same singleton view-model source the
// plugin's own mainViewModel.PlatformerViewModel/TopDownViewModel is initialized from
// (MainEntityInputMovementPlugin.CreateMainView). Setting BackingData + IsPlatformer/IsTopDown = true on
// that view model fires the exact same PropertyChanged handler
// (PlatformerPlugin.Controllers.MainController.HandleViewModelPropertyChanged /
// TopDownPlugin.Controllers.MainController.HandleViewModelPropertyChanged) that production wires up in
// GetViewModel() - real production logic, not a fake, just reached one layer below the WPF-view-owning
// plugin class.
//
// No MessageBox.Show/dialog in this call chain: verified by inspection of PlatformerPlugin/TopDownPlugin
// (no MessageBox.Show anywhere in either plugin folder) and of
// FlatRedBall.Glue.CreatedClass.CustomClassController.SetCsvRfsToUseCustomClass, the one shared helper on
// this path that does contain a MessageBox.Show - both plugins call it with force: true, which short-
// circuits the prompt (see SetRfsToUseNonNullClass: "if (force) result = DialogResult.Yes;"). CSV-extension
// files also bypass ElementCommands.CheckAndWarnAboutUnknownFileTypes's unrecognized-extension prompt
// entirely (that check explicitly excludes "csv").
[Collection(nameof(TaskManagerSequentialCollection))]
public class EntityInputMovementTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly string _originalRelativeDirectory;
    private readonly bool _originalSynchronousMode;
    private readonly IUiThreadMarshaller _originalMarshaller;
    private readonly string _tempProjectDirectory;

    public EntityInputMovementTests()
    {
        GlueTestBootstrap.EnsureInitialized();

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
    public async System.Threading.Tasks.Task MakeEntityPlatformer_ShouldMarkEntityAsPlatformer_AndAddMovementVariablesAndCsv()
    {
        var entity = new EntitySave { Name = "Entities\\Player" };
        GlueState.Self.CurrentGlueProject.Entities.Add(entity);
        GlueState.Self.CurrentElement = entity;

        entity.ImplementsICollidable.ShouldBeFalse();

        // The real logic behind WizardProjectLogic's
        // PluginManager.CallPluginMethod("Entity Input Movement Plugin", "MakeEntityPlatformer", playerEntity)
        // - see the class-level comment for why this bypasses MainEntityInputMovementPlugin itself.
        var viewModel = FlatRedBall.PlatformerPlugin.Controllers.MainController.Self.GetViewModel();
        viewModel.BackingData = entity;
        viewModel.IsPlatformer = true;

        // The IsPlatformer setter fires MainController.HandleViewModelPropertyChanged, an async void
        // handler whose CSV generation/save work goes through TaskManager - even with
        // TaskManager.SynchronousMode, WaitForTaskToFinish's internal polling still yields via a real
        // Task.Delay once, so the handler's tail (AddPlatformerVariables, the CSV ReferencedFileSave) can
        // still be in flight when the property setter returns. WizardProjectLogic itself awaits exactly
        // this same drain (TaskManager.Self.WaitForAllTasksFinished, e.g. ~line 200/261) before relying on
        // the results of plugin-triggered work, so this mirrors real production sequencing rather than
        // working around a test-only race.
        await TaskManager.Self.WaitForAllTasksFinished();

        // Real side effect: the entity's persisted property, read back by
        // PlatformerPlugin.Controllers.MainController.IsPlatformer(EntitySave).
        entity.Properties.GetValue<bool>(nameof(PlatformerEntityViewModel.IsPlatformer)).ShouldBeTrue();

        // Real side effect: marking an entity as a platformer also makes it ICollidable (see
        // MainController.HandleViewModelPropertyChanged's IsPlatformer case).
        entity.ImplementsICollidable.ShouldBeTrue();

        // Real side effect: the three platformer movement CustomVariables were added, typed against the
        // project's actual PlatformerValues custom class (see MainController.AddPlatformerVariables).
        var platformerValuesType = GlueState.Self.ProjectNamespace + ".DataTypes.PlatformerValues";
        var variableNames = entity.CustomVariables.Select(v => v.Name).ToArray();
        variableNames.ShouldContain("GroundMovement");
        variableNames.ShouldContain("AirMovement");
        variableNames.ShouldContain("AfterDoubleJump");
        entity.CustomVariables.ShouldAllBe(v => v.Type == platformerValuesType);

        // Real side effect: a PlatformerValues(Static).csv was generated to disk and wired up as a
        // ReferencedFileSave on the entity (see MainController.GenerateAndAddCsv).
        var relativeCsvFile = FlatRedBall.PlatformerPlugin.Generators.CsvGenerator.RelativeCsvFile;
        var csvRfs = entity.ReferencedFiles.FirstOrDefault(rfs => rfs.Name.EndsWith(relativeCsvFile));
        csvRfs.ShouldNotBeNull();
        csvRfs!.CreatesDictionary.ShouldBeTrue();

        var csvPath = FlatRedBall.PlatformerPlugin.Generators.CsvGenerator.Self.CsvPlatformerFileFor(entity).FullPath;
        File.Exists(csvPath).ShouldBeTrue();
        var csvContents = File.ReadAllText(csvPath);
        csvContents.ShouldContain("Ground");
        csvContents.ShouldContain("Air");

        // Real side effect: a shared "PlatformerValues" CustomClassSave was created and wired to the new
        // csv (see MainController.GenerateAndAddCsv).
        var customClass = GlueState.Self.CurrentGlueProject.CustomClasses.FirstOrDefault(c => c.Name == "PlatformerValues");
        customClass.ShouldNotBeNull();
        customClass!.CsvFilesUsingThis.ShouldContain(csvRfs.Name);
    }

    [Fact]
    public async System.Threading.Tasks.Task MakeEntityTopDown_ShouldMarkEntityAsTopDown_AndAddCsv()
    {
        var entity = new EntitySave { Name = "Entities\\Player" };
        GlueState.Self.CurrentGlueProject.Entities.Add(entity);
        GlueState.Self.CurrentElement = entity;

        entity.ImplementsICollidable.ShouldBeFalse();

        // The real logic behind WizardProjectLogic's
        // PluginManager.CallPluginMethod("Entity Input Movement Plugin", "MakeEntityTopDown", playerEntity)
        // - see the class-level comment for why this bypasses MainEntityInputMovementPlugin itself.
        var viewModel = TopDownPlugin.Controllers.MainController.Self.GetViewModel();
        viewModel.BackingData = entity;
        viewModel.IsTopDown = true;

        // See the matching comment in MakeEntityPlatformer_... above: drains the same
        // TaskManager-queued tail of HandleViewModelPropertyChanged that WizardProjectLogic itself awaits
        // via WaitForAllTasksFinished.
        await TaskManager.Self.WaitForAllTasksFinished();

        // Real side effect: the entity's persisted property, read back by
        // TopDownPlugin.Controllers.MainController.IsTopDown(EntitySave).
        entity.Properties.GetValue<bool>(nameof(TopDownEntityViewModel.IsTopDown)).ShouldBeTrue();

        // Real side effect: marking an entity as top-down also makes it ICollidable (see
        // MainController.HandleViewModelPropertyChanged's IsTopDown case).
        entity.ImplementsICollidable.ShouldBeTrue();

        // Real side effect: a TopDownValues(Static).csv was generated to disk and wired up as a
        // ReferencedFileSave on the entity (see MainController.GenerateAndAddCsv).
        var relativeCsvFile = TopDownPlugin.DataGenerators.CsvGenerator.RelativeCsvFile;
        var csvRfs = entity.ReferencedFiles.FirstOrDefault(rfs => rfs.Name.EndsWith(relativeCsvFile));
        csvRfs.ShouldNotBeNull();
        csvRfs!.CreatesDictionary.ShouldBeTrue();

        var csvPath = TopDownPlugin.DataGenerators.CsvGenerator.Self.CsvTopdownFileFor(entity).FullPath;
        File.Exists(csvPath).ShouldBeTrue();
        var csvContents = File.ReadAllText(csvPath);
        csvContents.ShouldContain("Default");

        // Real side effect: a shared "TopDownValues" CustomClassSave was created and wired to the new csv
        // (see MainController.GenerateAndAddCsv).
        var customClass = GlueState.Self.CurrentGlueProject.CustomClasses.FirstOrDefault(c => c.Name == "TopDownValues");
        customClass.ShouldNotBeNull();
        customClass!.CsvFilesUsingThis.ShouldContain(csvRfs.Name);
    }

    private class InlineUiThreadMarshaller : IUiThreadMarshaller
    {
        public void Invoke(Action action) => action();
        public T Invoke<T>(Func<T> func) => func();
        public System.Threading.Tasks.Task Invoke(Func<System.Threading.Tasks.Task> func) => func();
        public System.Threading.Tasks.Task<T> Invoke<T>(Func<System.Threading.Tasks.Task<T>> func) => func();
        public void BeginInvoke(Action action) => action();
    }
}
