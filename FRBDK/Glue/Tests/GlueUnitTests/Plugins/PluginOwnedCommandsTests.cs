using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.Tasks;
using GlueUnitTests.TestSupport;
using Moq;
using OfficialPlugins.CollisionPlugin.Managers;
using OfficialPlugins.MonoGameContent;
using OfficialPlugins.TreeViewPlugin.Logic;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Plugins;

/// <summary>
/// The Property Grid, FRB Source, Wizard, Git, MonoGame Content, Undo and Tree View plugins' typed commands, which
/// replaced <c>PluginManager.CallPluginMethod</c>, and the Glue callers that use them. Sequential with the other
/// tests that swap commands on <see cref="GlueCommands"/>.
/// </summary>
[Collection(nameof(TaskManagerSequentialCollection))]
public class PluginOwnedCommandsTests : IDisposable
{
    private IPropertyGridCommands _originalPropertyGridCommands;
    private IFrbSourceCommands _originalFrbSourceCommands;
    private IWizardCommands _originalWizardCommands;
    private IGitCommands _originalGitCommands;
    private IContentPipelineCommands _originalContentPipelineCommands;
    private IUndoCommands _originalUndoCommands;
    private ITreeViewCommands _originalTreeViewCommands;
    private readonly IFindManager _originalFind;
    private readonly IUiThreadMarshaller _originalMarshaller;

    public PluginOwnedCommandsTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        CaptureCommands();
        _originalFind = GlueState.Self.Find;
        _originalMarshaller = TaskManager.UiThreadMarshaller;
    }

    /// <summary>Registering plugins assigns their commands for the rest of the run, so a test that registers
    /// any calls this again afterwards and Dispose restores the registered ones.</summary>
    private void CaptureCommands()
    {
        _originalPropertyGridCommands = GlueCommands.Self.PropertyGridCommands;
        _originalFrbSourceCommands = GlueCommands.Self.FrbSourceCommands;
        _originalWizardCommands = GlueCommands.Self.WizardCommands;
        _originalGitCommands = GlueCommands.Self.GitCommands;
        _originalContentPipelineCommands = GlueCommands.Self.ContentPipelineCommands;
        _originalUndoCommands = GlueCommands.Self.UndoCommands;
        _originalTreeViewCommands = GlueCommands.Self.TreeViewCommands;
    }

    public void Dispose()
    {
        GlueCommands.Self.PropertyGridCommands = _originalPropertyGridCommands;
        GlueCommands.Self.FrbSourceCommands = _originalFrbSourceCommands;
        GlueCommands.Self.WizardCommands = _originalWizardCommands;
        GlueCommands.Self.GitCommands = _originalGitCommands;
        GlueCommands.Self.ContentPipelineCommands = _originalContentPipelineCommands;
        GlueCommands.Self.UndoCommands = _originalUndoCommands;
        GlueCommands.Self.TreeViewCommands = _originalTreeViewCommands;
        GlueState.Self.Find = _originalFind;
        TaskManager.UiThreadMarshaller = _originalMarshaller;
    }

    #region Registration

    [StaFact]
    public void PropertyGridPlugin_StartUp_AssignsPropertyGridCommands_AndShutDownClearsThem()
    {
        GlueCommands.Self.PropertyGridCommands = null;
        var plugin = new OfficialPlugins.VariableDisplay.MainPropertyGridPlugin();

        plugin.StartUp();
        GlueCommands.Self.PropertyGridCommands.ShouldNotBeNull();

        plugin.ShutDown(PluginShutDownReason.GlueShutDown);
        GlueCommands.Self.PropertyGridCommands.ShouldBeNull();
    }

    [StaFact]
    public void FrbSourcePlugin_StartUp_AssignsFrbSourceCommands_AndShutDownClearsThem()
    {
        GlueTestBootstrap.EnsureHeadlessProjectLoadReady();
        GlueCommands.Self.FrbSourceCommands = null;
        var plugin = new PluginTestbed.GlobalContentManagerPlugins.FrbSourcePlugin();

        plugin.StartUp();
        GlueCommands.Self.FrbSourceCommands.ShouldNotBeNull();

        plugin.ShutDown(PluginShutDownReason.PluginInitiated);
        GlueCommands.Self.FrbSourceCommands.ShouldBeNull();
    }

    [StaFact]
    public void WizardPlugin_StartUp_AssignsWizardCommands_AndShutDownClearsThem()
    {
        GlueTestBootstrap.EnsureHeadlessProjectLoadReady();
        GlueCommands.Self.WizardCommands = null;
        var plugin = new OfficialPlugins.Wizard.MainWizardPlugin();

        plugin.StartUp();
        GlueCommands.Self.WizardCommands.ShouldNotBeNull();

        plugin.ShutDown(PluginShutDownReason.PluginInitiated);
        GlueCommands.Self.WizardCommands.ShouldBeNull();
    }

    [Fact]
    public void GitPlugin_StartUp_AssignsGitCommands_AndShutDownClearsThem()
    {
        GlueCommands.Self.GitCommands = null;
        var plugin = new OfficialPlugins.Git.MainGitPlugin();

        plugin.StartUp();
        GlueCommands.Self.GitCommands.ShouldNotBeNull();

        plugin.ShutDown(PluginShutDownReason.PluginInitiated);
        GlueCommands.Self.GitCommands.ShouldBeNull();
    }

    /// <summary>
    /// Checks the registered MonoGame Content plugin, since a second StartUp would add its FileAliases code
    /// generator again for the rest of the run.
    /// </summary>
    [StaFact]
    public void ContentPipelinePlugin_StartUp_AssignsContentPipelineCommands_AndShutDownClearsThem()
    {
        GlueTestBootstrap.EnsureHeadlessProjectLoadReady();
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();
        CaptureCommands();
        var plugin = PluginManager.GetGlobalPluginManager().PluginContainers.Keys
            .OfType<MainContentPipelinePlugin>()
            .Single();
        GlueCommands.Self.ContentPipelineCommands.ShouldNotBeNull();

        plugin.ShutDown(PluginShutDownReason.PluginInitiated);
        GlueCommands.Self.ContentPipelineCommands.ShouldBeNull();
    }

    [StaFact]
    public void UndoPlugin_StartUp_AssignsUndoCommands_AndShutDownClearsThem()
    {
        GlueTestBootstrap.EnsureHeadlessProjectLoadReady();
        GlueCommands.Self.UndoCommands = null;
        var plugin = new OfficialPlugins.UndoPlugin.MainUndoPlugin();

        plugin.StartUp();
        GlueCommands.Self.UndoCommands.ShouldNotBeNull();

        plugin.ShutDown(PluginShutDownReason.PluginInitiated);
        GlueCommands.Self.UndoCommands.ShouldBeNull();
    }

    [StaFact]
    public void TreeViewPlugin_StartUp_AssignsTreeViewCommands_AndShutDownClearsThem()
    {
        GlueTestBootstrap.EnsureHeadlessProjectLoadReady();
        GlueCommands.Self.TreeViewCommands = null;
        var plugin = new OfficialPlugins.TreeViewPlugin.MainTreeViewPlugin();

        plugin.StartUp();
        GlueCommands.Self.TreeViewCommands.ShouldNotBeNull();

        plugin.ShutDown(PluginShutDownReason.PluginInitiated);
        GlueCommands.Self.TreeViewCommands.ShouldBeNull();
    }

    #endregion

    #region Callers

    [Fact]
    public void RefreshVariables_RefreshesThePropertyGrid_OnTheUiThread()
    {
        var marshaller = new RecordingMarshaller();
        TaskManager.UiThreadMarshaller = marshaller;
        var propertyGrid = new FakePropertyGridCommands();
        GlueCommands.Self.PropertyGridCommands = propertyGrid;

        GlueCommands.Self.RefreshCommands.RefreshVariables();

        propertyGrid.RefreshCount.ShouldBe(1);
        marshaller.InvokeCount.ShouldBe(1);
    }

    [Fact]
    public void RefreshVariables_DoesNothing_WhenThePropertyGridPluginIsNotLoaded()
    {
        GlueCommands.Self.PropertyGridCommands = null;

        Should.NotThrow(() => GlueCommands.Self.RefreshCommands.RefreshVariables());
    }

    [Fact]
    public void Undo_UndoesThroughTheUndoPlugin_OnTheUiThread()
    {
        var marshaller = new RecordingMarshaller();
        TaskManager.UiThreadMarshaller = marshaller;
        var undo = new FakeUndoCommands();
        GlueCommands.Self.UndoCommands = undo;

        GlueCommands.Self.Undo();

        undo.UndoCount.ShouldBe(1);
        marshaller.InvokeCount.ShouldBe(1);
    }

    [Fact]
    public void Undo_DoesNothing_WhenTheUndoPluginIsNotLoaded()
    {
        GlueCommands.Self.UndoCommands = null;

        Should.NotThrow(() => GlueCommands.Self.Undo());
    }

    [Fact]
    public void BookmarkRightClickItem_AddsTheNodeThroughTheTreeView()
    {
        var treeView = new FakeTreeViewCommands();
        GlueCommands.Self.TreeViewCommands = treeView;
        var node = MakeEntityNode();

        GetBookmarkItem(node).Handler();

        treeView.Bookmarked.ShouldBe(new[] { node });
    }

    [Fact]
    public void BookmarkRightClickItem_DoesNothing_WhenTheTreeViewPluginIsNotLoaded()
    {
        GlueCommands.Self.TreeViewCommands = null;

        Should.NotThrow(() => GetBookmarkItem(MakeEntityNode()).Handler());
    }

    [Fact]
    public void CollisionRelationshipCommands_FixNamedObjectCollisionType_IsFalseForANonRelationship()
    {
        new CollisionRelationshipCommands().FixNamedObjectCollisionType(new NamedObjectSave { InstanceName = "Player" })
            .ShouldBeFalse();
    }

    private static ITreeNode MakeEntityNode()
    {
        var node = new Mock<ITreeNode> { CallBase = true };
        node.Setup(n => n.TreeNodeType).Returns(TreeNodeType.EntityNode);
        node.Setup(n => n.Tag).Returns(new EntitySave { Name = "Entities\\Player" });
        node.Setup(n => n.Text).Returns("Player");
        return node.Object;
    }

    private static RightClickItemDescriptor GetBookmarkItem(ITreeNode node) =>
        RightClickHelper.GetItemDescriptors(node, new Mock<IGlueState>().Object, MenuShowingAction.RegularRightClick)
            .Single(item => item.Text == "Bookmark");

    #endregion

    private class RecordingMarshaller : IUiThreadMarshaller
    {
        public int InvokeCount { get; private set; }
        public void Invoke(Action action) { InvokeCount++; action(); }
        public T Invoke<T>(Func<T> func) { InvokeCount++; return func(); }
        public Task Invoke(Func<Task> func) { InvokeCount++; return func(); }
        public Task<T> Invoke<T>(Func<Task<T>> func) { InvokeCount++; return func(); }
        public void BeginInvoke(Action action) => action();
    }

    private class FakePropertyGridCommands : IPropertyGridCommands
    {
        public int RefreshCount { get; private set; }
        public void RefreshVariables() => RefreshCount++;
    }

    private class FakeUndoCommands : IUndoCommands
    {
        public int UndoCount { get; private set; }
        public void Undo() => UndoCount++;
    }

    private class FakeTreeViewCommands : ITreeViewCommands
    {
        public List<ITreeNode> Bookmarked { get; } = new();
        public void AddBookmark(ITreeNode treeNode) => Bookmarked.Add(treeNode);
    }
}
