using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GlueFormsCore.ViewModels;

namespace GlueCli.Commands;

/// <summary>
/// <c>remove-variable</c>: removes a screen's or entity's variable through the editor's Delete,
/// <c>DialogCommands.AskToRemoveCustomVariableAsync</c>, which asks its one confirm through
/// <see cref="DialogService.ShowDeleteImpl"/> and then calls <c>GluxCommands.RemoveCustomVariable</c>. The command
/// answers the confirm yes; a variable's delete offers no other choice.
/// </summary>
public static class RemoveVariableCommand
{
    public static readonly CliCommand Definition = new()
    {
        Name = "remove-variable",
        Summary = "Remove a variable from a screen or entity (a new, tunneled or exposed one).",
        Usage = "remove-variable <project> --element <element> --variable <name>",
        Description =
@"Removes the variable the way the editor's Delete does on the variable. What goes with it goes as in the
editor: events raised by the variable are removed, and derived elements lose their copy of it. Then saves
the .gluj and element files and regenerates code.

The editor's delete asks only ""Are you sure?"", which this command answers yes, so there are no answer flags.
Objects in other elements that set the variable on an instance keep that value, as in the editor; the run
reports them, and fails if that leaves errors.

The run fails (exit 1, changes not saved) when the element or variable is unknown (the message lists close
matches), or when the variable comes from a base element, where it has to be removed.

With --json, ""edit"" holds {element, variable, type, removedEvents, instancesSettingIt, changedElements}:
removedEvents lists the events removed with it, instancesSettingIt the Element.Object pairs that still set
it, changedElements every screen and entity the removal changed.",
        Options = new CliOption[]
        {
            new("element", "element", "Screen or entity that defines the variable. Required."),
            new("variable", "name", "The variable to remove. Required."),
        },
        Examples = new[]
        {
            ("remove-variable MyGame --element Entities\\Player --variable JumpSpeed", "Remove a variable."),
            ("remove-variable MyGame --element GameScreen --variable PlayerSpeed --json", "Remove a screen variable, JSON output."),
        },
        IsEdit = true,
        ValidateArguments = ValidateArguments,
        Run = RunAsync,
    };

    /// <summary>What --json reports under <c>edit</c>.</summary>
    public sealed record RemovedVariable(string Element, string Variable, string? Type, IReadOnlyList<string> RemovedEvents,
        IReadOnlyList<string> InstancesSettingIt, IReadOnlyList<string> ChangedElements);

    static string? ValidateArguments(CliContext context)
    {
        foreach (var required in new[] { "element", "variable" })
        {
            if (!context.Has(required))
            {
                return $"remove-variable needs --{required}.";
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

        var variableName = context.Get("variable")!;
        var variable = element.CustomVariables.FirstOrDefault(item => item.Name == variableName) ??
            element.CustomVariables.FirstOrDefault(item => string.Equals(item.Name, variableName, StringComparison.OrdinalIgnoreCase));
        var inBase = variable?.DefinedByBase == true ? ObjectFinder.Self.GetRootCustomVariable(variable) : null;
        if (variable == null || inBase != null)
        {
            inBase ??= element.GetCustomVariableRecursively(variableName);
            context.Fail(inBase != null
                ? $"{inBase.Name} comes from {ObjectFinder.Self.GetElementContaining(inBase)?.Name}, a base of {element.Name}; remove it there."
                : $"{element.Name} has no variable '{variableName}'." + (element.CustomVariables.Count == 0
                    ? " It defines no variables."
                    : CliLookup.DescribeCloseMatches(variableName, element.CustomVariables.Select(item => item.Name))));
            return;
        }

        var removedEvents = element.GetEventsOnVariable(variable.Name).Select(item => item.EventName).ToList();
        var instancesSettingIt = ObjectFinder.Self.GetAllNamedObjectsThatUseElement(element)
            .Where(nos => nos.GetCustomVariable(variable.Name)?.Value != null)
            .Select(nos => $"{ObjectFinder.Self.GetElementContaining(nos)?.Name}.{nos.InstanceName}")
            .ToList();
        var before = CliChanges.Snapshot();

        DeleteOptionsViewModel? asked = null;
        var previousShowDelete = DialogService.ShowDeleteImpl;
        DialogService.ShowDeleteImpl = viewModel =>
        {
            asked = viewModel;
            return true;
        };
        bool wasRemoved;
        try
        {
            wasRemoved = await GlueCommands.Self.DialogCommands.AskToRemoveCustomVariableAsync(variable);
            // The editor's Delete regenerates the element after the removal.
            GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element);
            await TaskManager.Self.WaitForAllTasksFinished();
        }
        finally
        {
            DialogService.ShowDeleteImpl = previousShowDelete;
        }

        if (!wasRemoved || asked == null || element.CustomVariables.Contains(variable))
        {
            context.Fail($"Glue did not remove {element.Name}.{variable.Name}.");
            return;
        }

        var result = new RemovedVariable(element.Name, variable.Name, variable.Type, removedEvents, instancesSettingIt,
            CliChanges.ChangedElements(before));
        var lines = new List<string> { $"Removed {element.Name}.{variable.Name} ({variable.Type})." };
        if (removedEvents.Count > 0)
        {
            lines.Add("  removed events: " + string.Join(", ", removedEvents));
        }
        if (instancesSettingIt.Count > 0)
        {
            lines.Add("  still set on: " + string.Join(", ", instancesSettingIt));
        }
        if (result.ChangedElements.Count > 0)
        {
            lines.Add("  changed elements: " + string.Join(", ", result.ChangedElements));
        }
        context.Report(result, string.Join(Environment.NewLine, lines));
    }
}
