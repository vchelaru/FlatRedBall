using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using GameCommunicationPlugin.GlueControl.Managers;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.GameCommunicationPlugin;

/// <summary>
/// When an object is added while the game runs in edit mode, Glue moves it to the camera's
/// position so it lands in view. A Camera named object is a handle on the main camera
/// (see NamedObjectSaveCodeGenerator - it generates "= FlatRedBall.SpriteManager.Camera"),
/// so "positioning" it would move the camera the user is looking through. See issue #1984.
/// </summary>
public class NewObjectCameraPositioningTests
{
    public NewObjectCameraPositioningTests()
    {
        GlueTestBootstrap.EnsureInitialized();
    }

    static NamedObjectSave CreateFlatRedBallTypeNos(AssetTypeInfo ati) => new NamedObjectSave
    {
        InstanceName = "TestInstance",
        SourceType = SourceType.FlatRedBallType,
        SourceClassType = ati.QualifiedRuntimeTypeName.QualifiedType
    };

    [Fact]
    public void ShouldPositionNewObjectAtCamera_ShouldBeTrue_ForSprite()
    {
        var nos = CreateFlatRedBallTypeNos(AvailableAssetTypes.CommonAtis.Sprite);

        RefreshManager.ShouldPositionNewObjectAtCamera(nos).ShouldBeTrue();
    }

    [Fact]
    public void ShouldPositionNewObjectAtCamera_ShouldBeTrue_ForEntity()
    {
        var nos = new NamedObjectSave
        {
            InstanceName = "PlayerInstance",
            SourceType = SourceType.Entity,
            SourceClassType = "Entities\\Player"
        };

        RefreshManager.ShouldPositionNewObjectAtCamera(nos).ShouldBeTrue();
    }

    [Fact]
    public void ShouldPositionNewObjectAtCamera_ShouldBeFalse_ForCamera()
    {
        var nos = CreateFlatRedBallTypeNos(AvailableAssetTypes.CommonAtis.Camera);

        RefreshManager.ShouldPositionNewObjectAtCamera(nos).ShouldBeFalse();
    }

    [Fact]
    public void ShouldPositionNewObjectAtCamera_ShouldBeFalse_ForNonPositionedType()
    {
        var nos = CreateFlatRedBallTypeNos(AvailableAssetTypes.CommonAtis.Layer);

        RefreshManager.ShouldPositionNewObjectAtCamera(nos).ShouldBeFalse();
    }
}
