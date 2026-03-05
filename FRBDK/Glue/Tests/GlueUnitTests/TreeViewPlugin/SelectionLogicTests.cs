using FlatRedBall.Glue.FormHelpers;
using Moq;
using OfficialPlugins.TreeViewPlugin.Logic;
using OfficialPlugins.TreeViewPlugin.Models;
using OfficialPlugins.TreeViewPlugin.ViewModels;
using OfficialPlugins.TreeViewPlugin.Views;
using Shouldly;
using Xunit;

namespace GlueUnitTests.TreeViewPlugin;

public class SelectionLogicTests
{
    readonly Mock<ITreeViewDisplay> _mockDisplay;
    readonly MainTreeViewViewModel _vm;
    readonly SelectionLogic _sut;

    public SelectionLogicTests()
    {
        _mockDisplay = new Mock<ITreeViewDisplay>();
        _vm = new MainTreeViewViewModel();
        _sut = new SelectionLogic(_vm, _mockDisplay.Object);
    }

    [WpfFact]
    public void Constructor_SetsCurrent()
    {
        SelectionLogic.Current.ShouldBeSameAs(_sut);
    }

    [WpfFact]
    public void CurrentNode_IsNullInitially()
    {
        _sut.CurrentNode.ShouldBeNull();
    }

    [WpfFact]
    public void DefaultFlags_AreCorrect()
    {
        _sut.IsPushingSelectionOutToGlue.ShouldBeTrue();
        _sut.IsUpdatingThisSelectionOnGlueEvent.ShouldBeTrue();
        _sut.SuppressFocus.ShouldBeFalse();
    }

    // HandleDeselection on a node that was never selected is a no-op on currentNodes
    // and always calls RefreshRightClickMenu (forcePushToGlue=false so GlueState is not touched).
    [WpfFact]
    public void HandleDeselection_NodeNotInSelection_CallsRefreshRightClickMenu()
    {
        _sut.IsPushingSelectionOutToGlue = false;
        var node = new NodeViewModel(TreeNodeType.GeneralDirectoryNode);

        _sut.HandleDeselection(node);

        _mockDisplay.Verify(v => v.RefreshRightClickMenu(), Times.Once);
        _sut.CurrentNode.ShouldBeNull();
    }
}
