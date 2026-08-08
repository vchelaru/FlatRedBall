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
/// <c>TreeNodeByTag</c> hands back a <see cref="SyntheticTreeNode"/> wrapping the tag - but only for a
/// <see cref="ReferencedFileSave"/> tag, so a test can set <c>GlueState.Self.CurrentReferencedFileSave</c>
/// directly, the same as production (see issue #2016's fix and REFACTORING.md for why this mattered: the
/// old always-null behavior meant that setter silently discarded whatever was assigned to it).
///
/// Deliberately NOT extended to every tag type <c>GlueState</c> resolves through <c>Find</c>
/// (<c>NamedObjectSave</c>, <c>EntitySave</c>, <c>StateSave</c>, ...): a first attempt that resolved any
/// tag broke two unrelated Wizard tests - giving <c>CurrentNamedObjectSave</c> a real node makes
/// <c>PluginManager.ReactToItemsSelected</c> actually drive <c>MainCollisionPlugin</c>'s real
/// selection-handling (previously dead code in a test host, since the always-null fake meant nothing was
/// ever really "selected"), which NREs in <c>CollidableNamedObjectController.RefreshViewModelTo</c> on a
/// pre-existing bug unrelated to this fake. So this stays scoped to the one tag type it was actually
/// needed for. A test that needs another tag type to resolve, or real tree relationships, should build a
/// real <c>FindManager</c> instead of extending this fake further without re-running the full suite.
/// </summary>
internal class FakeFindManager : IFindManager
{
    public ITreeNode NamedObjectTreeNode(NamedObjectSave namedObjectSave) => null!;
    public ITreeNode TreeNodeByTag(object tag) => tag is ReferencedFileSave rfs ? new SyntheticTreeNode(rfs) : null!;
    public ITreeNode GlobalContentTreeNode => null!;
    public string GlobalContentFilesPath => "";
    public bool IfReferencedFileSaveIsReferenced(ReferencedFileSave referencedFileSave) => false;
}
