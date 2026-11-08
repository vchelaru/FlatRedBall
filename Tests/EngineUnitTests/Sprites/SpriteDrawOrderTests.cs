using System;
using System.Linq;
using System.Runtime.Serialization;
using EngineUnitTests.TestSupport;
using FlatRedBall;
using FlatRedBall.Graphics;
using Microsoft.Xna.Framework.Graphics;
using Shouldly;

namespace EngineUnitTests.Sprites;

/// <summary>
/// SpriteManager.MoveAbove and MoveBelow reorder Sprites inside the list they are drawn from, without touching Z.
/// </summary>
[Collection(nameof(EngineStatefulTestCollection))]
public class SpriteDrawOrderTests
{
    static Texture2D FakeTexture() =>
        (Texture2D)FormatterServices.GetUninitializedObject(typeof(Texture2D));

    static Sprite[] AddSprites(int count, Layer? layer = null)
    {
        EngineTestBootstrap.EnsureInitialized();
        var texture = FakeTexture();
        var sprites = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            sprites[i] = SpriteManager.AddSprite(texture);
            if (layer != null)
            {
                SpriteManager.AddToLayer(sprites[i], layer);
            }
        }
        return sprites;
    }

    static void Cleanup(Sprite[] sprites)
    {
        foreach (var sprite in sprites)
        {
            SpriteManager.RemoveSprite(sprite);
        }
    }

    // Draw order of just these sprites, back to front, read from the list the renderer draws.
    static string Order(Sprite[] sprites, System.Collections.Generic.IList<Sprite> drawList) =>
        string.Join(",", drawList.Where(sprites.Contains).Select(sprite => Array.IndexOf(sprites, sprite)));

    [Fact]
    public void MoveAbove_PutsTheSpriteJustAfterTheReference_WithoutChangingZ()
    {
        var sprites = AddSprites(4);
        try
        {
            var drawList = SpriteManager.OrderedByDistanceFromCameraSprites;
            Order(sprites, drawList).ShouldBe("0,1,2,3");

            SpriteManager.MoveAbove(sprites[0], sprites[2]);

            Order(sprites, drawList).ShouldBe("1,2,0,3");
            sprites.ShouldAllBe(sprite => sprite.Z == 0);
        }
        finally
        {
            Cleanup(sprites);
        }
    }

    [Fact]
    public void MoveBelow_PutsTheSpriteJustBeforeTheReference()
    {
        var sprites = AddSprites(4);
        try
        {
            SpriteManager.MoveBelow(sprites[3], sprites[1]);

            Order(sprites, SpriteManager.OrderedByDistanceFromCameraSprites).ShouldBe("0,3,1,2");
        }
        finally
        {
            Cleanup(sprites);
        }
    }

    [Fact]
    public void TheMoveSurvivesTheRendererSort_WhenZIsEqual()
    {
        var sprites = AddSprites(3);
        try
        {
            SpriteManager.MoveBelow(sprites[2], sprites[0]);

            // The renderer's sort for the default Z ordering.
            SpriteManager.OrderedByDistanceFromCameraSprites.SortZInsertionAscending();

            Order(sprites, SpriteManager.OrderedByDistanceFromCameraSprites).ShouldBe("2,0,1");
        }
        finally
        {
            Cleanup(sprites);
        }
    }

    [Fact]
    public void ALayeredPairIsReorderedInTheLayersList()
    {
        EngineTestBootstrap.EnsureInitialized();
        var layer = SpriteManager.AddLayer();
        var sprites = AddSprites(3, layer);
        try
        {
            SpriteManager.MoveAbove(sprites[0], sprites[2]);

            Order(sprites, layer.Sprites).ShouldBe("1,2,0");
        }
        finally
        {
            Cleanup(sprites);
            SpriteManager.RemoveLayer(layer);
        }
    }

    [Fact]
    public void SpritesOnDifferentLayersCannotBeOrderedAgainstEachOther()
    {
        EngineTestBootstrap.EnsureInitialized();
        var layer = SpriteManager.AddLayer();
        var sprites = AddSprites(2);
        SpriteManager.AddToLayer(sprites[1], layer);
        try
        {
            Should.Throw<InvalidOperationException>(() => SpriteManager.MoveAbove(sprites[0], sprites[1]));
        }
        finally
        {
            Cleanup(sprites);
            SpriteManager.RemoveLayer(layer);
        }
    }

    [Fact]
    public void MovingASpriteAgainstItselfThrows()
    {
        var sprites = AddSprites(1);
        try
        {
            Should.Throw<ArgumentException>(() => SpriteManager.MoveAbove(sprites[0], sprites[0]));
        }
        finally
        {
            Cleanup(sprites);
        }
    }
}
