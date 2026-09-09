using System.Linq;
using EngineUnitTests.TestSupport;
using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Screens;
using GlueControl.Editing;
using Microsoft.Xna.Framework;
using Shouldly;

namespace EngineUnitTests.GlueControl;

// EditorVisuals is compiled directly from Glue's embedded-code source (see EngineUnitTests.csproj) -
// these tests pin issue #1729: editor visuals must no-op (return a live-but-unregistered dummy
// instance, never null) when ShowEditorVisuals is false, so a Release build never pays render/manager
// overhead for markers/handles, and external code that stores/mutates the return value never NREs.
[Collection(nameof(EngineStatefulTestCollection))]
public class EditorVisualsTests
{
    // ScreenManager.CurrentScreen has no public setter; a real running game always has an active,
    // not-yet-finished screen, and Arrow's registration logic branches on that. Reflection is the
    // only way to reproduce that state here without standing up full screen activation.
    static readonly System.Reflection.FieldInfo CurrentScreenField =
        typeof(ScreenManager).GetField("mCurrentScreen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
        ?? throw new System.InvalidOperationException("ScreenManager.mCurrentScreen field not found - has it been renamed?");

    static void WithActiveScreen(System.Action action)
    {
        var screen = new Screen { IsActivityFinished = false };
        CurrentScreenField.SetValue(null, screen);
        try
        {
            action();
        }
        finally
        {
            CurrentScreenField.SetValue(null, null);
        }
    }

    public EditorVisualsTests()
    {
        EngineTestBootstrap.EnsureInitialized();
    }

    [Fact]
    public void Line_WhenSuppressed_ReturnsDummyAndDoesNotRegisterWithShapeManager()
    {
        EditorVisuals.ShowEditorVisuals = false;

        var result = EditorVisuals.Line(Vector3.Zero, Vector3.One);

        result.ShouldNotBeNull();
        EditorVisuals.DefaultLayer.Lines.ShouldNotContain(result);
    }

    [Fact]
    public void Line_WhenNotSuppressed_StillRegistersWithShapeManager()
    {
        EditorVisuals.ShowEditorVisuals = true;

        var result = EditorVisuals.Line(Vector3.Zero, Vector3.One);

        EditorVisuals.DefaultLayer.Lines.ShouldContain(result);

        ShapeManager.Remove(result);
    }

    [Fact]
    public void Rectangle_WhenSuppressed_ReturnsDummyAndDoesNotRegisterWithShapeManager()
    {
        EditorVisuals.ShowEditorVisuals = false;

        var result = EditorVisuals.Rectangle(10, 10, Vector3.Zero);

        result.ShouldNotBeNull();
        EditorVisuals.DefaultLayer.AxisAlignedRectangles.ShouldNotContain(result);
    }

    [Fact]
    public void Circle_WhenSuppressed_ReturnsDummyAndDoesNotRegisterWithShapeManager()
    {
        EditorVisuals.ShowEditorVisuals = false;

        var result = EditorVisuals.Circle(5, Vector3.Zero);

        result.ShouldNotBeNull();
        EditorVisuals.DefaultLayer.Circles.ShouldNotContain(result);
    }

    [Fact]
    public void Polygon_WhenSuppressed_ReturnsDummyAndDoesNotRegisterWithShapeManager()
    {
        EditorVisuals.ShowEditorVisuals = false;

        var result = EditorVisuals.Polygon(Vector3.Zero);

        result.ShouldNotBeNull();
        EditorVisuals.DefaultLayer.Polygons.ShouldNotContain(result);
    }

    [Fact]
    public void Text_WhenSuppressed_ReturnsDummyAndDoesNotRegisterWithTextManager()
    {
        EditorVisuals.ShowEditorVisuals = false;

        var result = EditorVisuals.Text("hello", Vector3.Zero);

        result.ShouldNotBeNull();
        EditorVisuals.DefaultLayer.Texts.ShouldNotContain(result);
    }

    [Fact]
    public void Sprite_WhenSuppressed_ReturnsDummyAndDoesNotRegisterWithSpriteManager()
    {
        EditorVisuals.ShowEditorVisuals = false;

        var result = EditorVisuals.Sprite((Microsoft.Xna.Framework.Graphics.Texture2D)null!, Vector3.Zero);

        result.ShouldNotBeNull();
        EditorVisuals.DefaultLayer.Sprites.ShouldNotContain(result);
    }

    [Fact]
    public void Arrow_WhenSuppressedWithActiveScreen_ReturnsDummyAndDoesNotRegisterLinesWithShapeManager()
    {
        EditorVisuals.ShowEditorVisuals = false;

        WithActiveScreen(() =>
        {
            var result = EditorVisuals.Arrow(Vector3.Zero, Vector3.One);

            result.ShouldNotBeNull();
            EditorVisuals.DefaultLayer.Lines.Count().ShouldBe(0);
        });
    }

    [Fact]
    public void Arrow_WhenNotSuppressedWithActiveScreen_StillRegistersLinesWithShapeManager()
    {
        EditorVisuals.ShowEditorVisuals = true;

        WithActiveScreen(() =>
        {
            var result = EditorVisuals.Arrow(Vector3.Zero, Vector3.One);

            result.ShouldNotBeNull();
            // EditorVisuals.Arrow uses Arrow's default ctor args (firstArrow: false, secondArrow: true):
            // 1 main line + 3 second-arrowhead lines.
            EditorVisuals.DefaultLayer.Lines.Count().ShouldBe(4);

            foreach (var line in EditorVisuals.DefaultLayer.Lines.ToList())
            {
                ShapeManager.Remove(line);
            }
        });
    }
}
