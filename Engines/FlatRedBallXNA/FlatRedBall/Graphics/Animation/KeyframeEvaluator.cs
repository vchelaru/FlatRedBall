using System;
using SMath = System.Math;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using FlatRedBall.Content.Math.Geometry;
using FlatRedBall.Content.Polygon;
using Point = FlatRedBall.Math.Geometry.Point;

namespace FlatRedBall.Graphics.Animation
{
    /// <summary>
    /// A shape's offset and size at one instant of playback, in the units the .achx stores them in: a rectangle's
    /// size is ScaleX/ScaleY (half extents), a circle's is Radius (in SizeX), a polygon has no size.
    /// </summary>
    public struct KeyframeShapeValue
    {
        public float X;
        public float Y;
        public float SizeX;
        public float SizeY;
    }

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

            t = Progress(frames, start, end, frameIndex, elapsedInFrame);
        }

        // Linear progress through the segment from `start` to `end` (end == 0 means the loop wrap, which
        // spans through the last frame), at `elapsedInFrame` into `frameIndex`.
        private static float Progress(IReadOnlyList<AnimationFrame> frames, int start, int end, int frameIndex, double elapsedInFrame)
        {
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

            return duration > 0 ? (float)SMath.Min(SMath.Max(position / duration, 0.0), 1.0) : 0f;
        }

        #region Shapes

        /// <summary>
        /// The offset and size of <paramref name="shape"/> (a rectangle of frame <paramref name="frameIndex"/>)
        /// <paramref name="elapsedInFrame"/> seconds into that frame; size is ScaleX/ScaleY. Shapes match across frames
        /// by name and kind: a glide runs to the next keyframe of the same group among the following frames that hold
        /// the name, and stops where the name is missing, duplicated, or changes kind. A shape with no name, or whose
        /// name is duplicated in its own frame, snaps. Offset and size are independent groups, each with its own skip
        /// flag and curve, and the first frame of a run of frames holding the name is always a keyframe. A shape that
        /// is not in the frame returns its own values.
        /// </summary>
        public static KeyframeShapeValue EvaluateShape(IReadOnlyList<AnimationFrame> frames, bool loop, int frameIndex,
            double elapsedInFrame, AxisAlignedRectangleSave shape)
        {
            return EvaluateShape(frames, loop, frameIndex, elapsedInFrame, KeyframeShape.From(shape));
        }

        /// <summary>The offset and radius (returned in SizeX) of a circle; same contract as the rectangle overload.</summary>
        public static KeyframeShapeValue EvaluateShape(IReadOnlyList<AnimationFrame> frames, bool loop, int frameIndex,
            double elapsedInFrame, CircleSave shape)
        {
            return EvaluateShape(frames, loop, frameIndex, elapsedInFrame, KeyframeShape.From(shape));
        }

        /// <summary>The offset of a polygon (SizeX/SizeY are 0); same contract as the rectangle overload. Points glide separately, see <see cref="EvaluateShapePoints"/>.</summary>
        public static KeyframeShapeValue EvaluateShape(IReadOnlyList<AnimationFrame> frames, bool loop, int frameIndex,
            double elapsedInFrame, PolygonSave shape)
        {
            return EvaluateShape(frames, loop, frameIndex, elapsedInFrame, KeyframeShape.From(shape));
        }

        /// <summary>
        /// The points of <paramref name="polygon"/> (a polygon of frame <paramref name="frameIndex"/>)
        /// <paramref name="elapsedInFrame"/> seconds into that frame. Points are their own keyframe group, matched
        /// across frames like <see cref="EvaluateShape(IReadOnlyList{AnimationFrame}, bool, int, double, PolygonSave)"/>.
        /// Point i glides to point i of the next points keyframe; when the counts differ the glide snaps and the earlier
        /// keyframe's points hold until the next frame plays. Returns the keyframe's own array (no copy) whenever
        /// nothing glides.
        /// </summary>
        public static Point[] EvaluateShapePoints(IReadOnlyList<AnimationFrame> frames, bool loop, int frameIndex,
            double elapsedInFrame, PolygonSave polygon)
        {
            KeyframeShape shape = KeyframeShape.From(polygon);
            Point[] own = shape.Points;
            if (frames.Count == 0)
            {
                return own;
            }
            frameIndex = SMath.Min(SMath.Max(frameIndex, 0), frames.Count - 1);

            KeyframeShape current;
            if (!TryMatch(frames[frameIndex], shape.Name, shape.Kind, out current))
            {
                return own;
            }

            KeyframeShape from, to;
            float t;
            FindShapeSegment(frames, loop, frameIndex, elapsedInFrame, current, ShapeGroup.Points, out from, out to, out t);

            if (to == null || from.PointsCurve.IsSnap || from.Points.Length != to.Points.Length)
            {
                return from.Points;
            }

            float eased = from.PointsCurve.Evaluate(t);
            Point[] result = new Point[from.Points.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = new Point(
                    Lerp(from.Points[i].X, to.Points[i].X, eased),
                    Lerp(from.Points[i].Y, to.Points[i].Y, eased));
            }
            return result;
        }

        private static KeyframeShapeValue EvaluateShape(IReadOnlyList<AnimationFrame> frames, bool loop, int frameIndex,
            double elapsedInFrame, KeyframeShape shape)
        {
            KeyframeShapeValue own = new KeyframeShapeValue { X = shape.X, Y = shape.Y, SizeX = shape.SizeX, SizeY = shape.SizeY };
            if (frames.Count == 0)
            {
                return own;
            }
            frameIndex = SMath.Min(SMath.Max(frameIndex, 0), frames.Count - 1);

            KeyframeShape current;
            if (!TryMatch(frames[frameIndex], shape.Name, shape.Kind, out current))
            {
                return own;
            }

            KeyframeShape offsetFrom, offsetTo, sizeFrom, sizeTo;
            float offsetT, sizeT;
            FindShapeSegment(frames, loop, frameIndex, elapsedInFrame, current, ShapeGroup.Offset, out offsetFrom, out offsetTo, out offsetT);
            FindShapeSegment(frames, loop, frameIndex, elapsedInFrame, current, ShapeGroup.Size, out sizeFrom, out sizeTo, out sizeT);

            KeyframeShapeValue result = new KeyframeShapeValue { X = offsetFrom.X, Y = offsetFrom.Y, SizeX = sizeFrom.SizeX, SizeY = sizeFrom.SizeY };

            if (offsetTo != null && !offsetFrom.OffsetCurve.IsSnap)
            {
                float eased = offsetFrom.OffsetCurve.Evaluate(offsetT);
                result.X = Lerp(offsetFrom.X, offsetTo.X, eased);
                result.Y = Lerp(offsetFrom.Y, offsetTo.Y, eased);
            }

            if (sizeTo != null && !sizeFrom.SizeCurve.IsSnap)
            {
                float eased = sizeFrom.SizeCurve.Evaluate(sizeT);
                result.SizeX = Lerp(sizeFrom.SizeX, sizeTo.SizeX, eased);
                result.SizeY = Lerp(sizeFrom.SizeY, sizeTo.SizeY, eased);
            }

            return result;
        }

        // The one shape in `frame` with this name, when exactly one shape of any kind has it and it is of the
        // given kind. False for an empty name, a name held twice (ambiguous), or one of another kind: such a
        // shape snaps.
        private static bool TryMatch(AnimationFrame frame, string name, KeyframeShapeKind kind, out KeyframeShape match)
        {
            match = null;
            ShapeCollectionSave shapes = frame.ShapeCollectionSave;
            if (string.IsNullOrEmpty(name) || shapes == null)
            {
                return false;
            }

            int count = 0;
            AxisAlignedRectangleSave rectangle = null;
            CircleSave circle = null;
            PolygonSave polygon = null;

            for (int i = 0; i < shapes.AxisAlignedRectangleSaves.Count; i++)
            {
                if (shapes.AxisAlignedRectangleSaves[i].Name == name)
                {
                    rectangle = shapes.AxisAlignedRectangleSaves[i];
                    count++;
                }
            }
            for (int i = 0; i < shapes.CircleSaves.Count; i++)
            {
                if (shapes.CircleSaves[i].Name == name)
                {
                    circle = shapes.CircleSaves[i];
                    count++;
                }
            }
            for (int i = 0; i < shapes.PolygonSaves.Count; i++)
            {
                if (shapes.PolygonSaves[i].Name == name)
                {
                    polygon = shapes.PolygonSaves[i];
                    count++;
                }
            }

            if (count != 1)
            {
                return false;
            }

            if (rectangle != null && kind == KeyframeShapeKind.Rectangle)
            {
                match = KeyframeShape.From(rectangle);
            }
            else if (circle != null && kind == KeyframeShapeKind.Circle)
            {
                match = KeyframeShape.From(circle);
            }
            else if (polygon != null && kind == KeyframeShapeKind.Polygon)
            {
                match = KeyframeShape.From(polygon);
            }

            return match != null;
        }

        // Shape counterpart of FindSegment: `from` is the controlling keyframe of `current`'s name, `to` the
        // keyframe it glides to (null = hold `from`), `t` the linear progress. The segment only crosses frames
        // that hold the name; the walk back and forward both stop at the first frame that does not.
        private static void FindShapeSegment(IReadOnlyList<AnimationFrame> frames, bool loop, int frameIndex,
            double elapsedInFrame, KeyframeShape current, ShapeGroup group,
            out KeyframeShape from, out KeyframeShape to, out float t)
        {
            string name = current.Name;
            KeyframeShapeKind kind = current.Kind;

            int start = frameIndex;
            from = current;
            KeyframeShape previous;
            while (start > 0 && from.IsSkipped(group) && TryMatch(frames[start - 1], name, kind, out previous))
            {
                start--;
                from = previous;
            }

            int end = -1;
            to = null;
            for (int i = start + 1; ; i++)
            {
                bool wrapped = false;
                if (i >= frames.Count)
                {
                    if (!loop)
                    {
                        break;
                    }
                    i = 0;
                    wrapped = true;
                }

                KeyframeShape next;
                if (!TryMatch(frames[i], name, kind, out next))
                {
                    break;
                }

                // Frame 0 is the first frame of a run, so it is a keyframe whatever its flag says.
                if (wrapped || !next.IsSkipped(group))
                {
                    end = i;
                    to = next;
                    break;
                }
            }

            t = 0f;
            // Nothing to glide to: the name stops, a non-looping chain ends, or a looping run holds one keyframe.
            if (end < 0 || end == start)
            {
                to = null;
                return;
            }

            t = Progress(frames, start, end, frameIndex, elapsedInFrame);
        }

        private enum ShapeGroup
        {
            Offset,
            Size,
            Points
        }

        private enum KeyframeShapeKind
        {
            Rectangle,
            Circle,
            Polygon
        }

        // One per-frame shape as the evaluator sees it: the save object's fields plus its parsed curves.
        // A rectangle's size is ScaleX/ScaleY and a circle's is Radius (in SizeX); a polygon has points instead.
        private sealed class KeyframeShape
        {
            public string Name;
            public KeyframeShapeKind Kind;
            public float X;
            public float Y;
            public float SizeX;
            public float SizeY;
            public bool SkipOffset;
            public bool SkipSize;
            public bool SkipPoints;
            public KeyframeCurve OffsetCurve;
            public KeyframeCurve SizeCurve;
            public KeyframeCurve PointsCurve;
            public Point[] Points;

            public bool IsSkipped(ShapeGroup group)
            {
                switch (group)
                {
                    case ShapeGroup.Size: return SkipSize;
                    case ShapeGroup.Points: return SkipPoints;
                    default: return SkipOffset;
                }
            }

            public static KeyframeShape From(AxisAlignedRectangleSave save)
            {
                return new KeyframeShape
                {
                    Name = save.Name,
                    Kind = KeyframeShapeKind.Rectangle,
                    X = save.X,
                    Y = save.Y,
                    SizeX = save.ScaleX,
                    SizeY = save.ScaleY,
                    SkipOffset = save.SkipOffsetKeyframe,
                    SkipSize = save.SkipSizeKeyframe,
                    OffsetCurve = KeyframeCurve.Parse(save.OffsetInterpolation, save.OffsetEasing),
                    SizeCurve = KeyframeCurve.Parse(save.SizeInterpolation, save.SizeEasing),
                    Points = EmptyPoints
                };
            }

            public static KeyframeShape From(CircleSave save)
            {
                return new KeyframeShape
                {
                    Name = save.Name,
                    Kind = KeyframeShapeKind.Circle,
                    X = save.X,
                    Y = save.Y,
                    SizeX = save.Radius,
                    SkipOffset = save.SkipOffsetKeyframe,
                    SkipSize = save.SkipSizeKeyframe,
                    OffsetCurve = KeyframeCurve.Parse(save.OffsetInterpolation, save.OffsetEasing),
                    SizeCurve = KeyframeCurve.Parse(save.SizeInterpolation, save.SizeEasing),
                    Points = EmptyPoints
                };
            }

            public static KeyframeShape From(PolygonSave save)
            {
                return new KeyframeShape
                {
                    Name = save.Name,
                    Kind = KeyframeShapeKind.Polygon,
                    X = save.X,
                    Y = save.Y,
                    SkipOffset = save.SkipOffsetKeyframe,
                    SkipPoints = save.SkipPointsKeyframe,
                    OffsetCurve = KeyframeCurve.Parse(save.OffsetInterpolation, save.OffsetEasing),
                    PointsCurve = KeyframeCurve.Parse(save.PointsInterpolation, save.PointsEasing),
                    Points = save.Points ?? EmptyPoints
                };
            }
        }

        private static readonly Point[] EmptyPoints = new Point[0];

        // Endpoints are exact; anything else, including Back-curve swings below 0 or above 1 (Anticipate,
        // Overshoot), is the raw interpolation, as in FlatRedBall2. Clamping is the consumer's job
        // (the Sprite color setters clamp).
        private static float Lerp(float from, float to, float eased)
        {
            return eased == 0f ? from : eased == 1f ? to : from + (to - from) * eased;
        }

        private static double Lerp(double from, double to, float eased)
        {
            return eased == 0f ? from : eased == 1f ? to : from + (to - from) * eased;
        }

        #endregion

        private static bool IsSkipped(AnimationFrame frame, bool colorGroup)
        {
            return colorGroup ? frame.SkipColorKeyframe : frame.SkipOffsetKeyframe;
        }

        private static int? LerpChannel(int? from, int? to, int fromIdentity, int toIdentity, float eased)
        {
            if (from == null && to == null) return null;
            if (eased == 0f) return from;
            int a = from ?? fromIdentity;
            int b = to ?? toIdentity;
            if (eased == 1f) return b;
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
