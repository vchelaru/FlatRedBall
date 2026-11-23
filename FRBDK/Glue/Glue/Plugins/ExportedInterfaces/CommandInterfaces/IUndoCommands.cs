namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    /// <summary>
    /// The Undo plugin. The plugin assigns its implementation to
    /// <see cref="ExportedImplementations.GlueCommands.UndoCommands"/> at startup.
    /// </summary>
    public interface IUndoCommands
    {
        /// <summary>Undoes the most recent recorded change. Touches UI.</summary>
        void Undo();
    }
}
