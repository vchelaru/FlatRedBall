using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.Controls;

namespace GlueCli;

/// <summary>
/// While a command's edit runs, answers the questions the command has flags for and takes the informational
/// messages the command reports itself (as part of its <c>edit</c>). Anything a handler does not claim still
/// reaches the runner's handlers, which report it as a dialog message and fail the run, so a question nobody
/// has a flag for never gets answered silently. Dispose (or end the <c>using</c>) after the edit's queued tasks
/// have finished, or a late question bypasses the answers.
/// </summary>
public sealed class CliDialogs : IDisposable
{
    readonly (Action<string> message, Func<string, DialogButton, DialogButton, DialogButton?> confirm,
        Func<string, (string label, object value)[], object> choice, Action<string> suppressed) previous;

    /// <summary>The messages <c>takeMessage</c> claimed, in the order Glue sent them.</summary>
    public List<string> TakenMessages { get; } = new();

    /// <param name="answerConfirm">The answer to a yes/no question, or null to leave it to the runner.</param>
    /// <param name="answerChoice">The chosen option's value for a multiple-choice question, or null to leave it.</param>
    /// <param name="takeMessage">True for an informational message the command reports itself.</param>
    public CliDialogs(
        Func<string, DialogButton, DialogButton, DialogButton?>? answerConfirm = null,
        Func<string, (string label, object value)[], object?>? answerChoice = null,
        Func<string, bool>? takeMessage = null)
    {
        previous = (DialogService.ShowMessageImpl, DialogService.ShowConfirmImpl, DialogService.ShowChoiceImpl,
            GlueGui.SuppressedMessageHandler);
        var (previousMessage, previousConfirm, previousChoice, previousSuppressed) = previous;

        bool Take(string text)
        {
            if (takeMessage?.Invoke(text) != true)
            {
                return false;
            }
            lock (TakenMessages) { TakenMessages.Add(text); }
            return true;
        }

        DialogService.ShowMessageImpl = text =>
        {
            if (!Take(text)) { previousMessage(text); }
        };
        GlueGui.SuppressedMessageHandler = text =>
        {
            if (!Take(text)) { previousSuppressed?.Invoke(text); }
        };
        DialogService.ShowConfirmImpl = (text, primary, secondary) =>
            answerConfirm?.Invoke(text, primary, secondary) ?? previousConfirm(text, primary, secondary);
        DialogService.ShowChoiceImpl = (text, options) =>
            answerChoice?.Invoke(text, options) ?? previousChoice(text, options);
    }

    public void Dispose()
    {
        (DialogService.ShowMessageImpl, DialogService.ShowConfirmImpl, DialogService.ShowChoiceImpl,
            GlueGui.SuppressedMessageHandler) = previous;
    }
}
