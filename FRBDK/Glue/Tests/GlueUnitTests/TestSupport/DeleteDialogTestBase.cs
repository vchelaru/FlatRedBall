using FlatRedBall.Glue.Controls;
using GlueFormsCore.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace GlueUnitTests.TestSupport;

/// <summary>
/// The shared harness for "a delete asks exactly one question". Every delete in Glue is supposed to plan
/// its work, show one <see cref="DeleteOptionsViewModel"/>, and then apply the answers - so a test proves
/// itself by counting dialogs, not by inspecting one. See GitHub issues #429 and #2032.
/// </summary>
public abstract class DeleteDialogTestBase : IDisposable
{
    private readonly Func<DeleteOptionsViewModel, bool> _originalShowDeleteImpl = DialogService.ShowDeleteImpl;
    private readonly Func<string, DialogButton, DialogButton, DialogButton?> _originalShowConfirmImpl = DialogService.ShowConfirmImpl;
    private readonly Func<string, (string label, object value)[], object> _originalShowChoiceImpl = DialogService.ShowChoiceImpl;
    private readonly Action<string> _originalShowMessageImpl = DialogService.ShowMessageImpl;

    /// <summary>
    /// Every view model handed to the one delete dialog, in order. Length is the assertion that matters:
    /// the whole point is that a delete asks once.
    /// </summary>
    protected List<DeleteOptionsViewModel> ShownDeleteDialogs { get; } = new();

    /// <summary>
    /// Anything that reached one of the *other* dialog seams during a delete. A delete that populates this
    /// is asking a question outside the single dialog, which is the regression being guarded against.
    /// </summary>
    protected List<string> OtherPrompts { get; } = new();

    protected DeleteDialogTestBase()
    {
        DialogService.ShowConfirmImpl = (text, _, __) => { OtherPrompts.Add(text); return null; };
        DialogService.ShowChoiceImpl = (text, _) => { OtherPrompts.Add(text); return null; };
        DialogService.ShowMessageImpl = text => OtherPrompts.Add(text);
    }

    public void Dispose()
    {
        DialogService.ShowDeleteImpl = _originalShowDeleteImpl;
        DialogService.ShowConfirmImpl = _originalShowConfirmImpl;
        DialogService.ShowChoiceImpl = _originalShowChoiceImpl;
        DialogService.ShowMessageImpl = _originalShowMessageImpl;
    }

    /// <summary>
    /// Answers the delete dialog the way a user would: records what it was asked, optionally adjusts the
    /// options, and clicks Delete.
    /// </summary>
    protected void AnswerDeleteDialog(Action<DeleteOptionsViewModel> adjust = null, bool confirm = true)
    {
        DialogService.ShowDeleteImpl = viewModel =>
        {
            ShownDeleteDialogs.Add(viewModel);
            adjust?.Invoke(viewModel);
            return confirm;
        };
    }

    /// <summary>
    /// The single dialog the delete under test showed. Fails loudly when a delete showed none or several,
    /// which is the thing being pinned.
    /// </summary>
    protected DeleteOptionsViewModel TheOnlyDeleteDialog
    {
        get
        {
            if (ShownDeleteDialogs.Count != 1)
            {
                throw new Exception(
                    $"expected exactly one delete dialog, saw {ShownDeleteDialogs.Count}. " +
                    "Other prompts: " + string.Join(" | ", OtherPrompts));
            }

            return ShownDeleteDialogs[0];
        }
    }

    protected string OtherPromptsMessage =>
        "every question a delete asks belongs in the one delete dialog: " + string.Join(" | ", OtherPrompts);

    internal static async Task<TempDir> LoadFormsSampleAsync()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        var project = GoldProject.CopyOutOfRepo("Samples/FormsSampleProject");
        await GoldProject.LoadInGlueAsync(
            Path.Combine(project.Root, "FormsSampleProject", "FormsSampleProject.csproj"));

        return project;
    }
}
