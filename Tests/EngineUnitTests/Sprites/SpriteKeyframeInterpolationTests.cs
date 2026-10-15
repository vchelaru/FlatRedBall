using EngineUnitTests.TestSupport;
using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Graphics.Animation;
using Microsoft.Xna.Framework;
using Shouldly;

namespace EngineUnitTests.Sprites;

/// <summary>
/// A playing Sprite applies per-frame keyframe interpolation (offset + color) continuously, not only on frame change.
/// </summary>
[Collection(nameof(EngineStatefulTestCollection))]
public class SpriteKeyframeInterpolationTests
{
    public SpriteKeyframeInterpolationTests()
    {
        // TimeManager.Update needs the services' system clock.
        EngineTestBootstrap.EnsureInitialized();
    }

    static Sprite Play(params AnimationFrame[] frames)
    {
        var chain = new AnimationChain { Name = "c" };
        chain.AddRange(frames);
        var list = new AnimationChainList { chain };

        var sprite = new Sprite { AnimationChains = list };
        sprite.CurrentChainName = "c";
        sprite.Animate = true;
        return sprite;
    }

    // TimeManager caps one update at MaxFrameTime (0.5s), so longer waits are played as several updates.
    static void Step(Sprite sprite, double seconds)
    {
        while (seconds > 1e-9)
        {
            double chunk = System.Math.Min(seconds, TimeManager.MaxFrameTime);
            TimeManager.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(chunk)));
            sprite.AnimateSelf(TimeManager.CurrentTime);
            seconds -= chunk;
        }
    }

    static AnimationFrame Slide(float x, float y = 0, float length = 1f, string? type = null,
        string? easing = null, bool skip = false)
        => new()
        {
            FrameLength = length, RelativeX = x, RelativeY = y,
            OffsetInterpolation = type, OffsetEasing = easing, SkipOffsetKeyframe = skip,
        };

    static AnimationFrame Tint(int? alpha = null, int? red = null, string? type = null,
        bool skip = false, ColorOperation? op = null)
        => new()
        {
            FrameLength = 1f, Alpha = alpha / 255f, Red = red / 255f, ColorOperation = op,
            ColorInterpolation = type, SkipColorKeyframe = skip,
        };

    // ---- Offset ----

    [Fact]
    public void AnimateSelf_LinearOffset_SpriteGlidesBetweenFramesMidFrame()
    {
        var sprite = Play(Slide(0, 10, type: "Linear"), Slide(100, 30));

        Step(sprite, 0.5);

        sprite.CurrentFrameIndex.ShouldBe(0);
        sprite.RelativeX.ShouldBe(50f, 0.001f);
        sprite.RelativeY.ShouldBe(20f, 0.001f);
    }

    [Fact]
    public void AnimateSelf_LinearOffset_KeepsMovingEveryUpdate()
    {
        var sprite = Play(Slide(0, type: "Linear"), Slide(100));

        Step(sprite, 0.25);
        sprite.RelativeX.ShouldBe(25f, 0.001f);

        Step(sprite, 0.25);
        sprite.RelativeX.ShouldBe(50f, 0.001f);

        Step(sprite, 0.25);
        sprite.RelativeX.ShouldBe(75f, 0.001f);
    }

    [Fact]
    public void AnimateSelf_DefaultSnap_OffsetStaysAtFrameValueMidFrame()
    {
        var sprite = Play(Slide(10, 20), Slide(100, 30));

        Step(sprite, 0.7);

        sprite.RelativeX.ShouldBe(10f);
        sprite.RelativeY.ShouldBe(20f);
    }

    [Fact]
    public void AnimateSelf_EaseOutOffset_FollowsCurve()
    {
        var sprite = Play(Slide(0, type: "Quadratic", easing: "Out"), Slide(100));

        Step(sprite, 0.5);

        sprite.RelativeX.ShouldBe(75f, 0.001f);
    }

    [Fact]
    public void AnimateSelf_SkippedFrame_GlideSpansThroughIt()
    {
        var sprite = Play(Slide(0, type: "Linear"), Slide(999, skip: true), Slide(100));

        Step(sprite, 1.5);

        sprite.CurrentFrameIndex.ShouldBe(1);
        sprite.RelativeX.ShouldBe(75f, 0.001f);
    }

    [Fact]
    public void AnimateSelf_LoopingChain_LastKeyframeGlidesBackToFirst()
    {
        var sprite = Play(Slide(-10), Slide(10, type: "Linear"));

        Step(sprite, 1.5);

        sprite.RelativeX.ShouldBe(0f, 0.001f);
    }

    [Fact]
    public void AnimateSelf_UseAnimationRelativePositionFalse_PositionUntouched()
    {
        var sprite = Play(Slide(0, type: "Linear"), Slide(100));
        sprite.UseAnimationRelativePosition = false;
        sprite.RelativeX = 7;

        Step(sprite, 0.5);

        sprite.RelativeX.ShouldBe(7f);
    }

    [Fact]
    public void TimeIntoAnimation_MidFrame_AppliesInterpolatedOffset()
    {
        var sprite = Play(Slide(0, type: "Linear"), Slide(100));

        sprite.TimeIntoAnimation = 0.5;

        sprite.RelativeX.ShouldBe(50f, 0.001f);
    }

    [Fact]
    public void CurrentFrameIndex_Set_StartsAtFrameStartValue()
    {
        var sprite = Play(Slide(0, type: "Linear"), Slide(100, type: "Linear"), Slide(200));
        sprite.TimeIntoAnimation = 0.5;

        sprite.CurrentFrameIndex = 1;

        sprite.RelativeX.ShouldBe(100f, 0.001f);
    }

    // ---- Color ----

    [Fact]
    public void AnimateSelf_LinearAlpha_SpriteAlphaGlides()
    {
        var sprite = Play(Tint(alpha: 0, type: "Linear"), Tint(alpha: 255));

        Step(sprite, 0.5);

        sprite.Alpha.ShouldBe(128 / 255f, 0.0001f);
    }

    [Fact]
    public void AnimateSelf_LinearRedWithModulate_SpriteRedGlides()
    {
        var sprite = Play(
            Tint(red: 0, type: "Linear", op: ColorOperation.Modulate),
            Tint(red: 200, op: ColorOperation.Modulate));

        Step(sprite, 0.5);

        sprite.Red.ShouldBe(100 / 255f, 0.0001f);
        sprite.ColorOperation.ShouldBe(ColorOperation.Modulate);
    }

    [Fact]
    public void AnimateSelf_ColorOperationSnapsAtFrameChangeWhileChannelsGlide()
    {
        var sprite = Play(
            Tint(red: 0, type: "Linear", op: ColorOperation.Modulate),
            Tint(red: 200, op: ColorOperation.AddSubtract));

        Step(sprite, 0.5);

        sprite.ColorOperation.ShouldBe(ColorOperation.Modulate);
        sprite.Red.ShouldBe(100 / 255f, 0.0001f);
    }

    [Fact]
    public void AnimateSelf_DefaultSnapColor_AlphaIsFrameValueMidFrame()
    {
        var sprite = Play(Tint(alpha: 100), Tint(alpha: 255));

        Step(sprite, 0.7);

        sprite.Alpha.ShouldBe(100 / 255f);
    }

    [Fact]
    public void AnimateSelf_SkippedColorFrame_OwnValueIgnoredAndGlideSpansIt()
    {
        var sprite = Play(Tint(alpha: 0, type: "Linear"), Tint(alpha: 255, skip: true), Tint(alpha: 100));

        Step(sprite, 1.5);

        sprite.CurrentFrameIndex.ShouldBe(1);
        sprite.Alpha.ShouldBe(75 / 255f, 0.0001f);
    }

    [Fact]
    public void SetCurrentChainName_AfterGlidingChain_NewChainStartsFromItsOwnFirstFrame()
    {
        var gliding = new AnimationChain { Name = "c" };
        gliding.Add(Tint(alpha: 0, type: "Linear"));
        gliding.Add(Tint(alpha: 255));
        var other = new AnimationChain { Name = "other" };
        other.Add(Tint(alpha: 200));
        var sprite = new Sprite { AnimationChains = new AnimationChainList { gliding, other } };
        sprite.CurrentChainName = "c";
        sprite.Animate = true;
        Step(sprite, 0.5);

        sprite.CurrentChainName = "other";

        sprite.Alpha.ShouldBe(200 / 255f, 0.0001f);
    }

    // ---- Old animations are untouched ----

    [Fact]
    public void AnimateSelf_ChainWithoutKeyframeData_ColorAndOffsetExactlyAsBefore()
    {
        var first = new AnimationFrame { FrameLength = 1, RelativeX = 3, Alpha = 0.3f, Red = 0.123f, ColorOperation = ColorOperation.Modulate };
        var second = new AnimationFrame { FrameLength = 1, RelativeX = 9, Alpha = 0.9f };
        var sprite = Play(first, second);

        Step(sprite, 0.7);

        sprite.RelativeX.ShouldBe(3f);
        sprite.Alpha.ShouldBe(0.3f);
        sprite.Red.ShouldBe(0.123f);

        Step(sprite, 0.5);

        sprite.CurrentFrameIndex.ShouldBe(1);
        sprite.RelativeX.ShouldBe(9f);
        sprite.Alpha.ShouldBe(0.9f);
    }
}
