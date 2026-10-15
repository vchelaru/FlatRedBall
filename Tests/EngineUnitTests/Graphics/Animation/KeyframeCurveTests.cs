using FlatRedBall.Glue.StateInterpolation;
using FlatRedBall.Graphics.Animation;
using Shouldly;

namespace EngineUnitTests.Graphics.Animation;

public class KeyframeCurveTests
{
    [Fact]
    public void Default_IsSnapWithOutEasing()
    {
        var curve = default(KeyframeCurve);

        curve.IsSnap.ShouldBeTrue();
        curve.Type.ShouldBe(KeyframeInterpolationType.Instant);
        curve.Easing.ShouldBe(KeyframeEasing.Out);
    }

    [Fact]
    public void Parse_NullType_IsSnap()
    {
        KeyframeCurve.Parse(null, "In").IsSnap.ShouldBeTrue();
    }

    [Fact]
    public void Parse_OmittedEasing_IsOut()
    {
        KeyframeCurve.Parse("Quadratic", null).Easing.ShouldBe(KeyframeEasing.Out);
    }

    [Theory]
    [InlineData("Bogus")]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("7")]
    [InlineData("Instant")]
    public void Parse_UnknownOrInstantType_IsSnap(string type)
    {
        KeyframeCurve.Parse(type, "In").IsSnap.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Bogus")]
    [InlineData("9")]
    [InlineData("")]
    public void Parse_UnknownEasing_IsOut(string easing)
    {
        KeyframeCurve.Parse("Cubic", easing).Easing.ShouldBe(KeyframeEasing.Out);
    }

    [Fact]
    public void Parse_IsCaseInsensitive()
    {
        var curve = KeyframeCurve.Parse("cUbIc", "inout");

        curve.Type.ShouldBe(KeyframeInterpolationType.Cubic);
        curve.Easing.ShouldBe(KeyframeEasing.InOut);
    }

    [Fact]
    public void Parse_AllThirtyThreeCurves_AreRepresentable()
    {
        int count = 0;
        foreach (InterpolationType type in Enum.GetValues(typeof(InterpolationType)))
        {
            if (type == InterpolationType.Instant) continue;
            foreach (Easing easing in Enum.GetValues(typeof(Easing)))
            {
                var curve = KeyframeCurve.Parse(type.ToString(), easing.ToString());
                curve.IsSnap.ShouldBeFalse();
                curve.Type.ToString().ShouldBe(type.ToString());
                curve.Easing.ToString().ShouldBe(easing.ToString());
                count++;
            }
        }
        count.ShouldBe(33);
    }

    [Fact]
    public void Evaluate_Snap_HoldsZeroUntilSegmentEnd()
    {
        KeyframeCurve.Snap.Evaluate(0f).ShouldBe(0f);
        KeyframeCurve.Snap.Evaluate(0.999f).ShouldBe(0f);
        KeyframeCurve.Snap.Evaluate(1f).ShouldBe(1f);
    }

    [Theory]
    [InlineData("Back")]
    [InlineData("Bounce")]
    [InlineData("Elastic")]
    [InlineData("Cubic")]
    [InlineData("Linear")]
    public void Evaluate_EndpointsAreExact(string type)
    {
        foreach (var easing in new[] { "In", "Out", "InOut" })
        {
            var curve = KeyframeCurve.Parse(type, easing);
            curve.Evaluate(0f).ShouldBe(0f);
            curve.Evaluate(1f).ShouldBe(1f);
        }
    }

    [Fact]
    public void Evaluate_Linear_IgnoresEasing()
    {
        foreach (var easing in new[] { "In", "Out", "InOut" })
        {
            KeyframeCurve.Parse("Linear", easing).Evaluate(0.3f).ShouldBe(0.3f, 0.0001f);
        }
    }

    [Fact]
    public void Evaluate_Quadratic_MatchesKnownValues()
    {
        KeyframeCurve.Parse("Quadratic", "In").Evaluate(0.5f).ShouldBe(0.25f, 0.0001f);
        KeyframeCurve.Parse("Quadratic", "Out").Evaluate(0.5f).ShouldBe(0.75f, 0.0001f);
        KeyframeCurve.Parse("Quadratic", "InOut").Evaluate(0.5f).ShouldBe(0.5f, 0.0001f);
    }

    // The engine can't reference the StateInterpolation library (it references the engine), so the
    // curve functions are vendored. This pins the copy to the library for every curve.
    [Fact]
    public void Evaluate_MatchesStateInterpolationLibrary_ForEveryCurve()
    {
        foreach (InterpolationType type in Enum.GetValues(typeof(InterpolationType)))
        {
            if (type == InterpolationType.Instant) continue;
            foreach (Easing easing in Enum.GetValues(typeof(Easing)))
            {
                var library = Tweener.GetInterpolationFunction(type, easing);
                var curve = KeyframeCurve.Parse(type.ToString(), easing.ToString());

                for (int i = 1; i < 100; i++)
                {
                    float t = i / 100f;
                    curve.Evaluate(t).ShouldBe(library(t, 0f, 1f, 1f), 1e-5f, $"{type} {easing} at {t}");
                }
            }
        }
    }
}
