using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.SaveClasses;

namespace GlueUnitTests.TestSupport;

/// <summary>
/// Test-only <see cref="IFindManager"/>. <c>GlueState.Self.Find</c> is only ever set by
/// <c>MainTreeViewPlugin.StartUp</c> (which also constructs a WPF <c>MainTreeViewControl</c>) - never run
/// in a plain xunit host - so it's null by default there, and any production code path that resolves a
/// tree node from a model object (e.g. <c>GlueState.CurrentNamedObjectSave</c>'s setter calling
/// <c>Find.TreeNodeByTag</c>) NREs. Wired in by <see cref="GlueTestBootstrap"/>.
///
/// All lookups return null/empty/false: tests here don't assert on tree-node identity, they just need the
/// production selection-plumbing that reads through <c>GlueState.Self.Find</c> to not crash. A test that
/// needs real tree-node resolution should build a real <c>FindManager</c> instead of relying on this fake.
/// </summary>
internal class FakeFindManager : IFindManager
{
    public ITreeNode NamedObjectTreeNode(NamedObjectSave namedObjectSave) => null!;
    public ITreeNode TreeNodeByTag(object tag) => null!;
    public ITreeNode GlobalContentTreeNode => null!;
    public string GlobalContentFilesPath => "";
    public bool IfReferencedFileSaveIsReferenced(ReferencedFileSave referencedFileSave) => false;
}
