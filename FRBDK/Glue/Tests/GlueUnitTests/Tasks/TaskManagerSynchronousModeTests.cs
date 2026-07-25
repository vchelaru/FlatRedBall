using System.Threading;
using System.Threading.Tasks;
using FlatRedBall.Glue.Managers;
using Shouldly;

namespace GlueUnitTests.Tasks;

// TaskManager is a process-wide singleton (see Singleton<T>) and TaskManager.SynchronousMode /
// UiThreadMarshaller are process-wide static state. Both test classes in this file mutate that state,
// so they share a non-parallel xunit collection to avoid interleaving with each other.
[CollectionDefinition(nameof(TaskManagerSequentialCollection), DisableParallelization = true)]
public class TaskManagerSequentialCollection { }

// Pins the synchronous-execution-mode seam added for GitHub issue #1894: with TaskManager.SynchronousMode
// enabled, Add/AddAsync run the task inline on the calling thread instead of enqueueing it to
// TaskManager's dedicated background thread. This is what lets tests (like WizardProjectLogicTests) drive
// code that goes through TaskManager without needing to wait on, or even spin up, that background thread.
[Collection(nameof(TaskManagerSequentialCollection))]
public class TaskManagerSynchronousModeTests : System.IDisposable
{
    private readonly bool _originalSynchronousMode;

    public TaskManagerSynchronousModeTests()
    {
        _originalSynchronousMode = TaskManager.SynchronousMode;
        TaskManager.SynchronousMode = true;
    }

    public void Dispose()
    {
        TaskManager.SynchronousMode = _originalSynchronousMode;
    }

    [Fact]
    public void Add_Action_ShouldRunInline_OnCallingThread()
    {
        var callingThreadId = Thread.CurrentThread.ManagedThreadId;
        var ranOnThreadId = -1;

        TaskManager.Self.Add(() => ranOnThreadId = Thread.CurrentThread.ManagedThreadId, "test action");

        ranOnThreadId.ShouldBe(callingThreadId);
    }

    [Fact]
    public void Add_Func_ShouldRunInlineAndPopulateResult()
    {
        var glueTask = TaskManager.Self.Add(() => 42, "test func");

        ((int)glueTask.Result).ShouldBe(42);
    }

    [Fact]
    public async Task AddAsync_Action_ShouldRunBeforeAwaitCompletes()
    {
        var ran = false;

        await TaskManager.Self.AddAsync(() => ran = true, "test async action");

        ran.ShouldBeTrue();
    }

    [Fact]
    public async Task AddAsync_Func_ShouldRunInline()
    {
        var ran = false;

        await TaskManager.Self.AddAsync(async () =>
        {
            ran = true;
            await Task.CompletedTask;
        }, "test async func");

        ran.ShouldBeTrue();
    }

    [Fact]
    public async Task WaitForAllTasksFinished_ShouldReturnImmediately_SinceTasksAlreadyRanInline()
    {
        TaskManager.Self.Add(() => { }, "already done by the time we get here");

        // AreAllAsyncTasksDone should already be true - RunSynchronously blocks until the task completes -
        // so this shouldn't need to poll/wait at all.
        var didWait = await TaskManager.Self.WaitForAllTasksFinished();

        didWait.ShouldBeFalse();
    }

    [Fact]
    public void Add_ShouldNotRequireALiveMainGlueWindow()
    {
        // This test host never constructs a MainGlueWindow. If some code path regressed to touching
        // MainGlueWindow.Self directly (instead of going through TaskManager.UiThreadMarshaller), this
        // would NullReferenceException.
        Should.NotThrow(() => TaskManager.Self.Add(() => { }, "no live window needed"));
    }
}

// Pins the UI-thread-marshaller seam added for GitHub issue #1894: TaskManager.OnUiThread/BeginOnUiThread,
// and GlueTask's DoOnUiThread handling, now go through the injectable TaskManager.UiThreadMarshaller
// instead of calling global::Glue.MainGlueWindow.Self.Invoke/BeginInvoke directly. That means tests can
// supply an inline marshaller and never need a real WinForms window with a running message loop.
[Collection(nameof(TaskManagerSequentialCollection))]
public class TaskManagerUiThreadMarshallerTests : System.IDisposable
{
    private readonly IUiThreadMarshaller _originalMarshaller;
    private readonly bool _originalSynchronousMode;

    public TaskManagerUiThreadMarshallerTests()
    {
        _originalMarshaller = TaskManager.UiThreadMarshaller;
        _originalSynchronousMode = TaskManager.SynchronousMode;
    }

    public void Dispose()
    {
        TaskManager.UiThreadMarshaller = _originalMarshaller;
        TaskManager.SynchronousMode = _originalSynchronousMode;
    }

    private class RecordingInlineMarshaller : IUiThreadMarshaller
    {
        public int InvokeActionCallCount;
        public int BeginInvokeCallCount;

        public void Invoke(System.Action action) { InvokeActionCallCount++; action(); }
        public T Invoke<T>(System.Func<T> func) => func();
        public Task Invoke(System.Func<Task> func) => func();
        public Task<T> Invoke<T>(System.Func<Task<T>> func) => func();
        public void BeginInvoke(System.Action action) { BeginInvokeCallCount++; action(); }
    }

    [Fact]
    public void OnUiThread_Action_ShouldRouteThroughMarshaller_WhenNotOnUiThread()
    {
        var marshaller = new RecordingInlineMarshaller();
        TaskManager.UiThreadMarshaller = marshaller;
        var ran = false;

        // The xunit test thread is never MainGlueWindow's UI thread, so IsOnUiThread is false here,
        // exercising the marshaller branch.
        // Block-bodied lambda: see the identical note on GlueTask_WithDoOnUiThread below - an
        // expression-bodied `() => ran = true` is ambiguous between the Action and Func<T> overloads
        // now that OnUiThread<T> exists, and would otherwise resolve to OnUiThread<T> instead.
        TaskManager.Self.IsOnUiThread.ShouldBeFalse();
        TaskManager.Self.OnUiThread(() => { ran = true; });

        ran.ShouldBeTrue();
        marshaller.InvokeActionCallCount.ShouldBe(1);
    }

    [Fact]
    public void OnUiThread_Func_ShouldRouteThroughMarshallerAndReturnResult_WhenNotOnUiThread()
    {
        var marshaller = new RecordingInlineMarshaller();
        TaskManager.UiThreadMarshaller = marshaller;

        TaskManager.Self.IsOnUiThread.ShouldBeFalse();
        var result = TaskManager.Self.OnUiThread(() => 42);

        result.ShouldBe(42);
    }

    [Fact]
    public void BeginOnUiThread_ShouldRouteThroughMarshaller_WhenNotOnUiThread()
    {
        var marshaller = new RecordingInlineMarshaller();
        TaskManager.UiThreadMarshaller = marshaller;
        var ran = false;

        TaskManager.Self.BeginOnUiThread(() => ran = true);

        ran.ShouldBeTrue();
        marshaller.BeginInvokeCallCount.ShouldBe(1);
    }

    [Fact]
    public void GlueTask_WithDoOnUiThread_ShouldRouteThroughMarshaller_NotMainGlueWindowDirectly()
    {
        var marshaller = new RecordingInlineMarshaller();
        TaskManager.UiThreadMarshaller = marshaller;
        TaskManager.SynchronousMode = true;
        var ran = false;

        // Block-bodied lambda: unambiguously resolves to the Add(Action, ...) overload rather than
        // the Add<T>(Func<T>, ...) overload (an expression-bodied `() => ran = true` is convertible to
        // both, since assignment is a valid expression *and* statement, so it would otherwise resolve to
        // Add<bool> and route through UiThreadMarshaller.Invoke<T> instead of Invoke(Action)).
        TaskManager.Self.Add(() => { ran = true; }, "ui thread task", doOnUiThread: true);

        ran.ShouldBeTrue();
        marshaller.InvokeActionCallCount.ShouldBe(1);
    }
}
