using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FlatRedBall.Glue.Themes;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Common;

// MainGlueWindow gives every Window subclass an implicit style. The finder skips assemblies that cannot
// hold a Window and never rescans one, so these pin that it still finds exactly what a full scan finds.
public class ImplicitWindowTypeFinderTests
{
    static readonly Assembly GlueAssembly = typeof(Glue.MainGlueWindow).Assembly;
    static readonly Assembly OfficialPluginsAssembly = typeof(OfficialPlugins.TreeViewPlugin.MainTreeViewPlugin).Assembly;
    static readonly Assembly PluginBaseAssembly = typeof(FlatRedBall.Glue.Plugins.Interfaces.IPlugin).Assembly;

    [Fact]
    public void FindNewWindowTypes_ShouldMatchFullScan_AcrossSeveralRoots()
    {
        var finder = new ImplicitWindowTypeFinder();

        var found = new List<Type>();
        found.AddRange(finder.FindNewWindowTypes(GlueAssembly));
        found.AddRange(finder.FindNewWindowTypes(OfficialPluginsAssembly));

        var expected = FullScan(GlueAssembly).Union(FullScan(OfficialPluginsAssembly)).ToHashSet();

        found.Count.ShouldBe(found.Distinct().Count());
        found.ToHashSet().SetEquals(expected).ShouldBeTrue();
        found.ShouldContain(typeof(FlatRedBall.Glue.Themes.ThemeWindow));
    }

    [Fact]
    public void FindNewWindowTypes_ShouldReturnNothing_WhenRootsClosureWasAlreadySeen()
    {
        var finder = new ImplicitWindowTypeFinder();
        finder.FindNewWindowTypes(GlueAssembly).ShouldNotBeEmpty();

        finder.FindNewWindowTypes(PluginBaseAssembly).ShouldBeEmpty();
        finder.FindNewWindowTypes(GlueAssembly).ShouldBeEmpty();
    }

    [Fact]
    public void FindNewWindowTypes_ShouldNotReadTypes_OfAssembliesThatDoNotReachPresentationFramework()
    {
        var finder = new ImplicitWindowTypeFinder();
        finder.FindNewWindowTypes(GlueAssembly);

        finder.ScannedAssemblies.ShouldContain(GlueAssembly.FullName);
        finder.ScannedAssemblies.ShouldContain(typeof(System.Windows.Window).Assembly.FullName);
        finder.ScannedAssemblies.ShouldNotContain(typeof(object).Assembly.FullName);
        finder.ScannedAssemblies.ShouldNotContain(typeof(Microsoft.Xna.Framework.Vector2).Assembly.FullName);
    }

    // What MainGlueWindow did before the finder: read the types of every assembly in the closure.
    static HashSet<Type> FullScan(Assembly root)
    {
        var visited = new HashSet<string>();
        var queue = new Queue<Assembly>();
        var result = new HashSet<Type>();
        queue.Enqueue(root);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current.FullName)) continue;
            foreach (var type in current.GetTypes())
            {
                if (type.IsSubclassOf(typeof(System.Windows.Window)) && !type.IsAbstract) result.Add(type);
            }
            foreach (var name in current.GetReferencedAssemblies())
            {
                try { queue.Enqueue(Assembly.Load(name)); }
                catch (Exception) { }
            }
        }
        return result;
    }
}
