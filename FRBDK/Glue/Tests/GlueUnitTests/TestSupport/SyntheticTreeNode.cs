using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.SaveClasses;

namespace GlueUnitTests.TestSupport;

/// <summary>
/// A standalone <see cref="ITreeNode"/> wrapping one tag, with no real tree behind it - what
/// <see cref="FakeFindManager"/> hands back from <c>TreeNodeByTag</c> so that
/// <c>GlueState.CurrentReferencedFileSave</c>/<c>CurrentElement</c>/<c>CurrentNamedObjectSave</c>/etc.
/// setters (which all resolve a tag to a tree node before taking effect - see <c>GlueState.TakeSnapshot</c>)
/// work in tests exactly like they do in Glue.exe, without a test hand-rolling its own <c>ITreeNode</c>.
/// <see cref="TreeNodeType"/> is inferred from the tag's runtime type because some of those setters key off
/// it directly (<c>IsStateNode</c>/<c>IsStateCategoryNode</c>/<c>IsCustomVariable</c>) rather than testing
/// <c>Tag</c>'s type.
/// </summary>
internal sealed class SyntheticTreeNode : ITreeNode
{
    public SyntheticTreeNode(object tag)
    {
        Tag = tag;
        TreeNodeType = tag switch
        {
            ReferencedFileSave => TreeNodeType.ReferencedFileSaveNode,
            EntitySave => TreeNodeType.EntityNode,
            ScreenSave => TreeNodeType.ScreenNode,
            NamedObjectSave => TreeNodeType.NamedObjectSaveNode,
            StateSave => TreeNodeType.StateNode,
            StateSaveCategory => TreeNodeType.StateCategoryNode,
            CustomVariable => TreeNodeType.CustomVariableNode,
            EventResponseSave => TreeNodeType.EventNode,
            _ => TreeNodeType.Other,
        };
    }

    public object Tag { get; set; }
    public ITreeNode Parent => null;
    public string Text { get; set; } = "";
    public IEnumerable<ITreeNode> Children => Enumerable.Empty<ITreeNode>();
    public TreeNodeType TreeNodeType { get; }
    public void Remove(ITreeNode child) { }
    public void Add(ITreeNode child) { }
    public ITreeNode FindByName(string name) => null;
    public void RemoveGlobalContentTreeNodesIfDoesntExist(ITreeNode treeNode) { }
    public ITreeNode FindByTagRecursive(object tag) => Equals(tag, Tag) ? this : null;
    public void SortByTextConsideringDirectories() { }
}
