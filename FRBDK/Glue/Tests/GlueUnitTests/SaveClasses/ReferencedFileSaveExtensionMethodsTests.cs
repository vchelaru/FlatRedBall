using System;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.TestSupport;
using Shouldly;

namespace GlueUnitTests.SaveClasses;

// GitHub issue #2018: toggling "Is Shared Static" on a global content file crashed Glue with a
// NullReferenceException. The real bug wasn't a missing null check - the checkbox is meaningless there.
// IsSharedStatic only has an effect for Screen-owned files: Entity-owned files are forced static (unique-
// instance optimization) and global content is always static (GlobalContentCodeGenerator never reads the
// flag) - see the comment in ReferencedFileSave's constructor. GetIsSharedStaticEditable is the single
// source of truth both property grids (the legacy WinForms displayer and the WPF "Settings (Preview)"
// grid) consult to decide whether to show the checkbox at all.
public class ReferencedFileSaveExtensionMethodsTests : IDisposable
{
    private readonly GlueProjectSave _originalGlueProject;

    public ReferencedFileSaveExtensionMethodsTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        _originalGlueProject = ObjectFinder.Self.GlueProject;
        ObjectFinder.Self.GlueProject = new GlueProjectSave();
    }

    public void Dispose()
    {
        ObjectFinder.Self.GlueProject = _originalGlueProject;
    }

    [Fact]
    public void GetIsSharedStaticEditable_ShouldBeTrue_ForScreenOwnedFile()
    {
        var screen = new ScreenSave { Name = "Screens/GameScreen/GameScreen" };
        var rfs = new ReferencedFileSave { Name = "Screens/GameScreen/Test.png" };
        screen.ReferencedFiles.Add(rfs);
        ObjectFinder.Self.GlueProject.Screens.Add(screen);

        rfs.GetIsSharedStaticEditable().ShouldBeTrue();
    }

    [Fact]
    public void GetIsSharedStaticEditable_ShouldBeFalse_ForEntityOwnedFile()
    {
        var entity = new EntitySave { Name = "Entities/Enemy/Enemy" };
        var rfs = new ReferencedFileSave { Name = "Entities/Enemy/Test.png" };
        entity.ReferencedFiles.Add(rfs);
        ObjectFinder.Self.GlueProject.Entities.Add(entity);

        rfs.GetIsSharedStaticEditable().ShouldBeFalse();
    }

    [Fact]
    public void GetIsSharedStaticEditable_ShouldBeFalse_ForGlobalContentFile()
    {
        var rfs = new ReferencedFileSave { Name = "GlobalContent/Test.png" };
        ObjectFinder.Self.GlueProject.GlobalFiles.Add(rfs);

        rfs.GetIsSharedStaticEditable().ShouldBeFalse();
    }

    // GitHub issue #2108: a file referenced by an entity or screen looked identical in the tree view
    // whether it lived in that element's own folder or was a reference to content owned by someone else
    // (GlobalContent, another element's folder, or loose in Content). GetIsLinkedOutsideContainerFolder is
    // the single source of truth the tree view consults to pick the link-badged icon.

    [Fact]
    public void GetIsLinkedOutsideContainerFolder_ShouldBeFalse_ForFileInEntityFolder()
    {
        var entity = new EntitySave { Name = "Entities\\Player" };
        var rfs = new ReferencedFileSave { Name = "Entities/Player/PlayerSheet.png" };
        entity.ReferencedFiles.Add(rfs);
        ObjectFinder.Self.GlueProject.Entities.Add(entity);

        rfs.GetIsLinkedOutsideContainerFolder().ShouldBeFalse();
    }

    [Fact]
    public void GetIsLinkedOutsideContainerFolder_ShouldBeFalse_ForFileInEntitySubfolder()
    {
        var entity = new EntitySave { Name = "Entities\\Player" };
        var rfs = new ReferencedFileSave { Name = "Entities/Player/Animations/Run.achx" };
        entity.ReferencedFiles.Add(rfs);
        ObjectFinder.Self.GlueProject.Entities.Add(entity);

        rfs.GetIsLinkedOutsideContainerFolder().ShouldBeFalse();
    }

    [Fact]
    public void GetIsLinkedOutsideContainerFolder_ShouldBeTrue_ForEntityFileInGlobalContent()
    {
        var entity = new EntitySave { Name = "Entities\\Player" };
        var rfs = new ReferencedFileSave { Name = "GlobalContent/SharedPalette.png" };
        entity.ReferencedFiles.Add(rfs);
        ObjectFinder.Self.GlueProject.Entities.Add(entity);

        rfs.GetIsLinkedOutsideContainerFolder().ShouldBeTrue();
    }

    [Fact]
    public void GetIsLinkedOutsideContainerFolder_ShouldBeTrue_ForEntityFileInAnotherEntitysFolder()
    {
        var entity = new EntitySave { Name = "Entities\\Player" };
        var rfs = new ReferencedFileSave { Name = "Entities/Enemy/Tileset.tsx" };
        entity.ReferencedFiles.Add(rfs);
        ObjectFinder.Self.GlueProject.Entities.Add(entity);

        rfs.GetIsLinkedOutsideContainerFolder().ShouldBeTrue();
    }

    [Fact]
    public void GetIsLinkedOutsideContainerFolder_ShouldBeFalse_ForFileInScreenFolder()
    {
        var screen = new ScreenSave { Name = "Screens\\GameScreen" };
        var rfs = new ReferencedFileSave { Name = "Screens/GameScreen/Level1.tmx" };
        screen.ReferencedFiles.Add(rfs);
        ObjectFinder.Self.GlueProject.Screens.Add(screen);

        rfs.GetIsLinkedOutsideContainerFolder().ShouldBeFalse();
    }

    [Fact]
    public void GetIsLinkedOutsideContainerFolder_ShouldBeTrue_ForScreenFileInGlobalContent()
    {
        var screen = new ScreenSave { Name = "Screens\\GameScreen" };
        var rfs = new ReferencedFileSave { Name = "GlobalContent/SharedPalette.png" };
        screen.ReferencedFiles.Add(rfs);
        ObjectFinder.Self.GlueProject.Screens.Add(screen);

        rfs.GetIsLinkedOutsideContainerFolder().ShouldBeTrue();
    }

    [Fact]
    public void GetIsLinkedOutsideContainerFolder_ShouldBeFalse_ForGlobalFileInGlobalContent()
    {
        var rfs = new ReferencedFileSave { Name = "GlobalContent/SharedPalette.png" };
        ObjectFinder.Self.GlueProject.GlobalFiles.Add(rfs);

        rfs.GetIsLinkedOutsideContainerFolder().ShouldBeFalse();
    }

    // Wildcard files are only ever global (WildcardReferencedFileSaveLogic pulls the patterns out of
    // GlobalFiles), but the pattern itself can point outside GlobalContent - "Entities/Enemy/*.png" - so
    // a file can legitimately be both wildcard-created and linked.
    [Fact]
    public void GetIsLinkedOutsideContainerFolder_ShouldBeTrue_ForGlobalFileOutsideGlobalContent()
    {
        var rfs = new ReferencedFileSave { Name = "Entities/Enemy/EnemySheet.png", IsCreatedByWildcard = true };
        ObjectFinder.Self.GlueProject.GlobalFiles.Add(rfs);

        rfs.GetIsLinkedOutsideContainerFolder().ShouldBeTrue();
    }

    [Fact]
    public void GetIsFileOutsideContainerFolder_ShouldBeTrue_ForSiblingFolderWithSharedPrefix()
    {
        // "Entities/PlayerBall" must not count as being inside "Entities/Player".
        ReferencedFileSaveExtensionMethods
            .GetIsFileOutsideContainerFolder("Entities/PlayerBall/Ball.png", "Entities\\Player")
            .ShouldBeTrue();
    }

    [Fact]
    public void GetIsFileOutsideContainerFolder_ShouldBeFalse_ForEmptyFileName()
    {
        ReferencedFileSaveExtensionMethods
            .GetIsFileOutsideContainerFolder("", "Entities\\Player")
            .ShouldBeFalse();
    }
}
