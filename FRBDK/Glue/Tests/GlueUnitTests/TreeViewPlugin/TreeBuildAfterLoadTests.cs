using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.Tasks;
using GlueUnitTests.TestSupport;
using OfficialPlugins.TreeViewPlugin;
using Shouldly;

namespace GlueUnitTests.TreeViewPlugin;

/// <summary>
/// The project load runs on the UI thread, so the tree view posts its full build to run after the load
/// returns instead of building inside it. These pin the post, the project-closed-first case, and that a
/// refresh posted later still lands on the built tree.
/// </summary>
[Collection(nameof(TaskManagerSequentialCollection))]
public class TreeBuildAfterLoadTests : IDisposable
{
    readonly IUiThreadMarshaller _originalMarshaller;
    readonly GlueProjectSave _originalProject;
    readonly IFindManager _originalFind;
    readonly QueueingMarshaller _marshaller = new();
    readonly EntitySave _bear = new() { Name = @"Entities\Bear" };

    public TreeBuildAfterLoadTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        _originalMarshaller = TaskManager.UiThreadMarshaller;
        _originalProject = ObjectFinder.Self.GlueProject;
        _originalFind = GlueState.Self.Find;

        var project = new GlueProjectSave();
        project.Entities.Add(_bear);
        ObjectFinder.Self.GlueProject = project;
        TaskManager.UiThreadMarshaller = _marshaller;
    }

    public void Dispose()
    {
        TaskManager.UiThreadMarshaller = _originalMarshaller;
        ObjectFinder.Self.GlueProject = _originalProject;
        GlueState.Self.Find = _originalFind;
    }

    /// <summary>Runs Invoke inline and holds BeginInvoke posts until the test runs them, like a busy UI thread.</summary>
    class QueueingMarshaller : IUiThreadMarshaller
    {
        public readonly Queue<Action> Posted = new();
        public void Invoke(Action action) => action();
        public T Invoke<T>(Func<T> func) => func();
        public Task Invoke(Func<Task> func) => func();
        public Task<T> Invoke<T>(Func<Task<T>> func) => func();
        public void BeginInvoke(Action action) => Posted.Enqueue(action);
        public void RunPosted() { while (Posted.Count > 0) Posted.Dequeue()(); }
    }

    MainTreeViewPlugin StartPlugin()
    {
        var plugin = new MainTreeViewPlugin();
        plugin.StartUp();
        return plugin;
    }

    [StaFact]
    public void LoadedProject_BuildsTheTreeInAPost_NotInline()
    {
        var plugin = StartPlugin();

        plugin.ReactToLoadedGlux();

        GlueState.Self.Find.TreeNodeByTag(_bear).ShouldBeNull("the build must not run inside the load");
        _marshaller.Posted.Count.ShouldBe(1);

        _marshaller.RunPosted();

        GlueState.Self.Find.TreeNodeByTag(_bear).ShouldNotBeNull();
    }

    [StaFact]
    public void ProjectClosedBeforeThePostRuns_BuildsNothing()
    {
        var plugin = StartPlugin();
        plugin.ReactToLoadedGlux();

        ObjectFinder.Self.GlueProject = null;
        _marshaller.RunPosted();

        GlueState.Self.Find.TreeNodeByTag(_bear).ShouldBeNull();
    }

    [StaFact]
    public void ARefreshPostedAfterTheLoad_LandsOnTheBuiltTree()
    {
        // A task queued during the load marshals its refresh after the build was posted, so the
        // refresh finds the built node and updates it instead of meeting an empty tree.
        var plugin = StartPlugin();
        plugin.ReactToLoadedGlux();
        _marshaller.BeginInvoke(() => plugin.RefreshTreeNodeFor(_bear, TreeNodeRefreshType.All));

        _marshaller.RunPosted();

        var node = GlueState.Self.Find.TreeNodeByTag(_bear);
        node.ShouldNotBeNull();
        node.Parent.Children.Count(child => child.Tag == _bear).ShouldBe(1);
    }
}
