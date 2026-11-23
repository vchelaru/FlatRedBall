using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.SaveClasses;

namespace GlueUnitTests.Cli;

/// <summary>
/// Stands in for the tree view's node lookup in GlueCli tests: selecting a screen or entity gets an element
/// node, as in the exe, so plugins that act on the selected node (the property grid) react the same way.
/// Everything else falls through to the finder that was there before.
/// </summary>
sealed class ElementTreeNodeFinder : IFindManager
{
    readonly IFindManager? _inner;

    public ElementTreeNodeFinder(IFindManager? inner) => _inner = inner;

    public ITreeNode? TreeNodeByTag(object tag) => tag switch
    {
        ScreenSave => new ElementNode(tag, TreeNodeType.ScreenNode),
        EntitySave => new ElementNode(tag, TreeNodeType.EntityNode),
        _ => _inner?.TreeNodeByTag(tag),
    };

    public ITreeNode? NamedObjectTreeNode(NamedObjectSave namedObjectSave) => _inner?.NamedObjectTreeNode(namedObjectSave);
    public ITreeNode? GlobalContentTreeNode => _inner?.GlobalContentTreeNode;
    public string? GlobalContentFilesPath => _inner?.GlobalContentFilesPath;
    public bool IfReferencedFileSaveIsReferenced(ReferencedFileSave referencedFileSave) =>
        _inner?.IfReferencedFileSaveIsReferenced(referencedFileSave) ?? false;

    sealed class ElementNode : ITreeNode
    {
        public ElementNode(object tag, TreeNodeType type)
        {
            Tag = tag;
            TreeNodeType = type;
        }

        public TreeNodeType TreeNodeType { get; }
        public object Tag { get; set; }
        public ITreeNode? Parent => null;
        public string Text { get; set; } = "";
        public IEnumerable<ITreeNode> Children => Enumerable.Empty<ITreeNode>();
        public void Remove(ITreeNode child) { }
        public void Add(ITreeNode child) { }
        public ITreeNode? FindByName(string name) => null;
        public void RemoveGlobalContentTreeNodesIfDoesntExist(ITreeNode treeNode) { }
        public ITreeNode? FindByTagRecursive(object tag) => null;
        public void SortByTextConsideringDirectories() { }
    }
}
