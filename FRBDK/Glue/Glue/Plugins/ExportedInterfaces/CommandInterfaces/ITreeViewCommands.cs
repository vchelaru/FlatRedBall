using FlatRedBall.Glue.FormHelpers;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    /// <summary>
    /// The Tree View plugin, which owns the Explorer tab. The plugin assigns its implementation to
    /// <see cref="ExportedImplementations.GlueCommands.TreeViewCommands"/> at startup.
    /// </summary>
    public interface ITreeViewCommands
    {
        /// <summary>Adds the node to the bookmark list and shows the list. Touches UI.</summary>
        void AddBookmark(ITreeNode treeNode);
    }
}
