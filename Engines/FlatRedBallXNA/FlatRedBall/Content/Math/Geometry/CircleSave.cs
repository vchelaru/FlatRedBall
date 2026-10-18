using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using FlatRedBall.Math.Geometry;

namespace FlatRedBall.Content.Math.Geometry
{
    [Serializable]
    public class CircleSave
    {
        public float X;
        public float Y;
        public float Z;

        public float Radius;

        public string Name;
        public string Parent;

        public float Alpha = 1;
        public float Red = 1;
        public float Green = 1;
        public float Blue = 1;

        #region Keyframe interpolation (optional, only meaningful on a shape inside an animation frame)

        // Written only when set, and the curve names are kept as raw strings so names this engine doesn't
        // understand survive a save. See KeyframeEvaluator.EvaluateShape.

        /// <summary>
        /// When true this shape is not an offset keyframe in its frame: its own X/Y and offset curve are ignored,
        /// and the previous offset keyframe of the same-named shape glides across it.
        /// </summary>
        public bool SkipOffsetKeyframe;
        public bool ShouldSerializeSkipOffsetKeyframe() => SkipOffsetKeyframe;

        /// <summary>Curve type name from this shape's offset to the next offset keyframe's. Null, "Instant" or an unrecognized name snaps.</summary>
        public string OffsetInterpolation;
        public bool ShouldSerializeOffsetInterpolation() => OffsetInterpolation != null;

        /// <summary>In, Out or InOut for <see cref="OffsetInterpolation"/>. Null means Out.</summary>
        public string OffsetEasing;
        public bool ShouldSerializeOffsetEasing() => OffsetEasing != null;

        /// <summary>
        /// When true this shape is not a size keyframe in its frame: its own Radius and size curve are ignored.
        /// Independent of <see cref="SkipOffsetKeyframe"/>.
        /// </summary>
        public bool SkipSizeKeyframe;
        public bool ShouldSerializeSkipSizeKeyframe() => SkipSizeKeyframe;

        /// <summary>Curve type name from this shape's Radius to the next size keyframe's. See <see cref="OffsetInterpolation"/>.</summary>
        public string SizeInterpolation;
        public bool ShouldSerializeSizeInterpolation() => SizeInterpolation != null;

        /// <summary>In, Out or InOut for <see cref="SizeInterpolation"/>. Null means Out.</summary>
        public string SizeEasing;
        public bool ShouldSerializeSizeEasing() => SizeEasing != null;

        #endregion

        /// <summary>
        /// Returns a deep copy. Does not use XmlSerializer, so it is safe under Native AOT.
        /// </summary>
        public CircleSave Clone()
        {
            // Every field is a value type or an immutable string, so a shallow copy is a deep copy.
            // A reference-type field added later must be copied here.
            return (CircleSave)MemberwiseClone();
        }

        public static CircleSave FromCircle(FlatRedBall.Math.Geometry.Circle circle)
        {
            CircleSave circleSave = new CircleSave();
            circleSave.SetFrom(circle);

            return circleSave;
        }

        public void SetFrom(FlatRedBall.Math.Geometry.Circle circle)
        {
            X = circle.X;
            Y = circle.Y;
            Z = circle.Z;

            Radius = circle.Radius;

            Name = circle.Name;

            if (circle.Parent != null)
            {
                Parent = circle.Parent.Name;
            }

            Alpha = circle.Color.A / 255.0f;
            Red = circle.Color.R / 255.0f;
            Green = circle.Color.G / 255.0f;
            Blue = circle.Color.B / 255.0f;
        }

        public FlatRedBall.Math.Geometry.Circle ToCircle()
        {
            FlatRedBall.Math.Geometry.Circle circle = new FlatRedBall.Math.Geometry.Circle();

            SetValuesOn(circle);

            return circle;

        }

        public void SetValuesOn(Circle circle)
        {
            circle.X = this.X;
            circle.Y = this.Y;
            circle.Z = this.Z;

            circle.Radius = this.Radius;

            circle.Name = this.Name;

            circle.Color =
                new Color(
                    (byte)(Red * 255),
                    (byte)(Green * 255),
                    (byte)(Blue * 255),
                    (byte)(Alpha * 255));
        }
    }
}
