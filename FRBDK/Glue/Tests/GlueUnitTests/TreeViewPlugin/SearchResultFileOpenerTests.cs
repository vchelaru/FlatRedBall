using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using Moq;
using OfficialPlugins.TreeViewPlugin.ViewModels;
using Shouldly;
using System.Reflection;
using Xunit;

namespace GlueUnitTests.TreeViewPlugin;

/// <summary>
/// Tests for the OpenFileCommand on MainTreeViewViewModel.
/// [WpfFact] is required because NodeViewModel uses WPF types (BitmapImage, etc.)
/// in its constructors.
/// </summary>
public class SearchResultFileOpenerTests
{
    readonly Mock<IFileCommands> _mockFileCommands;
    readonly MainTreeViewViewModel _vm;

    public SearchResultFileOpenerTests()
    {
        if (AvailableAssetTypes.CommonAtis == null)
        {
            typeof(AvailableAssetTypes)
                .GetProperty(nameof(AvailableAssetTypes.CommonAtis))!
                .SetValue(null, new CommonAtis(), BindingFlags.NonPublic | BindingFlags.Static, null, null, null);
        }

        _mockFileCommands = new Mock<IFileCommands>();

        var mockGlueCommands = new Mock<IGlueCommands>();
        mockGlueCommands.Setup(x => x.FileCommands).Returns(_mockFileCommands.Object);

        _vm = new MainTreeViewViewModel(Mock.Of<IGlueState>(), mockGlueCommands.Object);
    }

    // 1: file node calls OpenReferencedFileInDefaultProgram
    [WpfFact]
    public void OpenFileCommand_FileNode_CallsOpenReferencedFileInDefaultProgram()
    {
        var rfs = new ReferencedFileSave { Name = "Content/MyFile.png" };
        var node = new NodeViewModel(TreeNodeType.ReferencedFileSaveNode) { Tag = rfs };

        _vm.OpenFileCommand.Execute(node);

        _mockFileCommands.Verify(x => x.OpenReferencedFileInDefaultProgram(rfs), Times.Once);
    }

    // 2: non-file node does not call OpenReferencedFileInDefaultProgram
    [WpfFact]
    public void OpenFileCommand_NonFileNode_DoesNotCallOpen()
    {
        var entity = new EntitySave { Name = "Entities\\Player" };
        var node = new NodeViewModel(TreeNodeType.EntityNode) { Tag = entity };

        _vm.OpenFileCommand.Execute(node);

        _mockFileCommands.Verify(x => x.OpenReferencedFileInDefaultProgram(It.IsAny<ReferencedFileSave>()), Times.Never);
    }

    // 3: null node does not throw
    [WpfFact]
    public void OpenFileCommand_NullNode_DoesNotThrow()
    {
        Should.NotThrow(() => _vm.OpenFileCommand.Execute(null));

        _mockFileCommands.Verify(x => x.OpenReferencedFileInDefaultProgram(It.IsAny<ReferencedFileSave>()), Times.Never);
    }
}
