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

        TaskManager.Self.IsOnUiThread.ShouldBeFalse();
        new DialogCommands().ShowToast("hello world", TimeSpan.FromMilliseconds(1));
        // ShowToast is async void; give the pre-await portion a chance to run before asserting.
        await Task.Yield();

        passedText.ShouldBe("hello world");
        marshaller.InvokeActionCallCount.ShouldBe(1);
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
