using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;

namespace GlueCli;

/// <summary>
/// Name lookups every command needs, with the "did you mean" messages agents rely on to correct a call.
/// </summary>
public static class CliLookup
{
    /// <summary>
    /// The screen or entity <paramref name="name"/> refers to: its full name (<c>Screens\GameScreen</c>,
    /// either slash) or, when that is unambiguous, its name without folders (<c>GameScreen</c>).
    /// </summary>
    public static GlueElement? FindElement(string name, out string? error)
    {
        error = null;
        var project = GlueState.Self.CurrentGlueProject;
        var all = project.Screens.Cast<GlueElement>().Concat(project.Entities).ToList();
        var normalized = name.Replace('/', '\\').Trim('\\');

        var exact = all.FirstOrDefault(element => string.Equals(element.Name, normalized, StringComparison.OrdinalIgnoreCase));
        if (exact != null)
        {
            return exact;
        }

        var byShortName = all
            .Where(element => string.Equals(StripFolders(element.Name), normalized, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (byShortName.Count == 1)
        {
            return byShortName[0];
        }
        if (byShortName.Count > 1)
        {
            error = $"'{name}' matches more than one element; pass the full name: " +
                string.Join(", ", byShortName.Select(element => element.Name));
            return null;
        }

        error = $"No screen or entity named '{name}'." + DescribeCloseMatches(name, all.Select(element => element.Name));
        return null;
    }

    /// <summary><c>Entities\Enemies\Slime</c> becomes <c>Slime</c>.</summary>
    public static string StripFolders(string name)
    {
        var index = name.LastIndexOfAny(new[] { '\\', '/' });
        return index == -1 ? name : name.Substring(index + 1);
    }

    /// <summary>
    /// " Did you mean: A, B?" listing the candidates close to <paramref name="input"/>, or " Available: ..."
    /// when none is close and the list is short, so an error message always gives the caller something to
    /// try. Empty when there is nothing useful to list.
    /// </summary>
    public static string DescribeCloseMatches(string input, IEnumerable<string> candidates)
    {
        var distinct = candidates.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var close = CloseMatches(input, distinct);
        if (close.Count > 0)
        {
            return " Did you mean: " + string.Join(", ", close) + "?";
        }
        if (distinct.Count is > 0 and <= 15)
        {
            return " Available: " + string.Join(", ", distinct.OrderBy(item => item, StringComparer.OrdinalIgnoreCase)) + ".";
        }
        return "";
    }

    /// <summary>
    /// Candidates whose name without folders contains <paramref name="input"/>, or is within a few typos of
    /// it, closest first. At most 8.
    /// </summary>
    public static List<string> CloseMatches(string input, IEnumerable<string> candidates)
    {
        var needle = StripFolders(input.Replace('/', '\\')).ToLowerInvariant();
        if (needle.Length == 0)
        {
            return new List<string>();
        }
        var allowedDistance = Math.Max(1, needle.Length / 3);

        return candidates
            .Select(candidate =>
            {
                var shortName = StripFolders(candidate).ToLowerInvariant();
                var distance = shortName.Contains(needle) ? 0 : EditDistance(needle, shortName);
                return (candidate, distance);
            })
            .Where(item => item.distance <= allowedDistance)
            .OrderBy(item => item.distance)
            .ThenBy(item => item.candidate.Length)
            .Select(item => item.candidate)
            .Take(8)
            .ToList();
    }

    static int EditDistance(string a, string b)
    {
        var previous = Enumerable.Range(0, b.Length + 1).ToArray();
        for (var i = 1; i <= a.Length; i++)
        {
            var current = new int[b.Length + 1];
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
            }
            previous = current;
        }
        return previous[b.Length];
    }
}
