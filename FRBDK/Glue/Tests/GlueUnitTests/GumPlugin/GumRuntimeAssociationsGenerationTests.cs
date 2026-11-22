using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.TestSupport;
using GlueUnitTests.Tasks;
using GumPlugin.Managers;
using Shouldly;

// Same namespace choice as GumProjectCreationTests.cs - see its comment for why this can't nest under
// "GumPlugin".
namespace GlueUnitTests.GumPluginTests;

// GumIdb.Generated.cs registers every Gum element's runtime type, so it depends on the set of elements, not
// on any one element's generated code. Generating all elements used to rebuild it once per element.
[Collection(nameof(TaskManagerSequentialCollection))]
public class GumRuntimeAssociationsGenerationTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly string _originalRelativeDirectory;
    private readonly bool _originalSynchronousMode;
    private readonly IUiThreadMarshaller _originalMarshaller;
    private readonly Gum.DataTypes.GumProjectSave? _originalGumProjectSave;
    private readonly string _tempProjectDirectory;

    public GumRuntimeAssociationsGenerationTests()
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
            // best-effort cleanup
        }
    }

    [Fact]
    public async Task GenerateDerivedGueRuntimesAsync_ShouldGenerateGumIdbOnce_ForAllElements()
    {
        var creationLogic = new NewGumProjectCreationLogic(new GumxPropertiesManager());
        await creationLogic.CreateGumProjectInternal(shouldAlsoAddForms: false, askToOverwrite: false);
        var gumIdb = Directory.GetFiles(_tempProjectDirectory, "GumIdb.Generated.cs", SearchOption.AllDirectories)
            .ShouldHaveSingleItem();
        File.Delete(gumIdb);
        AppState.Self.AllLoadedElements.Count().ShouldBeGreaterThan(1);
        var countBefore = CodeGeneratorManager.Self.RuntimeAssociationsGenerationCount;

        await CodeGeneratorManager.Self.GenerateDerivedGueRuntimesAsync(forceReload: true);
        await TaskManager.Self.WaitForAllTasksFinished();

        (CodeGeneratorManager.Self.RuntimeAssociationsGenerationCount - countBefore).ShouldBe(1);
        File.Exists(gumIdb).ShouldBeTrue();
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
