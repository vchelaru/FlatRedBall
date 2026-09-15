using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins.NewFiles;
using FlatRedBall.Graphics;
using Xunit;

namespace GlueUnitTests.Plugins;

/// <summary>
/// GitHub issue #2249: a brand-new (empty) .achx created via Glue's "Add New File" window
/// used to inherit <c>AnimationChainListSave</c>'s legacy UV default, which pops a "Convert to
/// Pixel Coordinates" warning the first time the file is opened in the Avalonia AnimationEditor -
/// even though the file has no UV data to convert. <see cref="AnimationChainListSave.CoordinateType"/>'s
/// class-level default itself must stay UV: <c>AsepriteAnimationChainLoader</c> constructs a save
/// instance and fills it with genuinely-normalized UV coordinates without ever setting the property,
/// so flipping the shared default would silently reinterpret those as pixel values instead.
/// </summary>
public class NewFilePluginTests
{
    [Fact]
    public void CreateSaveInstance_ForAnimationChainListSave_DefaultsToPixelCoordinates()
    {
        var instance = NewFilePlugin.CreateSaveInstance(typeof(AnimationChainListSave));

        var achx = Assert.IsType<AnimationChainListSave>(instance);
        Assert.Equal(TextureCoordinateType.Pixel, achx.CoordinateType);
    }

    [Fact]
    public void CreateSaveInstance_ForOtherSaveTypes_UsesThePlainDefaultConstructor()
    {
        var instance = NewFilePlugin.CreateSaveInstance(typeof(AnimationChainSave));

        Assert.IsType<AnimationChainSave>(instance);
    }

    /// <summary>
    /// GitHub issue #2249's original fix only patched <see cref="NewFilePlugin.CreateSaveInstance"/>,
    /// which is reached from <c>SaveNewFileAtLocation</c> only when <see cref="AssetTypeInfo.SaveType"/>
    /// is non-null. The real .achx <see cref="AssetTypeInfo"/> is CSV-loaded: its
    /// <see cref="AssetTypeInfo.QualifiedSaveTypeName"/> setter resolves <c>SaveType</c> via
    /// <c>Type.GetType("...AnimationChainListSave, FlatRedBall")</c> - and no engine assembly is
    /// actually named "FlatRedBall" (it's "FlatRedBallDesktopGLNet6", "FlatRedBall.FNA", etc.), so that
    /// resolution always fails and <c>SaveType</c> stays null. That routed real achx creation into the
    /// <c>QualifiedSaveTypeName</c> fallback branch, which called <c>Activator.CreateInstance</c>
    /// directly and never applied the pixel default.
    /// </summary>
    [Fact]
    public void TryCreateSaveInstance_ForAssetTypeInfoWithOnlyQualifiedSaveTypeName_DefaultsAchxToPixelCoordinates()
    {
        var ati = new AssetTypeInfo
        {
            QualifiedSaveTypeName = typeof(AnimationChainListSave).FullName
        };

        // Sanity-check the CSV-load quirk this test exists to cover: SaveType did NOT resolve
        // from QualifiedSaveTypeName alone.
        Assert.Null(ati.SaveType);

        var found = NewFilePlugin.TryCreateSaveInstance(ati, out var type, out var saveInstance);

        Assert.True(found);
        Assert.Equal(typeof(AnimationChainListSave), type);
        var achx = Assert.IsType<AnimationChainListSave>(saveInstance);
        Assert.Equal(TextureCoordinateType.Pixel, achx.CoordinateType);
    }

    [Fact]
    public void TryCreateSaveInstance_ForUnresolvableQualifiedSaveTypeName_ReturnsFalse()
    {
        var ati = new AssetTypeInfo
        {
            QualifiedSaveTypeName = "Not.A.Real.Type"
        };

        var found = NewFilePlugin.TryCreateSaveInstance(ati, out var type, out var saveInstance);

        Assert.False(found);
        Assert.Null(type);
        Assert.Null(saveInstance);
    }
}
