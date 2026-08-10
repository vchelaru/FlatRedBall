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
/// Folder nodes under Screens/Entities come from Directory.GetDirectories, and elements come from the
/// .gluj - two passes that never see each other. In FRB1 they cannot collide, because a screen's
/// definition and its content folder hang off different roots (CurrentGlueProjectDirectory vs
/// ContentDirectory). An FRB2 project puts everything Glue authors under one folder, so
/// Screens/NewScreen.glsj and the content folder Screens/NewScreen/ become siblings and the screen
/// gets a second, duplicate node.
/// </summary>
public class DirectoryNodeCollisionTests : IDisposable
{
    readonly string _root;
    readonly string _originalRelativeDirectory;
    readonly GlueProjectSave _originalGlueProject;
    readonly MainTreeViewViewModel _viewModel;

    public DirectoryNodeCollisionTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _root = Path.Combine(Path.GetTempPath(), "TreeViewCollision_" + Guid.NewGuid()) +
            Path.DirectorySeparatorChar;
        Directory.CreateDirectory(Path.Combine(_root, "Screens"));

        // AddScreenTreeNode finishes by refreshing the new node, which reads
        // GlueState.Self.CurrentGlueProject.StartUpScreen off the static singleton - not the injected
        // mock. Without one this test passes mid-suite (some earlier test left a project behind) and
        // NREs on its own.
        _originalGlueProject = ObjectFinder.Self.GlueProject;
        ObjectFinder.Self.GlueProject = new GlueProjectSave();

        // AddScreenTreeNode resolves the element's containing folder with FileManager.MakeRelative,
        // which reads the process-wide RelativeDirectory. Left to whatever ran before, this test
        // passes mid-suite and fails on its own.
        _originalRelativeDirectory = FlatRedBall.IO.FileManager.RelativeDirectory;
        FlatRedBall.IO.FileManager.RelativeDirectory = _root;

        var glueCommands = new Mock<IGlueCommands>();
        // Only reached by the orphan-cleanup pass, which asks where a directory node points so it can
        // drop nodes whose directory is gone. Answering with the real path keeps live nodes alive.
        glueCommands
            .Setup(item => item.GetAbsoluteFileName(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns((string relative, bool _) => Path.Combine(_root, relative ?? string.Empty));

        _viewModel = new MainTreeViewViewModel(new Mock<IGlueState>().Object, glueCommands.Object);
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
    public void AddDirectoryNodes_DoesNotAddASecondNode_WhenAScreenAlreadyOwnsThatName()
    {
        Directory.CreateDirectory(Path.Combine(_root, "Screens", "NewScreen"));
        AddScreenNode("Screens\\NewScreen");

        _viewModel.AddDirectoryNodes(_root + "Screens/", _viewModel.ScreenRootNode, ScreenOrEntity.Screen);

        _viewModel.ScreenRootNode.Children
            .Count(node => node.Text == "NewScreen")
            .ShouldBe(1, "The screen's own content folder should not appear beside the screen.");
    }

    [StaFact]
    public void AddDirectoryNodes_StillAddsAFolderNode_WhenNoElementOwnsThatName()
    {
        // A folder the user made to organize screens is not owned by any element, so it still shows.
        Directory.CreateDirectory(Path.Combine(_root, "Screens", "Levels"));

        _viewModel.AddDirectoryNodes(_root + "Screens/", _viewModel.ScreenRootNode, ScreenOrEntity.Screen);

        _viewModel.ScreenRootNode.Children
            .Count(node => node.Text == "Levels")
            .ShouldBe(1);
    }

    [StaFact]
    public void AddDirectoryNodes_ThenTheElement_LeavesOnlyTheElementNode()
    {
        // The other order, and the one the screenshot showed: directories are enumerated on glux load
        // before any element node exists, so the skip-check has nothing to defer to and the folder node
        // is created. The element pass has to clear it when the element arrives.
        Directory.CreateDirectory(Path.Combine(_root, "Screens", "NewScreen"));

        _viewModel.AddDirectoryNodes(_root + "Screens/", _viewModel.ScreenRootNode, ScreenOrEntity.Screen);
        _viewModel.AddScreenTreeNode(new ScreenSave { Name = "Screens\\NewScreen" });

        _viewModel.ScreenRootNode.Children
            .Count(node => node.Text == "NewScreen")
            .ShouldBe(1, "The folder node should have been replaced by the screen, not kept beside it.");
    }

    void AddScreenNode(string screenName)
    {
        var screen = new ScreenSave { Name = screenName };
        var node = new GlueElementNodeViewModel(_viewModel.ScreenRootNode, screen, createChildrenNodes: false);
        _viewModel.ScreenRootNode.Children.Add(node);
    }
}
