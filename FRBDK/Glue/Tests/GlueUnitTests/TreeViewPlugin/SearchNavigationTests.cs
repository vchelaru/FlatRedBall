using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using Moq;
using OfficialPlugins.TreeViewPlugin.ViewModels;
using Shouldly;
using Xunit;

namespace GlueUnitTests.TreeViewPlugin;

/// <summary>
/// Tests for the pure tree-navigation methods in MainTreeViewViewModel.Search.cs.
/// These methods operate only on NodeViewModel tree structure — no static singletons required.
/// [WpfFact] is required because NodeViewModel uses WPF types (BitmapImage, Visibility, etc.)
/// in its static constructor and instance constructor.
/// </summary>
public class SearchNavigationTests
{
    readonly MainTreeViewViewModel _vm;

    public SearchNavigationTests()
    {
        _vm = new MainTreeViewViewModel(Mock.Of<IGlueState>(), Mock.Of<IGlueCommands>());
    }

    // Creates a directory node (no Tag) and attaches it to parent.
    // IsDirectoryNode() returns true for these because Parent is set and Tag is null.
    static NodeViewModel AddDir(NodeViewModel parent, string name)
    {
        var node = new NodeViewModel(TreeNodeType.GeneralDirectoryNode) { Text = name };
        parent.Add(node);
        return node;
    }

    // Creates a node with a tag and attaches it to parent.
    static NodeViewModel AddTagged(NodeViewModel parent, string name, object tag)
    {
        var node = new NodeViewModel(TreeNodeType.GeneralDirectoryNode) { Text = name, Tag = tag };
        parent.Add(node);
        return node;
    }

    #region TreeNodeByDirectory

    [WpfFact]
    public void TreeNodeByDirectory_EmptyPath_ReturnsNull()
    {
        _vm.TreeNodeByDirectory(string.Empty, _vm.EntityRootNode).ShouldBeNull();
    }

    [WpfFact]
    public void TreeNodeByDirectory_DirectChild_Found()
    {
        var enemies = AddDir(_vm.EntityRootNode, "Enemies");

        _vm.TreeNodeByDirectory("Enemies", _vm.EntityRootNode).ShouldBeSameAs(enemies);
    }

    [WpfFact]
    public void TreeNodeByDirectory_NestedPath_Found()
    {
        var animals = AddDir(_vm.EntityRootNode, "Animals");
        var dog = AddDir(animals, "Dog");

        _vm.TreeNodeByDirectory("Animals/Dog", _vm.EntityRootNode).ShouldBeSameAs(dog);
    }

    [WpfFact]
    public void TreeNodeByDirectory_MissingIntermediate_ReturnsNull()
    {
        // "Animals" does not exist, so "Animals/Dog" cannot be found
        _vm.TreeNodeByDirectory("Animals/Dog", _vm.EntityRootNode).ShouldBeNull();
    }

    [WpfFact]
    public void TreeNodeByDirectory_UnknownLeaf_ReturnsNull()
    {
        AddDir(_vm.EntityRootNode, "Enemies");

        _vm.TreeNodeByDirectory("NonExistent", _vm.EntityRootNode).ShouldBeNull();
    }

    [WpfFact]
    public void TreeNodeByDirectory_IsCaseInsensitive()
    {
        var enemies = AddDir(_vm.EntityRootNode, "Enemies");

        _vm.TreeNodeByDirectory("enemies", _vm.EntityRootNode).ShouldBeSameAs(enemies);
    }

    // "Enemies/" — trailing slash means indexOfSlash == Length-1, so the method
    // returns the node directly rather than recursing into an empty remainder.
    [WpfFact]
    public void TreeNodeByDirectory_TrailingSlash_FindsNode()
    {
        var enemies = AddDir(_vm.EntityRootNode, "Enemies");

        _vm.TreeNodeByDirectory("Enemies/", _vm.EntityRootNode).ShouldBeSameAs(enemies);
    }

    #endregion

    #region TreeNodeForDirectoryOrEntityNode / TreeNodeForDirectoryOrScreenNode

    [WpfFact]
    public void TreeNodeForDirectoryOrEntityNode_EmptyPath_ReturnsEntityRoot()
    {
        _vm.TreeNodeForDirectoryOrEntityNode(string.Empty, _vm.EntityRootNode)
            .ShouldBeSameAs(_vm.EntityRootNode);
    }

    [WpfFact]
    public void TreeNodeForDirectoryOrEntityNode_WithPath_FindsDirectory()
    {
        var folder = AddDir(_vm.EntityRootNode, "UI");

        _vm.TreeNodeForDirectoryOrEntityNode("UI", _vm.EntityRootNode)
            .ShouldBeSameAs(folder);
    }

    [WpfFact]
    public void TreeNodeForDirectoryOrScreenNode_EmptyPath_ReturnsScreenRoot()
    {
        _vm.TreeNodeForDirectoryOrScreenNode(string.Empty, _vm.ScreenRootNode)
            .ShouldBeSameAs(_vm.ScreenRootNode);
    }

    [WpfFact]
    public void TreeNodeForDirectoryOrScreenNode_WithPath_FindsDirectory()
    {
        var folder = AddDir(_vm.ScreenRootNode, "Menus");

        _vm.TreeNodeForDirectoryOrScreenNode("Menus", _vm.ScreenRootNode)
            .ShouldBeSameAs(folder);
    }

    #endregion

    #region GetTreeNodeByQualifiedPath

    [WpfFact]
    public void GetTreeNodeByQualifiedPath_EntityRootOnly_ReturnsEntityRoot()
    {
        _vm.GetTreeNodeByQualifiedPath("Entities").ShouldBeSameAs(_vm.EntityRootNode);
    }

    [WpfFact]
    public void GetTreeNodeByQualifiedPath_ScreenRootOnly_ReturnsScreenRoot()
    {
        _vm.GetTreeNodeByQualifiedPath("Screens").ShouldBeSameAs(_vm.ScreenRootNode);
    }

    [WpfFact]
    public void GetTreeNodeByQualifiedPath_GlobalContentRootOnly_ReturnsGlobalContentRoot()
    {
        _vm.GetTreeNodeByQualifiedPath("Global Content Files")
            .ShouldBeSameAs(_vm.GlobalContentRootNode);
    }

    [WpfFact]
    public void GetTreeNodeByQualifiedPath_EntityChild_Found()
    {
        var player = AddTagged(_vm.EntityRootNode, "Player", new object());

        _vm.GetTreeNodeByQualifiedPath("Entities/Player").ShouldBeSameAs(player);
    }

    [WpfFact]
    public void GetTreeNodeByQualifiedPath_ScreenChild_Found()
    {
        var gameScreen = AddTagged(_vm.ScreenRootNode, "GameScreen", new object());

        _vm.GetTreeNodeByQualifiedPath("Screens/GameScreen").ShouldBeSameAs(gameScreen);
    }

    [WpfFact]
    public void GetTreeNodeByQualifiedPath_GlobalContentChild_Found()
    {
        var soundBank = AddTagged(_vm.GlobalContentRootNode, "SoundBank", new object());

        _vm.GetTreeNodeByQualifiedPath("Global Content Files/SoundBank").ShouldBeSameAs(soundBank);
    }

    [WpfFact]
    public void GetTreeNodeByQualifiedPath_DeepNestedChild_Found()
    {
        var folder = AddDir(_vm.EntityRootNode, "Enemies");
        var grunt = new NodeViewModel(TreeNodeType.GeneralDirectoryNode) { Text = "Grunt" };
        folder.Add(grunt);

        _vm.GetTreeNodeByQualifiedPath("Entities/Enemies/Grunt").ShouldBeSameAs(grunt);
    }

    [WpfFact]
    public void GetTreeNodeByQualifiedPath_NotFound_ReturnsNull()
    {
        _vm.GetTreeNodeByQualifiedPath("Entities/NonExistent").ShouldBeNull();
    }

    [WpfFact]
    public void GetTreeNodeByQualifiedPath_WrongRootName_Throws()
    {
        Should.Throw<InvalidOperationException>(
            () => _vm.GetTreeNodeByQualifiedPath("Unknown/Something"));
    }

    #endregion

    #region GetTreeNodeByTag

    [WpfFact]
    public void GetTreeNodeByTag_TagInEntityTree_Found()
    {
        var tag = new object();
        var node = AddTagged(_vm.EntityRootNode, "Player", tag);

        _vm.GetTreeNodeByTag(tag).ShouldBeSameAs(node);
    }

    [WpfFact]
    public void GetTreeNodeByTag_TagInScreenTree_Found()
    {
        var tag = new object();
        var node = AddTagged(_vm.ScreenRootNode, "GameScreen", tag);

        _vm.GetTreeNodeByTag(tag).ShouldBeSameAs(node);
    }

    [WpfFact]
    public void GetTreeNodeByTag_TagInGlobalContentTree_Found()
    {
        var tag = new object();
        var node = AddTagged(_vm.GlobalContentRootNode, "SoundBank", tag);

        _vm.GetTreeNodeByTag(tag).ShouldBeSameAs(node);
    }

    [WpfFact]
    public void GetTreeNodeByTag_TagInNestedNode_Found()
    {
        var folder = AddDir(_vm.EntityRootNode, "Enemies");
        var tag = new object();
        var nested = new NodeViewModel(TreeNodeType.GeneralDirectoryNode) { Text = "Grunt", Tag = tag };
        folder.Add(nested);

        _vm.GetTreeNodeByTag(tag).ShouldBeSameAs(nested);
    }

    [WpfFact]
    public void GetTreeNodeByTag_TagNotInAnyTree_ReturnsNull()
    {
        var tag = new object();

        _vm.GetTreeNodeByTag(tag).ShouldBeNull();
    }

    [WpfFact]
    public void GetTreeNodeByTag_NullTag_DoesNotMatchUntaggedFolderNodes()
    {
        // Folder/category nodes (like a Screen's directory node) have a null Tag. An unresolved
        // selection (e.g. a NamedObjectSave the game couldn't be matched to) must not land on one
        // of these - see issue #2131 (clicking an object in Edit Mode selected the Screen folder).
        AddDir(_vm.ScreenRootNode, "GameScreen");

        _vm.GetTreeNodeByTag(null).ShouldBeNull();
    }

    #endregion

    #region IsInTreeView

    [WpfFact]
    public void IsInTreeView_NodeUnderEntityRoot_ReturnsTrue()
    {
        var node = AddTagged(_vm.EntityRootNode, "Player", new object());

        _vm.IsInTreeView(node).ShouldBeTrue();
    }

    [WpfFact]
    public void IsInTreeView_NodeUnderScreenRoot_ReturnsTrue()
    {
        var node = AddTagged(_vm.ScreenRootNode, "GameScreen", new object());

        _vm.IsInTreeView(node).ShouldBeTrue();
    }

    [WpfFact]
    public void IsInTreeView_NodeUnderGlobalContentRoot_ReturnsTrue()
    {
        var node = AddTagged(_vm.GlobalContentRootNode, "SoundBank", new object());

        _vm.IsInTreeView(node).ShouldBeTrue();
    }

    [WpfFact]
    public void IsInTreeView_DeepNestedNodeUnderEntityRoot_ReturnsTrue()
    {
        var folder = AddDir(_vm.EntityRootNode, "Enemies");
        var nested = new NodeViewModel(TreeNodeType.GeneralDirectoryNode) { Text = "Grunt" };
        folder.Add(nested);

        _vm.IsInTreeView(nested).ShouldBeTrue();
    }

    [WpfFact]
    public void IsInTreeView_OrphanNode_ReturnsFalse()
    {
        var orphan = new NodeViewModel(TreeNodeType.GeneralDirectoryNode) { Text = "Orphan" };

        _vm.IsInTreeView(orphan).ShouldBeFalse();
    }

    #endregion
}
