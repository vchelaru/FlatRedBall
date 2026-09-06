using System;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.TestSupport;
using OfficialPlugins.TreeViewPlugin.ViewModels;
using Shouldly;

namespace GlueUnitTests.TreeViewPlugin;

/// <summary>
/// Tests for NodeViewModel.GetIconFor, the single place the tree view picks a file node's icon.
/// GitHub issue #2108: a file referenced from outside its container's own content folder now gets a
/// link-badged icon so it can't be mistaken for content the element owns.
/// </summary>
public class NodeViewModelFileIconTests : IDisposable
{
    private readonly GlueProjectSave _originalGlueProject;

    public NodeViewModelFileIconTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        _originalGlueProject = ObjectFinder.Self.GlueProject;
        ObjectFinder.Self.GlueProject = new GlueProjectSave();
    }

    public void Dispose()
    {
        ObjectFinder.Self.GlueProject = _originalGlueProject;
    }

    private static ReferencedFileSave AddEntityFile(string entityName, string fileName, bool isCreatedByWildcard = false)
    {
        var entity = new EntitySave { Name = entityName };
        var rfs = new ReferencedFileSave { Name = fileName, IsCreatedByWildcard = isCreatedByWildcard };
        entity.ReferencedFiles.Add(rfs);
        ObjectFinder.Self.GlueProject.Entities.Add(entity);
        return rfs;
    }

    [Fact]
    public void GetIconFor_ShouldBeStandardIcon_ForFileInEntityFolder()
    {
        var rfs = AddEntityFile("Entities\\Player", "Entities/Player/PlayerSheet.png");

        NodeViewModel.GetIconFor(rfs).ShouldBe(NodeViewModel.FileIcon);
    }

    [Fact]
    public void GetIconFor_ShouldBeLinkIcon_ForFileOutsideEntityFolder()
    {
        var rfs = AddEntityFile("Entities\\Player", "GlobalContent/SharedPalette.png");

        NodeViewModel.GetIconFor(rfs).ShouldBe(NodeViewModel.FileIconLink);
    }

    [Fact]
    public void GetIconFor_ShouldBeWildcardIcon_ForWildcardFileInGlobalContent()
    {
        var rfs = new ReferencedFileSave { Name = "GlobalContent/Tiles.png", IsCreatedByWildcard = true };
        ObjectFinder.Self.GlueProject.GlobalFiles.Add(rfs);

        NodeViewModel.GetIconFor(rfs).ShouldBe(NodeViewModel.FileIconWildcard);
    }

    [Fact]
    public void GetIconFor_ShouldBeWildcardLinkIcon_ForWildcardFileOutsideGlobalContent()
    {
        // A wildcard pattern like "Entities/Enemy/*.png" lives in GlobalFiles but matches files outside
        // GlobalContent, so both badges apply at once.
        var rfs = new ReferencedFileSave { Name = "Entities/Enemy/EnemySheet.png", IsCreatedByWildcard = true };
        ObjectFinder.Self.GlueProject.GlobalFiles.Add(rfs);

        NodeViewModel.GetIconFor(rfs).ShouldBe(NodeViewModel.FileIconWildcardLink);
    }

    [Fact]
    public void GetIconFor_ShouldBeStandardIcon_ForNullFile()
    {
        NodeViewModel.GetIconFor(null!).ShouldBe(NodeViewModel.FileIcon);
    }

    // The tree view's two loops already know the container, so they call the overload that takes it
    // rather than making ObjectFinder search every element to rediscover it.

    [Fact]
    public void GetIconForWithContainer_ShouldMatchTheLookupOverload_ForEntityOwnedFiles()
    {
        var entity = new EntitySave { Name = "Entities\\Player" };
        var localFile = new ReferencedFileSave { Name = "Entities/Player/PlayerSheet.png" };
        var linkedFile = new ReferencedFileSave { Name = "GlobalContent/SharedPalette.png" };

        NodeViewModel.GetIconFor(localFile, entity).ShouldBe(NodeViewModel.FileIcon);
        NodeViewModel.GetIconFor(linkedFile, entity).ShouldBe(NodeViewModel.FileIconLink);
    }

    [Fact]
    public void GetIconForWithContainer_ShouldTreatNullContainerAsGlobalContent()
    {
        var localFile = new ReferencedFileSave { Name = "GlobalContent/SharedPalette.png" };
        var linkedFile = new ReferencedFileSave { Name = "Entities/Player/PlayerSheet.png" };

        NodeViewModel.GetIconFor(localFile, null).ShouldBe(NodeViewModel.FileIcon);
        NodeViewModel.GetIconFor(linkedFile, null).ShouldBe(NodeViewModel.FileIconLink);
    }
}
