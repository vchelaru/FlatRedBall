using FlatRedBall.Graphics;
using FlatRedBall.Graphics.Animation;
using Shouldly;

namespace EngineUnitTests.Graphics.Animation;

/// <summary>
/// Per-frame keyframe interpolation: every frame is a keyframe unless it opts out per group; a keyframe's
/// curve runs to the next keyframe of the same group, spanning the summed frame lengths in between.
/// Mirrors FlatRedBall2's KeyframeEvaluatorTests so both engines agree.
/// </summary>
public class KeyframeEvaluatorTests
{
    static AnimationFrame Frame(float x = 0, float y = 0, float length = 1f,
        string? offsetType = null, string? offsetEasing = null, bool skipOffset = false)
        => new()
        {
            FrameLength = length,
            RelativeX = x,
            RelativeY = y,
            OffsetInterpolation = offsetType,
            OffsetEasing = offsetEasing,
            SkipOffsetKeyframe = skipOffset,
        };

    static AnimationChain Chain(params AnimationFrame[] frames)
    {
        var chain = new AnimationChain { Name = "c" };
        chain.AddRange(frames);
        return chain;
    }

    static Microsoft.Xna.Framework.Vector2 Offset(AnimationChain chain, bool loop, int index, double elapsed)
        => KeyframeEvaluator.EvaluateOffset(chain, loop, index, elapsed);

    // ---- Offset: snap is today's behavior -------------------------------------------------------

    [Fact]
    public void EvaluateOffset_DefaultFrames_ReturnsFrameOwnOffsetAtAnyElapsed()
    {
        var chain = Chain(Frame(10, 20), Frame(50, 60), Frame(-5, 7));

        for (int i = 0; i < 3; i++)
            foreach (double elapsed in new[] { 0.0, 0.3, 0.999, 1.0 })
            {
                var offset = Offset(chain, true, i, elapsed);
                offset.X.ShouldBe(chain[i].RelativeX);
                offset.Y.ShouldBe(chain[i].RelativeY);
            }
    }

    [Fact]
    public void EvaluateOffset_ExplicitInstant_BehavesAsSnap()
    {
        var chain = Chain(Frame(0, offsetType: "Instant"), Frame(100));

        Offset(chain, true, 0, 0.99).X.ShouldBe(0f);
        Offset(chain, true, 0, 1.0).X.ShouldBe(0f);
    }

    // ---- Offset: glide --------------------------------------------------------------------------

    [Fact]
    public void EvaluateOffset_LinearToNextFrame_ReturnsMidpointAtHalfway()
    {
        var chain = Chain(Frame(0, 10, offsetType: "Linear"), Frame(100, 30));

        var offset = Offset(chain, true, 0, 0.5);

        offset.X.ShouldBe(50f, 0.001f);
        offset.Y.ShouldBe(20f, 0.001f);
    }

    [Fact]
    public void EvaluateOffset_ArrivesExactlyAtNextKeyframeValue()
    {
        var chain = Chain(Frame(0, offsetType: "Linear"), Frame(100));

        Offset(chain, true, 1, 0).X.ShouldBe(100f);
    }

    [Fact]
    public void EvaluateOffset_AtSegmentEnd_ReturnsNextKeyframeValueExactly()
    {
        var chain = Chain(Frame(0, offsetType: "Back", offsetEasing: "InOut"), Frame(100));

        Offset(chain, true, 0, 1.0).X.ShouldBe(100f);
    }

    [Fact]
    public void EvaluateOffset_UnequalFrameLengths_UsesOwnFrameLength()
    {
        var chain = Chain(Frame(0, length: 4f, offsetType: "Linear"), Frame(100, length: 1f));

        Offset(chain, true, 0, 1.0).X.ShouldBe(25f, 0.001f);
    }

    // ---- Offset: skipped frames -----------------------------------------------------------------

    [Fact]
    public void EvaluateOffset_SkippedFrameBetween_GlideSpansSummedFrameLengths()
    {
        var chain = Chain(
            Frame(0, offsetType: "Linear"),
            Frame(skipOffset: true),
            Frame(100));

        Offset(chain, true, 0, 0.5).X.ShouldBe(25f, 0.001f);
        Offset(chain, true, 1, 0.5).X.ShouldBe(75f, 0.001f);
        Offset(chain, true, 2, 0).X.ShouldBe(100f);
    }

    [Fact]
    public void EvaluateOffset_SkippedFrame_OwnValueAndCurveAreIgnored()
    {
        var chain = Chain(
            Frame(0, offsetType: "Linear"),
            Frame(999, 999, offsetType: "Cubic", offsetEasing: "In", skipOffset: true),
            Frame(100));

        var offset = Offset(chain, true, 1, 0.5);

        offset.X.ShouldBe(75f, 0.001f);
        offset.Y.ShouldBe(0f, 0.001f);
    }

    [Fact]
    public void EvaluateOffset_SkippedFramesAfterSnapKeyframe_HoldStartThenSnapAtNextKeyframe()
    {
        var chain = Chain(
            Frame(10),
            Frame(skipOffset: true),
            Frame(skipOffset: true),
            Frame(90));

        Offset(chain, true, 1, 0.9).X.ShouldBe(10f);
        Offset(chain, true, 2, 0.9).X.ShouldBe(10f);
        Offset(chain, true, 3, 0).X.ShouldBe(90f);
    }

    [Fact]
    public void EvaluateOffset_FirstFrameMarkedSkipped_StillActsAsKeyframe()
    {
        var chain = Chain(Frame(0, offsetType: "Linear", skipOffset: true), Frame(100));

        Offset(chain, true, 0, 0.5).X.ShouldBe(50f, 0.001f);
    }

    // ---- Offset: ends of the chain --------------------------------------------------------------

    [Fact]
    public void EvaluateOffset_LoopingChain_LastKeyframeGlidesBackToFirst()
    {
        var chain = Chain(Frame(-10), Frame(10, offsetType: "Linear"));

        Offset(chain, true, 1, 0.5).X.ShouldBe(0f, 0.001f);
    }

    [Fact]
    public void EvaluateOffset_LoopingChain_TrailingSkippedFramesAreCoveredByWrapGlide()
    {
        var chain = Chain(
            Frame(0),
            Frame(100, offsetType: "Linear"),
            Frame(skipOffset: true));

        Offset(chain, true, 1, 0.5).X.ShouldBe(75f, 0.001f);
        Offset(chain, true, 2, 0.5).X.ShouldBe(25f, 0.001f);
    }

    [Fact]
    public void EvaluateOffset_NonLoopingChain_LastFrameHolds()
    {
        var chain = Chain(Frame(0, offsetType: "Linear"), Frame(100, offsetType: "Linear"));

        Offset(chain, false, 1, 0.5).X.ShouldBe(100f);
    }

    [Fact]
    public void EvaluateOffset_NonLoopingChain_TrailingSkippedFramesHoldLastKeyframe()
    {
        var chain = Chain(
            Frame(0),
            Frame(100, offsetType: "Linear"),
            Frame(skipOffset: true));

        Offset(chain, false, 2, 0.5).X.ShouldBe(100f);
    }

    [Fact]
    public void EvaluateOffset_SingleFrameChain_HoldsOwnValueEvenWhenLooping()
    {
        var chain = Chain(Frame(7, 9, offsetType: "Linear"));

        var offset = Offset(chain, true, 0, 0.5);

        offset.X.ShouldBe(7f);
        offset.Y.ShouldBe(9f);
    }

    [Fact]
    public void EvaluateOffset_LoopingWithSingleKeyframeAndSkippedRest_Holds()
    {
        var chain = Chain(Frame(7, offsetType: "Linear"), Frame(99, skipOffset: true));

        Offset(chain, true, 1, 0.5).X.ShouldBe(7f);
    }

    [Fact]
    public void EvaluateOffset_EmptyChain_ReturnsZero()
    {
        Offset(Chain(), true, 0, 0).ShouldBe(Microsoft.Xna.Framework.Vector2.Zero);
    }

    // ---- Offset: easing -------------------------------------------------------------------------

    [Theory]
    [InlineData("In", 25f)]
    [InlineData("Out", 75f)]
    [InlineData("InOut", 50f)]
    [InlineData(null, 75f)]
    [InlineData("NotAnEasing", 75f)]
    public void EvaluateOffset_QuadraticAtHalfway_FollowsEasing(string? easing, float expected)
    {
        var chain = Chain(Frame(0, offsetType: "Quadratic", offsetEasing: easing), Frame(100));

        Offset(chain, true, 0, 0.5).X.ShouldBe(expected, 0.001f);
    }

    [Theory]
    [InlineData("In")]
    [InlineData("Out")]
    [InlineData("InOut")]
    public void EvaluateOffset_LinearIgnoresEasing(string easing)
    {
        var chain = Chain(Frame(0, offsetType: "Linear", offsetEasing: easing), Frame(100));

        Offset(chain, true, 0, 0.25).X.ShouldBe(25f, 0.001f);
    }

    [Theory]
    [InlineData("In")]
    [InlineData("Out")]
    [InlineData("InOut")]
    public void EvaluateOffset_InstantIgnoresEasing_HoldsStartUntilSegmentEnd(string easing)
    {
        var chain = Chain(Frame(0, offsetType: "Instant", offsetEasing: easing), Frame(100));

        Offset(chain, true, 0, 0.999).X.ShouldBe(0f);
    }

    [Theory]
    [InlineData("Bogus")]
    [InlineData("")]
    [InlineData("3")]
    public void EvaluateOffset_UnknownCurveName_TreatedAsSnap(string type)
    {
        var chain = Chain(Frame(0, offsetType: type), Frame(100));

        Offset(chain, true, 0, 0.5).X.ShouldBe(0f);
    }

    [Fact]
    public void EvaluateOffset_EasingWithoutType_StaysSnap()
    {
        var chain = Chain(Frame(0, offsetEasing: "In"), Frame(100));

        Offset(chain, true, 0, 0.5).X.ShouldBe(0f);
    }

    [Fact]
    public void EvaluateOffset_StringChangedAfterAssignment_UsesNewCurve()
    {
        var first = Frame(0, offsetType: "Linear");
        var chain = Chain(first, Frame(100));
        Offset(chain, true, 0, 0.5).X.ShouldBe(50f, 0.001f);

        first.OffsetInterpolation = null;

        Offset(chain, true, 0, 0.5).X.ShouldBe(0f);
    }

    // ---- Color ----------------------------------------------------------------------------------

    static AnimationFrame ColorFrame(int? red = null, int? green = null, int? alpha = null,
        string? type = null, string? easing = null, bool skip = false, ColorOperation? op = null)
        => new()
        {
            FrameLength = 1f,
            Red = red / 255f,
            Green = green / 255f,
            Alpha = alpha / 255f,
            ColorInterpolation = type,
            ColorEasing = easing,
            SkipColorKeyframe = skip,
            ColorOperation = op,
        };

    [Fact]
    public void EvaluateColor_DefaultFrames_ReturnsResolvedFrameColor()
    {
        var chain = Chain(
            ColorFrame(red: 10, alpha: 200),
            ColorFrame(green: 40));

        var c0 = KeyframeEvaluator.EvaluateColor(chain, true, 0, 0.7);
        c0.Red.ShouldBe(10);
        c0.Green.ShouldBeNull();
        c0.Alpha.ShouldBe(200);

        var c1 = KeyframeEvaluator.EvaluateColor(chain, true, 1, 0.7);
        c1.Red.ShouldBe(10);
        c1.Green.ShouldBe(40);
        c1.Alpha.ShouldBe(200);
    }

    [Fact]
    public void EvaluateColor_ChannelsInterpolateIndependently()
    {
        var chain = Chain(
            ColorFrame(red: 0, green: 0, type: "Linear"),
            ColorFrame(red: 100, green: 200));

        var c = KeyframeEvaluator.EvaluateColor(chain, true, 0, 0.5);

        c.Red.ShouldBe(50);
        c.Green.ShouldBe(100);
    }

    [Fact]
    public void EvaluateColor_NullChannelOnKeyframeInheritsBeforeInterpolating()
    {
        var chain = Chain(
            ColorFrame(red: 0),
            ColorFrame(type: "Linear"),
            ColorFrame(red: 100));

        KeyframeEvaluator.EvaluateColor(chain, true, 1, 0.5).Red.ShouldBe(50);
    }

    [Fact]
    public void EvaluateColor_ChannelNullAtBothEnds_StaysNull()
    {
        var chain = Chain(ColorFrame(red: 0, type: "Linear"), ColorFrame(red: 100));

        KeyframeEvaluator.EvaluateColor(chain, true, 0, 0.5).Green.ShouldBeNull();
    }

    [Fact]
    public void EvaluateColor_ChannelNullAtOneEnd_UsesOperationIdentity()
    {
        // Unset Alpha reads as 255 (opaque), so 255 -> 100 at the midpoint is 177.5, rounded away from zero.
        var chain = Chain(ColorFrame(type: "Linear"), ColorFrame(alpha: 100));

        KeyframeEvaluator.EvaluateColor(chain, true, 0, 0.5).Alpha.ShouldBe(178);
    }

    [Theory]
    [InlineData(ColorOperation.Add)]
    [InlineData(ColorOperation.AddSubtract)]
    public void EvaluateColor_AddOperationNullChannel_UsesZeroIdentity(ColorOperation op)
    {
        var chain = Chain(
            ColorFrame(op: op, type: "Linear"),
            ColorFrame(red: 100, op: op));

        KeyframeEvaluator.EvaluateColor(chain, true, 0, 0.5).Red.ShouldBe(50);
    }

    [Fact]
    public void EvaluateColor_ModulateNullChannel_Uses255Identity()
    {
        var chain = Chain(
            ColorFrame(type: "Linear", op: ColorOperation.Modulate),
            ColorFrame(red: 100, op: ColorOperation.Modulate));

        KeyframeEvaluator.EvaluateColor(chain, true, 0, 0.5).Red.ShouldBe(178);
    }

    [Fact]
    public void EvaluateColor_RoundsAwayFromZero_ForNegativeValues()
    {
        var chain = Chain(
            ColorFrame(red: 0, type: "Linear", op: ColorOperation.AddSubtract),
            ColorFrame(red: -5, op: ColorOperation.AddSubtract));

        // -2.5 rounds away from zero to -3.
        KeyframeEvaluator.EvaluateColor(chain, true, 0, 0.5).Red.ShouldBe(-3);
    }

    [Fact]
    public void EvaluateColor_SkippedFrame_GlideSpansItAndItsOwnValueIsIgnored()
    {
        var chain = Chain(
            ColorFrame(red: 0, type: "Linear"),
            ColorFrame(red: 255, skip: true),
            ColorFrame(red: 100));

        KeyframeEvaluator.EvaluateColor(chain, true, 1, 0.5).Red.ShouldBe(75);
    }

    [Fact]
    public void EvaluateColor_KeyframeAfterSkippedFrame_DoesNotInheritSkippedFramesChannel()
    {
        var chain = Chain(
            ColorFrame(red: 20),
            ColorFrame(red: 255, skip: true),
            ColorFrame());

        KeyframeEvaluator.EvaluateColor(chain, false, 2, 0).Red.ShouldBe(20);
    }

    [Fact]
    public void EvaluateColor_SkippedFrameAfterSnapKeyframe_HoldsKeyframeColor()
    {
        var chain = Chain(
            ColorFrame(red: 20),
            ColorFrame(red: 255, skip: true),
            ColorFrame(red: 90));

        KeyframeEvaluator.EvaluateColor(chain, true, 1, 0.9).Red.ShouldBe(20);
        KeyframeEvaluator.EvaluateColor(chain, true, 2, 0).Red.ShouldBe(90);
    }

    [Fact]
    public void EvaluateColor_FirstFrameMarkedSkipped_StillActsAsKeyframe()
    {
        var chain = Chain(ColorFrame(red: 0, type: "Linear", skip: true), ColorFrame(red: 100));

        KeyframeEvaluator.EvaluateColor(chain, true, 0, 0.5).Red.ShouldBe(50);
    }

    [Fact]
    public void EvaluateOffsetAndColorGroupsAreIndependent()
    {
        var chain = Chain(
            new AnimationFrame { FrameLength = 1f, OffsetInterpolation = "Linear", ColorInterpolation = "Linear", Red = 0f },
            new AnimationFrame { FrameLength = 1f, RelativeX = 100, Red = 100 / 255f, SkipColorKeyframe = true },
            new AnimationFrame { FrameLength = 1f, RelativeX = 200, Red = 200 / 255f });

        // Frame 1 is an offset keyframe (snap), but color is skipped there and glides 0 -> 200 over 2s.
        Offset(chain, true, 1, 0.5).X.ShouldBe(100f);
        KeyframeEvaluator.EvaluateColor(chain, true, 1, 0.5).Red.ShouldBe(150);
    }

    [Fact]
    public void EvaluateColor_LoopingLastKeyframeGlidesToFirst()
    {
        var chain = Chain(ColorFrame(alpha: 0), ColorFrame(alpha: 200, type: "Linear"));

        KeyframeEvaluator.EvaluateColor(chain, true, 1, 0.5).Alpha.ShouldBe(100);
    }

    [Fact]
    public void EvaluateColor_NonLoopingLastFrameHolds()
    {
        var chain = Chain(ColorFrame(alpha: 0, type: "Linear"), ColorFrame(alpha: 200, type: "Linear"));

        KeyframeEvaluator.EvaluateColor(chain, false, 1, 0.5).Alpha.ShouldBe(200);
    }

    // ---- Color inheritance (skip-aware) ----------------------------------------------------------

    [Fact]
    public void ResolveColor_ChannelSetOnEarlierFrame_CarriesForward()
    {
        var chain = Chain(ColorFrame(alpha: 250), ColorFrame(), ColorFrame());

        KeyframeEvaluator.ResolveColor(chain, 2).Alpha.ShouldBe(250);
    }

    [Fact]
    public void ResolveColor_ChannelsSetOnDifferentFrames_ResolveIndependently()
    {
        var chain = Chain(ColorFrame(red: 200), ColorFrame(green: 50), ColorFrame());

        var resolved = KeyframeEvaluator.ResolveColor(chain, 2);

        resolved.Red.ShouldBe(200);
        resolved.Green.ShouldBe(50);
        resolved.Alpha.ShouldBeNull();
    }

    [Fact]
    public void ResolveColor_LaterFrameReSetsChannel_ReturnsMostRecentValue()
    {
        var chain = Chain(ColorFrame(alpha: 250), ColorFrame(alpha: 100), ColorFrame());

        KeyframeEvaluator.ResolveColor(chain, 2).Alpha.ShouldBe(100);
    }

    [Fact]
    public void ResolveColor_SkippedFrameChannels_AreNotInherited()
    {
        var chain = Chain(ColorFrame(red: 20), ColorFrame(red: 255, skip: true), ColorFrame());

        KeyframeEvaluator.ResolveColor(chain, 1).Red.ShouldBe(20);
        KeyframeEvaluator.ResolveColor(chain, 2).Red.ShouldBe(20);
    }

    [Fact]
    public void ResolveColor_SkippedFrameColorOperation_StillApplies()
    {
        var chain = Chain(
            ColorFrame(op: ColorOperation.Modulate),
            ColorFrame(op: ColorOperation.AddSubtract, skip: true));

        KeyframeEvaluator.ResolveColor(chain, 1).Operation.ShouldBe(ColorOperation.AddSubtract);
    }

    [Fact]
    public void ResolveColor_FirstFrameMarkedSkipped_StillContributes()
    {
        var chain = Chain(ColorFrame(red: 20, skip: true), ColorFrame());

        KeyframeEvaluator.ResolveColor(chain, 1).Red.ShouldBe(20);
    }

    [Fact]
    public void ResolveColor_OutOfRangeIndex_Clamps()
    {
        var chain = Chain(ColorFrame(red: 20), ColorFrame(red: 30));

        KeyframeEvaluator.ResolveColor(chain, 9).Red.ShouldBe(30);
    }

    // ---- HasKeyframeData ------------------------------------------------------------------------

    [Fact]
    public void HasKeyframeData_PlainFrames_IsFalse()
    {
        Chain(Frame(1), ColorFrame(red: 5)).HasKeyframeData().ShouldBeFalse();
    }

    [Theory]
    [InlineData("offsetType")]
    [InlineData("colorType")]
    [InlineData("skipOffset")]
    [InlineData("skipColor")]
    public void HasKeyframeData_AnyKeyframeField_IsTrue(string which)
    {
        var frame = new AnimationFrame { FrameLength = 1 };
        switch (which)
        {
            case "offsetType": frame.OffsetInterpolation = "Linear"; break;
            case "colorType": frame.ColorInterpolation = "Linear"; break;
            case "skipOffset": frame.SkipOffsetKeyframe = true; break;
            case "skipColor": frame.SkipColorKeyframe = true; break;
        }

        Chain(Frame(), frame).HasKeyframeData().ShouldBeTrue();
    }

    [Fact]
    public void HasKeyframeData_UnknownCurveNameOnly_IsFalse()
    {
        // Unrecognized types are snap, and snap with no skip flags is exactly the old behavior.
        Chain(Frame(offsetType: "FromTheFuture"), Frame()).HasKeyframeData().ShouldBeFalse();
    }
}
