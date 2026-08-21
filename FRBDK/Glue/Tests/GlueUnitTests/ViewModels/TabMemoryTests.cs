using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GlueFormsCore.ViewModels;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.ViewModels;

/// <summary>
/// GitHub issue #2068: selecting a new file/object reset whichever tab (Variables/Events/Custom
/// Code/...) the user had open back to a fixed default, because <c>4183732ae</c> removed all
/// tab memory in response to issue #1593. #1593 was actually caused by a different, since-replaced
/// design (a single *global* "most recently clicked" timestamp, added by <c>3d39674aa</c>) that let a
/// tab clicked while viewing one node type keep winning for every other type too. These tests drive
/// the real <see cref="PluginManager.TabControlViewModel"/> through <see cref="GlueState.CurrentTreeNode"/>
/// the same way a real tree click does, so they cover the actual reported symptom rather than calling
/// <see cref="TabContainerViewModel.ShowMostRecentTabFor"/> directly.
/// </summary>
public class TabMemoryTests
{
    // A standalone ITreeNode, not resolved through any real tree - same idea as TestSupport's
    // SyntheticTreeNode, but not tied to that type's ReferencedFileSave-only constructor.
    private sealed class FakeTreeNode : ITreeNode
    {
        public FakeTreeNode(object tag, string text, TreeNodeType treeNodeType)
        {
            Tag = tag;
            Text = text;
            TreeNodeType = treeNodeType;
        }

        public object Tag { get; set; }
        public ITreeNode Parent => null;
        public string Text { get; set; }
        public IEnumerable<ITreeNode> Children => Enumerable.Empty<ITreeNode>();
        public TreeNodeType TreeNodeType { get; }
        public void Remove(ITreeNode child) { }
        public void Add(ITreeNode child) { }
        public ITreeNode FindByName(string name) => null;
        public void RemoveGlobalContentTreeNodesIfDoesntExist(ITreeNode treeNode) { }
        public ITreeNode FindByTagRecursive(object tag) => Equals(tag, Tag) ? this : null;
        public void SortByTextConsideringDirectories() { }
    }

    [Fact]
    public void ShowMostRecentTabFor_ShouldKeepManuallySelectedTab_WhenSelectingAnotherObjectOfSameType()
    {
        GlueTestBootstrap.EnsureInitialized();

        var container = PluginManager.TabControlViewModel.RightTabItems;
        var (originalTabs, cleanup) = IsolateContainer(container);
        try
        {
            var variablesTab = new PluginTab { IsPreferredDisplayerForType = t => t == nameof(EntitySave) };
            var eventsTab = new PluginTab();
            container.Add(variablesTab);
            container.Add(eventsTab);

            var nodeA = new FakeTreeNode(new EntitySave(), "EntityA", TreeNodeType.EntityNode);
            GlueState.Self.CurrentTreeNode = nodeA;
            container.SelectedTab.ShouldBe(variablesTab, "no tab has been manually selected yet, so the preferred default should show");

            eventsTab.IsSelected = true;

            var nodeB = new FakeTreeNode(new EntitySave(), "EntityB", TreeNodeType.EntityNode);
            GlueState.Self.CurrentTreeNode = nodeB;

            container.SelectedTab.ShouldBe(eventsTab, "selecting another EntitySave should keep showing the tab the user last chose for that type");
        }
        finally
        {
            cleanup();
        }
    }

    [Fact]
    public void ShowMostRecentTabFor_ShouldNotLeakRememberedTab_AcrossDifferentNodeTypes()
    {
        GlueTestBootstrap.EnsureInitialized();

        var container = PluginManager.TabControlViewModel.RightTabItems;
        var (originalTabs, cleanup) = IsolateContainer(container);
        try
        {
            var variablesTab = new PluginTab
            {
                IsPreferredDisplayerForType = t => t == nameof(EntitySave) || t == "Variables"
            };
            var eventsTab = new PluginTab();
            container.Add(variablesTab);
            container.Add(eventsTab);

            var nodeA = new FakeTreeNode(new EntitySave(), "EntityA", TreeNodeType.EntityNode);
            GlueState.Self.CurrentTreeNode = nodeA;
            eventsTab.IsSelected = true;

            // A "Variables" bookmark node (as clicked from search/bookmarks) is a distinct type key
            // ("Variables", via Text since Tag is null) from "EntitySave" - this is the #1593 scenario:
            // it must always show Variables, regardless of what was last clicked while viewing an entity.
            var bookmarkNode = new FakeTreeNode(null, "Variables", TreeNodeType.Other);
            GlueState.Self.CurrentTreeNode = bookmarkNode;

            container.SelectedTab.ShouldBe(variablesTab, "a distinct node type must not inherit another type's remembered tab");
        }
        finally
        {
            cleanup();
        }
    }

    [Fact]
    public void ShowMostRecentTabFor_ShouldNotSelectStaleRememberedTab_WhenThatTabIsNoLongerShown()
    {
        // GitHub issue #2139: occasionally, selecting an object in the Explorer left neither the
        // Variables nor Properties tab selected. Root cause: TabsForTypes remembers a tab by reflection
        // type name (e.g. "NamedObjectSave"), but a plugin can Hide() that exact tab for a *different*
        // instance of the same type (e.g. a type-specific tab that only shows for some NamedObjectSaves).
        // ShowMostRecentTabFor must not blindly trust a remembered tab that isn't currently shown.
        GlueTestBootstrap.EnsureInitialized();

        var container = PluginManager.TabControlViewModel.RightTabItems;
        var (originalTabs, cleanup) = IsolateContainer(container);
        try
        {
            var variablesTab = new PluginTab { IsPreferredDisplayerForType = t => t == nameof(NamedObjectSave) };
            var specialTab = new PluginTab();
            container.Add(variablesTab);
            container.Add(specialTab);

            var nodeA = new FakeTreeNode(new NamedObjectSave(), "ObjectA", TreeNodeType.NamedObjectSaveNode);
            GlueState.Self.CurrentTreeNode = nodeA;
            specialTab.IsSelected = true;
            container.TabsForTypes[nameof(NamedObjectSave)].ShouldBe(specialTab);

            // Simulate a plugin hiding the type-specific tab for a different NamedObjectSave instance
            // that doesn't support it - specialTab is no longer in Tabs, but is still remembered.
            container.Remove(specialTab);

            var nodeB = new FakeTreeNode(new NamedObjectSave(), "ObjectB", TreeNodeType.NamedObjectSaveNode);
            GlueState.Self.CurrentTreeNode = nodeB;

            container.Tabs.ShouldContain(container.SelectedTab, "the selected tab must actually be shown, never a stale/hidden remembered tab");
            container.SelectedTab.ShouldBe(variablesTab);
        }
        finally
        {
            cleanup();
        }
    }

    private static (List<PluginTab> originalTabs, System.Action cleanup) IsolateContainer(TabContainerViewModel container)
    {
        var originalTabs = container.Tabs.ToList();
        var originalTabsForTypes = new Dictionary<string, PluginTab>(container.TabsForTypes);
        var originalTreeNode = GlueState.Self.CurrentTreeNode;

        foreach (var tab in originalTabs)
        {
            container.Remove(tab);
        }
        container.TabsForTypes.Clear();

        return (originalTabs, () =>
        {
            GlueState.Self.CurrentTreeNode = null;

            foreach (var tab in container.Tabs.ToList())
            {
                container.Remove(tab);
            }
            foreach (var tab in originalTabs)
            {
                container.Add(tab);
            }

            container.TabsForTypes.Clear();
            foreach (var kvp in originalTabsForTypes)
            {
                container.TabsForTypes[kvp.Key] = kvp.Value;
            }

            GlueState.Self.CurrentTreeNode = originalTreeNode;
        });
    }
}
