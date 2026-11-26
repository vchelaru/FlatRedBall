using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EditorObjects.IoC;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.Tasks;
using GlueUnitTests.TestSupport;
using OfficialPlugins.ErrorPlugin.Logic;
using Shouldly;
using Xunit;

namespace GlueUnitTests.ErrorPlugin;

/// <summary>
/// Every project-change event refreshes the Errors tab (GitHub issue #2535). Each test makes an edit the way the
/// editor does, through GluxCommands, and checks the Errors list against <see cref="FlaggedNameReporter"/>, which
/// reports every screen, entity, object, variable, state or global file whose name starts with "Flagged".
/// </summary>
[Collection(nameof(TaskManagerSequentialCollection))]
public class CentralErrorRefreshTests : IDisposable
{
    readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    readonly GlueProjectSave _originalGlueProject;
    readonly string _originalRelativeDirectory;
    readonly bool _originalSynchronousMode;
    readonly IUiThreadMarshaller _originalMarshaller;
    readonly Action _originalRefreshErrorsAction;
    readonly string _tempProjectDirectory;
    readonly FlaggedNameReporter _reporter = new();
    readonly GlueProjectSave _project = new();

    public CentralErrorRefreshTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = ObjectFinder.Self.GlueProject;
        _originalRelativeDirectory = FlatRedBall.IO.FileManager.RelativeDirectory;
        _originalSynchronousMode = TaskManager.SynchronousMode;
        _originalMarshaller = TaskManager.UiThreadMarshaller;
        _originalRefreshErrorsAction = RefreshCommands.RefreshErrorsAction;

        GlueState.Self.CurrentMainProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);
        ObjectFinder.Self.GlueProject = _project;
        FlatRedBall.IO.FileManager.RelativeDirectory = _tempProjectDirectory + "\\";

        TaskManager.SynchronousMode = true;
        TaskManager.UiThreadMarshaller = new InlineUiThreadMarshaller();

        ClearErrorList();
        Container.Get<GlueErrorManager>().Add(_reporter);

        // What MainErrorPlugin.StartUp assigns; its StartUp also builds a WPF view, so it isn't run here.
        RefreshCommands.RefreshErrorsAction = () => RefreshLogic.RefreshAllErrors(GlueState.Self.ErrorList, printOutput: false);
    }

    public void Dispose()
    {
        Container.Get<GlueErrorManager>().ErrorReporters.Remove(_reporter);
        GlueState.Self.CurrentMainProject = _originalMainProject;
        ObjectFinder.Self.GlueProject = _originalGlueProject;
        FlatRedBall.IO.FileManager.RelativeDirectory = _originalRelativeDirectory;
        TaskManager.SynchronousMode = _originalSynchronousMode;
        TaskManager.UiThreadMarshaller = _originalMarshaller;
        RefreshCommands.RefreshErrorsAction = _originalRefreshErrorsAction;
        ClearErrorList();

        try
        {
            Directory.Delete(_tempProjectDirectory, recursive: true);
        }
        catch
        {
            // a stray temp dir isn't worth failing the test over
        }
    }

    [Fact]
    public async Task AddingAndRemovingAnObject_ShouldAddAndRemoveItsError()
    {
        var screen = await AddScreen("GameScreen");
        var nos = NewSprite("FlaggedSprite");

        await GlueCommands.Self.GluxCommands.AddNamedObjectToAsync(nos, screen, listToAddTo: null, selectNewNos: false,
            performSaveAndGenerateCode: false, updateUi: false);
        await Drain();
        ErrorIds().ShouldContain("FlaggedSprite");

        await GlueCommands.Self.GluxCommands.RemoveNamedObjectListAsync(new List<NamedObjectSave> { nos },
            performSaveAndGenerateCode: false, updateUi: false);
        await Drain();
        ErrorIds().ShouldNotContain("FlaggedSprite");
    }

    [Fact]
    public async Task RemovingAnObject_ShouldReportTheObjectThatTargetsIt()
    {
        var screen = await AddScreen("GameScreen");
        var target = NewSprite("Target");
        var pointer = NewSprite("Pointer");
        pointer.Properties.SetValue(FlaggedNameReporter.TargetProperty, "Target");
        await GlueCommands.Self.GluxCommands.AddNamedObjectToAsync(target, screen, listToAddTo: null, selectNewNos: false,
            performSaveAndGenerateCode: false, updateUi: false);
        await GlueCommands.Self.GluxCommands.AddNamedObjectToAsync(pointer, screen, listToAddTo: null, selectNewNos: false,
            performSaveAndGenerateCode: false, updateUi: false);
        await Drain();
        ErrorIds().ShouldNotContain("Pointer -> Target");

        await GlueCommands.Self.GluxCommands.RemoveNamedObjectListAsync(new List<NamedObjectSave> { target },
            performSaveAndGenerateCode: false, updateUi: false);
        await Drain();

        ErrorIds().ShouldContain("Pointer -> Target");
    }

    [Fact]
    public async Task AddingACustomVariable_ShouldReportIt()
    {
        var screen = await AddScreen("GameScreen");

        await GlueCommands.Self.GluxCommands.ElementCommands.AddCustomVariableToElementAsync(
            new CustomVariable { Name = "FlaggedVariable", Type = "float" }, screen, save: false);
        await Drain();

        ErrorIds().ShouldContain("FlaggedVariable");
    }

    [Fact]
    public async Task CreatingAnEntity_ShouldReportIt()
    {
        GlueCommands.Self.GluxCommands.EntityCommands.AddEntity("FlaggedEntity");
        await Drain();

        ErrorIds().ShouldContain("Entities\\FlaggedEntity");
    }

    [Fact]
    public async Task AddingAState_ShouldReportIt()
    {
        var screen = await AddScreen("GameScreen");

        await GlueCommands.Self.GluxCommands.AddStateSave(new StateSave { Name = "FlaggedState" }, category: null, screen);
        await Drain();

        ErrorIds().ShouldContain("FlaggedState");
    }

    [Fact]
    public async Task AddingAGlobalFile_ShouldReportIt()
    {
        await GlueCommands.Self.GluxCommands.AddReferencedFileToGlobalContentAsync(
            new ReferencedFileSave { Name = "GlobalContent/FlaggedFile.png" }, generateAndSave: false, updateUi: false);
        await Drain();

        ErrorIds().ShouldContain("GlobalContent/FlaggedFile.png");
    }

    [Fact]
    public async Task SuppressingAutomaticErrorChecking_ShouldStopTheRefresh()
    {
        var screen = await AddScreen("GameScreen");
        _project.IsSuppressingAutomaticErrorChecking = true;
        _reporter.RunCount = 0;

        await GlueCommands.Self.GluxCommands.ElementCommands.AddCustomVariableToElementAsync(
            new CustomVariable { Name = "FlaggedVariable", Type = "float" }, screen, save: false);
        await Drain();

        _reporter.RunCount.ShouldBe(0);
        ErrorIds().ShouldNotContain("FlaggedVariable");
    }

    [Fact]
    public async Task ManyChangesInARow_ShouldRefreshOnce()
    {
        var screen = await AddScreen("GameScreen");

        // A real queue: SynchronousMode runs every task inline, so nothing could coalesce.
        var selfField = typeof(Singleton<TaskManager>).GetField("mSelf", BindingFlags.NonPublic | BindingFlags.Static)!;
        var synchronousTaskManager = (TaskManager)selfField.GetValue(null)!;
        TaskManager.SynchronousMode = false;
        var taskManager = new TaskManager(idleLoopDelayMs: 50);
        selfField.SetValue(null, taskManager);
        try
        {
            // Holds the queue so all three edits are queued before any runs, as when the user edits faster than
            // the queue drains.
            using var gate = new ManualResetEventSlim();
            taskManager.Add(() => gate.Wait(), "Hold the queue");
            var edits = Enumerable.Range(0, 3)
                .Select(i => GlueCommands.Self.GluxCommands.ElementCommands.AddCustomVariableToElementAsync(
                    new CustomVariable { Name = "FlaggedVariable" + i, Type = "float" }, screen, save: false))
                .ToList();
            _reporter.RunCount = 0;

            gate.Set();
            await Task.WhenAll(edits).WaitAsync(TimeSpan.FromSeconds(30));
            // Queued after the refresh the last edit asked for, in the same tier, so it runs after it.
            await taskManager.AddAsync(() => { }, "After the refresh", TaskExecutionPreference.AddOrMoveToEnd)
                .WaitAsync(TimeSpan.FromSeconds(30));

            _reporter.RunCount.ShouldBe(1);
            ErrorIds().ShouldBe(new[] { "FlaggedVariable0", "FlaggedVariable1", "FlaggedVariable2" }, ignoreOrder: true);
        }
        finally
        {
            taskManager.StopLoop();
            selfField.SetValue(null, synchronousTaskManager);
            TaskManager.SynchronousMode = true;
        }
    }

    static Task<ScreenSave> AddScreen(string name) => GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen(name);

    static NamedObjectSave NewSprite(string name)
    {
        var nos = new NamedObjectSave();
        nos.SetDefaults();
        nos.InstanceName = name;
        nos.SourceType = SourceType.FlatRedBallType;
        nos.SourceClassType = "Sprite";
        return nos;
    }

    static Task Drain() => TaskManager.Self.WaitForAllTasksFinished();

    static List<string> ErrorIds()
    {
        lock (GlueState.ErrorListSyncLock)
        {
            return GlueState.Self.ErrorList.Errors.OfType<FlaggedError>().Select(item => item.UniqueId).ToList();
        }
    }

    static void ClearErrorList()
    {
        lock (GlueState.ErrorListSyncLock)
        {
            GlueState.Self.ErrorList.Errors.Clear();
        }
    }

    /// <summary>
    /// Reports every name starting with "Flagged", and every object whose <see cref="TargetProperty"/> names an
    /// object its element doesn't have.
    /// </summary>
    class FlaggedNameReporter : ErrorReporterBase
    {
        public const string TargetProperty = "TestTarget";

        public int RunCount;

        public override ErrorViewModel[] GetAllErrors()
        {
            RunCount++;
            var project = GlueState.Self.CurrentGlueProject;
            var names = new List<string>();
            foreach (GlueElement element in project.Screens.Concat<GlueElement>(project.Entities))
            {
                names.Add(element.Name);
                names.AddRange(element.AllNamedObjects.Select(item => item.InstanceName));
                names.AddRange(element.CustomVariables.Select(item => item.Name));
                names.AddRange(element.AllStates.Select(item => item.Name));

                foreach (var nos in element.AllNamedObjects)
                {
                    var target = nos.Properties.GetValue<string>(TargetProperty);
                    if (target != null && element.GetNamedObjectRecursively(target) == null)
                    {
                        names.Add(nos.InstanceName + " -> " + target);
                    }
                }
            }
            names.AddRange(project.GlobalFiles.Select(item => item.Name));

            return names
                .Where(name => name.Contains("Flagged") || name.Contains(" -> "))
                .Select(name => (ErrorViewModel)new FlaggedError(name))
                .ToArray();
        }
    }

    class FlaggedError : ErrorViewModel
    {
        readonly string id;
        public FlaggedError(string id)
        {
            this.id = id;
            Details = id;
        }
        public override string UniqueId => id;
    }

    class InlineUiThreadMarshaller : IUiThreadMarshaller
    {
        public void Invoke(Action action) => action();
        public T Invoke<T>(Func<T> func) => func();
        public Task Invoke(Func<Task> func) => func();
        public Task<T> Invoke<T>(Func<Task<T>> func) => func();
        public void BeginInvoke(Action action) => action();
    }
}
