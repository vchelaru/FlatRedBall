using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;

namespace GlueCli;

/// <summary>
/// Lookups and change tracking shared by the state commands (add-state-category, add-state, set-state-variable,
/// rename-state, delete-state): find a category or state by name with close matches, and list the elements an
/// edit changed.
/// </summary>
public static class CliStates
{
    /// <summary>The category <paramref name="name"/> in <paramref name="element"/> itself, or null after failing the run.</summary>
    public static StateSaveCategory? FindCategory(CliContext context, GlueElement element, string name)
    {
        var categories = element.StateCategoryList;
        var category = categories.FirstOrDefault(item => item.Name == name) ??
            categories.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
        if (category != null)
        {
            return category;
        }
        var inBase = ObjectFinder.Self.GetAllBaseElementsRecursively(element)
            .FirstOrDefault(baseElement => baseElement.StateCategoryList.Any(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase)));
        context.Fail(inBase != null
            ? $"The category '{name}' belongs to {inBase.Name}, the base of {element.Name}; pass --element {inBase.Name}."
            : $"{element.Name} has no state category '{name}'." +
              (categories.Count == 0 ? " It has no state categories." : CliLookup.DescribeCloseMatches(name, categories.Select(item => item.Name))));
        return null;
    }

    /// <summary>The state <paramref name="name"/> in <paramref name="category"/>, or null after failing the run.</summary>
    public static StateSave? FindState(CliContext context, GlueElement element, StateSaveCategory category, string name)
    {
        var state = category.States.FirstOrDefault(item => item.Name == name) ??
            category.States.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
        if (state == null)
        {
            context.Fail($"{category.Name} in {element.Name} has no state '{name}'." +
                (category.States.Count == 0 ? " It has no states." : CliLookup.DescribeCloseMatches(name, category.States.Select(item => item.Name))));
        }
        return state;
    }

    /// <summary>Every screen and entity serialized, to compare against after the edit.</summary>
    public static Dictionary<GlueElement, string> SerializeElements()
    {
        var project = GlueState.Self.CurrentGlueProject;
        return project.Screens.Cast<GlueElement>().Concat(project.Entities)
            .ToDictionary(element => element, element => Newtonsoft.Json.JsonConvert.SerializeObject(element));
    }

    /// <summary>The names of the elements that differ from <paramref name="before"/>, sorted.</summary>
    public static List<string> ChangedElements(Dictionary<GlueElement, string> before) =>
        SerializeElements()
            .Where(pair => !before.TryGetValue(pair.Key, out var json) || json != pair.Value)
            .Select(pair => pair.Key.Name)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>"  changed elements: A, B" for the text output, or nothing.</summary>
    public static IEnumerable<string> ChangedElementsLine(IReadOnlyList<string> changedElements) =>
        changedElements.Count == 0 ? Enumerable.Empty<string>() : new[] { "  changed elements: " + string.Join(", ", changedElements) };

    /// <summary>A comma-separated option value as names, without blanks.</summary>
    public static List<string> SplitList(string? value) =>
        (value ?? "").Split(',').Select(item => item.Trim()).Where(item => item.Length > 0).ToList();
}
