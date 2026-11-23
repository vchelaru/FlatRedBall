using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using OfficialPlugins.UndoPlugin.NewFolder;

namespace OfficialPlugins.UndoPlugin
{
    /// <summary>The Undo plugin's <see cref="IUndoCommands"/>.</summary>
    public class UndoCommands : IUndoCommands
    {
        public void Undo() => UndoManager.HandleUndo();
    }
}
