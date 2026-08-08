using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.SaveClasses;

namespace GlueUnitTests.TestSupport;

/// <summary>
/// A standalone <see cref="ITreeNode"/> wrapping one <see cref="ReferencedFileSave"/> tag, with no real
/// tree behind it - what <see cref="FakeFindManager"/> hands back from <c>TreeNodeByTag</c> so that
/// <c>GlueState.CurrentReferencedFileSave</c>'s setter (which resolves a tag to a tree node before taking
/// effect - see <c>GlueState.TakeSnapshot</c>) works in tests exactly like it does in Glue.exe, without a
/// test hand-rolling its own <c>ITreeNode</c>. See <see cref="FakeFindManager"/>'s doc comment for why this
/// is scoped to just this one tag type rather than every tag <c>GlueState</c> can resolve through <c>Find</c>.
/// </summary>
internal sealed class SyntheticTreeNode : ITreeNode
{
    public SyntheticTreeNode(ReferencedFileSave tag) => Tag = tag;

    public object Tag { get; set; }
    public ITreeNode Parent => null;
    public string Text { get; set; } = "";
    public IEnumerable<ITreeNode> Children => Enumerable.Empty<ITreeNode>();
    public TreeNodeType TreeNodeType => TreeNodeType.ReferencedFileSaveNode;
    public void Remove(ITreeNode child) { }
    public void Add(ITreeNode child) { }
    public ITreeNode FindByName(string name) => null;
    public void RemoveGlobalContentTreeNodesIfDoesntExist(ITreeNode treeNode) { }
    public ITreeNode FindByTagRecursive(object tag) => Equals(tag, Tag) ? this : null;
    public void SortByTextConsideringDirectories() { }
}
