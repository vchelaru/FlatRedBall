using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Services;

namespace GlueCli;

/// <summary>
/// What add-entity and add-screen share: the --name and --folder arguments, base element lookup, and finding
/// what the add put into screens.
/// </summary>
static class ElementNaming
{
    /// <summary>The --name checks that need no project: present, and with no folder in it.</summary>
    public static string? ValidateNameArgument(CliContext context, string commandName)
    {
        if (!context.Has("name"))
        {
            return $"{commandName} needs --name.";
        }
        if (context.Get("name")!.IndexOfAny(new[] { '\\', '/' }) != -1)
        {
            return "--name is the name without folders; pass the folder with --folder.";
        }
        return null;
    }

    /// <summary>
    /// The view model Directory for --folder: <c>Entities\Enemies\</c> for <c>Enemies</c> or
    /// <c>Entities/Enemies</c>, <c>Entities\</c> without --folder. Null after failing the run on an invalid
    /// folder name.
    /// </summary>
    public static string? ResolveDirectory(CliContext context, string root)
    {
        var rootPrefix = root + "\\";
        if (!context.Has("folder"))
        {
            return rootPrefix;
        }

        var folder = context.Get("folder")!.Replace('/', '\\').Trim('\\');
        if (folder.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            folder = folder.Substring(rootPrefix.Length);
        }
        else if (string.Equals(folder, root, StringComparison.OrdinalIgnoreCase))
        {
            folder = "";
        }

        var nameVerifier = Builder.Get<NameVerifier>();
        foreach (var segment in folder.Split('\\', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!nameVerifier.IsDirectoryNameValid(segment, out var why))
            {
                context.Fail($"'{segment}' is not a valid folder name: {why}");
                return null;
            }
        }
        return folder.Length == 0 ? rootPrefix : rootPrefix + folder + "\\";
    }

    /// <summary>
    /// The element of <paramref name="candidates"/> that <paramref name="name"/> refers to, by full name (either
    /// slash) or by name without folders when that is unambiguous.
    /// </summary>
    public static T? FindElement<T>(string name, IEnumerable<T> candidates, string kind, out string? error) where T : GlueElement
    {
        error = null;
        var all = candidates.ToList();
        var normalized = name.Replace('/', '\\').Trim('\\');

        var exact = all.FirstOrDefault(element => string.Equals(element.Name, normalized, StringComparison.OrdinalIgnoreCase));
        if (exact != null)
        {
            return exact;
        }
        var byShortName = all.Where(element =>
            string.Equals(CliLookup.StripFolders(element.Name), normalized, StringComparison.OrdinalIgnoreCase)).ToList();
        if (byShortName.Count == 1)
        {
            return byShortName[0];
        }
        error = byShortName.Count > 1
            ? $"'{name}' matches more than one {kind}; pass the full name: " + string.Join(", ", byShortName.Select(element => element.Name))
            : $"No {kind} named '{name}'." + CliLookup.DescribeCloseMatches(name, all.Select(element => element.Name));
        return null;
    }

    /// <summary>Every screen's object names, to tell afterwards what an add put into screens.</summary>
    public static Dictionary<ScreenSave, HashSet<string>> SnapshotScreenObjects() =>
        GlueState.Self.CurrentGlueProject.Screens.ToDictionary(screen => screen,
            screen => screen.AllNamedObjects.Select(nos => nos.InstanceName).ToHashSet());

    /// <summary><c>Screens\GameScreen.EnemyList</c> for each object in an existing screen not in <paramref name="before"/>.</summary>
    public static List<string> NewScreenObjects(Dictionary<ScreenSave, HashSet<string>> before) =>
        before.SelectMany(pair => pair.Key.AllNamedObjects
                .Where(nos => !pair.Value.Contains(nos.InstanceName))
                .Select(nos => $"{pair.Key.Name}.{nos.InstanceName}"))
            .ToList();
}
