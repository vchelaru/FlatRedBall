using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;

namespace GlueCommonUnitTests.SaveClasses;

// Both overloads read the shared static ObjectFinderCore.Self (via MakeAbsoluteContent), so this
// can't run concurrently with any other test class that swaps it out - hence the shared collection
// (see ObjectFinderCoreCollection in NamedObjectSaveElementExtensionsTests.cs).
[Collection(nameof(ObjectFinderCoreCollection))]
public class ReferencedFileSaveSourceExtensionsTests
{
    readonly FakeObjectFinderCore _finder = new() { ContentDirectory = "C:/MyProject/Content/" };

    public ReferencedFileSaveSourceExtensionsTests()
    {
        ObjectFinderCore.Self = _finder;
    }

    [Fact]
    public void IsFileSourceForThis_FilePath_MatchesAbsoluteSourceFile()
    {
        var rfs = new ReferencedFileSave { SourceFile = "Textures/sprite.png" };

        Assert.True(rfs.IsFileSourceForThis(new FilePath("C:/MyProject/Content/Textures/sprite.png")));
    }

    [Fact]
    public void IsFileSourceForThis_FilePath_NoMatch_ReturnsFalse()
    {
        var rfs = new ReferencedFileSave { SourceFile = "Textures/sprite.png" };

        Assert.False(rfs.IsFileSourceForThis(new FilePath("C:/MyProject/Content/Textures/other.png")));
    }

    [Fact]
    public void IsFileSourceForThis_FilePath_EmptySourceFile_ReturnsFalse()
    {
        var rfs = new ReferencedFileSave { SourceFile = "" };

        Assert.False(rfs.IsFileSourceForThis(new FilePath("C:/MyProject/Content/Textures/sprite.png")));
    }

    [Fact]
    public void IsFileSourceForThis_StringFileName_MatchesCaseInsensitively()
    {
        var rfs = new ReferencedFileSave { SourceFile = "Textures/sprite.png" };

        Assert.True(rfs.IsFileSourceForThis("C:/MyProject/Content/TEXTURES/SPRITE.PNG"));
    }

    [Fact]
    public void IsFileSourceForThis_StringFileName_NoMatch_ReturnsFalse()
    {
        var rfs = new ReferencedFileSave { SourceFile = "Textures/sprite.png" };

        Assert.False(rfs.IsFileSourceForThis("C:/MyProject/Content/Textures/other.png"));
    }
}
