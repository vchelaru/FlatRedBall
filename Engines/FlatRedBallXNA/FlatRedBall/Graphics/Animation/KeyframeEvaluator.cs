using System;
using SMath = System.Math;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace FlatRedBall.Graphics.Animation
{
    /// <summary>
    /// The color channels at one instant of playback, in the 0 - 255 domain the .achx stores them in.
    /// A null channel means no frame ever set it.
    /// </summary>
    public struct KeyframeColor
    {
        public int? Red;
        public int? Green;
        public int? Blue;
        public int? Alpha;
    }

    /// <summary>
    /// A frame's color channels after inheritance, in the 0 - 255 domain: each channel is the most recent value
    /// set at or before the frame, or null if no frame so far set it.
    /// </summary>
    public struct ResolvedKeyframeColor
    {
        public int? Red;
        public int? Green;
        public int? Blue;
        public int? Alpha;
        public ColorOperation? Operation;
    }

    /// <summary>
    /// Evaluates per-frame keyframe interpolation. Every frame is a keyframe unless it opts out per group; a
    /// keyframe's curve runs to the next keyframe of the same group, spanning the summed frame lengths in
    /// between. The groups (Offset: RelativeX/Y; Color: red/green/blue/alpha) are independent. The first
    /// frame is always a keyframe; a non-looping chain's last keyframe holds; a looping chain's last
    /// keyframe glides back to the first. With the default (snap) curve the result is exactly the frame's own
    /// value. Mirrors FlatRedBall2's KeyframeEvaluator so both engines agree on the same file.
    /// </summary>
    public static class KeyframeEvaluator
    {
        /// <summary>
        /// Whether any frame uses keyframe features (a skip flag or a non-snap curve). A chain where this is
        /// false is played exactly as it always was.
        /// </summary>
        public static bool HasKeyframeData(this IReadOnlyList<AnimationFrame> frames)
        {
            for (int i = 0; i < frames.Count; i++)
            {
                if (frames[i].HasKeyframeData)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// The offset to display <paramref name="elapsedInFrame"/> seconds into frame <paramref name="frameIndex"/>.
        /// An empty list yields (0, 0); an out-of-range index clamps.
        /// </summary>
        public static Vector2 EvaluateOffset(IReadOnlyList<AnimationFrame> frames, bool loop, int frameIndex, double elapsedInFrame)
        {
            if (frames.Count == 0)
            {
                return Vector2.Zero;
            }
            frameIndex = SMath.Min(SMath.Max(frameIndex, 0), frames.Count - 1);

            int start, end;
            float t;
            FindSegment(frames, loop, frameIndex, elapsedInFrame, false, out start, out end, out t);

            // Snap holds even at t == 1: the next keyframe takes over only when playback moves onto its frame.
            AnimationFrame from = frames[start];
            if (end < 0 || from.OffsetCurve.IsSnap)
            {
                return new Vector2(from.RelativeX, from.RelativeY);
            }

            float eased = from.OffsetCurve.Evaluate(t);
            AnimationFrame to = frames[end];
            return new Vector2(Lerp(from.RelativeX, to.RelativeX, eased), Lerp(from.RelativeY, to.RelativeY, eased));
        }

        /// <summary>
        /// The color channels to display, same contract as <see cref="EvaluateOffset"/>. Each channel glides
        /// independently; a channel unset at one end counts as the color operation's identity (255, or 0 for
        /// Add/AddSubtract) and a channel unset at both ends stays null. Results round away from zero.
        /// </summary>
        public static KeyframeColor EvaluateColor(IReadOnlyList<AnimationFrame> frames, bool loop, int frameIndex, double elapsedInFrame)
        {
            if (frames.Count == 0)
            {
                return default(KeyframeColor);
            }
            frameIndex = SMath.Min(SMath.Max(frameIndex, 0), frames.Count - 1);

            int start, end;
            float t;
            FindSegment(frames, loop, frameIndex, elapsedInFrame, true, out start, out end, out t);

            ResolvedKeyframeColor from = ResolveColor(frames, start);
            KeyframeColor result = new KeyframeColor
            {
                Red = from.Red, Green = from.Green, Blue = from.Blue, Alpha = from.Alpha
            };

            AnimationFrame startFrame = frames[start];
            if (end < 0 || startFrame.ColorCurve.IsSnap)
            {
                return result;
            }

            float eased = startFrame.ColorCurve.Evaluate(t);
            ResolvedKeyframeColor to = ResolveColor(frames, end);
            int fromIdentity = ChannelDefault(from.Operation);
            int toIdentity = ChannelDefault(to.Operation);

            result.Red = LerpChannel(from.Red, to.Red, fromIdentity, toIdentity, eased);
            result.Green = LerpChannel(from.Green, to.Green, fromIdentity, toIdentity, eased);
            result.Blue = LerpChannel(from.Blue, to.Blue, fromIdentity, toIdentity, eased);
            result.Alpha = LerpChannel(from.Alpha, to.Alpha, 255, 255, eased);
            return result;
        }

        /// <summary>
        /// Resolves the color of the frame at <paramref name="frameIndex"/> (clamped): a null channel inherits
        /// the most recent earlier frame that sets it. A frame with SkipColorKeyframe (other than the first)
        /// contributes no channels, since its own values are ignored, but its ColorOperation still applies.
        /// </summary>
        public static ResolvedKeyframeColor ResolveColor(IReadOnlyList<AnimationFrame> frames, int frameIndex)
        {
            ResolvedKeyframeColor result = default(ResolvedKeyframeColor);
            if (frames.Count == 0)
            {
                return result;
            }

            int start = SMath.Min(SMath.Max(frameIndex, 0), frames.Count - 1);
            for (int i = start; i >= 0; i--)
            {
                AnimationFrame frame = frames[i];
                if (i == 0 || !frame.SkipColorKeyframe)
                {
                    if (result.Red == null) result.Red = ToByte(frame.Red);
                    if (result.Green == null) result.Green = ToByte(frame.Green);
                    if (result.Blue == null) result.Blue = ToByte(frame.Blue);
                    if (result.Alpha == null) result.Alpha = ToByte(frame.Alpha);
                }
                if (result.Operation == null) result.Operation = frame.ColorOperation;
            }

            return result;
        }

        /// <summary>
        /// The R/G/B value an unset channel contributes under a color operation: 0 for Add and AddSubtract
        /// (the two runtime forms of the file's Add), 255 otherwise.
        /// </summary>
        public static int ChannelDefault(ColorOperation? operation)
        {
            return operation == ColorOperation.Add || operation == ColorOperation.AddSubtract ? 0 : 255;
        }

        // Locates the keyframe segment containing frameIndex: `start` is the controlling keyframe, `end` the
        // keyframe it glides to (-1 = nothing to glide to, hold `start`), `t` the linear progress through it.
        private static void FindSegment(IReadOnlyList<AnimationFrame> frames, bool loop, int frameIndex,
            double elapsedInFrame, bool colorGroup, out int start, out int end, out float t)
        {
            start = frameIndex;
            while (start > 0 && IsSkipped(frames[start], colorGroup)) start--;

            end = frameIndex + 1;
            while (end < frames.Count && IsSkipped(frames[end], colorGroup)) end++;
            if (end >= frames.Count)
            {
                end = loop ? 0 : -1;
            }

            t = 0f;
            // Nothing to glide to: a non-looping chain's last keyframe, or a looping chain with one keyframe.
            if (end < 0 || end == start)
            {
                end = -1;
                return;
            }

            int segmentEnd = end == 0 ? frames.Count : end;
            double duration = 0;
            double position = elapsedInFrame;
            for (int i = start; i < segmentEnd; i++)
            {
                double length = frames[i].FrameLength;
                duration += length;
                if (i < frameIndex)
                {
                    position += length;
                }
            }

            t = duration > 0 ? (float)SMath.Min(SMath.Max(position / duration, 0.0), 1.0) : 0f;
        }

        private static bool IsSkipped(AnimationFrame frame, bool colorGroup)
        {
            return colorGroup ? frame.SkipColorKeyframe : frame.SkipOffsetKeyframe;
        }

        private static float Lerp(float from, float to, float eased)
        {
            return eased <= 0f ? from : eased >= 1f ? to : from + (to - from) * eased;
        }

        private static int? LerpChannel(int? from, int? to, int fromIdentity, int toIdentity, float eased)
        {
            if (from == null && to == null) return null;
            if (eased <= 0f) return from;
            int a = from ?? fromIdentity;
            int b = to ?? toIdentity;
            if (eased >= 1f) return b;
            return (int)SMath.Round(a + (b - a) * (double)eased, MidpointRounding.AwayFromZero);
        }

        // Runtime frames hold 0 - 1 floats (negative allowed for Add); the file's domain is 0 - 255 ints.
        private static int? ToByte(float? channel)
        {
            if (channel == null) return null;
            return (int)SMath.Round(channel.Value * 255f, MidpointRounding.AwayFromZero);
        }
    }
}
