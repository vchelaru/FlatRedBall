using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;

namespace GlueCli;

/// <summary>
/// Which screens and entities an edit changed, for a command's report: take a <see cref="Snapshot"/> before the
/// edit and pass it to <see cref="ChangedElements"/> after.
/// </summary>
public static class CliChanges
{
    public static Dictionary<GlueElement, string> Snapshot()
    {
        var project = GlueState.Self.CurrentGlueProject;
        return project.Screens.Cast<GlueElement>().Concat(project.Entities)
            .ToDictionary(element => element, element => Newtonsoft.Json.JsonConvert.SerializeObject(element));
    }

    /// <summary>The names of the elements whose JSON differs from <paramref name="before"/>, or that are new.</summary>
    public static List<string> ChangedElements(Dictionary<GlueElement, string> before) =>
        Snapshot()
            .Where(pair => !before.TryGetValue(pair.Key, out var json) || json != pair.Value)
            .Select(pair => pair.Key.Name)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
