using System;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Managers;
using GlueUnitTests.Tasks;
using Shouldly;

namespace GlueUnitTests.Controls;

// DialogService is the shared seam for all simple message/confirm/choice dialogs in Glue (added to fix a
// cross-thread WPF crash class - see ProjectLoader.CheckForMissingCustomFile and #1921's
// DuplicateProjectEntryDialog). Every public method routes through TaskManager.Self.OnUiThread, so these
// tests share TaskManagerSequentialCollection with the other TaskManager-state tests (see
// TaskManagerSynchronousModeTests.cs) since both mutate TaskManager's process-wide UiThreadMarshaller state.
[Collection(nameof(TaskManagerSequentialCollection))]
public class DialogServiceTests : IDisposable
{
    private readonly Action<string> _originalShowMessageImpl = DialogService.ShowMessageImpl;
    private readonly Func<string, DialogButton, DialogButton, DialogButton?> _originalShowConfirmImpl = DialogService.ShowConfirmImpl;
    private readonly Func<string, (string label, object value)[], object> _originalShowChoiceImpl = DialogService.ShowChoiceImpl;
    private readonly IUiThreadMarshaller _originalMarshaller = TaskManager.UiThreadMarshaller;

    public DialogServiceTests()
    {
        // The xunit test thread is never MainGlueWindow's UI thread, so DialogService's OnUiThread call
        // always takes the marshaller branch. Default to an inline marshaller so tests that don't care about
        // marshalling specifics don't need a live WinForms message loop; tests that DO care about the
        // marshalling itself install their own RecordingInlineMarshaller.
        TaskManager.UiThreadMarshaller = new RecordingInlineMarshaller();
    }

    public void Dispose()
    {
        DialogService.ShowMessageImpl = _originalShowMessageImpl;
        DialogService.ShowConfirmImpl = _originalShowConfirmImpl;
        DialogService.ShowChoiceImpl = _originalShowChoiceImpl;
        TaskManager.UiThreadMarshaller = _originalMarshaller;
    }

    private class RecordingInlineMarshaller : IUiThreadMarshaller
    {
        public int InvokeFuncCallCount;
        public int InvokeActionCallCount;

        public void Invoke(Action action) { InvokeActionCallCount++; action(); }
        public T Invoke<T>(Func<T> func) { InvokeFuncCallCount++; return func(); }
        public System.Threading.Tasks.Task Invoke(Func<System.Threading.Tasks.Task> func) => func();
        public System.Threading.Tasks.Task<T> Invoke<T>(Func<System.Threading.Tasks.Task<T>> func) => func();
        public void BeginInvoke(Action action) => action();
    }

    [Fact]
    public void ShowMessage_ShouldInvokeShowMessageImpl_WithTheGivenText()
    {
        string passedText = null;
        DialogService.ShowMessageImpl = text => passedText = text;

        DialogService.ShowMessage("hello world");

        passedText.ShouldBe("hello world");
    }

    [Fact]
    public void ShowMessage_ShouldRouteThroughOnUiThread_WhenNotOnUiThread()
    {
        var marshaller = new RecordingInlineMarshaller();
        TaskManager.UiThreadMarshaller = marshaller;
        DialogService.ShowMessageImpl = _ => { };

        TaskManager.Self.IsOnUiThread.ShouldBeFalse();
        DialogService.ShowMessage("hello");

        marshaller.InvokeActionCallCount.ShouldBe(1);
    }

    [Fact]
    public void ShowConfirm_ShouldPassTextAndButtons_AndReturnTheImplsResult()
    {
        string passedText = null;
        DialogButton passedPrimary = default;
        DialogButton passedSecondary = default;
        DialogService.ShowConfirmImpl = (text, primary, secondary) =>
        {
            passedText = text;
            passedPrimary = primary;
            passedSecondary = secondary;
            return DialogButton.Retry;
        };

        var result = DialogService.ShowConfirm("are you sure?", DialogButton.Ok, DialogButton.Cancel);

        passedText.ShouldBe("are you sure?");
        passedPrimary.ShouldBe(DialogButton.Ok);
        passedSecondary.ShouldBe(DialogButton.Cancel);
        result.ShouldBe(DialogButton.Retry);
    }

    [Fact]
    public void ShowConfirm_ShouldDefaultToYesNo_WhenButtonsNotSpecified()
    {
        DialogButton passedPrimary = default;
        DialogButton passedSecondary = default;
        DialogService.ShowConfirmImpl = (text, primary, secondary) =>
        {
            passedPrimary = primary;
            passedSecondary = secondary;
            return null;
        };

        var result = DialogService.ShowConfirm("are you sure?");

        passedPrimary.ShouldBe(DialogButton.Yes);
        passedSecondary.ShouldBe(DialogButton.No);
        result.ShouldBeNull();
    }

    [Fact]
    public void ShowConfirm_ShouldRouteThroughOnUiThread_WhenNotOnUiThread()
    {
        var marshaller = new RecordingInlineMarshaller();
        TaskManager.UiThreadMarshaller = marshaller;
        DialogService.ShowConfirmImpl = (_, __, ___) => DialogButton.Yes;

        var result = DialogService.ShowConfirm("text");

        result.ShouldBe(DialogButton.Yes);
        marshaller.InvokeFuncCallCount.ShouldBe(1);
    }

    private enum FakeChoice
    {
        FirstOption,
        SecondOption,
        ThirdOption
    }

    [Fact]
    public void ShowChoice_ShouldPassTextAndBoxedOptions_ToTheImpl_AndUnboxTheResult()
    {
        string passedText = null;
        (string label, object value)[] passedOptions = null;
        DialogService.ShowChoiceImpl = (text, options) =>
        {
            passedText = text;
            passedOptions = options;
            return FakeChoice.SecondOption;
        };

        var result = DialogService.ShowChoice("pick one",
            ("First", FakeChoice.FirstOption),
            ("Second", FakeChoice.SecondOption),
            ("Third", FakeChoice.ThirdOption));

        passedText.ShouldBe("pick one");
        passedOptions.Length.ShouldBe(3);
        passedOptions[1].label.ShouldBe("Second");
        passedOptions[1].value.ShouldBe(FakeChoice.SecondOption);
        result.ShouldBe(FakeChoice.SecondOption);
    }

    [Fact]
    public void ShowChoice_ShouldReturnDefault_WhenImplReturnsNull()
    {
        DialogService.ShowChoiceImpl = (_, __) => null;

        var result = DialogService.ShowChoice("pick one",
            ("First", FakeChoice.FirstOption),
            ("Second", FakeChoice.SecondOption));

        result.ShouldBe(FakeChoice.FirstOption); // default(FakeChoice) == FirstOption (value 0)
    }

    [Fact]
    public void ShowChoice_ShouldRouteThroughOnUiThread_WhenNotOnUiThread()
    {
        var marshaller = new RecordingInlineMarshaller();
        TaskManager.UiThreadMarshaller = marshaller;
        DialogService.ShowChoiceImpl = (_, __) => FakeChoice.ThirdOption;

        var result = DialogService.ShowChoice("pick one",
            ("First", FakeChoice.FirstOption),
            ("Third", FakeChoice.ThirdOption));

        result.ShouldBe(FakeChoice.ThirdOption);
        marshaller.InvokeFuncCallCount.ShouldBe(1);
    }
}
