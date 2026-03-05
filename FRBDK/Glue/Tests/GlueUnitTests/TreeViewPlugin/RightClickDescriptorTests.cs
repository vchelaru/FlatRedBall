using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.SaveClasses;
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
    static Mock<ITreeNode> MakeNode(TreeNodeType type, object tag = null)
    {
        var mock = new Mock<ITreeNode>() { CallBase = true };
        mock.Setup(n => n.TreeNodeType).Returns(type);
        mock.Setup(n => n.Tag).Returns(tag);
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

        texts.ShouldContain(Localization.Texts.Duplicate);
    }

    [Fact]
    public void EntityNode_DuplicateComesAfterRename()
    {
        var node = MakeNode(TreeNodeType.EntityNode, new EntitySave { Name = "Entities\\Player" });

        var items = RightClickHelper.GetItemDescriptors(node.Object, MakeGlueState().Object, MenuShowingAction.RegularRightClick)
                                    .Where(d => !d.IsSeparator)
                                    .ToList();

        int renameIdx = items.FindIndex(d => d.Text == "Rename");
        int duplicateIdx = items.FindIndex(d => d.Text == Localization.Texts.Duplicate);

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

        texts.ShouldNotContain(Localization.Texts.Duplicate);
    }

    #endregion

    #region ScreenNode

    [Fact]
    public void ScreenNode_ContainsDuplicate()
    {
        var node = MakeNode(TreeNodeType.ScreenNode, new ScreenSave { Name = "Screens\\GameScreen" });

        var texts = GetTexts(node.Object);

        texts.ShouldContain(Localization.Texts.Duplicate);
    }

    [Fact]
    public void ScreenNode_DuplicateComesAfterRename()
    {
        var node = MakeNode(TreeNodeType.ScreenNode, new ScreenSave { Name = "Screens\\GameScreen" });

        var items = RightClickHelper.GetItemDescriptors(node.Object, MakeGlueState().Object, MenuShowingAction.RegularRightClick)
                                    .Where(d => !d.IsSeparator)
                                    .ToList();

        int renameIdx = items.FindIndex(d => d.Text == "Rename");
        int duplicateIdx = items.FindIndex(d => d.Text == Localization.Texts.Duplicate);

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

        texts.ShouldNotContain(Localization.Texts.Duplicate);
    }

    #endregion
}
