using EngineUnitTests.TestSupport;
using FlatRedBall.Forms;
using FlatRedBall.Forms.Controls;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace EngineUnitTests.Forms;

// TreeViewLogic used to find its default item visual only through the obsolete
// FrameworkElement.DefaultFormsComponents (a Type, built by reflection). It now reads DefaultFormsTemplates
// first, which needs no reflection and so works under trimming/AOT.
[Collection(nameof(EngineStatefulTestCollection))]
public class TreeViewItemCreationTests : IDisposable
{
    readonly VisualTemplate? _originalTemplate;
    readonly bool _hadOriginalTemplate;

    public TreeViewItemCreationTests()
    {
        EngineTestBootstrap.EnsureInitialized();
        _hadOriginalTemplate = FrameworkElement.DefaultFormsTemplates.TryGetValue(typeof(TreeViewItem), out _originalTemplate);
    }

    public void Dispose()
    {
        if (_hadOriginalTemplate)
        {
            FrameworkElement.DefaultFormsTemplates[typeof(TreeViewItem)] = _originalTemplate!;
        }
        else
        {
            FrameworkElement.DefaultFormsTemplates.Remove(typeof(TreeViewItem));
        }
    }

    [Fact]
    public void AddingItem_WithTreeViewItemTemplateRegistered_ShouldBuildVisualFromTemplate()
    {
        int templateCalls = 0;
        FrameworkElement.DefaultFormsTemplates[typeof(TreeViewItem)] = new VisualTemplate(() =>
        {
            templateCalls++;
            return CreateTreeViewItemVisual();
        });

        var treeView = new TreeView(CreateTreeViewVisual());
        treeView.Items.Add("first");

        templateCalls.ShouldBe(1);
        treeView.InnerPanel.Children.Count.ShouldBe(1);
    }

    [Fact]
    public void AddingItem_WithExplicitGumType_ShouldNotUseTemplate()
    {
        int templateCalls = 0;
        FrameworkElement.DefaultFormsTemplates[typeof(TreeViewItem)] = new VisualTemplate(() =>
        {
            templateCalls++;
            return CreateTreeViewItemVisual();
        });

        var treeView = new TreeView(CreateTreeViewVisual());
        treeView.TreeViewItemGumType = typeof(TestTreeViewItemVisual);
        treeView.Items.Add("first");

        templateCalls.ShouldBe(0);
        treeView.InnerPanel.Children.Count.ShouldBe(1);
        treeView.InnerPanel.Children[0].ShouldBeOfType<TestTreeViewItemVisual>();
    }

    static GraphicalUiElement CreateTreeViewVisual()
    {
        var visual = new GraphicalUiElement(new InvisibleRenderable());
        var innerPanel = new GraphicalUiElement(new InvisibleRenderable()) { Name = "InnerPanelInstance" };
        innerPanel.Parent = visual;
        return visual;
    }

    static GraphicalUiElement CreateTreeViewItemVisual() => new TestTreeViewItemVisual(true, true);

    // Matches the (bool, bool) constructor shape TreeViewLogic requires of a reflected item visual.
    public class TestTreeViewItemVisual : GraphicalUiElement
    {
        public TestTreeViewItemVisual(bool fullInstantiation, bool tryCreateFormsObject) : base(new InvisibleRenderable())
        {
            var innerPanel = new GraphicalUiElement(new InvisibleRenderable()) { Name = "InnerPanelInstance" };
            innerPanel.Parent = this;
        }
    }
}
