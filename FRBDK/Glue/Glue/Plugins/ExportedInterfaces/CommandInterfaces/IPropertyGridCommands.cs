namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    /// <summary>
    /// The Main Property Grid Plugin's Variables grid. The plugin assigns its implementation to
    /// <see cref="ExportedImplementations.GlueCommands.PropertyGridCommands"/> at startup.
    /// </summary>
    public interface IPropertyGridCommands
    {
        /// <summary>Refills the Variables grid from the current selection. Touches UI.</summary>
        void RefreshVariables();
    }
}
