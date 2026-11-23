using System.Globalization;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;

namespace GlueCli.Commands;

/// <summary>
/// <c>set-state-variable</c>: sets or clears one variable's value in a state, the way a cell of the State Data
/// tab does (StateCategoryViewModel.ApplyValueToModel: StateSave.SetValue or removing the instruction, then
/// PluginManager.ReactToStateVariableChanged). The tab lives in OfficialPlugins, which GlueCli does not
/// reference, so the few lines it runs are repeated here. The text is converted the way set-variable converts it.
/// </summary>
public static class SetStateVariableCommand
{
    public static readonly CliCommand Definition = new()
    {
        Name = "set-state-variable",
        Summary = "Set or clear the value a state gives one of the element's variables.",
        Usage = "set-state-variable <project> --element <element> --category <category> --state <state> --variable <name> (--value <text> | --clear) [--include]",
        Description =
@"Sets the value the way a cell of the editor's State Data tab does, then saves the .gluj and element files
and regenerates the element's code (the state's assignments).

<text> is converted to the variable's type with invariant culture: 1.5, true, an enum value name. Files,
colors and states are stored as text, as the editor stores them. --clear removes the value, so the state
leaves the variable alone.

A state can only set a variable its category includes. Pass --include to include the variable in the
category first (the State Data tab's include button); without it the run fails. Shared (static) variables
and list-typed variables can never be in a state.

The run fails (exit 1, nothing saved) when the element, category, state or variable is unknown, or the text
does not convert to the variable's type. The message lists close matches or names the expected type.

With --json, ""edit"" holds {element, category, state, variable, type, oldValue, newValue, includedVariable,
changedElements}: oldValue and newValue are null when the state does not set the variable;
includedVariable is true when --include added the variable to the category.",
        Options = new CliOption[]
        {
            new("element", "element", "Screen or entity that has the category. Required."),
            new("category", "category", "State category of the state. Required."),
            new("state", "state", "State to change. Required."),
            new("variable", "name", "The element's variable. Required."),
            new("value", "text", "New value as text."),
            new("clear", null, "Remove the state's value for the variable instead of setting one."),
            new("include", null, "Include the variable in the category when it is excluded. Default: fail."),
        },
        Examples = new[]
        {
            ("set-state-variable MyGame --element Entities\\Enemy --category Health --state Wounded --variable MovementSpeed --value 80", "Make the Wounded state slow the enemy down."),
            ("set-state-variable MyGame --element Entities\\Enemy --category Health --state Wounded --variable Drag --value 2 --include --json", "Include Drag in the category and set it, JSON output."),
            ("set-state-variable MyGame --element Entities\\Enemy --category Health --state Wounded --variable MovementSpeed --clear", "Stop the state from setting MovementSpeed."),
        },
        IsEdit = true,
        ValidateArguments = ValidateArguments,
        Run = RunAsync,
    };

    /// <summary>What --json reports under <c>edit</c>.</summary>
    public sealed record SetStateVariable(string Element, string Category, string State, string Variable, string? Type,
        object? OldValue, object? NewValue, bool IncludedVariable, IReadOnlyList<string> ChangedElements);

    static string? ValidateArguments(CliContext context)
    {
        foreach (var required in new[] { "element", "category", "state", "variable" })
        {
            if (!context.Has(required))
            {
                return $"set-state-variable needs --{required}.";
            }
        }
        if (context.Has("value") == context.Has("clear"))
        {
            return "Pass exactly one of --value or --clear.";
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

        // The State Data tab's columns: the element's own variables that are not shared.
        var variableName = context.Get("variable")!;
        var variables = element.CustomVariables;
        var variable = variables.FirstOrDefault(item => item.Name == variableName) ??
            variables.FirstOrDefault(item => string.Equals(item.Name, variableName, StringComparison.OrdinalIgnoreCase));
        if (variable == null)
        {
            var inBase = element.GetCustomVariableRecursively(variableName) != null;
            context.Fail($"{element.Name} has no variable '{variableName}'." +
                (inBase ? " It is defined by a base element; states can only set the element's own variables." :
                 variables.Count == 0 ? " It defines no variables." : CliLookup.DescribeCloseMatches(variableName, variables.Select(item => item.Name))));
            return;
        }
        var target = $"{element.Name}.{category.Name}.{state.Name}.{variable.Name}";
        if (variable.IsShared)
        {
            context.Fail($"{element.Name}.{variable.Name} is shared (static), so states cannot set it.");
            return;
        }

        var include = category.ExcludedVariables.Contains(variable.Name) && !context.Has("clear");
        if (include)
        {
            if (!context.Has("include"))
            {
                context.Fail($"{category.Name} in {element.Name} excludes {variable.Name}, so its states cannot set it. " +
                    "Pass --include to include it in the category.");
                return;
            }
            var canInclude = GlueCommands.Self.GluxCommands.ElementCommands.CanVariableBeIncludedInStates(variable.Name, element);
            if (!canInclude.Succeeded)
            {
                context.Fail($"{element.Name}.{variable.Name} cannot be included: {canInclude.Message}");
                return;
            }
        }

        var keepsText = variable.GetIsVariableState(element) || variable.GetIsFile() || variable.GetIsObjectType() ||
            variable.Type is "Color" or "Microsoft.Xna.Framework.Color";
        var typeName = string.IsNullOrEmpty(variable.OverridingPropertyType) ? variable.Type : variable.OverridingPropertyType;
        object? value = null;
        if (context.Has("value"))
        {
            var text = context.Get("value")!;
            if (text.Length == 0)
            {
                context.Fail($"An empty --value clears the cell in the editor; pass --clear to remove {target}.");
                return;
            }
            if (!SetVariableCommand.TryConvert(context, text, keepsText ? null : typeName, target, out value))
            {
                return;
            }
        }

        // The State Data tab shows the selected category.
        GlueState.Self.CurrentStateSaveCategory = category;
        var before = CliStates.SerializeElements();
        var oldValue = state.InstructionSaves.FirstOrDefault(item => item.Member == variable.Name)?.Value;

        if (include)
        {
            // StateCategoryViewModel.HandleExcludedVariablesChanged
            category.ExcludedVariables.Remove(variable.Name);
            await PluginManager.ReactToStateCategoryExcludedVariablesChangedAsync(category, variable.Name, StateCategoryVariableAction.Included);
        }

        // StateCategoryViewModel.ApplyValueToModel
        if (value == null)
        {
            var existing = state.InstructionSaves.FirstOrDefault(item => item.Member == variable.Name);
            if (existing != null)
            {
                state.InstructionSaves.Remove(existing);
            }
        }
        else
        {
            state.SetValue(variable.Name, value);
        }
        PluginManager.ReactToStateVariableChanged(state, category, variable.Name);
        // StateViewModel.HandleStateVariablePropertyChanged
        GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element);
        await TaskManager.Self.WaitForAllTasksFinished();

        var newValue = state.InstructionSaves.FirstOrDefault(item => item.Member == variable.Name)?.Value;
        var result = new SetStateVariable(element.Name, category.Name, state.Name, variable.Name, typeName, oldValue, newValue,
            include, CliStates.ChangedElements(before));
        var lines = new List<string>();
        if (include)
        {
            lines.Add($"Included {variable.Name} in {category.Name}.");
        }
        lines.Add($"Set {target} ({typeName}): {Describe(oldValue)} -> {Describe(newValue)}.");
        lines.AddRange(CliStates.ChangedElementsLine(result.ChangedElements));
        context.Report(result, string.Join(Environment.NewLine, lines));
    }

    static string Describe(object? value) => value switch
    {
        null => "(not set)",
        string text => $"\"{text}\"",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "",
    };
}
