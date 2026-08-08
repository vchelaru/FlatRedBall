using System;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers.PropertyGrids;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.TestSupport;
using Shouldly;

namespace GlueUnitTests.PropertyGrid;

// GitHub issue #2018: toggling "Is Shared Static" on a global content file crashed Glue. IsSharedStatic
// is meaningless outside a Screen (see ReferencedFileSaveExtensionMethodsTests), so it shouldn't be
// shown as an editable property at all for Entity-owned or global content files - not just guarded
// against crashing when changed.
public class ReferencedFileSavePropertyGridDisplayerTests : IDisposable
{
    private readonly GlueProjectSave _originalGlueProject;

    public ReferencedFileSavePropertyGridDisplayerTests()
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
    public void IsSharedStatic_ShouldBeShown_ForScreenOwnedFile()
    {
        var screen = new ScreenSave { Name = "Screens/GameScreen/GameScreen" };
        var rfs = new ReferencedFileSave { Name = "Screens/GameScreen/Test.png" };
        screen.ReferencedFiles.Add(rfs);
        ObjectFinder.Self.GlueProject.Screens.Add(screen);

        var displayer = new ReferencedFileSavePropertyGridDisplayer();
        displayer.Instance = rfs;

        displayer.GetPropertyGridMember(nameof(ReferencedFileSave.IsSharedStatic)).ShouldNotBeNull();
    }

    [Fact]
    public void IsSharedStatic_ShouldBeHidden_ForEntityOwnedFile()
    {
        var entity = new EntitySave { Name = "Entities/Enemy/Enemy" };
        var rfs = new ReferencedFileSave { Name = "Entities/Enemy/Test.png" };
        entity.ReferencedFiles.Add(rfs);
        ObjectFinder.Self.GlueProject.Entities.Add(entity);

        var displayer = new ReferencedFileSavePropertyGridDisplayer();
        displayer.Instance = rfs;

        displayer.GetPropertyGridMember(nameof(ReferencedFileSave.IsSharedStatic)).ShouldBeNull();
    }

    [Fact]
    public void IsSharedStatic_ShouldBeHidden_ForGlobalContentFile()
    {
        var rfs = new ReferencedFileSave { Name = "GlobalContent/Test.png" };
        ObjectFinder.Self.GlueProject.GlobalFiles.Add(rfs);

        var displayer = new ReferencedFileSavePropertyGridDisplayer();
        displayer.Instance = rfs;

        displayer.GetPropertyGridMember(nameof(ReferencedFileSave.IsSharedStatic)).ShouldBeNull();
    }
}
