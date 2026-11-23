namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    /// <summary>
    /// The Git plugin. The plugin assigns its implementation to
    /// <see cref="ExportedImplementations.GlueCommands.GitCommands"/> at startup.
    /// </summary>
    public interface IGitCommands
    {
        /// <summary>Creates or updates the .gitignore next to the .sln. Does nothing when no project is loaded.</summary>
        void AddGitIgnore();
    }
}
