using System;
using System.Linq;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.Managers;
using GlueFormsCore.Controls;
using GlueFormsCore.ViewModels;

namespace FlatRedBall.Glue.Controls
{
    /// <summary>
    /// The result of a simple confirm/choice dialog. Used both as the return type of <see cref="DialogService.ShowConfirm"/>
    /// and as the button-identity vocabulary for the default WPF implementations.
    /// </summary>
    public enum DialogButton
    {
        Ok,
        Yes,
        No,
        Cancel,
        Retry
    }

    /// <summary>
    /// Central seam for all simple message/confirm/choice dialogs in Glue. Every public method here routes
    /// through <see cref="TaskManager.OnUiThread{T}"/>, so callers never need to think about which thread
    /// they're on - it's always safe to call these from a background task (e.g. code reached via the
    /// file-watcher / glux-reload path), not just from a WPF event handler.
    /// </summary>
    /// <remarks>
    /// This is the generalization of the swappable-delegate pattern first introduced for
    /// <c>DuplicateProjectEntryDialog.Show</c> (see #1921) - each of the three <c>*Impl</c> properties can be
    /// replaced in unit tests so calling code can be tested without a live WPF window. WPF-specific code
    /// (<c>MultiButtonMessageBoxWpf</c>, <c>System.Windows.MessageBox</c>) is isolated to the three
    /// <c>Default*</c> methods below - nothing else in the codebase should reference those types directly.
    /// </remarks>
    public static class DialogService
    {
        public static Action<string> ShowMessageImpl { get; set; } = DefaultShowMessage;
        public static Func<string, DialogButton, DialogButton, DialogButton?> ShowConfirmImpl { get; set; } = DefaultShowConfirm;
        public static Func<string, (string label, object value)[], object> ShowChoiceImpl { get; set; } = DefaultShowChoice;
        public static Func<DeleteOptionsViewModel, bool> ShowDeleteImpl { get; set; } = DefaultShowDelete;

        /// <summary>
        /// Shows a simple single-button ("OK") informational message.
        /// </summary>
        public static void ShowMessage(string text)
        {
            TaskManager.Self.OnUiThread(() => ShowMessageImpl(text));
        }

        /// <summary>
        /// Shows a two-button confirm dialog and returns which button was clicked, or null if the dialog was
        /// closed without clicking a button (e.g. Escape).
        /// </summary>
        public static DialogButton? ShowConfirm(string text, DialogButton primary = DialogButton.Yes, DialogButton secondary = DialogButton.No)
        {
            return TaskManager.Self.OnUiThread(() => ShowConfirmImpl(text, primary, secondary));
        }

        /// <summary>
        /// Shows an N-button choice dialog, one button per entry in <paramref name="options"/>, and returns the
        /// <typeparamref name="TResult"/> value associated with whichever button was clicked. If the dialog is
        /// closed without clicking a button (e.g. Escape), returns <c>default(TResult)</c> - callers that need
        /// different escape behavior should order their options so the intended fallback is the default value
        /// (e.g. an enum whose 0 value means "do nothing"/"cancel").
        /// </summary>
        public static TResult ShowChoice<TResult>(string text, params (string label, TResult value)[] options)
        {
            var boxedOptions = options
                .Select(o => (o.label, (object)o.value))
                .ToArray();

            var result = TaskManager.Self.OnUiThread(() => ShowChoiceImpl(text, boxedOptions));

            return result is TResult typed ? typed : default;
        }

        /// <summary>
        /// Shows the one dialog a delete asks everything through - what is being deleted, which optional
        /// parts to include, and what to do with the files left behind. Returns true if the user confirmed.
        /// See GitHub issue #429 for why this replaced a chain of separate prompts.
        /// </summary>
        public static bool ShowDelete(DeleteOptionsViewModel viewModel)
        {
            return TaskManager.Self.OnUiThread(() => ShowDeleteImpl(viewModel));
        }

        private static void DefaultShowMessage(string text)
        {
            if (!GlueGui.ShowGui)
            {
                return;
            }

            var mbmb = new MultiButtonMessageBoxWpf
            {
                MessageText = text
            };

            mbmb.AddActionButton("Copy to Clipboard", () => ClipboardService.SetText(text));
            mbmb.AddButton("OK", DialogButton.Ok);

            mbmb.ShowDialog();
        }

        private static bool DefaultShowDelete(DeleteOptionsViewModel viewModel)
        {
            if (!GlueGui.ShowGui)
            {
                return false;
            }

            var window = new DeleteOptionsWindow
            {
                DataContext = viewModel
            };

            return window.ShowDialog() == true;
        }

        private static DialogButton? DefaultShowConfirm(string text, DialogButton primary, DialogButton secondary)
        {
            // Headless (unit tests, automated Glue) has no desktop to put a modal on. Returning null is the
            // same answer the caller gets when the user closes the window without clicking a button.
            if (!GlueGui.ShowGui)
            {
                return null;
            }

            var mbmb = new MultiButtonMessageBoxWpf
            {
                MessageText = text
            };

            mbmb.AddButton(primary.ToString(), primary);
            mbmb.AddButton(secondary.ToString(), secondary);

            if (mbmb.ShowDialog() == true)
            {
                return (DialogButton)mbmb.ClickedResult;
            }

            return null;
        }

        private static object DefaultShowChoice(string text, (string label, object value)[] options)
        {
            if (!GlueGui.ShowGui)
            {
                return null;
            }

            var mbmb = new MultiButtonMessageBoxWpf
            {
                MessageText = text
            };

            foreach (var option in options)
            {
                mbmb.AddButton(option.label, option.value);
            }

            if (mbmb.ShowDialog() == true)
            {
                return mbmb.ClickedResult;
            }

            return null;
        }
    }
}
