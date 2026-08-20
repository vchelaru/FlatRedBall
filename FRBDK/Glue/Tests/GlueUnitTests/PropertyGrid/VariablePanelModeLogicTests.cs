using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.VariableDisplay;
using Shouldly;
using Xunit;

namespace GlueUnitTests.PropertyGrid;

// GitHub issue #2134: selecting a folder (or anything else with no properties to show) hid the
// Variables tab entirely, which collapsed the right panel to 0 width and snapped it back open on
// the next selection - flickering the whole main editor layout. VariablePanelModeLogic.DetermineMode
// is the decision extracted out of MainPropertyGridPlugin.HandleItemSelect so the "what should the
// panel show" branching can be pinned without standing up GlueState/WPF.
public class VariablePanelModeLogicTests
{
    private sealed class FakeTreeNode : ITreeNode
    {
        public TreeNodeType TreeNodeType { get; set; }
        public object Tag { get; set; }
        public ITreeNode Parent => null;
        public string Text { get; set; } = "";
        public IEnumerable<ITreeNode> Children => Enumerable.Empty<ITreeNode>();
        public void Remove(ITreeNode child) { }
        public void Add(ITreeNode child) { }
        public ITreeNode FindByName(string name) => null;
        public void RemoveGlobalContentTreeNodesIfDoesntExist(ITreeNode treeNode) { }
        public ITreeNode FindByTagRecursive(object tag) => null;
        public void SortByTextConsideringDirectories() { }
    }

    [Fact]
    public void DetermineMode_ShouldReturnEmpty_WhenFolderSelected()
    {
        // A folder node selection means none of the "current" GlueState values are set, and the
        // selected node is neither an element node nor the root custom variables node:
        var folderNode = new FakeTreeNode { TreeNodeType = TreeNodeType.GeneralDirectoryNode };

        var mode = VariablePanelModeLogic.DetermineMode(
            currentNamedObjectSave: null,
            currentElement: null,
            currentStateSave: null,
            currentStateSaveCategory: null,
            selectedTreeNode: folderNode,
            currentReferencedFileSave: null);

        mode.ShouldBe(VariablePanelMode.Empty);
    }

    [Fact]
    public void DetermineMode_ShouldReturnEmpty_WhenNamedObjectIsList()
    {
        var nos = new NamedObjectSave { SourceType = SourceType.FlatRedBallType, SourceClassType = "PositionedObjectList<T>" };

        var mode = VariablePanelModeLogic.DetermineMode(
            currentNamedObjectSave: nos,
            currentElement: null,
            currentStateSave: null,
            currentStateSaveCategory: null,
            selectedTreeNode: null,
            currentReferencedFileSave: null);

        mode.ShouldBe(VariablePanelMode.Empty);
    }

    [Fact]
    public void DetermineMode_ShouldReturnNamedObject_WhenNamedObjectIsNotList()
    {
        var nos = new NamedObjectSave { SourceType = SourceType.FlatRedBallType, SourceClassType = "Sprite" };

        var mode = VariablePanelModeLogic.DetermineMode(
            currentNamedObjectSave: nos,
            currentElement: null,
            currentStateSave: null,
            currentStateSaveCategory: null,
            selectedTreeNode: null,
            currentReferencedFileSave: null);

        mode.ShouldBe(VariablePanelMode.NamedObject);
    }

    [Fact]
    public void DetermineMode_ShouldReturnEmpty_WhenStateSelected()
    {
        var mode = VariablePanelModeLogic.DetermineMode(
            currentNamedObjectSave: null,
            currentElement: null,
            currentStateSave: new StateSave(),
            currentStateSaveCategory: null,
            selectedTreeNode: null,
            currentReferencedFileSave: null);

        mode.ShouldBe(VariablePanelMode.Empty);
    }

    [Fact]
    public void DetermineMode_ShouldReturnEmpty_WhenStateCategorySelected()
    {
        var mode = VariablePanelModeLogic.DetermineMode(
            currentNamedObjectSave: null,
            currentElement: null,
            currentStateSave: null,
            currentStateSaveCategory: new StateSaveCategory(),
            selectedTreeNode: null,
            currentReferencedFileSave: null);

        mode.ShouldBe(VariablePanelMode.Empty);
    }

    [Fact]
    public void DetermineMode_ShouldReturnElement_WhenElementNodeSelected()
    {
        var screen = new ScreenSave { Name = "Screens/GameScreen/GameScreen" };
        var elementNode = new FakeTreeNode { TreeNodeType = TreeNodeType.ScreenNode };

        var mode = VariablePanelModeLogic.DetermineMode(
            currentNamedObjectSave: null,
            currentElement: screen,
            currentStateSave: null,
            currentStateSaveCategory: null,
            selectedTreeNode: elementNode,
            currentReferencedFileSave: null);

        mode.ShouldBe(VariablePanelMode.Element);
    }

    [Fact]
    public void DetermineMode_ShouldReturnEmpty_WhenElementSetButUnrelatedNodeSelected()
    {
        // Regression guard: an element being "current" isn't enough on its own - e.g. a folder
        // under the currently-open screen/entity shouldn't show that element's variables:
        var screen = new ScreenSave { Name = "Screens/GameScreen/GameScreen" };
        var folderNode = new FakeTreeNode { TreeNodeType = TreeNodeType.GeneralDirectoryNode };

        var mode = VariablePanelModeLogic.DetermineMode(
            currentNamedObjectSave: null,
            currentElement: screen,
            currentStateSave: null,
            currentStateSaveCategory: null,
            selectedTreeNode: folderNode,
            currentReferencedFileSave: null);

        mode.ShouldBe(VariablePanelMode.Empty);
    }

    [Fact]
    public void DetermineMode_ShouldReturnReferencedFile_WhenReferencedFileSelected()
    {
        var rfs = new ReferencedFileSave { Name = "GlobalContent/Test.png" };

        var mode = VariablePanelModeLogic.DetermineMode(
            currentNamedObjectSave: null,
            currentElement: null,
            currentStateSave: null,
            currentStateSaveCategory: null,
            selectedTreeNode: null,
            currentReferencedFileSave: rfs);

        mode.ShouldBe(VariablePanelMode.ReferencedFile);
    }

    [Fact]
    public void DetermineMode_ShouldReturnEmpty_WhenNothingSelected()
    {
        var mode = VariablePanelModeLogic.DetermineMode(
            currentNamedObjectSave: null,
            currentElement: null,
            currentStateSave: null,
            currentStateSaveCategory: null,
            selectedTreeNode: null,
            currentReferencedFileSave: null);

        mode.ShouldBe(VariablePanelMode.Empty);
    }
}
