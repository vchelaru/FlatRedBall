using FlatRedBall.Graphics;
using Microsoft.Xna.Framework;
using Shouldly;

namespace EngineUnitTests.Graphics;

public class TextureCoordinateFlipperTests
{
    static readonly Vector2 TL = new Vector2(0, 0);
    static readonly Vector2 TR = new Vector2(1, 0);
    static readonly Vector2 BR = new Vector2(1, 1);
    static readonly Vector2 BL = new Vector2(0, 1);

    [Fact]
    public void Flip_NoFlips_ShouldReturnCornersUnchanged()
    {
        var result = TextureCoordinateFlipper.Flip(TL, TR, BR, BL, false, false, false);

        result.TopLeft.ShouldBe(TL);
        result.TopRight.ShouldBe(TR);
        result.BottomRight.ShouldBe(BR);
        result.BottomLeft.ShouldBe(BL);
    }

    [Fact]
    public void Flip_Horizontal_ShouldSwapLeftAndRightColumns()
    {
        var result = TextureCoordinateFlipper.Flip(TL, TR, BR, BL, flipDiagonal: false, flipHorizontal: true, flipVertical: false);

        result.TopLeft.ShouldBe(TR);
        result.TopRight.ShouldBe(TL);
        result.BottomRight.ShouldBe(BL);
        result.BottomLeft.ShouldBe(BR);
    }

    [Fact]
    public void Flip_Vertical_ShouldSwapTopAndBottomRows()
    {
        var result = TextureCoordinateFlipper.Flip(TL, TR, BR, BL, flipDiagonal: false, flipHorizontal: false, flipVertical: true);

        result.TopLeft.ShouldBe(BL);
        result.TopRight.ShouldBe(BR);
        result.BottomRight.ShouldBe(TR);
        result.BottomLeft.ShouldBe(TL);
    }

    [Fact]
    public void Flip_Diagonal_ShouldSwapTopRightAndBottomLeft_AndKeepTopLeftAndBottomRightFixed()
    {
        var result = TextureCoordinateFlipper.Flip(TL, TR, BR, BL, flipDiagonal: true, flipHorizontal: false, flipVertical: false);

        result.TopLeft.ShouldBe(TL);
        result.TopRight.ShouldBe(BL);
        result.BottomRight.ShouldBe(BR);
        result.BottomLeft.ShouldBe(TR);
    }

    [Fact]
    public void Flip_HorizontalAndVertical_ShouldBeEquivalentTo180DegreeRotation()
    {
        var result = TextureCoordinateFlipper.Flip(TL, TR, BR, BL, flipDiagonal: false, flipHorizontal: true, flipVertical: true);

        result.TopLeft.ShouldBe(BR);
        result.TopRight.ShouldBe(BL);
        result.BottomRight.ShouldBe(TL);
        result.BottomLeft.ShouldBe(TR);
    }

    [Fact]
    public void Flip_DiagonalThenHorizontal_ShouldNotEqualHorizontalThenDiagonal()
    {
        // Diagonal does not commute with horizontal/vertical, so the fixed application order
        // (diagonal, then horizontal, then vertical) is load-bearing: swapping it changes the result.
        var diagonalThenHorizontal = TextureCoordinateFlipper.Flip(TL, TR, BR, BL, flipDiagonal: true, flipHorizontal: true, flipVertical: false);

        // Manually apply horizontal first, then diagonal, to get the "wrong order" result for comparison.
        var horizontalFirst = TextureCoordinateFlipper.Flip(TL, TR, BR, BL, flipDiagonal: false, flipHorizontal: true, flipVertical: false);
        var horizontalThenDiagonal = TextureCoordinateFlipper.Flip(
            horizontalFirst.TopLeft, horizontalFirst.TopRight, horizontalFirst.BottomRight, horizontalFirst.BottomLeft,
            flipDiagonal: true, flipHorizontal: false, flipVertical: false);

        diagonalThenHorizontal.ShouldNotBe(horizontalThenDiagonal);
    }

    [Fact]
    public void Flip_AllThreeFlips_ShouldBeEquivalentToTheOtherDiagonal()
    {
        // Composing all three (an odd number of reflections) yields a reflection, not a rotation:
        // specifically the anti-diagonal flip, which fixes TopRight/BottomLeft and swaps TopLeft/BottomRight.
        var result = TextureCoordinateFlipper.Flip(TL, TR, BR, BL, flipDiagonal: true, flipHorizontal: true, flipVertical: true);

        result.TopLeft.ShouldBe(BR);
        result.TopRight.ShouldBe(TR);
        result.BottomRight.ShouldBe(TL);
        result.BottomLeft.ShouldBe(BL);
    }
}
