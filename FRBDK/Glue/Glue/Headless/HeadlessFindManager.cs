using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System.Linq;

namespace FlatRedBall.Glue.Headless;

/// <summary>
/// <see cref="IFindManager"/> for a headless host. <c>GlueState.Self.Find</c> is only ever set by
/// <c>MainTreeViewPlugin.StartUp</c> (which also constructs a WPF <c>MainTreeViewControl</c>) - never run
/// headless - so it's null by default there. Wired in by <see cref="GlueHeadlessHost"/>.
///
/// There is no tree, so nothing resolves to a tree node. That is fine for selection: <c>GlueState</c>'s
/// Current* setters select by model object and only consult <c>Find</c> so a tree view can follow along
/// (GitHub issue #2268), so <c>GlueState.Self.CurrentNamedObjectSave = nos</c> works here the same as in
/// Glue.exe, and plugins' <c>ReactToItemsSelected</c> handlers really run. Code that needs actual tree
/// relationships needs a real <c>FindManager</c>.
/// </summary>
internal class HeadlessFindManager : IFindManager
{
    public ITreeNode NamedObjectTreeNode(NamedObjectSave namedObjectSave) => null!;
    public ITreeNode TreeNodeByTag(object tag) => null!;
    public ITreeNode GlobalContentTreeNode => null!;
    public string GlobalContentFilesPath => "";
    /// <summary>
    /// The same answer the real <c>FindManager</c> gives. It reads no tree state at all despite living in
    /// the tree view plugin, so there is nothing to stand in for: a hard-coded false just meant every
    /// removal-of-a-file path early-returned headless, which reads as "the removal ran and did nothing".
    /// </summary>
    public bool IfReferencedFileSaveIsReferenced(ReferencedFileSave referencedFileSave)
    {
        var container = referencedFileSave?.GetContainer();

        return container != null
            ? container.GetAllReferencedFileSavesRecursively().Contains(referencedFileSave!)
            : GlueState.Self.CurrentGlueProject?.GlobalFiles.Contains(referencedFileSave!) == true;
    }
}
