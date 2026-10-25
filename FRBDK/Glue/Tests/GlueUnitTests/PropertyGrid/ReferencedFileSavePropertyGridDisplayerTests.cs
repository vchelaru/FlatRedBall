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

    // GitHub issue #2344: the legacy grid's container/extension/state rules moved into
    // ReferencedFileSave.GetHiddenMemberNames so the WPF grid could share them. These pin that the
    // legacy grid still hides CSV members on a non-CSV, still shows them on a CSV, and still follows
    // the shared rule set exactly.

    private static ReferencedFileSavePropertyGridDisplayer ShowFile(string name, bool inEntity = true)
    {
        var rfs = new ReferencedFileSave { Name = name };
        if (inEntity)
        {
            var entity = new EntitySave { Name = "Entities/ObjectBall/ObjectBall" };
            entity.ReferencedFiles.Add(rfs);
            ObjectFinder.Self.GlueProject.Entities.Add(entity);
        }
        else
        {
            ObjectFinder.Self.GlueProject.GlobalFiles.Add(rfs);
        }

        return new ReferencedFileSavePropertyGridDisplayer { Instance = rfs };
    }

    [Fact]
    public void CsvMembers_ShouldBeHidden_ForAchx()
    {
        var displayer = ShowFile("Entities/ObjectBall/BallColor.achx");

        displayer.GetPropertyGridMember(nameof(ReferencedFileSave.CreatesDictionary)).ShouldBeNull();
        displayer.GetPropertyGridMember(nameof(ReferencedFileSave.CsvDelimiter)).ShouldBeNull();
        displayer.GetPropertyGridMember(nameof(ReferencedFileSave.TreatAsCsv)).ShouldBeNull();
        displayer.GetPropertyGridMember(nameof(ReferencedFileSave.UniformRowType)).ShouldBeNull();
        displayer.GetPropertyGridMember(nameof(ReferencedFileSave.IsDatabaseForLocalizing)).ShouldBeNull();
    }

    [Fact]
    public void CsvMembers_ShouldBeShown_ForCsv()
    {
        var displayer = ShowFile("GlobalContent/Data.csv", inEntity: false);

        displayer.GetPropertyGridMember(nameof(ReferencedFileSave.CreatesDictionary)).ShouldNotBeNull();
        displayer.GetPropertyGridMember(nameof(ReferencedFileSave.CsvDelimiter)).ShouldNotBeNull();
        displayer.GetPropertyGridMember(nameof(ReferencedFileSave.UniformRowType)).ShouldNotBeNull();
        displayer.GetPropertyGridMember(nameof(ReferencedFileSave.IsDatabaseForLocalizing)).ShouldNotBeNull();
        displayer.GetPropertyGridMember(nameof(ReferencedFileSave.TreatAsCsv)).ShouldBeNull();
    }

    [Fact]
    public void TreatAsCsv_ShouldBeOffered_OnlyForTxt()
    {
        ShowFile("GlobalContent/Notes.txt", inEntity: false)
            .GetPropertyGridMember(nameof(ReferencedFileSave.TreatAsCsv)).ShouldNotBeNull();
        ShowFile("GlobalContent/Data.csv", inEntity: false)
            .GetPropertyGridMember(nameof(ReferencedFileSave.TreatAsCsv)).ShouldBeNull();
    }

    [Fact]
    public void Members_ShouldFollowTheSharedRuleSet_ForSeveralFileKinds()
    {
        var files = new[]
        {
            ("Entities/ObjectBall/BallColor.achx", true),
            ("GlobalContent/Data.csv", false),
            ("GlobalContent/Notes.txt", false),
            ("GlobalContent/Texture.png", false),
        };

        foreach (var (name, inEntity) in files)
        {
            var displayer = ShowFile(name, inEntity);
            foreach (var hidden in ((ReferencedFileSave)displayer.Instance).GetHiddenMemberNames())
            {
                displayer.GetPropertyGridMember(hidden).ShouldBeNull($"{name}: {hidden}");
            }
        }
    }
}
