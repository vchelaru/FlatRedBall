using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Services;

namespace GlueCli.Commands;

/// <summary>
/// <c>add-state</c>: adds a state to a category through GluxCommands.AddStateSave, the call the tree view's
/// "Add State" dialog ends in, after the dialog's NameVerifier.IsStateNameValid check.
/// </summary>
public static class AddStateCommand
{
    public static readonly CliCommand Definition = new()
    {
        Name = "add-state",
        Summary = "Add a state to a state category of a screen or entity.",
        Usage = "add-state <project> --element <element> --category <category> --name <name>",
        Description =
@"Adds the state the way the editor's ""Add State"" does, then saves the .gluj and element files and
regenerates the element's code. The new state sets no variables; set them with set-state-variable.

The run fails (exit 1, changes not saved) when the element or category is unknown, or the name is invalid or
already used by a state in the category (or the same category of the element's base). The message lists
close matches.

With --json, ""edit"" holds {element, category, state, changedElements}.",
        Options = new CliOption[]
        {
            new("element", "element", "Screen or entity that has the category. Required."),
            new("category", "category", "State category to add the state to. Required."),
            new("name", "name", "Name of the new state. Required."),
        },
        Examples = new[]
        {
            ("add-state MyGame --element Entities\\Enemy --category Health --name Wounded", "Add the state Wounded to the Health category."),
        },
        IsEdit = true,
        ValidateArguments = ValidateArguments,
        Run = RunAsync,
    };

    /// <summary>What --json reports under <c>edit</c>.</summary>
    public sealed record AddedState(string Element, string Category, string State, IReadOnlyList<string> ChangedElements);

    static string? ValidateArguments(CliContext context)
    {
        foreach (var required in new[] { "element", "category", "name" })
        {
            if (!context.Has(required))
            {
                return $"add-state needs --{required}.";
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

        var name = context.Get("name")!;
        if (!Builder.Get<NameVerifier>().IsStateNameValid(name, element, category, null, out var whyInvalid))
        {
            context.Fail($"Cannot add the state {name} to {category.Name} in {element.Name}: {whyInvalid}");
            return;
        }
        if (category.States.Any(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            context.Fail($"Cannot add the state {name} to {category.Name} in {element.Name}: it already has a state with that name.");
            return;
        }

        var before = CliStates.SerializeElements();
        var state = new StateSave { Name = name };
        await GlueCommands.Self.GluxCommands.AddStateSave(state, category, element);
        await TaskManager.Self.WaitForAllTasksFinished();

        if (!category.States.Contains(state))
        {
            context.Fail($"Glue did not add the state {name} to {category.Name} in {element.Name}.");
            return;
        }

        var result = new AddedState(element.Name, category.Name, state.Name, CliStates.ChangedElements(before));
        var lines = new List<string> { $"Added state {result.State} to {result.Category} in {result.Element}." };
        lines.AddRange(CliStates.ChangedElementsLine(result.ChangedElements));
        context.Report(result, string.Join(Environment.NewLine, lines));
    }
}
