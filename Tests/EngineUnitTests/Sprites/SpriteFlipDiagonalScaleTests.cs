using System.Reflection;
using System.Runtime.Serialization;
using FlatRedBall;
using FlatRedBall.Graphics.Animation;
using Microsoft.Xna.Framework.Graphics;
using Shouldly;

namespace EngineUnitTests.Sprites;

/// <summary>
/// A diagonal flip transposes the texture region, so a TextureScale Sprite's width and height swap with it.
/// </summary>
public class SpriteFlipDiagonalScaleTests
{
    // A 100x100 texture without a graphics device.
    static Texture2D Texture()
    {
        var texture = (Texture2D)FormatterServices.GetUninitializedObject(typeof(Texture2D));
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        typeof(Texture2D).GetField("width", flags)!.SetValue(texture, 100);
        typeof(Texture2D).GetField("height", flags)!.SetValue(texture, 100);
        return texture;
    }

    // A frame showing a 22 wide, 46 tall region.
    static AnimationFrame Frame(bool flipDiagonal) => new AnimationFrame
    {
        Texture = Texture(),
        LeftCoordinate = 0.10f,
        RightCoordinate = 0.32f,
        TopCoordinate = 0.20f,
        BottomCoordinate = 0.66f,
        FlipDiagonal = flipDiagonal,
        FrameLength = 1,
    };

    static Sprite SpriteShowing(bool flipDiagonal)
    {
        var sprite = new Sprite { TextureScale = 1 };
        sprite.UpdateToAnimationFrame(Frame(flipDiagonal));
        return sprite;
    }

    [Fact]
    public void UpdateToAnimationFrame_NoDiagonalFlip_SizesFromTheRegion()
    {
        var sprite = SpriteShowing(false);

        sprite.ScaleX.ShouldBe(11, 0.01f);
        sprite.ScaleY.ShouldBe(23, 0.01f);
    }

    [Fact]
    public void UpdateToAnimationFrame_DiagonalFlip_SwapsWidthAndHeight()
    {
        var sprite = SpriteShowing(true);

        sprite.ScaleX.ShouldBe(23, 0.01f);
        sprite.ScaleY.ShouldBe(11, 0.01f);
    }

    [Fact]
    public void FlipDiagonal_SetDirectly_ResizesTheSprite()
    {
        var sprite = SpriteShowing(false);

        sprite.FlipDiagonal = true;

        sprite.ScaleX.ShouldBe(23, 0.01f);
        sprite.ScaleY.ShouldBe(11, 0.01f);
    }
}
