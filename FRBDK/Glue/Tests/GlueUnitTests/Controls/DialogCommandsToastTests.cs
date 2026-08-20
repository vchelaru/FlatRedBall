using System;
using System.Threading.Tasks;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using GlueUnitTests.Tasks;
using GlueUnitTests.TestSupport;
using Shouldly;

namespace GlueUnitTests.Controls;

// Covers issue #2130: ShowToast/HideToast used to touch MainPanelControl's WPF elements directly, so a
// call reached from a background TaskManager task (e.g. an Edit Mode load, or an add-object failure
// response) crashed Glue with a WPF cross-thread InvalidOperationException. Both should route through
// TaskManager.Self.OnUiThread like DialogService does (see DialogServiceTests) instead of touching WPF
// directly - shares TaskManagerSequentialCollection since both mutate TaskManager's process-wide
// UiThreadMarshaller state.
[Collection(nameof(TaskManagerSequentialCollection))]
public class DialogCommandsToastTests : IDisposable
{
    private readonly Action<string> _originalShowToastPanelImpl = DialogCommands.ShowToastPanelImpl;
    private readonly Action _originalHideToastPanelImpl = DialogCommands.HideToastPanelImpl;
    private readonly IUiThreadMarshaller _originalMarshaller = TaskManager.UiThreadMarshaller;

    public DialogCommandsToastTests()
    {
        GlueTestBootstrap.EnsureInitialized();
    }

    public void Dispose()
    {
        DialogCommands.ShowToastPanelImpl = _originalShowToastPanelImpl;
        DialogCommands.HideToastPanelImpl = _originalHideToastPanelImpl;
        TaskManager.UiThreadMarshaller = _originalMarshaller;
    }

    private class RecordingInlineMarshaller : IUiThreadMarshaller
    {
        public int InvokeActionCallCount;

        public void Invoke(Action action) { InvokeActionCallCount++; action(); }
        public T Invoke<T>(Func<T> func) => func();
        public Task Invoke(Func<Task> func) => func();
        public Task<T> Invoke<T>(Func<Task<T>> func) => func();
        public void BeginInvoke(Action action) => action();
    }

    [Fact]
    public async Task ShowToast_ShouldRouteThroughOnUiThread_WhenNotOnUiThread()
    {
        var marshaller = new RecordingInlineMarshaller();
        TaskManager.UiThreadMarshaller = marshaller;
        string passedText = null;
        DialogCommands.ShowToastPanelImpl = text => passedText = text;
        var hidden = new TaskCompletionSource<bool>();
        DialogCommands.HideToastPanelImpl = () => hidden.TrySetResult(true);

        TaskManager.Self.IsOnUiThread.ShouldBeFalse();
        // ShowToast is async void, but it runs synchronously up to its first await (Task.Delay) before
        // returning here - so the OnUiThread call for the show itself has already happened.
        new DialogCommands().ShowToast("hello world", TimeSpan.FromMilliseconds(10));

        passedText.ShouldBe("hello world");
        marshaller.InvokeActionCallCount.ShouldBe(1);

        // Drain the toast's own real-time auto-hide before the test ends instead of leaving its
        // Task.Delay pending - an earlier version of this test used a long delay to dodge that, but the
        // leftover timer kept the test host process alive until it fired against already-restored
        // (real, WPF-touching) impls from a later test's Dispose(), NRE-ing there (issue #2130 PR).
        var completed = await Task.WhenAny(hidden.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        completed.ShouldBe(hidden.Task, "the toast's auto-hide should have fired via HideToastPanelImpl within 5 seconds");
        marshaller.InvokeActionCallCount.ShouldBe(2);
    }

    [Fact]
    public void HideToast_ShouldRouteThroughOnUiThread_WhenNotOnUiThread()
    {
        var marshaller = new RecordingInlineMarshaller();
        TaskManager.UiThreadMarshaller = marshaller;
        var wasHidden = false;
        DialogCommands.HideToastPanelImpl = () => wasHidden = true;

        TaskManager.Self.IsOnUiThread.ShouldBeFalse();
        new DialogCommands().HideToast();

        wasHidden.ShouldBeTrue();
        marshaller.InvokeActionCallCount.ShouldBe(1);
    }
}
