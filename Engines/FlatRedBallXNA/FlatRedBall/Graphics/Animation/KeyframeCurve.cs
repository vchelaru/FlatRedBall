using System;
using SMath = System.Math;

namespace FlatRedBall.Graphics.Animation
{
    /// <summary>
    /// The curve family of a <see cref="KeyframeCurve"/>. Names match the FlatRedBall StateInterpolation
    /// InterpolationType names, which are the names stored in .achx/.achj files.
    /// </summary>
    public enum KeyframeInterpolationType
    {
        Back,
        Bounce,
        Circular,
        Cubic,
        Elastic,
        Exponential,
        /// <summary>Snap: hold the start value until the next keyframe takes over.</summary>
        Instant,
        Linear,
        Quadratic,
        Quartic,
        Quintic,
        Sinusoidal
    }

    /// <summary>The direction of a <see cref="KeyframeCurve"/>'s easing.</summary>
    public enum KeyframeEasing
    {
        In,
        Out,
        InOut
    }

    /// <summary>
    /// How a keyframe gets from its own value to the next keyframe's: an interpolation type plus an easing
    /// direction. The default value is <see cref="Snap"/> (hold the start value, switch when playback reaches
    /// the next keyframe), which is also what an omitted or unrecognized type means in the file.
    /// Easing defaults to <see cref="KeyframeEasing.Out"/>.
    /// </summary>
    /// <remarks>
    /// The .achx/.achj files store the type and easing as plain names (see
    /// <see cref="AnimationFrame.OffsetInterpolation"/>), not as enum values, because the enums' zero values
    /// are <c>Back</c> and <c>In</c>. <see cref="Parse"/> maps the names back. Linear ignores easing, and so
    /// does Snap. The curve functions are float-only copies of the StateInterpolation library's, which the
    /// engine can't reference (that library references the engine); a unit test pins them to the library.
    /// </remarks>
    public readonly struct KeyframeCurve : IEquatable<KeyframeCurve>
    {
        // Nullable so default(KeyframeCurve) means Snap/Out, matching an omitted file field.
        private readonly KeyframeInterpolationType? _type;
        private readonly KeyframeEasing? _easing;

        private KeyframeCurve(KeyframeInterpolationType type, KeyframeEasing easing)
        {
            _type = type;
            _easing = easing;
        }

        /// <summary>Hold the start value until the next keyframe, then switch. The default.</summary>
        public static KeyframeCurve Snap => default;

        /// <summary>The curve family. <see cref="KeyframeInterpolationType.Instant"/> means snap.</summary>
        public KeyframeInterpolationType Type => _type ?? KeyframeInterpolationType.Instant;

        /// <summary>The easing direction. Ignored for snap and linear.</summary>
        public KeyframeEasing Easing => _easing ?? KeyframeEasing.Out;

        /// <summary>Whether this curve holds the start value rather than gliding.</summary>
        public bool IsSnap => Type == KeyframeInterpolationType.Instant;

        /// <summary>
        /// Builds a curve from the names stored in a file. An omitted, empty, "Instant" or unrecognized
        /// <paramref name="type"/> is snap; an omitted or unrecognized <paramref name="easing"/> is
        /// <see cref="KeyframeEasing.Out"/>. Names match case-insensitively; numeric strings are not accepted.
        /// </summary>
        public static KeyframeCurve Parse(string type, string easing)
        {
            KeyframeInterpolationType parsedType;
            if (!TryParseName(type, out parsedType) || parsedType == KeyframeInterpolationType.Instant)
            {
                return Snap;
            }

            KeyframeEasing parsedEasing;
            if (!TryParseName(easing, out parsedEasing))
            {
                parsedEasing = KeyframeEasing.Out;
            }

            return new KeyframeCurve(parsedType, parsedEasing);
        }

        /// <summary>
        /// Maps linear progress <paramref name="t"/> (0 at the keyframe, 1 at the next one) to eased progress.
        /// Endpoints are exact: 0 returns 0 and 1 returns 1, even for curves that overshoot in between.
        /// Snap returns 0 for every <paramref name="t"/> below 1.
        /// </summary>
        public float Evaluate(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            if (IsSnap) return 0f;
            if (Type == KeyframeInterpolationType.Linear) return t;
            return KeyframeEasingFunctions.Ease(Type, Easing, t);
        }

        public bool Equals(KeyframeCurve other) => Type == other.Type && Easing == other.Easing;

        public override bool Equals(object obj) => obj is KeyframeCurve other && Equals(other);

        public override int GetHashCode() => ((int)Type * 397) ^ (int)Easing;

        public override string ToString() => IsSnap ? "Snap" : Type + " " + Easing;

        public static bool operator ==(KeyframeCurve left, KeyframeCurve right) => left.Equals(right);

        public static bool operator !=(KeyframeCurve left, KeyframeCurve right) => !left.Equals(right);

        private static bool TryParseName<TEnum>(string name, out TEnum value) where TEnum : struct
        {
            value = default(TEnum);
            // Enum.TryParse would accept "3" and comma lists; the file stores names only.
            if (string.IsNullOrWhiteSpace(name) || !char.IsLetter(name[0])) return false;
            return Enum.TryParse(name, true, out value) && Enum.IsDefined(typeof(TEnum), value);
        }
    }

    // Float-only copies of the StateInterpolation easing functions with start 0, change 1, duration 1.
    // Arithmetic is kept identical to the originals so results match the library bit for bit.
    internal static class KeyframeEasingFunctions
    {
        private const float BackOvershoot = 1.70158f;

        public static float Ease(KeyframeInterpolationType type, KeyframeEasing easing, float t)
        {
            // Straight switches rather than delegates so a playing animation allocates nothing per update.
            switch (type)
            {
                case KeyframeInterpolationType.Back:
                    return easing == KeyframeEasing.In ? BackIn(t) : easing == KeyframeEasing.InOut ? BackInOut(t) : BackOut(t);
                case KeyframeInterpolationType.Bounce:
                    return easing == KeyframeEasing.In ? BounceIn(t) : easing == KeyframeEasing.InOut ? BounceInOut(t) : BounceOut(t);
                case KeyframeInterpolationType.Circular:
                    return easing == KeyframeEasing.In ? CircularIn(t) : easing == KeyframeEasing.InOut ? CircularInOut(t) : CircularOut(t);
                case KeyframeInterpolationType.Cubic:
                    return easing == KeyframeEasing.In ? CubicIn(t) : easing == KeyframeEasing.InOut ? CubicInOut(t) : CubicOut(t);
                case KeyframeInterpolationType.Elastic:
                    return easing == KeyframeEasing.In ? ElasticIn(t) : easing == KeyframeEasing.InOut ? ElasticInOut(t) : ElasticOut(t);
                case KeyframeInterpolationType.Exponential:
                    return easing == KeyframeEasing.In ? ExponentialIn(t) : easing == KeyframeEasing.InOut ? ExponentialInOut(t) : ExponentialOut(t);
                case KeyframeInterpolationType.Quadratic:
                    return easing == KeyframeEasing.In ? QuadraticIn(t) : easing == KeyframeEasing.InOut ? QuadraticInOut(t) : QuadraticOut(t);
                case KeyframeInterpolationType.Quartic:
                    return easing == KeyframeEasing.In ? QuarticIn(t) : easing == KeyframeEasing.InOut ? QuarticInOut(t) : QuarticOut(t);
                case KeyframeInterpolationType.Quintic:
                    return easing == KeyframeEasing.In ? QuinticIn(t) : easing == KeyframeEasing.InOut ? QuinticInOut(t) : QuinticOut(t);
                case KeyframeInterpolationType.Sinusoidal:
                    return easing == KeyframeEasing.In ? SinusoidalIn(t) : easing == KeyframeEasing.InOut ? SinusoidalInOut(t) : SinusoidalOut(t);
                default:
                    return t;
            }
        }

        private static float BackIn(float t)
        {
            return t * t * ((BackOvershoot + 1) * t - BackOvershoot);
        }

        private static float BackOut(float t)
        {
            t = t - 1;
            return t * t * ((BackOvershoot + 1) * t + BackOvershoot) + 1;
        }

        private static float BackInOut(float t)
        {
            float s = BackOvershoot;
            t /= 0.5f;
            if (t < 1)
            {
                return 0.5f * (t * t * (((s *= 1.525f) + 1) * t - s));
            }
            t -= 2;
            return 0.5f * (t * t * (((s *= 1.525f) + 1) * t + s) + 2);
        }

        private static float BounceOut(float t)
        {
            if (t < (1 / 2.75))
            {
                return 7.5625f * t * t;
            }
            else if (t < (2 / 2.75))
            {
                return 7.5625f * (t -= (1.5f / 2.75f)) * t + .75f;
            }
            else if (t < (2.5 / 2.75))
            {
                return 7.5625f * (t -= (2.25f / 2.75f)) * t + .9375f;
            }
            else
            {
                return 7.5625f * (t -= (2.625f / 2.75f)) * t + .984375f;
            }
        }

        private static float BounceIn(float t)
        {
            return 1 - BounceOut(1 - t);
        }

        private static float BounceInOut(float t)
        {
            if (t < 0.5f) return BounceIn(t * 2) * 0.5f;
            return BounceOut(t * 2 - 1) * .5f + 0.5f;
        }

        private static float CircularIn(float t)
        {
            return -((float)SMath.Sqrt(1 - t * t) - 1);
        }

        private static float CircularOut(float t)
        {
            t = t - 1;
            return (float)SMath.Sqrt(1 - t * t);
        }

        private static float CircularInOut(float t)
        {
            t /= 0.5f;
            if (t < 1)
            {
                return -0.5f * ((float)SMath.Sqrt(1 - t * t) - 1);
            }
            t -= 2;
            return 0.5f * ((float)SMath.Sqrt(1 - t * t) + 1);
        }

        private static float CubicIn(float t)
        {
            return t * t * t;
        }

        private static float CubicOut(float t)
        {
            t = t - 1;
            return t * t * t + 1;
        }

        private static float CubicInOut(float t)
        {
            t /= 0.5f;
            if (t < 1)
            {
                return 0.5f * t * t * t;
            }
            t -= 2;
            return 0.5f * (t * t * t + 2);
        }

        private static float ElasticIn(float t)
        {
            float p = .3f;
            float s = p / 4;
            t -= 1;
            return -(float)(SMath.Pow(2, 10 * t) * SMath.Sin((t - s) * (2 * SMath.PI) / p));
        }

        private static float ElasticOut(float t)
        {
            float p = .3f;
            float s = p / 4;
            return (float)(SMath.Pow(2, -10 * t) * SMath.Sin((t - s) * (2 * SMath.PI) / p) + 1);
        }

        private static float ElasticInOut(float t)
        {
            float p = .3f * 1.5f;
            float s = p / 4;
            t /= 0.5f;
            if (t < 1)
            {
                t -= 1;
                return -.5f * (float)(SMath.Pow(2, 10 * t) * SMath.Sin((t - s) * (2 * SMath.PI) / p));
            }
            t -= 1;
            return (float)(SMath.Pow(2, -10 * t) * SMath.Sin((t - s) * (2 * SMath.PI) / p) * .5 + 1);
        }

        private static float ExponentialIn(float t)
        {
            return (float)SMath.Pow(2, 10 * (t - 1));
        }

        private static float ExponentialOut(float t)
        {
            return (float)(-SMath.Pow(2, -10 * t) + 1);
        }

        private static float ExponentialInOut(float t)
        {
            t /= 0.5f;
            if (t < 1)
            {
                return 1 / 2f * (float)SMath.Pow(2, 10 * (t - 1));
            }
            return 1 / 2f * (float)(-SMath.Pow(2, -10 * --t) + 2);
        }

        private static float QuadraticIn(float t)
        {
            return t * t;
        }

        private static float QuadraticOut(float t)
        {
            return -1 * t * (t - 2);
        }

        private static float QuadraticInOut(float t)
        {
            t /= 0.5f;
            if (t < 1)
            {
                return 1 / 2f * t * t;
            }
            return -1 / 2f * ((--t) * (t - 2) - 1);
        }

        private static float QuarticIn(float t)
        {
            return t * t * t * t;
        }

        private static float QuarticOut(float t)
        {
            t = t - 1;
            return -(t * t * t * t - 1);
        }

        private static float QuarticInOut(float t)
        {
            t /= 0.5f;
            if (t < 1)
            {
                return 1 / 2f * t * t * t * t;
            }
            t -= 2;
            return -1 / 2f * (t * t * t * t - 2);
        }

        private static float QuinticIn(float t)
        {
            return t * t * t * t * t;
        }

        private static float QuinticOut(float t)
        {
            t = t - 1;
            return t * t * t * t * t + 1;
        }

        private static float QuinticInOut(float t)
        {
            t /= 0.5f;
            if (t < 1)
            {
                return 1 / 2f * t * t * t * t * t;
            }
            t -= 2;
            return 1 / 2f * (t * t * t * t * t + 2);
        }

        private static float SinusoidalIn(float t)
        {
            return -(float)SMath.Cos(t * (SMath.PI / 2)) + 1;
        }

        private static float SinusoidalOut(float t)
        {
            return (float)SMath.Sin(t * (SMath.PI / 2));
        }

        private static float SinusoidalInOut(float t)
        {
            return -1 / 2f * ((float)SMath.Cos(SMath.PI * t) - 1);
        }
    }
}
