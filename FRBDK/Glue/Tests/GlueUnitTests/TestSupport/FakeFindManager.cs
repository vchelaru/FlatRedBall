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
/// <c>TreeNodeByTag</c>/<c>NamedObjectTreeNode</c> hand back a <see cref="SyntheticTreeNode"/> wrapping
/// the given tag, so a test can set <c>GlueState.Self.CurrentReferencedFileSave</c>/<c>CurrentElement</c>/
/// <c>CurrentNamedObjectSave</c>/etc. directly - the same as production, no hand-rolled <c>ITreeNode</c>
/// needed per test (see issue #2016's fix and REFACTORING.md for why this mattered: the old always-null
/// behavior meant those setters silently discarded whatever was assigned to them). There is still no real
/// *tree* behind this - a test needing parent/child/sibling relationships or <c>FindByName</c> should build
/// a real <c>FindManager</c> instead of relying on this fake.
///
/// <see cref="GlobalContentTreeNode"/> and <see cref="IfReferencedFileSaveIsReferenced"/> are unrelated to
/// tag resolution and still return empty/false.
/// </summary>
internal class FakeFindManager : IFindManager
{
    public ITreeNode NamedObjectTreeNode(NamedObjectSave namedObjectSave) => TreeNodeByTag(namedObjectSave);
    public ITreeNode TreeNodeByTag(object tag) => tag == null ? null! : new SyntheticTreeNode(tag);
    public ITreeNode GlobalContentTreeNode => null!;
    public string GlobalContentFilesPath => "";
    public bool IfReferencedFileSaveIsReferenced(ReferencedFileSave referencedFileSave) => false;
}
