using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;

namespace GlueCommonUnitTests.SaveClasses;

// GitHub issue #2344: the WPF "Settings (Preview)" grid showed every ReferencedFileSave member (CSV
// settings on an .achx, etc.) because only the legacy WinForms grid applied the container / extension /
// state rules. GetHiddenMemberNames is the one place those rules live; both grids consume it.
// Reads the shared statics ObjectFinderCore.Self and AvailableAssetTypesCore.Self - hence the shared
// collection (see ObjectFinderCoreCollection in NamedObjectSaveElementExtensionsTests.cs).
[Collection(nameof(ObjectFinderCoreCollection))]
public class ReferencedFileSaveHiddenMembersTests
{
    const string SongType = "Microsoft.Xna.Framework.Media.Song";

    readonly FakeObjectFinderCore _finder = new();
    readonly FakeAvailableAssetTypesCore _assetTypes = new();

    public ReferencedFileSaveHiddenMembersTests()
    {
        ObjectFinderCore.Self = _finder;
        AvailableAssetTypesCore.Self = _assetTypes;
        _finder.GlueProject = new GlueProjectSave();
    }

    ReferencedFileSave InEntity(string name)
    {
        var rfs = new ReferencedFileSave { Name = name };
        _finder.SetContainer(rfs, new EntitySave { Name = "Entities\\ObjectBall" });
        return rfs;
    }

    ReferencedFileSave InScreen(string name)
    {
        var rfs = new ReferencedFileSave { Name = name };
        _finder.SetContainer(rfs, new ScreenSave { Name = "Screens\\GameScreen" });
        return rfs;
    }

    static ReferencedFileSave Global(string name) => new ReferencedFileSave { Name = name };

    static readonly string[] CsvOnlyMembers =
    {
        nameof(ReferencedFileSave.CreatesDictionary),
        nameof(ReferencedFileSave.IsDatabaseForLocalizing),
        nameof(ReferencedFileSave.UniformRowType),
        nameof(ReferencedFileSave.CsvDelimiter),
        nameof(ReferencedFileSave.TreatAsCsv),
    };

    [Fact]
    public void Achx_HidesEveryCsvMember()
    {
        var hidden = InEntity("Entities/ObjectBall/BallColor.achx").GetHiddenMemberNames();

        foreach (var member in CsvOnlyMembers)
        {
            Assert.Contains(member, hidden);
        }
    }

    [Fact]
    public void AnyFile_HidesComputedIsCsvOrTreatedAsCsv()
    {
        Assert.Contains(nameof(ReferencedFileSave.IsCsvOrTreatedAsCsv), InEntity("a.achx").GetHiddenMemberNames());
        Assert.Contains(nameof(ReferencedFileSave.IsCsvOrTreatedAsCsv), InEntity("a.csv").GetHiddenMemberNames());
        Assert.Contains(nameof(ReferencedFileSave.IsCsvOrTreatedAsCsv), Global("a.txt").GetHiddenMemberNames());
    }

    [Fact]
    public void Achx_KeepsMembersThatApply()
    {
        var hidden = InEntity("Entities/ObjectBall/BallColor.achx").GetHiddenMemberNames();

        Assert.DoesNotContain(nameof(ReferencedFileSave.Name), hidden);
        Assert.DoesNotContain(nameof(ReferencedFileSave.Summary), hidden);
        Assert.DoesNotContain(nameof(ReferencedFileSave.LoadedAtRuntime), hidden);
        Assert.DoesNotContain(nameof(ReferencedFileSave.AddToManagers), hidden);
        Assert.DoesNotContain(nameof(ReferencedFileSave.SourceFile), hidden);
    }

    [Fact]
    public void Csv_ShowsCsvMembersExceptTreatAsCsv()
    {
        var hidden = Global("GlobalContent/Data.csv").GetHiddenMemberNames();

        Assert.DoesNotContain(nameof(ReferencedFileSave.CreatesDictionary), hidden);
        Assert.DoesNotContain(nameof(ReferencedFileSave.IsDatabaseForLocalizing), hidden);
        Assert.DoesNotContain(nameof(ReferencedFileSave.UniformRowType), hidden);
        Assert.DoesNotContain(nameof(ReferencedFileSave.CsvDelimiter), hidden);
        Assert.Contains(nameof(ReferencedFileSave.TreatAsCsv), hidden);
    }

    [Fact]
    public void Txt_NotTreatedAsCsv_HidesCsvMembersButOffersTreatAsCsv()
    {
        var hidden = Global("GlobalContent/Notes.txt").GetHiddenMemberNames();

        Assert.Contains(nameof(ReferencedFileSave.CreatesDictionary), hidden);
        Assert.Contains(nameof(ReferencedFileSave.IsDatabaseForLocalizing), hidden);
        Assert.Contains(nameof(ReferencedFileSave.UniformRowType), hidden);
        Assert.Contains(nameof(ReferencedFileSave.CsvDelimiter), hidden);
        Assert.DoesNotContain(nameof(ReferencedFileSave.TreatAsCsv), hidden);
    }

    [Fact]
    public void Txt_TreatedAsCsv_ShowsCsvMembers()
    {
        var rfs = Global("GlobalContent/Notes.txt");
        rfs.TreatAsCsv = true;

        var hidden = rfs.GetHiddenMemberNames();

        Assert.DoesNotContain(nameof(ReferencedFileSave.CreatesDictionary), hidden);
        Assert.DoesNotContain(nameof(ReferencedFileSave.IsDatabaseForLocalizing), hidden);
        Assert.DoesNotContain(nameof(ReferencedFileSave.UniformRowType), hidden);
        Assert.DoesNotContain(nameof(ReferencedFileSave.CsvDelimiter), hidden);
        Assert.DoesNotContain(nameof(ReferencedFileSave.TreatAsCsv), hidden);
    }

    [Fact]
    public void IsManuallyUpdated_HiddenForEntityFile_EvenIfAssetTypeSupportsIt()
    {
        _assetTypes.AddAssetType(new AssetTypeInfo { Extension = "scnx", MakeManuallyUpdatedMethod = "ManuallyUpdate" });

        Assert.Contains(nameof(ReferencedFileSave.IsManuallyUpdated), InEntity("a.scnx").GetHiddenMemberNames());
        Assert.DoesNotContain(nameof(ReferencedFileSave.IsManuallyUpdated), InScreen("a.scnx").GetHiddenMemberNames());
        Assert.DoesNotContain(nameof(ReferencedFileSave.IsManuallyUpdated), Global("a.scnx").GetHiddenMemberNames());
    }

    [Fact]
    public void IsManuallyUpdated_HiddenWhenAssetTypeHasNoMakeManuallyUpdatedMethod()
    {
        _assetTypes.AddAssetType(new AssetTypeInfo { Extension = "png" });

        Assert.Contains(nameof(ReferencedFileSave.IsManuallyUpdated), InScreen("a.png").GetHiddenMemberNames());
        Assert.Contains(nameof(ReferencedFileSave.IsManuallyUpdated), InScreen("a.unknown").GetHiddenMemberNames());
    }

    [Fact]
    public void IsSharedStatic_OnlyShownForScreenFiles()
    {
        Assert.DoesNotContain(nameof(ReferencedFileSave.IsSharedStatic), InScreen("a.png").GetHiddenMemberNames());
        Assert.Contains(nameof(ReferencedFileSave.IsSharedStatic), InEntity("a.png").GetHiddenMemberNames());
        Assert.Contains(nameof(ReferencedFileSave.IsSharedStatic), Global("a.png").GetHiddenMemberNames());
    }

    [Fact]
    public void DestroyOnUnload_OnlyShownForSongOutsideEntities()
    {
        _assetTypes.AddAssetType(new AssetTypeInfo
        {
            Extension = "mp3",
            QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = SongType }
        });
        _assetTypes.AddAssetType(new AssetTypeInfo
        {
            Extension = "png",
            QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = "Microsoft.Xna.Framework.Graphics.Texture2D" }
        });

        Assert.DoesNotContain(nameof(ReferencedFileSave.DestroyOnUnload), InScreen("a.mp3").GetHiddenMemberNames());
        Assert.DoesNotContain(nameof(ReferencedFileSave.DestroyOnUnload), Global("a.mp3").GetHiddenMemberNames());
        Assert.Contains(nameof(ReferencedFileSave.DestroyOnUnload), InEntity("a.mp3").GetHiddenMemberNames());
        Assert.Contains(nameof(ReferencedFileSave.DestroyOnUnload), InScreen("a.png").GetHiddenMemberNames());
        Assert.Contains(nameof(ReferencedFileSave.DestroyOnUnload), InScreen("a.unknown").GetHiddenMemberNames());
    }

    [Fact]
    public void UseContentPipeline_OnlyShownWhenAssetTypeHasContentProcessor()
    {
        _assetTypes.AddAssetType(new AssetTypeInfo { Extension = "png", ContentProcessor = "TextureProcessor" });
        _assetTypes.AddAssetType(new AssetTypeInfo { Extension = "achx" });

        Assert.DoesNotContain(nameof(ReferencedFileSave.UseContentPipeline), InEntity("a.png").GetHiddenMemberNames());
        Assert.Contains(nameof(ReferencedFileSave.UseContentPipeline), InEntity("a.achx").GetHiddenMemberNames());
    }

    [Fact]
    public void NotLoadedAtRuntime_HidesRuntimeOnlyMembers()
    {
        var rfs = InScreen("a.png");
        rfs.LoadedAtRuntime = false;

        var hidden = rfs.GetHiddenMemberNames();

        Assert.Contains(nameof(ReferencedFileSave.IsSharedStatic), hidden);
        Assert.Contains(nameof(ReferencedFileSave.IsManuallyUpdated), hidden);
        Assert.Contains(nameof(ReferencedFileSave.LoadedOnlyWhenReferenced), hidden);
        Assert.Contains(nameof(ReferencedFileSave.HasPublicProperty), hidden);
        Assert.Contains(nameof(ReferencedFileSave.IncludeDirectoryRelativeToContainer), hidden);
        // LoadedAtRuntime itself must stay, or it could never be turned back on.
        Assert.DoesNotContain(nameof(ReferencedFileSave.LoadedAtRuntime), hidden);
    }

    [Fact]
    public void LoadedAtRuntime_KeepsRuntimeOnlyMembers()
    {
        var hidden = InScreen("a.png").GetHiddenMemberNames();

        Assert.DoesNotContain(nameof(ReferencedFileSave.LoadedOnlyWhenReferenced), hidden);
        Assert.DoesNotContain(nameof(ReferencedFileSave.HasPublicProperty), hidden);
        Assert.DoesNotContain(nameof(ReferencedFileSave.IncludeDirectoryRelativeToContainer), hidden);
    }

    [Fact]
    public void BuildMembers_HiddenWhenSourceFileEmpty_ShownWhenSet()
    {
        var rfs = InEntity("a.png");

        var hidden = rfs.GetHiddenMemberNames();
        Assert.Contains(nameof(ReferencedFileSave.BuildTool), hidden);
        Assert.Contains(nameof(ReferencedFileSave.AdditionalArguments), hidden);
        Assert.Contains(nameof(ReferencedFileSave.ConditionalCompilationSymbols), hidden);
        Assert.DoesNotContain(nameof(ReferencedFileSave.SourceFile), hidden);

        rfs.SourceFile = "Source/a.psd";
        hidden = rfs.GetHiddenMemberNames();
        Assert.DoesNotContain(nameof(ReferencedFileSave.BuildTool), hidden);
        Assert.DoesNotContain(nameof(ReferencedFileSave.AdditionalArguments), hidden);
        Assert.DoesNotContain(nameof(ReferencedFileSave.ConditionalCompilationSymbols), hidden);
    }
}
