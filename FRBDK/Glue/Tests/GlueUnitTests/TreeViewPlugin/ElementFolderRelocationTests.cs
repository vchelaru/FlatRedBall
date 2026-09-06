using System;
using System.IO;
using System.Linq;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.TestSupport;
using Moq;
using OfficialPlugins.TreeViewPlugin.Models;
using OfficialPlugins.TreeViewPlugin.ViewModels;
using Shouldly;

namespace GlueUnitTests.TreeViewPlugin;

/// <summary>
/// Renaming an element into a folder that does not exist yet - typing "NewFolder/Ball" into the tree's
/// inline rename - creates that folder on disk as part of the rename. The tree has to show the element
/// in it.
/// </summary>
/// <remarks>
/// Folder nodes come from <c>Directory.GetDirectories</c>, which only ran on project load, so the node
/// the element needs to move into did not exist and the relocation in
/// <c>MainTreeViewViewModel.RefreshTreeNodeFor</c> was skipped. The element stayed under its old parent
/// while its files, .glsj/.glej and namespace had all moved - the tree was the only thing saying
/// otherwise, until the project was reopened.
/// </remarks>
public class ElementFolderRelocationTests : IDisposable
{
    readonly string _root;
    readonly string _originalRelativeDirectory;
    readonly GlueProjectSave _originalGlueProject;
    readonly GlueProjectSave _project = new();
    readonly MainTreeViewViewModel _viewModel;

    public ElementFolderRelocationTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _root = Path.Combine(Path.GetTempPath(), "TreeViewRelocate_" + Guid.NewGuid()) +
            Path.DirectorySeparatorChar;
        Directory.CreateDirectory(Path.Combine(_root, "Entities"));

        // Node refreshes read the static singleton rather than the injected mock, so without one this
        // passes mid-suite off whatever an earlier test left behind and NREs on its own.
        _originalGlueProject = ObjectFinder.Self.GlueProject;
        ObjectFinder.Self.GlueProject = _project;

        _originalRelativeDirectory = FlatRedBall.IO.FileManager.RelativeDirectory;
        FlatRedBall.IO.FileManager.RelativeDirectory = _root;

        var glueState = new Mock<IGlueState>();
        glueState.Setup(item => item.CurrentGlueProject).Returns(_project);
        glueState.Setup(item => item.CurrentGlueProjectDirectory).Returns(_root);
        glueState.Setup(item => item.ContentDirectory).Returns(_root);

        var glueCommands = new Mock<IGlueCommands>();
        glueCommands
            .Setup(item => item.GetAbsoluteFileName(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns((string relative, bool _) => Path.Combine(_root, relative ?? string.Empty));

        _viewModel = new MainTreeViewViewModel(glueState.Object, glueCommands.Object);
    }

    public void Dispose()
    {
        FlatRedBall.IO.FileManager.RelativeDirectory = _originalRelativeDirectory;
        ObjectFinder.Self.GlueProject = _originalGlueProject;

        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    [StaFact]
    public void RefreshTreeNodeFor_MovesTheElementIntoAFolderCreatedSinceTheProjectLoaded()
    {
        var entity = AddEntityAtRoot("Entities\\Ball");

        // What the rename does before the tree is refreshed: the folder appears on disk and the
        // element's name gains it.
        Directory.CreateDirectory(Path.Combine(_root, "Entities", "NewFolder"));
        entity.Name = "Entities\\NewFolder\\Ball";

        _viewModel.RefreshTreeNodeFor(entity, TreeNodeRefreshType.All);

        var folderNode = _viewModel.EntityRootNode.Children.FirstOrDefault(node => node.Text == "NewFolder");

        folderNode.ShouldNotBeNull("the folder the rename created should have a node");
        folderNode.Children.ShouldContain(node => node.Tag == entity,
            "the element should have moved into the folder its name now names");
        _viewModel.EntityRootNode.Children.ShouldNotContain(node => node.Tag == entity,
            "the element should no longer sit beside the folder it moved into");
    }

    [StaFact]
    public void RefreshTreeNodeFor_StillMovesTheElement_WhenTheFolderNodeAlreadyExists()
    {
        // The guard against fixing this by always re-scanning and losing the relocation itself: a folder
        // that was already on the tree still has to receive the element.
        var entity = AddEntityAtRoot("Entities\\Ball");

        Directory.CreateDirectory(Path.Combine(_root, "Entities", "Existing"));
        _viewModel.AddDirectoryNodes(_root + "Entities/", _viewModel.EntityRootNode, ScreenOrEntity.Entity);

        entity.Name = "Entities\\Existing\\Ball";

        _viewModel.RefreshTreeNodeFor(entity, TreeNodeRefreshType.All);

        _viewModel.EntityRootNode.Children
            .First(node => node.Text == "Existing")
            .Children.ShouldContain(node => node.Tag == entity);
    }

    EntitySave AddEntityAtRoot(string name)
    {
        var entity = new EntitySave { Name = name };
        _project.Entities.Add(entity);

        var node = new GlueElementNodeViewModel(_viewModel.EntityRootNode, entity, createChildrenNodes: false);
        _viewModel.EntityRootNode.Children.Add(node);

        return entity;
    }
}
