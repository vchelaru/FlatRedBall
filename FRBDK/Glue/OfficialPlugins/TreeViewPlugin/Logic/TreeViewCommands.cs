using System;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;

namespace OfficialPlugins.TreeViewPlugin.Logic;

/// <summary>The Tree View plugin's <see cref="ITreeViewCommands"/>. Bookmarks live in its view, so the plugin passes
/// in the view's method.</summary>
internal class TreeViewCommands : ITreeViewCommands
{
    readonly Action<ITreeNode> addBookmark;

    public TreeViewCommands(Action<ITreeNode> addBookmark) => this.addBookmark = addBookmark;

    public void AddBookmark(ITreeNode treeNode) => addBookmark(treeNode);
}
