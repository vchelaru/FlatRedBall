using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Services;
using FlatRedBall.Glue.SetVariable;

namespace GlueCli.Commands;

/// <summary>
/// <c>rename-state</c>: renames a state the way its Name in the Properties grid does (SetPropertyManager's state
/// branch): set the name, StateSaveSetVariableLogic.ReactToStateSaveChangedValue repoints variables, objects and
/// other states that used the old name, then the plugins hear of the change. The grid validates by showing a
/// message and reverting, so NameVerifier.IsStateNameValid runs here first.
/// </summary>
public static class RenameStateCommand
{
    public static readonly CliCommand Definition = new()
    {
        Name = "rename-state",
        Summary = "Rename a state, updating the variables and objects that use it.",
        Usage = "rename-state <project> --element <element> --category <category> --state <state> --to <new name>",
        Description =
@"Renames the state the way the editor's Properties grid does, then saves the .gluj and element files and
regenerates the changed code. Variables whose value is the old state, objects whose CurrentState or state
variable is the old state, and states in other elements that set it follow. Hand-written code is not changed.
To rename a category, use rename-state-category.

The run fails (exit 1, changes not saved) when the element, category or state is unknown, or the new name is
invalid or already used in the category. The message lists close matches.

With --json, ""edit"" holds {element, category, oldName, newName, changedElements}.",
        Options = new CliOption[]
        {
            new("element", "element", "Screen or entity that has the category. Required."),
            new("category", "category", "State category of the state. Required."),
            new("state", "state", "State to rename. Required."),
            new("to", "new name", "The new name. Required."),
        },
        Examples = new[]
        {
            ("rename-state MyGame --element Entities\\Enemy --category Health --state Wounded --to Hurt", "Rename a state and everything that sets it."),
        },
        IsEdit = true,
        ValidateArguments = ValidateArguments,
        Run = RunAsync,
    };

    /// <summary>What --json reports under <c>edit</c>.</summary>
    public sealed record RenamedState(string Element, string Category, string OldName, string NewName, IReadOnlyList<string> ChangedElements);

    static string? ValidateArguments(CliContext context)
    {
        foreach (var required in new[] { "element", "category", "state", "to" })
        {
            if (!context.Has(required))
            {
                return $"rename-state needs --{required}.";
            }
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
        var state = CliStates.FindState(context, element, category, context.Get("state")!);
        if (state == null)
        {
            return;
        }

        var oldName = state.Name;
        var newName = context.Get("to")!;
        if (newName == oldName)
        {
            context.Fail($"{oldName} already has that name.");
            return;
        }
        if (!Builder.Get<NameVerifier>().IsStateNameValid(newName, element, category, state, out var whyInvalid))
        {
            context.Fail($"Cannot rename {oldName} in {category.Name} to {newName}: {whyInvalid}");
            return;
        }

        // The grid edits the selected state, and the reaction validates against the selection.
        GlueState.Self.CurrentStateSave = state;
        var before = CliStates.SerializeElements();
        state.Name = newName;
        var updateTreeView = true;
        Builder.Get<StateSaveSetVariableLogic>().ReactToStateSaveChangedValue(state, category, nameof(StateSave.Name), oldName, element, ref updateTreeView);
        PluginManager.ReactToChangedProperty(nameof(StateSave.Name), oldName, element, null!);
        GlueCommands.Self.GenerateCodeCommands.GenerateElementAndReferencedObjectCode(element);
        await TaskManager.Self.WaitForAllTasksFinished();

        if (state.Name != newName)
        {
            context.Fail($"Glue did not rename {oldName} in {category.Name}.");
            return;
        }

        var result = new RenamedState(element.Name, category.Name, oldName, newName, CliStates.ChangedElements(before));
        var lines = new List<string> { $"Renamed state {oldName} in {category.Name} of {element.Name} to {newName}." };
        lines.AddRange(CliStates.ChangedElementsLine(result.ChangedElements));
        context.Report(result, string.Join(Environment.NewLine, lines));
    }
}
