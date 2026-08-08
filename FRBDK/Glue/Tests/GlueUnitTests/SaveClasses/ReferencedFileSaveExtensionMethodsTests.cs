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
}
