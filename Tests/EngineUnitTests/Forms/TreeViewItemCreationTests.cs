using System;
using System.Collections.Generic;
using System.Linq;
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
        treeView.TreeViewItemGumType = typeof(TestReflectedVisual);
        treeView.Items.Add("first");

        templateCalls.ShouldBe(0);
        treeView.InnerPanel.Children.Count.ShouldBe(1);
        treeView.InnerPanel.Children[0].ShouldBeOfType<TestReflectedVisual>();
    }

    static GraphicalUiElement CreateTreeViewVisual()
    {
        var visual = new GraphicalUiElement(new InvisibleRenderable());
        var innerPanel = new GraphicalUiElement(new InvisibleRenderable()) { Name = "InnerPanelInstance" };
        innerPanel.Parent = visual;
        return visual;
    }

    // What Glue's generated registration builds: a full visual with no Forms object.
    static GraphicalUiElement CreateTreeViewItemVisual() => new TestTreeViewItemVisual(true, false);

    // A runtime reached by reflection that does not build its own Forms object, so the reflective path can be
    // compared with the template path. (With a runtime that does build one the reflective path throws: that
    // TreeViewItem's constructor detaches InnerPanelInstance, and the wrapper TreeViewLogic then creates can no
    // longer find it. The template path, which asks for no Forms object, avoids that.)
    public class TestReflectedVisual : TestTreeViewItemVisual
    {
        public TestReflectedVisual(bool fullInstantiation, bool tryCreateFormsObject) : base(fullInstantiation, false)
        {
        }
    }

    // The reflective and template paths must produce items that behave the same: same shape, same
    // expand/collapse behavior and events.
    [Fact]
    public void ItemFromTemplate_ShouldBehaveLikeItemFromReflectedType()
    {
        var reflectedLog = RunScenario(treeView => treeView.TreeViewItemGumType = typeof(TestReflectedVisual));

        var templateLog = RunScenario(treeView =>
            FrameworkElement.DefaultFormsTemplates[typeof(TreeViewItem)] = new VisualTemplate(CreateTreeViewItemVisual));

        reflectedLog.ShouldBe(templateLog);
        // Fixture sanity: the scenario must actually have exercised expand/collapse.
        templateLog.ShouldContain("expanded=1");
        templateLog.ShouldContain("collapsed=1");
    }

    static List<string> RunScenario(Action<TreeView> configure)
    {
        var treeView = new TreeView(CreateTreeViewVisual());
        configure(treeView);
        treeView.Items.Add("root");

        var log = new List<string>();
        var itemVisual = treeView.InnerPanel.Children[0];
        var item = itemVisual.FormsControlAsObject.ShouldBeOfType<TreeViewItem>();

        int expanded = 0;
        int collapsed = 0;
        item.Expanded += (_, _) => expanded++;
        item.Collapsed += (_, _) => collapsed++;

        log.Add($"parentIsInnerPanel={itemVisual.Parent == treeView.InnerPanel}");
        log.Add($"children={string.Join(",", itemVisual.Children.Select(c => c.Name))}");
        log.Add($"startsExpanded={item.IsExpanded}");

        item.Items.Add("child");
        log.Add($"childItems={item.TreeViewItems.Count}");

        item.IsExpanded = true;
        log.Add($"expandedAfterCheck={item.IsExpanded}");
        log.Add($"childrenAfterExpand={string.Join(",", itemVisual.Children.Select(c => c.Name))}");

        item.IsExpanded = false;
        log.Add($"expandedAfterUncheck={item.IsExpanded}");
        log.Add($"childrenAfterCollapse={string.Join(",", itemVisual.Children.Select(c => c.Name))}");

        log.Add($"expanded={expanded}");
        log.Add($"collapsed={collapsed}");
        return log;
    }

    // Mirrors the shape of a Glue-generated runtime: children first, then a Forms object only when asked.
    // The (bool, bool) constructor is what TreeViewLogic requires of a reflected item visual.
    public class TestTreeViewItemVisual : GraphicalUiElement
    {
        public TestTreeViewItemVisual(bool fullInstantiation, bool tryCreateFormsObject) : base(new InvisibleRenderable())
        {
            var innerPanel = new GraphicalUiElement(new InvisibleRenderable()) { Name = "InnerPanelInstance" };
            innerPanel.Parent = this;


            if (tryCreateFormsObject)
            {
                FormsControlAsObject = new TreeViewItem(this);
            }
        }
    }
}
