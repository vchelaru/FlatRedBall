using FlatRedBall.Glue.Headless;

namespace GlueCli;

/// <summary>
/// Brings Glue up, once per process, ready for <see cref="GlueCliRunner"/> to load a project.
/// </summary>
public interface IGlueCliHost
{
    /// <param name="gameProjectDirectory">The folder of the .csproj about to be loaded.</param>
    /// <param name="reportMessage">Receives messages production code would have shown in a dialog while
    /// starting up.</param>
    /// <returns>One line per plugin that failed to start. Any line makes the run fail.</returns>
    IReadOnlyList<string> Prepare(string gameProjectDirectory, Action<string> reportMessage);
}

/// <summary>
/// The real host: Glue's startup with plugins loaded from the Plugins folder next to the exe, as Glue.exe
/// loads them. See <see cref="GlueHeadlessHost.EnsureProjectLoadReadyWithInstalledPlugins"/>.
/// </summary>
internal sealed class InstalledPluginsHost : IGlueCliHost
{
    public IReadOnlyList<string> Prepare(string gameProjectDirectory, Action<string> reportMessage)
    {
        GlueHeadlessHost.EnsureInitialized();
        return GlueHeadlessHost.EnsureProjectLoadReadyWithInstalledPlugins(reportMessage, gameProjectDirectory);
    }
}
