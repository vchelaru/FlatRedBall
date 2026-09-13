using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;

namespace GlueCommonUnitTests.SaveClasses;

// GetAssetTypeInfo reads the shared static AvailableAssetTypesCore.Self, so this can't run
// concurrently with any other test class that swaps it out - hence the shared collection (see
// ObjectFinderCoreCollection in NamedObjectSaveElementExtensionsTests.cs).
[Collection(nameof(ObjectFinderCoreCollection))]
public class ReferencedFileSaveAssetTypeExtensionsTests
{
    readonly FakeAvailableAssetTypesCore _availableAssetTypes = new();

    public ReferencedFileSaveAssetTypeExtensionsTests()
    {
        AvailableAssetTypesCore.Self = _availableAssetTypes;
    }

    [Fact]
    public void GetAssetTypeInfo_RuntimeTypeSet_MatchesByExtensionAndQualifiedRuntime_ReturnsMatch()
    {
        var ati = new AssetTypeInfo
        {
            Extension = "png",
            QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = "FlatRedBall.Graphics.Texture2D" }
        };
        _availableAssetTypes.AddAssetType(ati);

        var rfs = new ReferencedFileSave { Name = "sprite.png", RuntimeType = "FlatRedBall.Graphics.Texture2D" };

        Assert.Same(ati, rfs.GetAssetTypeInfo());
    }

    [Fact]
    public void GetAssetTypeInfo_RuntimeTypeSet_NoExtensionMatch_FallsBackToAllAssetTypesByQualifiedType()
    {
        var ati = new AssetTypeInfo
        {
            Extension = "jpg",
            QualifiedRuntimeTypeName = new PlatformSpecificType { QualifiedType = "FlatRedBall.Graphics.Texture2D" }
        };
        _availableAssetTypes.AddAssetType(ati);

        var rfs = new ReferencedFileSave { Name = "sprite.png", RuntimeType = "FlatRedBall.Graphics.Texture2D" };

        Assert.Same(ati, rfs.GetAssetTypeInfo());
    }

    [Fact]
    public void GetAssetTypeInfo_RuntimeTypeSet_NoMatchAnywhere_ReturnsNull()
    {
        var rfs = new ReferencedFileSave { Name = "sprite.png", RuntimeType = "Unknown.Type" };

        Assert.Null(rfs.GetAssetTypeInfo());
    }

    [Fact]
    public void GetAssetTypeInfo_NoRuntimeType_ResolvesByExtension()
    {
        var ati = new AssetTypeInfo { Extension = "png" };
        _availableAssetTypes.AddAssetType(ati);

        var rfs = new ReferencedFileSave { Name = "sprite.png", RuntimeType = null };

        Assert.Same(ati, rfs.GetAssetTypeInfo());
    }
}
