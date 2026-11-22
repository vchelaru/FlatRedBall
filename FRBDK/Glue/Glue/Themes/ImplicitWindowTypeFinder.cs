using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FlatRedBall.Glue.Themes;

/// <summary>
/// Finds the concrete WPF Window subclasses in an assembly and everything it references, so
/// MainGlueWindow can give each one an implicit style. Called once for Glue and once per plugin
/// assembly at startup, so it remembers what it has scanned and only reads the types of assemblies
/// that can contain a Window: an assembly can only derive from Window if it references
/// PresentationFramework, directly or through the assembly that holds its base class.
/// </summary>
public class ImplicitWindowTypeFinder
{
    readonly Assembly windowAssembly;
    readonly Type windowType;

    readonly HashSet<string> visitedAssemblies = new();
    readonly Dictionary<string, List<string>> referencesByAssembly = new();
    readonly HashSet<string> scannedAssemblies = new();

    public ImplicitWindowTypeFinder() : this(typeof(System.Windows.Window)) { }

    public ImplicitWindowTypeFinder(Type windowType)
    {
        this.windowType = windowType;
        windowAssembly = windowType.Assembly;
    }

    /// <summary>
    /// Names of the assemblies whose types have been read so far.
    /// </summary>
    public IReadOnlyCollection<string> ScannedAssemblies => scannedAssemblies;

    /// <summary>
    /// Returns the concrete Window subclasses in <paramref name="root"/> and its referenced assemblies
    /// that no earlier call returned.
    /// </summary>
    public List<Type> FindNewWindowTypes(Assembly root)
    {
        var found = new List<Type>();
        var newlyVisited = VisitNewAssemblies(root);
        var canContainWindow = GetAssembliesThatCanContainWindow();

        foreach (var assembly in newlyVisited)
        {
            if (!canContainWindow.Contains(assembly.FullName))
            {
                continue;
            }
            scannedAssemblies.Add(assembly.FullName);
            found.AddRange(assembly.GetTypes().Where(t => t.IsSubclassOf(windowType) && !t.IsAbstract));
        }
        return found;
    }

    // Breadth-first walk of the reference graph. An assembly visited by an earlier call had its whole
    // reference closure visited then too, so the walk stops there.
    List<Assembly> VisitNewAssemblies(Assembly root)
    {
        var result = new List<Assembly>();
        var queue = new Queue<Assembly>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visitedAssemblies.Add(current.FullName)) continue;

            result.Add(current);
            var references = new List<string>();
            referencesByAssembly[current.FullName] = references;

            foreach (var referencedName in current.GetReferencedAssemblies())
            {
                try
                {
                    var referenced = Assembly.Load(referencedName);
                    references.Add(referenced.FullName);
                    if (!visitedAssemblies.Contains(referenced.FullName))
                    {
                        queue.Enqueue(referenced);
                    }
                }
                catch (Exception e)
                {
                    _ = e;
                }
            }
        }
        return result;
    }

    // The assemblies that reach PresentationFramework through the references seen so far, found by
    // walking the reference edges backwards from it. Assembly graphs can have cycles, which this handles.
    HashSet<string> GetAssembliesThatCanContainWindow()
    {
        var referencedBy = new Dictionary<string, List<string>>();
        foreach (var (assembly, references) in referencesByAssembly)
        {
            foreach (var reference in references)
            {
                if (!referencedBy.TryGetValue(reference, out var list))
                {
                    referencedBy[reference] = list = new List<string>();
                }
                list.Add(assembly);
            }
        }

        var result = new HashSet<string> { windowAssembly.FullName };
        var queue = new Queue<string>(result);
        while (queue.Count > 0)
        {
            if (referencedBy.TryGetValue(queue.Dequeue(), out var referrers))
            {
                foreach (var referrer in referrers)
                {
                    if (result.Add(referrer)) queue.Enqueue(referrer);
                }
            }
        }
        return result;
    }
}
