using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers.Projects;
using GlueUnitTests.TestSupport;
using GlueUnitTests.Tasks;
using GumPlugin.Managers;
using Microsoft.Build.Evaluation;
using Shouldly;

// Same namespace choice as GumProjectCreationTests.cs - see its comment for why this can't nest under
// "GumPlugin".
namespace GlueUnitTests.GumPluginTests;

// FRB2 has no generated Screen/Entity classes to reference Gum's generated runtime types from, and every
// .cs file this pipeline writes is already suppressed by CodeWritePolicy (backstopped through
// FileCommands.SaveIfDiffers). Before the fix, CodeGeneratorManager still ran the entire generation pass
// on every project load regardless - wasted work and a misleading "Generating Gum <element>" log line
// per Gum element every time an FRB2 project with a Gum project was reopened.
[Collection(nameof(TaskManagerSequentialCollection))]
public class Frb2GumRuntimeGenerationSuppressionTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly string _originalRelativeDirectory;
    private readonly bool _originalSynchronousMode;
    private readonly IUiThreadMarshaller _originalMarshaller;
    private readonly Gum.DataTypes.GumProjectSave? _originalGumProjectSave;
    private readonly string _tempProjectDirectory;

    public Frb2GumRuntimeGenerationSuppressionTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = ObjectFinder.Self.GlueProject;
        _originalRelativeDirectory = FlatRedBall.IO.FileManager.RelativeDirectory;
        _originalSynchronousMode = TaskManager.SynchronousMode;
        _originalMarshaller = TaskManager.UiThreadMarshaller;
        _originalGumProjectSave = Gum.Managers.ObjectFinder.Self.GumProjectSave;

        var vsProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);

        GlueState.Self.CurrentMainProject = vsProject;
        ObjectFinder.Self.GlueProject = new GlueProjectSave();
        FlatRedBall.IO.FileManager.RelativeDirectory = _tempProjectDirectory + "\\";

        TaskManager.SynchronousMode = true;
        TaskManager.UiThreadMarshaller = new InlineUiThreadMarshaller();
        Gum.Managers.ObjectFinder.Self.GumProjectSave = null;
    }

    public void Dispose()
    {
        GlueState.Self.CurrentMainProject = _originalMainProject;
        ObjectFinder.Self.GlueProject = _originalGlueProject;
        FlatRedBall.IO.FileManager.RelativeDirectory = _originalRelativeDirectory;
        TaskManager.SynchronousMode = _originalSynchronousMode;
        TaskManager.UiThreadMarshaller = _originalMarshaller;
        Gum.Managers.ObjectFinder.Self.GumProjectSave = _originalGumProjectSave;

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
    public async Task GenerateDerivedGueRuntimesAsyncAndGenerateAllBehaviors_OnAnFrb2Project_TouchNothingOnDisk()
    {
        // Create a real Gum project first (as a non-FRB2 project, same as GumProjectCreationTests) so
        // there is something for the on-load pipeline to iterate.
        var creationLogic = new NewGumProjectCreationLogic(new GumxPropertiesManager());
        await creationLogic.CreateGumProjectInternal(shouldAlsoAddForms: false, askToOverwrite: false);

        // Now simulate reopening the same project as FRB2 - swap the loaded project's type, exactly like
        // RectangleFillStrokeRuntimeVersionCheckTests does for the same reason.
        var csprojPath = GlueState.Self.CurrentMainProject.FullFileName.FullPath;
        GlueState.Self.CurrentMainProject = new Frb2Project(
            new Project(csprojPath, null, null, new ProjectCollection()));

        // Snapshot the whole tree rather than one known path: an Frb2Project resolves its content
        // folder differently (Content/FrbEditor/) than the plain test project the Gum project was
        // created under, so a full-tree diff catches every side effect regardless of where the pipeline
        // would have targeted - including the GumRuntimes/ directory itself getting created empty.
        var entriesBefore = SnapshotTree();

        await CodeGeneratorManager.Self.GenerateDerivedGueRuntimesAsync(forceReload: true);
        await CodeGeneratorManager.Self.GenerateAllBehaviors();

        SnapshotTree().ShouldBe(entriesBefore);
    }

    private string[] SnapshotTree() =>
        Directory.GetFileSystemEntries(_tempProjectDirectory, "*", SearchOption.AllDirectories)
            .OrderBy(entry => entry, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private class InlineUiThreadMarshaller : IUiThreadMarshaller
    {
        public void Invoke(Action action) => action();
        public T Invoke<T>(Func<T> func) => func();
        public Task Invoke(Func<Task> func) => func();
        public Task<T> Invoke<T>(Func<Task<T>> func) => func();
        public void BeginInvoke(Action action) => action();
    }
}
