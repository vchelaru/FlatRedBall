using AnimationEditor.Core.ViewModels;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Content.Math.Geometry;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AnimationEditor.Core.Tests;

// ── Pure tests (no singleton state) ──────────────────────────────────────────

public class TreeBuilderPureTests
{
    // ── BuildTree ─────────────────────────────────────────────────────────────

    [Fact]
    public void BuildTree_EmptyAcls_ReturnsEmptyList()
    {
        var acls = new AnimationChainListSave();
        var result = TreeBuilder.BuildTree(acls);
        Assert.Empty(result);
    }

    [Fact]
    public void BuildTree_SingleChain_CreatesOneRootNode()
    {
        var acls = new AnimationChainListSave();
        var chain = new AnimationChainSave { Name = "Walk" };
        acls.AnimationChains.Add(chain);

        var result = TreeBuilder.BuildTree(acls);

        Assert.Single(result);
        Assert.Equal("Walk", result[0].Header);
        Assert.Same(chain, result[0].Data);
    }

    [Fact]
    public void BuildTree_ChainWithFrames_CreatesChildNodes()
    {
        var acls = new AnimationChainListSave();
        var chain = new AnimationChainSave { Name = "Run" };
        chain.Frames.Add(new AnimationFrameSave { TextureName = "sprites/run1.png" });
        chain.Frames.Add(new AnimationFrameSave { TextureName = "sprites/run2.png" });
        acls.AnimationChains.Add(chain);

        var root = TreeBuilder.BuildTree(acls)[0];

        Assert.Equal(2, root.Children.Count);
        Assert.Equal("run1.png", root.Children[0].Header);
        Assert.Equal("run2.png", root.Children[1].Header);
    }

    [Fact]
    public void BuildTree_WithNullExpandedNames_AllNodesDefaultExpanded()
    {
        var acls = new AnimationChainListSave();
        acls.AnimationChains.Add(new AnimationChainSave { Name = "A" });
        acls.AnimationChains.Add(new AnimationChainSave { Name = "B" });

        var result = TreeBuilder.BuildTree(acls, expandedChainNames: null);

        Assert.All(result, n => Assert.True(n.IsExpanded));
    }

    [Fact]
    public void BuildTree_WithExpandedNames_OnlyListedNodesAreExpanded()
    {
        var acls = new AnimationChainListSave();
        acls.AnimationChains.Add(new AnimationChainSave { Name = "Walk" });
        acls.AnimationChains.Add(new AnimationChainSave { Name = "Run" });
        acls.AnimationChains.Add(new AnimationChainSave { Name = "Idle" });

        var result = TreeBuilder.BuildTree(acls, expandedChainNames: new[] { "Walk", "Idle" });

        Assert.True(result.First(n => n.Header == "Walk").IsExpanded);
        Assert.False(result.First(n => n.Header == "Run").IsExpanded);
        Assert.True(result.First(n => n.Header == "Idle").IsExpanded);
    }

    [Fact]
    public void BuildTree_WithEmptyExpandedNamesList_AllNodesCollapsed()
    {
        var acls = new AnimationChainListSave();
        acls.AnimationChains.Add(new AnimationChainSave { Name = "Walk" });

        var result = TreeBuilder.BuildTree(acls, expandedChainNames: Enumerable.Empty<string>());

        Assert.False(result[0].IsExpanded);
    }

    // ── BuildChainNode ────────────────────────────────────────────────────────

    [Fact]
    public void BuildChainNode_SetsHeaderAndData()
    {
        var chain = new AnimationChainSave { Name = "Jump" };
        var node = TreeBuilder.BuildChainNode(chain);
        Assert.Equal("Jump", node.Header);
        Assert.Same(chain, node.Data);
    }

    [Fact]
    public void BuildChainNode_DefaultsIsExpandedTrue()
    {
        var chain = new AnimationChainSave { Name = "Jump" };
        var node = TreeBuilder.BuildChainNode(chain);
        Assert.True(node.IsExpanded);
    }

    // ── BuildFrameNode & BuildFrameHeader ─────────────────────────────────────

    [Fact]
    public void BuildFrameHeader_WithTexturePath_ReturnsFileNameOnly()
    {
        var frame = new AnimationFrameSave { TextureName = "sprites/player/walk1.png" };
        Assert.Equal("walk1.png", TreeBuilder.BuildFrameHeader(frame));
    }

    [Fact]
    public void BuildFrameHeader_WithNoTextureName_ReturnsUntexturedLabel()
    {
        var frame = new AnimationFrameSave { TextureName = "" };
        Assert.Equal("<UNTEXTURED>", TreeBuilder.BuildFrameHeader(frame));
    }

    [Fact]
    public void BuildFrameNode_WithShapes_AddsShapeChildren()
    {
        var frame = new AnimationFrameSave
        {
            TextureName = "Tex.png",
            ShapeCollectionSave = new ShapeCollectionSave()
        };
        frame.ShapeCollectionSave.AxisAlignedRectangleSaves.Add(
            new AxisAlignedRectangleSave { Name = "HitBox" });
        frame.ShapeCollectionSave.CircleSaves.Add(
            new CircleSave { Name = "HurtCircle" });

        var node = TreeBuilder.BuildFrameNode(frame);

        Assert.Equal(2, node.Children.Count);
        Assert.Equal("HitBox",     node.Children[0].Header);
        Assert.Equal("HurtCircle", node.Children[1].Header);
    }

    // ── GetExpandedChainNames ─────────────────────────────────────────────────

    [Fact]
    public void GetExpandedChainNames_ReturnsNamesOfExpandedChainNodes()
    {
        var chainA = new AnimationChainSave { Name = "A" };
        var chainB = new AnimationChainSave { Name = "B" };
        var nodes = new List<TreeNodeVm>
        {
            new TreeNodeVm { Data = chainA, IsExpanded = true  },
            new TreeNodeVm { Data = chainB, IsExpanded = false },
        };

        var names = TreeBuilder.GetExpandedChainNames(nodes).ToList();

        Assert.Single(names);
        Assert.Equal("A", names[0]);
    }

    [Fact]
    public void GetExpandedChainNames_IgnoresExpandedFrameNodes()
    {
        var frame = new AnimationFrameSave { TextureName = "Tex.png" };
        var nodes = new List<TreeNodeVm>
        {
            new TreeNodeVm { Data = frame, IsExpanded = true }
        };

        var names = TreeBuilder.GetExpandedChainNames(nodes).ToList();

        Assert.Empty(names);
    }

    // ── FindNodeForData ───────────────────────────────────────────────────────

    [Fact]
    public void FindNodeForData_FindsRootLevelNode()
    {
        var chain = new AnimationChainSave { Name = "Walk" };
        var node  = new TreeNodeVm { Data = chain };
        var roots = new List<TreeNodeVm> { node };

        Assert.Same(node, TreeBuilder.FindNodeForData(roots, chain));
    }

    [Fact]
    public void FindNodeForData_FindsNestedFrameNode()
    {
        var frame     = new AnimationFrameSave { TextureName = "Tex.png" };
        var frameNode = new TreeNodeVm { Data = frame };
        var chainNode = new TreeNodeVm { Data = new AnimationChainSave { Name = "Walk" } };
        chainNode.Children.Add(frameNode);

        Assert.Same(frameNode, TreeBuilder.FindNodeForData(new[] { chainNode }, frame));
    }

    [Fact]
    public void FindNodeForData_ReturnsNull_WhenNotFound()
    {
        var roots = new List<TreeNodeVm>
        {
            new TreeNodeVm { Data = new AnimationChainSave { Name = "X" } }
        };
        var unrelated = new AnimationChainSave { Name = "Y" };

        Assert.Null(TreeBuilder.FindNodeForData(roots, unrelated));
    }
}

// ── Singleton-touching tests (RouteNodeSelection) ─────────────────────────────

[Collection("SequentialSingletons")]
public class TreeBuilderSingletonTests
{
    [Fact]
    public void RouteNodeSelection_ChainNode_SetsSelectedChain()
    {
        var acls  = TestHelpers.SetupFreshAcls();
        var chain = new AnimationChainSave { Name = "Walk" };
        acls.AnimationChains.Add(chain);
        var vm = new TreeNodeVm { Data = chain };

        var handled = TreeBuilder.RouteNodeSelection(vm);

        Assert.True(handled);
        Assert.Same(chain, AnimationEditor.Core.SelectedState.Self.SelectedChain);
    }

    [Fact]
    public void RouteNodeSelection_FrameNode_SetsSelectedFrame()
    {
        var acls  = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Run", frameCount: 1);
        var frame = chain.Frames[0];
        var vm    = new TreeNodeVm { Data = frame };

        var handled = TreeBuilder.RouteNodeSelection(vm);

        Assert.True(handled);
        Assert.Same(frame, AnimationEditor.Core.SelectedState.Self.SelectedFrame);
    }

    [Fact]
    public void RouteNodeSelection_RectangleNode_SetsSelectedRectangle()
    {
        TestHelpers.SetupFreshAcls();
        var rect = new AxisAlignedRectangleSave { Name = "HitBox" };
        var vm   = new TreeNodeVm { Data = rect };

        var handled = TreeBuilder.RouteNodeSelection(vm);

        Assert.True(handled);
        Assert.Same(rect, AnimationEditor.Core.SelectedState.Self.SelectedRectangle);
    }

    [Fact]
    public void RouteNodeSelection_CircleNode_SetsSelectedCircle()
    {
        TestHelpers.SetupFreshAcls();
        var circle = new CircleSave { Name = "HurtCircle" };
        var vm     = new TreeNodeVm { Data = circle };

        var handled = TreeBuilder.RouteNodeSelection(vm);

        Assert.True(handled);
        Assert.Same(circle, AnimationEditor.Core.SelectedState.Self.SelectedCircle);
    }

    [Fact]
    public void RouteNodeSelection_NullData_ReturnsFalse()
    {
        TestHelpers.SetupFreshAcls();
        var vm = new TreeNodeVm { Data = null };

        var handled = TreeBuilder.RouteNodeSelection(vm);

        Assert.False(handled);
    }
}
