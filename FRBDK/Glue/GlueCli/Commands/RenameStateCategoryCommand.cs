using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Services;

namespace GlueCli.Commands;

/// <summary>
/// <c>rename-state-category</c>: renames a category the way the State Data tab's name box does
/// (StateCategoryViewModel): StateSaveCategoryRenameLogic.Rename renames the category and what refers to it, then
/// all code is regenerated. The tab does not validate the name, so the command checks it first.
/// </summary>
public static class RenameStateCategoryCommand
{
    public static readonly CliCommand Definition = new()
    {
        Name = "rename-state-category",
        Summary = "Rename a state category, updating the variables and objects that use it.",
        Usage = "rename-state-category <project> --element <element> --category <category> --to <new name>",
        Description =
@"Renames the category the way the editor's State Data tab does, then saves the .gluj and element files and
regenerates code. Variables typed as the category follow, in any element. So does its Current<Category>State
variable in the element and the elements that inherit it, with the states, events, instance variables and
tunneled variables that set it. Hand-written code is not changed.

The run fails (exit 1, nothing saved) when the element or category is unknown, the new name is invalid, the
element, its base or a derived element already has a category with that name, or Current<new name>State is
already a variable there. The message lists close matches.

With --json, ""edit"" holds {element, oldName, newName, changedElements}.",
        Options = new CliOption[]
        {
            new("element", "element", "Screen or entity that has the category. Required."),
            new("category", "category", "State category to rename. Required."),
            new("to", "new name", "The new name. Required."),
        },
        Examples = new[]
        {
            ("rename-state-category MyGame --element Entities\\Enemy --category Health --to Condition", "Rename a category and everything typed as it."),
        },
        IsEdit = true,
        ValidateArguments = ValidateArguments,
        Run = RunAsync,
    };

    /// <summary>What --json reports under <c>edit</c>.</summary>
    public sealed record RenamedStateCategory(string Element, string OldName, string NewName, IReadOnlyList<string> ChangedElements);

    static string? ValidateArguments(CliContext context)
    {
        foreach (var required in new[] { "element", "category", "to" })
        {
            if (!context.Has(required))
            {
                return $"rename-state-category needs --{required}.";
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

        var oldName = category.Name;
        var newName = context.Get("to")!;
        if (newName == oldName)
        {
            context.Fail($"{oldName} already has that name.");
            return;
        }
        if (!Builder.Get<NameVerifier>().IsStateCategoryNameValid(newName, out var whyInvalid))
        {
            context.Fail($"Cannot rename {oldName} to {newName}: {whyInvalid}");
            return;
        }
        // A category of the same name in the element, a base or a derived element generates code that does not compile.
        var related = new[] { element }
            .Concat(ObjectFinder.Self.GetAllBaseElementsRecursively(element))
            .Concat(ObjectFinder.Self.GetAllElementsThatInheritFrom(element))
            .ToList();
        var clash = related.FirstOrDefault(item => item.StateCategoryList.Any(other => other != category &&
            string.Equals(other.Name, newName, StringComparison.OrdinalIgnoreCase)));
        if (clash != null)
        {
            context.Fail($"Cannot rename {oldName} to {newName}: " +
                (clash == element ? $"{element.Name} already has a category with that name." : $"{clash.Name} already has a category with that name."));
            return;
        }
        var newProperty = "Current" + newName + "State";
        var variableOwner = related.FirstOrDefault(item => item.CustomVariables.Any(variable => variable.Name == newProperty));
        if (variableOwner != null)
        {
            context.Fail($"Cannot rename {oldName} to {newName}: {variableOwner.Name} already has a variable {newProperty}.");
            return;
        }

        var before = CliStates.SerializeElements();
        StateSaveCategoryRenameLogic.Rename(GlueState.Self.CurrentGlueProject, element, category, newName);
        // What the tab does after the rename.
        GlueCommands.Self.RefreshCommands.RefreshUi(category);
        GlueCommands.Self.GenerateCodeCommands.GenerateAllCode();
        await TaskManager.Self.WaitForAllTasksFinished();

        var result = new RenamedStateCategory(element.Name, oldName, newName, CliStates.ChangedElements(before));
        var lines = new List<string> { $"Renamed state category {oldName} in {element.Name} to {newName}." };
        lines.AddRange(CliStates.ChangedElementsLine(result.ChangedElements));
        context.Report(result, string.Join(Environment.NewLine, lines));
    }
}
