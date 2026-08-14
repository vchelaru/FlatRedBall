using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.TestSupport;
using Moq;
using Shouldly;

namespace GlueUnitTests.TreeViewPlugin;

/// <summary>
/// Tests for RightClickHelper.GetItemDescriptors — asserts which items appear for each node type
/// without standing up the editor. Only descriptor Text values are checked; Handler correctness
/// is not verified here because handlers still call GlueCommands.Self / GluxCommands.Self.
/// </summary>
public class RightClickDescriptorTests
{
    public RightClickDescriptorTests()
    {
        // IsNamedObjectNode's branch reads AvailableAssetTypes.CommonAtis (via GetAssetTypeInfo()
        // comparisons), which is only populated by real Glue startup / this bootstrap.
        GlueTestBootstrap.EnsureInitialized();
    }

    static Mock<ITreeNode> MakeNode(TreeNodeType type, object tag = null)
    {
        var mock = new Mock<ITreeNode>() { CallBase = true };
        mock.Setup(n => n.TreeNodeType).Returns(type);
        mock.Setup(n => n.Tag).Returns(tag);
        // Real tree nodes always have Text; the type-check predicates (e.g. IsRootLayerNode) call
        // Text.Equals(...), so leaving it null makes CallBase throw. Use the type name — it just needs
        // to be non-null and not collide with a special node name like "Layers".
        mock.Setup(n => n.Text).Returns(type.ToString());
        return mock;
    }

    static Mock<IGlueState> MakeGlueState() => new Mock<IGlueState>();

    static IReadOnlyList<string> GetTexts(ITreeNode node, IGlueState state = null) =>
        RightClickHelper.GetItemDescriptors(node, state ?? MakeGlueState().Object, MenuShowingAction.RegularRightClick)
                        .Where(d => !d.IsSeparator)
                        .Select(d => d.Text)
                        .ToList();

    #region EntityNode

    [Fact]
    public void EntityNode_ContainsDuplicate()
    {
        var node = MakeNode(TreeNodeType.EntityNode, new EntitySave { Name = "Entities\\Player" });

        var texts = GetTexts(node.Object);

        texts.ShouldContain("Duplicate");
    }

    [Fact]
    public void EntityNode_DuplicateComesAfterRename()
    {
        var node = MakeNode(TreeNodeType.EntityNode, new EntitySave { Name = "Entities\\Player" });

        var items = RightClickHelper.GetItemDescriptors(node.Object, MakeGlueState().Object, MenuShowingAction.RegularRightClick)
                                    .Where(d => !d.IsSeparator)
                                    .ToList();

        int renameIdx = items.FindIndex(d => d.Text == "Rename");
        int duplicateIdx = items.FindIndex(d => d.Text == "Duplicate");

        renameIdx.ShouldBeGreaterThanOrEqualTo(0);
        duplicateIdx.ShouldBeGreaterThan(renameIdx);
    }

    [Fact]
    public void EntityNode_RightButtonDrag_DoesNotContainDuplicate()
    {
        var entity = new EntitySave { Name = "Entities\\Player" };
        var node = MakeNode(TreeNodeType.EntityNode, entity);
        var dragged = MakeNode(TreeNodeType.EntityNode, new EntitySave { Name = "Entities\\Enemy" });

        var texts = RightClickHelper
            .GetItemDescriptors(node.Object, MakeGlueState().Object, MenuShowingAction.RightButtonDrag, draggedNode: dragged.Object)
            .Where(d => !d.IsSeparator)
            .Select(d => d.Text)
            .ToList();

        texts.ShouldNotContain("Duplicate");
    }

    [Fact]
    public void EntityNode_HasExportNotExportEntity()
    {
        var node = MakeNode(TreeNodeType.EntityNode, new EntitySave { Name = "Entities\\Player" });

        var texts = GetTexts(node.Object);

        texts.ShouldContain("Export");
        texts.ShouldNotContain("Export Entity");
    }

    [Fact]
    public void EntityNode_HasRemoveNotRemoveItem()
    {
        var node = MakeNode(TreeNodeType.EntityNode, new EntitySave { Name = "Entities\\Player" });

        var texts = GetTexts(node.Object);

        texts.ShouldContain("Remove");
        texts.ShouldNotContain("Remove Item");
    }

    #endregion

    #region ScreenNode

    [Fact]
    public void ScreenNode_ContainsDuplicate()
    {
        var node = MakeNode(TreeNodeType.ScreenNode, new ScreenSave { Name = "Screens\\GameScreen" });

        var texts = GetTexts(node.Object);

        texts.ShouldContain("Duplicate");
    }

    [Fact]
    public void ScreenNode_DuplicateComesAfterRename()
    {
        var node = MakeNode(TreeNodeType.ScreenNode, new ScreenSave { Name = "Screens\\GameScreen" });

        var items = RightClickHelper.GetItemDescriptors(node.Object, MakeGlueState().Object, MenuShowingAction.RegularRightClick)
                                    .Where(d => !d.IsSeparator)
                                    .ToList();

        int renameIdx = items.FindIndex(d => d.Text == "Rename");
        int duplicateIdx = items.FindIndex(d => d.Text == "Duplicate");

        renameIdx.ShouldBeGreaterThanOrEqualTo(0);
        duplicateIdx.ShouldBeGreaterThan(renameIdx);
    }

    [Fact]
    public void ScreenNode_RightButtonDrag_DoesNotContainDuplicate()
    {
        var node = MakeNode(TreeNodeType.ScreenNode, new ScreenSave { Name = "Screens\\GameScreen" });
        var dragged = MakeNode(TreeNodeType.EntityNode, new EntitySave { Name = "Entities\\Player" });

        var texts = RightClickHelper
            .GetItemDescriptors(node.Object, MakeGlueState().Object, MenuShowingAction.RightButtonDrag, draggedNode: dragged.Object)
            .Where(d => !d.IsSeparator)
            .Select(d => d.Text)
            .ToList();

        texts.ShouldNotContain("Duplicate");
    }

    [Fact]
    public void ScreenNode_HasExportNotExportScreen()
    {
        var node = MakeNode(TreeNodeType.ScreenNode, new ScreenSave { Name = "Screens\\GameScreen" });

        var texts = GetTexts(node.Object);

        texts.ShouldContain("Export");
        texts.ShouldNotContain("Export Screen");
    }

    [Fact]
    public void ScreenNode_HasRemoveNotRemoveItem()
    {
        var node = MakeNode(TreeNodeType.ScreenNode, new ScreenSave { Name = "Screens\\GameScreen" });

        var texts = GetTexts(node.Object);

        texts.ShouldContain("Remove");
        texts.ShouldNotContain("Remove Item");
    }

    #endregion

    #region RootEntityNode

    [Fact]
    public void RootEntityNode_ContainsViewInExplorer()
    {
        var node = MakeNode(TreeNodeType.EntityRootNode);

        var texts = GetTexts(node.Object);

        texts.ShouldContain("View in Explorer");
    }

    #endregion

    #region RootScreenNode

    [Fact]
    public void RootScreenNode_ContainsViewInExplorer()
    {
        var node = MakeNode(TreeNodeType.ScreenRootNode);

        var texts = GetTexts(node.Object);

        texts.ShouldContain("View in Explorer");
    }

    #endregion

    #region ReferencedFileNode

    [Fact]
    public void ReferencedFileNode_ContainsRename()
    {
        var rfs = new ReferencedFileSave { Name = "GlobalContent/SomeFile.png" };
        var node = MakeNode(TreeNodeType.ReferencedFileSaveNode, rfs);

        var state = MakeGlueState();
        state.Setup(s => s.CurrentReferencedFileSave).Returns(rfs);

        var texts = GetTexts(node.Object, state.Object);

        texts.ShouldContain("Rename");
    }

    [Fact]
    public void ReferencedFileNode_HasRemoveNotRemoveFromContext()
    {
        var rfs = new ReferencedFileSave { Name = "GlobalContent/SomeFile.png" };
        var node = MakeNode(TreeNodeType.ReferencedFileSaveNode, rfs);

        var state = MakeGlueState();
        state.Setup(s => s.CurrentReferencedFileSave).Returns(rfs);

        var texts = GetTexts(node.Object, state.Object);

        texts.ShouldContain("Remove");
        texts.ShouldNotContain("Remove from Screen");
        texts.ShouldNotContain("Remove from Entity");
        texts.ShouldNotContain("Remove from Global Content");
    }

    [Fact]
    public void ReferencedFileNode_CopyNameComesAfterRemove()
    {
        var rfs = new ReferencedFileSave { Name = "GlobalContent/SomeFile.png" };
        var node = MakeNode(TreeNodeType.ReferencedFileSaveNode, rfs);

        var state = MakeGlueState();
        state.Setup(s => s.CurrentReferencedFileSave).Returns(rfs);

        var items = RightClickHelper.GetItemDescriptors(node.Object, state.Object, MenuShowingAction.RegularRightClick)
                                    .Where(d => !d.IsSeparator)
                                    .ToList();

        int removeIdx = items.FindIndex(d => d.Text == "Remove");
        int copyNameIdx = items.FindIndex(d => d.Text == "Copy Name...");

        removeIdx.ShouldBeGreaterThanOrEqualTo(0);
        copyNameIdx.ShouldBeGreaterThan(removeIdx);
    }

    #endregion

    #region NamedObjectNode

    [Fact]
    public void NamedObjectNode_ContainsRename()
    {
        var nos = new NamedObjectSave { InstanceName = "PlayerInstance" };
        var node = MakeNode(TreeNodeType.NamedObjectSaveNode, nos);

        var texts = GetTexts(node.Object);

        texts.ShouldContain("Rename");
    }

    [Fact]
    public void NamedObjectNode_RenameComesBeforeDuplicate()
    {
        var nos = new NamedObjectSave { InstanceName = "PlayerInstance" };
        var node = MakeNode(TreeNodeType.NamedObjectSaveNode, nos);

        var items = RightClickHelper.GetItemDescriptors(node.Object, MakeGlueState().Object, MenuShowingAction.RegularRightClick)
                                    .Where(d => !d.IsSeparator)
                                    .ToList();

        int renameIdx = items.FindIndex(d => d.Text == "Rename");
        int duplicateIdx = items.FindIndex(d => d.Text == "Duplicate");

        renameIdx.ShouldBeGreaterThanOrEqualTo(0);
        duplicateIdx.ShouldBeGreaterThan(renameIdx);
    }

    #endregion
}
