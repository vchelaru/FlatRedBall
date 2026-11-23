namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    /// <summary>
    /// Gum project queries, which the Gum Plugin owns. The plugin assigns its implementation to
    /// <see cref="ExportedImplementations.GlueCommands.GumCommands"/> at startup, so the property is null when
    /// the plugin is not loaded.
    /// </summary>
    public interface IGumCommands
    {
        /// <summary>Whether the loaded Glue project has a Gum project.</summary>
        bool HasGum();
    }
}
