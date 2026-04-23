using AnimationEditor.Core;
using AnimationEditor.Core.CommandsAndState;
using AnimationEditor.Core.Data;
using Xunit;

namespace AnimationEditor.Core.Tests;

[Collection("SequentialSingletons")]
public class AppStateTests
{
    [Fact]
    public void WireframeZoomValue_DefaultIs100()
    {
        TestHelpers.SetupFreshAcls();

        Assert.Equal(100, AppState.Self.WireframeZoomValue);
    }

    [Fact]
    public void WireframeZoomValue_WhenSet_StoresNewValue()
    {
        TestHelpers.SetupFreshAcls();

        AppState.Self.WireframeZoomValue = 200;

        Assert.Equal(200, AppState.Self.WireframeZoomValue);
    }

    [Fact]
    public void WireframeZoomValue_WhenSet_FiresAfterZoomChange()
    {
        TestHelpers.SetupFreshAcls();
        bool fired = false;
        ApplicationEvents.Self.AfterZoomChange += () => fired = true;

        AppState.Self.WireframeZoomValue = 150;

        Assert.True(fired);
    }

    [Fact]
    public void UnitType_WhenSet_StoresValue()
    {
        TestHelpers.SetupFreshAcls();

        AppState.Self.UnitType = UnitType.TextureCoordinate;

        Assert.Equal(UnitType.TextureCoordinate, AppState.Self.UnitType);
    }

    [Fact]
    public void UnitType_WhenSet_FiresWireframeTextureChange()
    {
        TestHelpers.SetupFreshAcls();
        bool fired = false;
        ApplicationEvents.Self.WireframeTextureChange += () => fired = true;

        AppState.Self.UnitType = UnitType.TextureCoordinate;

        Assert.True(fired);
    }

    [Fact]
    public void GridSize_DefaultIs16()
    {
        TestHelpers.SetupFreshAcls();

        Assert.Equal(16, AppState.Self.GridSize);
    }

    [Fact]
    public void GridSize_WhenSet_StoresValue()
    {
        TestHelpers.SetupFreshAcls();

        AppState.Self.GridSize = 32;

        Assert.Equal(32, AppState.Self.GridSize);
    }

    [Fact]
    public void IsSnapToGridChecked_DefaultIsFalse()
    {
        TestHelpers.SetupFreshAcls();

        Assert.False(AppState.Self.IsSnapToGridChecked);
    }

    [Fact]
    public void IsSnapToGridChecked_WhenSet_StoresValue()
    {
        TestHelpers.SetupFreshAcls();

        AppState.Self.IsSnapToGridChecked = true;

        Assert.True(AppState.Self.IsSnapToGridChecked);
    }

    [Fact]
    public void CurrentFrame_DelegatesToSelectedStateSelectedFrame()
    {
        var acls = TestHelpers.SetupFreshAcls();
        var chain = TestHelpers.MakeChain(acls, "Run", 1);
        SelectedState.Self.SelectedFrame = chain.Frames[0];

        Assert.Same(SelectedState.Self.SelectedFrame, AppState.Self.CurrentFrame);
    }

    [Fact]
    public void CurrentFrame_WhenNoFrameSelected_ReturnsNull()
    {
        TestHelpers.SetupFreshAcls();

        Assert.Null(AppState.Self.CurrentFrame);
    }

    [Fact]
    public void ProjectFolder_WhenSet_StoresValue()
    {
        TestHelpers.SetupFreshAcls();

        AppState.Self.ProjectFolder = "C:/MyGame/Content";

        Assert.Equal("C:/MyGame/Content", AppState.Self.ProjectFolder);
    }
}
