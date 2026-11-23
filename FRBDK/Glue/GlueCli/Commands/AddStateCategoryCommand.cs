using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Services;

namespace GlueCli.Commands;

/// <summary>
/// <c>add-state-category</c>: adds a state category through ElementCommands.AddStateSaveCategoryAsync, the call
/// the tree view's "Add State Category" dialog ends in. A new category excludes every variable; --include and
/// --exclude then move variables into it the way the State Data tab's include/exclude buttons do
/// (ElementCommands.CanVariableBeIncludedInStates, then PluginManager.ReactToStateCategoryExcludedVariablesChangedAsync).
/// </summary>
public static class AddStateCategoryCommand
{
    public static readonly CliCommand Definition = new()
    {
        Name = "add-state-category",
        Summary = "Add a state category to a screen or entity, choosing which variables its states set.",
        Usage = "add-state-category <project> --element <element> --name <name> [--include <var,...> | --exclude <var,...>]",
        Description =
@"Adds the category the way the editor's ""Add State Category"" does, then saves the .gluj and element files
and regenerates the element's code (the category's enum and state properties).

A category's states can only set the variables the category includes. Like the editor, a new category
includes none of the element's variables. --include names the variables to include; --exclude includes
every variable except the ones named. Shared (static) variables and list-typed variables cannot be in a
state, so --exclude skips them and --include fails on them.

Add states with add-state and set their values with set-state-variable.

The run fails (exit 1, changes not saved) when the element is unknown, the name is invalid or already used by a
category of the element or its base, or a variable is unknown or cannot be included. The message lists
close matches.

With --json, ""edit"" holds {element, category, includedVariables, excludedVariables, changedElements}.",
        Options = new CliOption[]
        {
            new("element", "element", "Screen or entity to add the category to. Required."),
            new("name", "name", "Name of the new category. Required."),
            new("include", "var,...", "Variables the category's states set (comma-separated). Default: none."),
            new("exclude", "var,...", "Include every variable except these (comma-separated)."),
        },
        Examples = new[]
        {
            ("add-state-category MyGame --element Entities\\Enemy --name Health --include MovementSpeed,Damage", "Add a category whose states set two variables."),
            ("add-state-category MyGame --element Entities\\Enemy --name Size --exclude X,Y,Z --json", "Include every variable but the position, JSON output."),
        },
        IsEdit = true,
        ValidateArguments = ValidateArguments,
        Run = RunAsync,
    };

    /// <summary>What --json reports under <c>edit</c>.</summary>
    public sealed record AddedCategory(string Element, string Category, IReadOnlyList<string> IncludedVariables,
        IReadOnlyList<string> ExcludedVariables, IReadOnlyList<string> ChangedElements);

    static string? ValidateArguments(CliContext context)
    {
        foreach (var required in new[] { "element", "name" })
        {
            if (!context.Has(required))
            {
                return $"add-state-category needs --{required}.";
            }
        }
        if (context.Has("include") && context.Has("exclude"))
        {
            return "Pass --include or --exclude, not both.";
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

        var name = context.Get("name")!;
        if (!Builder.Get<NameVerifier>().IsStateCategoryNameValid(name, out var whyInvalid))
        {
            context.Fail($"Cannot add the category {name} to {element.Name}: {whyInvalid}");
            return;
        }
        // The editor's dialog does not check this; a second category of the same name generates code that does not compile.
        var owner = new[] { element }.Concat(ObjectFinder.Self.GetAllBaseElementsRecursively(element))
            .FirstOrDefault(item => item.StateCategoryList.Any(category => string.Equals(category.Name, name, StringComparison.OrdinalIgnoreCase)));
        if (owner != null)
        {
            context.Fail($"Cannot add the category {name} to {element.Name}: " +
                (owner == element ? "it already has a category with that name." : $"its base {owner.Name} already has a category with that name."));
            return;
        }

        // The State Data tab lists the element's own variables that are not shared.
        var listed = element.CustomVariables.Where(item => !item.IsShared).Select(item => item.Name).ToList();
        List<string> toInclude;
        if (context.Has("include"))
        {
            toInclude = new List<string>();
            foreach (var requested in CliStates.SplitList(context.Get("include")))
            {
                var variable = FindVariable(context, element, requested);
                if (variable == null)
                {
                    return;
                }
                if (variable.IsShared)
                {
                    context.Fail($"{element.Name}.{variable.Name} is shared (static), so states cannot set it.");
                    return;
                }
                var canInclude = GlueCommands.Self.GluxCommands.ElementCommands.CanVariableBeIncludedInStates(variable.Name, element);
                if (!canInclude.Succeeded)
                {
                    context.Fail($"{element.Name}.{variable.Name} cannot be included: {canInclude.Message}");
                    return;
                }
                if (!toInclude.Contains(variable.Name))
                {
                    toInclude.Add(variable.Name);
                }
            }
        }
        else if (context.Has("exclude"))
        {
            var excluded = new List<string>();
            foreach (var requested in CliStates.SplitList(context.Get("exclude")))
            {
                var variable = FindVariable(context, element, requested);
                if (variable == null)
                {
                    return;
                }
                excluded.Add(variable.Name);
            }
            toInclude = listed
                .Where(item => !excluded.Contains(item) &&
                    GlueCommands.Self.GluxCommands.ElementCommands.CanVariableBeIncludedInStates(item, element).Succeeded)
                .ToList();
        }
        else
        {
            toInclude = new List<string>();
        }

        var before = CliStates.SerializeElements();
        await GlueCommands.Self.GluxCommands.ElementCommands.AddStateSaveCategoryAsync(name, element);
        await TaskManager.Self.WaitForAllTasksFinished();

        var category = element.StateCategoryList.LastOrDefault(item => item.Name == name);
        if (category == null)
        {
            context.Fail($"Glue did not add the category {name} to {element.Name}.");
            return;
        }

        if (toInclude.Count > 0)
        {
            // StateCategoryViewModel.HandleExcludedVariablesChanged
            category.ExcludedVariables.RemoveAll(toInclude.Contains);
            foreach (var included in toInclude)
            {
                await PluginManager.ReactToStateCategoryExcludedVariablesChangedAsync(category, included, StateCategoryVariableAction.Included);
            }
            GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element);
            await TaskManager.Self.WaitForAllTasksFinished();
        }

        var result = new AddedCategory(element.Name, category.Name, toInclude, category.ExcludedVariables.ToList(), CliStates.ChangedElements(before));
        var lines = new List<string>
        {
            $"Added state category {result.Category} to {result.Element}.",
            "  includes: " + (result.IncludedVariables.Count == 0 ? "(no variables)" : string.Join(", ", result.IncludedVariables)),
        };
        lines.AddRange(CliStates.ChangedElementsLine(result.ChangedElements));
        context.Report(result, string.Join(Environment.NewLine, lines));
    }

    static CustomVariable? FindVariable(CliContext context, GlueElement element, string name)
    {
        var variables = element.CustomVariables;
        var variable = variables.FirstOrDefault(item => item.Name == name) ??
            variables.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
        if (variable == null)
        {
            context.Fail($"{element.Name} has no variable '{name}'." +
                (variables.Count == 0 ? " It defines no variables." : CliLookup.DescribeCloseMatches(name, variables.Select(item => item.Name))));
        }
        return variable;
    }
}
