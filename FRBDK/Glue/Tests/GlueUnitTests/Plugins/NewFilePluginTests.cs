using FlatRedBall.Content.AnimationChain;
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
}
