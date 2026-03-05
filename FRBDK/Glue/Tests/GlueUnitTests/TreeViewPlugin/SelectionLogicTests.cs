using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using Moq;
using System.Collections.Generic;
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
    readonly List<IReadOnlyList<ITreeNode>> _reportedSelections = new();

    public SelectionLogicTests()
    {
        _mockDisplay = new Mock<ITreeViewDisplay>();
        _vm = new MainTreeViewViewModel(Mock.Of<IGlueState>(), Mock.Of<IGlueCommands>());
        _sut = new SelectionLogic(_vm, _mockDisplay.Object, nodes => _reportedSelections.Add(nodes));
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

    // Verifies the delegate wiring: NodeViewModel.IsSelected=true invokes NodeSelected.
    // We replace the static delegate with a spy so the test doesn't depend on GlueState.Self.
    [WpfFact]
    public void IsSelected_True_InvokesNodeSelectedDelegate()
    {
        var savedDelegate = NodeViewModel.NodeSelected;
        try
        {
            NodeViewModel? capturedNode = null;
            bool capturedFocus = false;
            bool capturedReplace = false;
            NodeViewModel.NodeSelected = (node, focus, replace) => { capturedNode = node; capturedFocus = focus; capturedReplace = replace; };

            var nodeVm = new NodeViewModel(TreeNodeType.GeneralDirectoryNode);
            nodeVm.IsSelected = true;

            capturedNode.ShouldBeSameAs(nodeVm);
            capturedFocus.ShouldBeTrue();
            capturedReplace.ShouldBeTrue();
        }
        finally
        {
            NodeViewModel.NodeSelected = savedDelegate;
        }
    }

    // Verifies the delegate wiring: NodeViewModel.IsSelected=false invokes NodeDeselected.
    [WpfFact]
    public void IsSelected_False_InvokesNodeDeselectedDelegate()
    {
        var savedDelegate = NodeViewModel.NodeDeselected;
        try
        {
            NodeViewModel? capturedNode = null;
            NodeViewModel.NodeDeselected = node => capturedNode = node;

            var nodeVm = new NodeViewModel(TreeNodeType.GeneralDirectoryNode);
            nodeVm.SetSelectNoSelectionLogic(true); // set true without firing NodeSelected
            nodeVm.IsSelected = false;

            capturedNode.ShouldBeSameAs(nodeVm);
        }
        finally
        {
            NodeViewModel.NodeDeselected = savedDelegate;
        }
    }
}
