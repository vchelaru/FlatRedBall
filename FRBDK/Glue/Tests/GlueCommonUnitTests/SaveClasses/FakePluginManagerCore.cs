using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.SaveClasses;

namespace GlueCommonUnitTests.SaveClasses;

/// <summary>
/// Hand-rolled test double for <see cref="IPluginManagerCore"/> - the real implementation
/// (<c>PluginManager</c>) lives in Glue.csproj and isn't reachable from this net8.0 test project.
/// Records every CustomVariable it was asked to assign a displayer for.
/// </summary>
public class FakePluginManagerCore : IPluginManagerCore
{
    public List<CustomVariable> DisplayerRequests { get; } = new();

    public void TryAssignPreferredDisplayerFromName(CustomVariable customVariable) =>
        DisplayerRequests.Add(customVariable);
}
