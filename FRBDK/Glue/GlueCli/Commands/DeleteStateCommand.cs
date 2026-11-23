using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GlueFormsCore.ViewModels;

namespace GlueCli.Commands;

/// <summary>
/// <c>delete-state</c>: deletes a state, or with no --state a whole state category, through the editor's delete
/// flow (<c>DialogCommands.AskToRemoveStateAsync</c> / <c>AskToRemoveStateCategoryAsync</c>), answering the delete
/// dialog from flags as <see cref="DeleteCommand"/> does.
/// </summary>
public static class DeleteStateCommand
{
    public static readonly CliCommand Definition = new()
    {
        Name = "delete-state",
        Summary = "Delete a state, or a whole state category, from a screen or entity.",
        Usage = "delete-state <project> --element <element> --category <category> (--state <state> | --whole-category) [--keep-variables]",
        Description =
@"Deletes the way the editor's Delete does, answering its delete dialog from the flags below. Then saves the
.gluj and element files and regenerates the changed code.

--state deletes that state from the category. --whole-category deletes the category with its states; every
variable typed as the category (in any screen or entity) goes with it, as in the editor.

  --keep-variables  When deleting a state of an older project's uncategorized state list, keep the
                    variables left with no state (the dialog's ""Remove the variable X..."", checked by
                    default). Categorized states never ask this.

The run fails (exit 1, changes not saved) when the element, category or state is unknown. The message lists
close matches.

With --json, ""edit"" holds {kind, element, category, state, alsoRemoved, options, changedElements}: kind is
state or category, state is null for a category, alsoRemoved lists the variables the delete removed with it,
options are the dialog's options with {text, checked}.",
        Options = new CliOption[]
        {
            new("element", "element", "Screen or entity that has the category. Required."),
            new("category", "category", "State category of the state, or the category to delete. Required."),
            new("state", "state", "State to delete."),
            new("whole-category", null, "Delete the category and its states instead of one state."),
            new("keep-variables", null, "Keep variables the deleted state leaves with no state. Default: remove them."),
        },
        Examples = new[]
        {
            ("delete-state MyGame --element Entities\\Enemy --category Health --state Wounded", "Delete one state."),
            ("delete-state MyGame --element Entities\\Enemy --category Health --whole-category --json", "Delete the Health category and the variables typed as it, JSON output."),
        },
        IsEdit = true,
        ValidateArguments = ValidateArguments,
        Run = RunAsync,
    };

    /// <summary>What --json reports under <c>edit</c>.</summary>
    public sealed record DeletedState(string Kind, string Element, string Category, string? State, IReadOnlyList<string> AlsoRemoved,
        IReadOnlyList<DeleteCommand.DeleteOption> Options, IReadOnlyList<string> ChangedElements);

    static string? ValidateArguments(CliContext context)
    {
        foreach (var required in new[] { "element", "category" })
        {
            if (!context.Has(required))
            {
                return $"delete-state needs --{required}.";
            }
        }
        if (context.Has("state") == context.Has("whole-category"))
        {
            return "Pass exactly one of --state or --whole-category.";
        }
        if (context.Has("keep-variables") && !context.Has("state"))
        {
            return "--keep-variables only goes with --state.";
        }
        return null;
    }

    static async Task RunAsync(CliContext context)
    {
        var element = CliLookup.FindElement(context.Get("element")!, out var elementError);
        if (element == null)
        {
            context.Fail(elementError!);
            return;
        }
        GlueState.Self.CurrentElement = element;

        var category = CliStates.FindCategory(context, element, context.Get("category")!);
        if (category == null)
        {
            return;
        }
        StateSave? state = null;
        if (context.Has("state"))
        {
            state = CliStates.FindState(context, element, category, context.Get("state")!);
            if (state == null)
            {
                return;
            }
        }

        var before = CliStates.SerializeElements();
        DeleteOptionsViewModel? answered = null;
        var previousShowDelete = DialogService.ShowDeleteImpl;
        DialogService.ShowDeleteImpl = viewModel =>
        {
            if (context.Has("keep-variables"))
            {
                foreach (var option in viewModel.Options.Where(item => item.Tag is DeletionPlanner.RemoveCustomVariableTag))
                {
                    option.IsChecked = false;
                }
            }
            answered = viewModel;
            return true;
        };
        bool wasDeleted;
        try
        {
            var dialogs = GlueCommands.Self.DialogCommands;
            wasDeleted = state != null
                ? await dialogs.AskToRemoveStateAsync(state)
                : await dialogs.AskToRemoveStateCategoryAsync(category);
            await FlatRedBall.Glue.Managers.TaskManager.Self.WaitForAllTasksFinished();
        }
        finally
        {
            DialogService.ShowDeleteImpl = previousShowDelete;
        }

        if (!wasDeleted || answered == null)
        {
            context.Fail($"Glue did not delete {state?.Name ?? category.Name}.");
            return;
        }

        var alsoRemoved = state != null
            ? answered.Options.Where(option => option.IsChecked && option.Tag is DeletionPlanner.RemoveCustomVariableTag)
                .Select(option => ((DeletionPlanner.RemoveCustomVariableTag)option.Tag).CustomVariable.ToString()).ToList()
            : answered.ObjectsToRemove.ToList();
        var result = new DeletedState(state != null ? "state" : "category", element.Name, category.Name, state?.Name, alsoRemoved,
            answered.Options.Select(option => new DeleteCommand.DeleteOption(option.Display, option.IsChecked)).ToList(),
            CliStates.ChangedElements(before));

        var lines = new List<string>
        {
            state != null
                ? $"Deleted state {state.Name} from {category.Name} in {element.Name}."
                : $"Deleted state category {category.Name} from {element.Name}.",
        };
        lines.AddRange(alsoRemoved.Select(item => $"  also removed {item}"));
        lines.AddRange(CliStates.ChangedElementsLine(result.ChangedElements));
        context.Report(result, string.Join(Environment.NewLine, lines));
    }
}
