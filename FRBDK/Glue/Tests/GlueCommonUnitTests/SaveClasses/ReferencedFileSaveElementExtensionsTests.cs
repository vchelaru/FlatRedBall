using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;

namespace GlueCommonUnitTests.SaveClasses;

// GetContainer/GetContainerType/ReferencedFileSaveToString/GetIsSharedStaticEditable/
// GetIsLinkedOutsideContainerFolder all read the shared static ObjectFinderCore.Self, so this can't run
// concurrently with any other test class that swaps it out - hence the shared collection (see
// ObjectFinderCoreCollection in NamedObjectSaveElementExtensionsTests.cs).
[Collection(nameof(ObjectFinderCoreCollection))]
public class ReferencedFileSaveElementExtensionsTests
{
    readonly FakeObjectFinderCore _finder = new();

    public ReferencedFileSaveElementExtensionsTests()
    {
        ObjectFinderCore.Self = _finder;
    }

    [Fact]
    public void GetContainer_GlueProjectNull_ReturnsNull()
    {
        _finder.GlueProject = null;
        var rfs = new ReferencedFileSave();

        Assert.Null(rfs.GetContainer());
    }

    [Fact]
    public void GetContainer_GlueProjectSet_ReturnsContainerFromFinder()
    {
        _finder.GlueProject = new GlueProjectSave();
        var container = new EntitySave { Name = "Entities\\Player" };
        var rfs = new ReferencedFileSave();
        _finder.SetContainer(rfs, container);

        Assert.Same(container, rfs.GetContainer());
    }

    [Fact]
    public void GetContainerType_NoContainer_ReturnsNone()
    {
        _finder.GlueProject = null;
        var rfs = new ReferencedFileSave();

        Assert.Equal(ContainerType.None, rfs.GetContainerType());
    }

    [Fact]
    public void GetContainerType_EntityContainer_ReturnsEntity()
    {
        _finder.GlueProject = new GlueProjectSave();
        var rfs = new ReferencedFileSave();
        _finder.SetContainer(rfs, new EntitySave { Name = "Entities\\Player" });

        Assert.Equal(ContainerType.Entity, rfs.GetContainerType());
    }

    [Fact]
    public void GetContainerType_ScreenContainer_ReturnsScreen()
    {
        _finder.GlueProject = new GlueProjectSave();
        var rfs = new ReferencedFileSave();
        _finder.SetContainer(rfs, new ScreenSave { Name = "Screens\\GameScreen" });

        Assert.Equal(ContainerType.Screen, rfs.GetContainerType());
    }

    [Fact]
    public void ReferencedFileSaveToString_NoContainer_IncludesGlobalContentSuffix()
    {
        _finder.GlueProject = null;
        var rfs = new ReferencedFileSave { Name = "sprite.png" };

        Assert.Equal("sprite.png (in GlobalContent)", ReferencedFileSaveElementExtensions.ReferencedFileSaveToString(rfs));
    }

    [Fact]
    public void ReferencedFileSaveToString_HasContainer_IncludesContainerName()
    {
        _finder.GlueProject = new GlueProjectSave();
        var rfs = new ReferencedFileSave { Name = "sprite.png" };
        var container = new EntitySave { Name = "Entities\\Player" };
        _finder.SetContainer(rfs, container);

        Assert.Equal($"sprite.png (in {container})", ReferencedFileSaveElementExtensions.ReferencedFileSaveToString(rfs));
    }

    [Fact]
    public void GetIsSharedStaticEditable_ScreenContainer_ReturnsTrue()
    {
        _finder.GlueProject = new GlueProjectSave();
        var rfs = new ReferencedFileSave();
        _finder.SetContainer(rfs, new ScreenSave { Name = "Screens\\GameScreen" });

        Assert.True(rfs.GetIsSharedStaticEditable());
    }

    [Fact]
    public void GetIsSharedStaticEditable_EntityContainer_ReturnsFalse()
    {
        _finder.GlueProject = new GlueProjectSave();
        var rfs = new ReferencedFileSave();
        _finder.SetContainer(rfs, new EntitySave { Name = "Entities\\Player" });

        Assert.False(rfs.GetIsSharedStaticEditable());
    }

    [Fact]
    public void GetIsSharedStaticEditable_NoContainer_ReturnsFalse()
    {
        _finder.GlueProject = null;
        var rfs = new ReferencedFileSave();

        Assert.False(rfs.GetIsSharedStaticEditable());
    }

    [Fact]
    public void GetIsLinkedOutsideContainerFolder_FileUnderContainerFolder_ReturnsFalse()
    {
        _finder.GlueProject = new GlueProjectSave();
        var rfs = new ReferencedFileSave { Name = "Entities/Player/PlayerSheet.png" };
        _finder.SetContainer(rfs, new EntitySave { Name = "Entities\\Player" });

        Assert.False(rfs.GetIsLinkedOutsideContainerFolder());
    }

    [Fact]
    public void GetIsLinkedOutsideContainerFolder_FileOutsideContainerFolder_ReturnsTrue()
    {
        _finder.GlueProject = new GlueProjectSave();
        var rfs = new ReferencedFileSave { Name = "Entities/Other/PlayerSheet.png" };
        _finder.SetContainer(rfs, new EntitySave { Name = "Entities\\Player" });

        Assert.True(rfs.GetIsLinkedOutsideContainerFolder());
    }

    [Fact]
    public void GetIsLinkedOutsideContainerFolder_WithKnownContainer_SkipsFinderLookup()
    {
        var rfs = new ReferencedFileSave { Name = "Entities/Player/PlayerSheet.png" };
        var container = new EntitySave { Name = "Entities\\Player" };

        Assert.False(rfs.GetIsLinkedOutsideContainerFolder(container));
    }

    [Fact]
    public void GetIsFileOutsideContainerFolder_GlobalContent_MatchesGlobalContentFolder()
    {
        Assert.False(ReferencedFileSaveElementExtensions.GetIsFileOutsideContainerFolder("GlobalContent/sprite.png", null));
        Assert.True(ReferencedFileSaveElementExtensions.GetIsFileOutsideContainerFolder("Entities/Player/sprite.png", null));
    }

    [Fact]
    public void GetIsFileOutsideContainerFolder_EmptyFileName_ReturnsFalse()
    {
        Assert.False(ReferencedFileSaveElementExtensions.GetIsFileOutsideContainerFolder("", "Entities\\Player"));
    }

    [Fact]
    public void GetInstanceName_NotRelativeToContainer_StripsPathAndExtension()
    {
        var rfs = new ReferencedFileSave { Name = "Entities/Player/Player Sheet.png" };

        Assert.Equal("PlayerSheet", rfs.GetInstanceName());
    }

    [Fact]
    public void GetInstanceName_StripsInvalidCharacters()
    {
        var rfs = new ReferencedFileSave { Name = "sprite-sheet(1).png" };

        Assert.Equal("sprite_sheet1", rfs.GetInstanceName());
    }

    [Fact]
    public void GetInstanceName_LeadingDigit_PrefixesWithUnderscore()
    {
        var rfs = new ReferencedFileSave { Name = "1up.png" };

        Assert.Equal("_1up", rfs.GetInstanceName());
    }

    [Fact]
    public void GetInstanceName_CachesResult()
    {
        var rfs = new ReferencedFileSave { Name = "sprite.png" };

        var first = rfs.GetInstanceName();
        rfs.CachedInstanceName = "Overridden";

        Assert.Equal("Overridden", rfs.GetInstanceName());
    }

    [Fact]
    public void GetInstanceName_RelativeToContainer_ContainerFound_MakesRelativeToContainerFolder()
    {
        _finder.GlueProject = new GlueProjectSave();
        var rfs = new ReferencedFileSave
        {
            Name = "Entities/Player/Textures/Sheet.png",
            IncludeDirectoryRelativeToContainer = true
        };
        _finder.SetContainer(rfs, new EntitySave { Name = "Entities/Player" });

        Assert.Equal("Textures_Sheet", rfs.GetInstanceName());
    }

    [Fact]
    public void GetInstanceName_RelativeToContainer_NotUnderContainerFolder_StripsPathOnly()
    {
        _finder.GlueProject = new GlueProjectSave();
        var rfs = new ReferencedFileSave
        {
            Name = "Entities/Other/Sheet.png",
            IncludeDirectoryRelativeToContainer = true
        };
        _finder.SetContainer(rfs, new EntitySave { Name = "Entities/Player" });

        Assert.Equal("Sheet", rfs.GetInstanceName());
    }

    [Fact]
    public void GetInstanceName_RelativeToContainer_NoContainer_StripsGlobalContentPrefix()
    {
        _finder.GlueProject = null;
        var rfs = new ReferencedFileSave
        {
            Name = "GlobalContent/Folder/Sheet.png",
            IncludeDirectoryRelativeToContainer = true
        };

        Assert.Equal("Folder_Sheet", rfs.GetInstanceName());
    }
}
