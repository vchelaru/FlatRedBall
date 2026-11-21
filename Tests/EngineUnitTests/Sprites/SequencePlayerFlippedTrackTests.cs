using System.Reflection;
using System.Runtime.Serialization;
using FlatRedBall;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Graphics;
using FlatRedBall.Graphics.Animation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shouldly;

namespace EngineUnitTests.Sprites;

/// <summary>
/// A track with flipHorizontal draws each frame as the unflipped track would, mirrored left to right, whatever flips the
/// frame itself has (including the diagonal flip, which does not commute with the others).
/// </summary>
public class SequencePlayerFlippedTrackTests
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

    // One frame showing a 22 wide, 46 tall region.
    static AnimationChainList Source(bool diagonal, bool horizontal, bool vertical)
    {
        var chain = new FlatRedBall.Graphics.Animation.AnimationChain { Name = "Leg", Loop = true };
        chain.Add(new AnimationFrame
        {
            Texture = Texture(),
            LeftCoordinate = 0.10f,
            RightCoordinate = 0.32f,
            TopCoordinate = 0.20f,
            BottomCoordinate = 0.66f,
            FlipDiagonal = diagonal,
            FlipHorizontal = horizontal,
            FlipVertical = vertical,
            FrameLength = 1,
        });
        return new AnimationChainList { chain };
    }

    // Where each corner of the quad samples the texture, after the Sprite's flips.
    static (Vector2 TopLeft, Vector2 TopRight, Vector2 BottomRight, Vector2 BottomLeft) Corners(Sprite sprite)
    {
        return TextureCoordinateFlipper.Flip(
            new Vector2(sprite.LeftTextureCoordinate, sprite.TopTextureCoordinate),
            new Vector2(sprite.RightTextureCoordinate, sprite.TopTextureCoordinate),
            new Vector2(sprite.RightTextureCoordinate, sprite.BottomTextureCoordinate),
            new Vector2(sprite.LeftTextureCoordinate, sprite.BottomTextureCoordinate),
            sprite.FlipDiagonal, sprite.FlipHorizontal, sprite.FlipVertical);
    }

    static Sprite PlayedTrack(AnimationChainList source, bool flipHorizontal)
    {
        var track = new SequenceTrackSave { Name = "Leg", FlipHorizontal = flipHorizontal };
        track.Chains.Add(new SequenceChainReferenceSave { Source = "leg.achj", Chain = "Leg", Start = 0, Loops = 1 });
        var sequence = new SequenceSave { Name = "Idle", Loop = true };
        sequence.Tracks.Add(track);
        var file = new SequenceListSave();
        file.Sequences.Add(sequence);

        var idle = new SequencePlayer(new PositionedObject(), new SequenceList(file, _ => source))["Idle"];
        idle.Play();
        return idle.Tracks[0].Sprite;
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public void FlippedTrack_DrawsTheUnflippedTrackMirroredLeftToRight(bool diagonal, bool horizontal, bool vertical)
    {
        Sprite plain = PlayedTrack(Source(diagonal, horizontal, vertical), flipHorizontal: false);
        Sprite flipped = PlayedTrack(Source(diagonal, horizontal, vertical), flipHorizontal: true);

        var expected = Corners(plain);
        var actual = Corners(flipped);

        actual.TopLeft.ShouldBe(expected.TopRight);
        actual.TopRight.ShouldBe(expected.TopLeft);
        actual.BottomLeft.ShouldBe(expected.BottomRight);
        actual.BottomRight.ShouldBe(expected.BottomLeft);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FlippedTrack_KeepsTheSameSizeAsTheUnflippedTrack(bool diagonal)
    {
        Sprite plain = PlayedTrack(Source(diagonal, false, false), flipHorizontal: false);
        Sprite flipped = PlayedTrack(Source(diagonal, false, false), flipHorizontal: true);

        flipped.ScaleX.ShouldBe(plain.ScaleX, 0.001f);
        flipped.ScaleY.ShouldBe(plain.ScaleY, 0.001f);
    }
}
