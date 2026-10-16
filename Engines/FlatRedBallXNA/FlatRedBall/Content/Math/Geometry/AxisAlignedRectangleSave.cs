using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FlatRedBall.Content.Math.Geometry
{
    [Serializable]
    public class AxisAlignedRectangleSave
    {
        #region Fields

        public float X;
        public float Y;
        public float Z;

        public float ScaleX;
        public float ScaleY;

        [XmlIgnore]
        public float Width
        {
            get => ScaleX * 2;
            set => ScaleX = value / 2.0f;
        }

        [XmlIgnore]
        public float Height
        {
            get => ScaleY * 2;
            set => ScaleY = value / 2.0f;
        }

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
        /// When true this shape is not a size keyframe in its frame: its own ScaleX/ScaleY and size curve are ignored.
        /// Independent of <see cref="SkipOffsetKeyframe"/>.
        /// </summary>
        public bool SkipSizeKeyframe;
        public bool ShouldSerializeSkipSizeKeyframe() => SkipSizeKeyframe;

        /// <summary>Curve type name from this shape's ScaleX/ScaleY to the next size keyframe's. See <see cref="OffsetInterpolation"/>.</summary>
        public string SizeInterpolation;
        public bool ShouldSerializeSizeInterpolation() => SizeInterpolation != null;

        /// <summary>In, Out or InOut for <see cref="SizeInterpolation"/>. Null means Out.</summary>
        public string SizeEasing;
        public bool ShouldSerializeSizeEasing() => SizeEasing != null;

        #endregion

        // This class does not support visibility.
        // The reason is because setting visibility
        // on a AxisAlignedRectangle (which would occur
        // in the ToAxisAlignedRectangle method) would add
        // it to the manager.  We can't do that because the ToRuntime
        // methods should not add the object to any manager.  Maybe we'll
        // fix this at some point in the future.
        //public bool Visible;
        //public bool ShouldSerializeVisible()
        //{
        //    return Visible == false;
        //}

        #endregion

        public static AxisAlignedRectangleSave FromAxisAlignedRectangle(FlatRedBall.Math.Geometry.AxisAlignedRectangle rectangle)
        {
            AxisAlignedRectangleSave aars = new AxisAlignedRectangleSave();
            aars.SetFrom(rectangle);

            return aars;
        }

        public void SetFrom(FlatRedBall.Math.Geometry.AxisAlignedRectangle rectangle)
        {
            X = rectangle.X;
            Y = rectangle.Y;
            Z = rectangle.Z;

            ScaleX = rectangle.ScaleX;
            ScaleY = rectangle.ScaleY;

            Name = rectangle.Name;

            Alpha = rectangle.Color.A / 255.0f;
            Red = rectangle.Color.R / 255.0f;
            Green = rectangle.Color.G / 255.0f;
            Blue = rectangle.Color.B / 255.0f;
            if (rectangle.Parent != null)
            {
                Parent = rectangle.Parent.Name;
            }
        }

        public FlatRedBall.Math.Geometry.AxisAlignedRectangle ToAxisAlignedRectangle()
        {
            FlatRedBall.Math.Geometry.AxisAlignedRectangle rectangle = new FlatRedBall.Math.Geometry.AxisAlignedRectangle();
            SetValuesOn(rectangle);

            return rectangle;

        }

        public void SetValuesOn(AxisAlignedRectangle rectangle)
        {
            rectangle.X = this.X;
            rectangle.Y = this.Y;
            rectangle.Z = this.Z;

            rectangle.ScaleX = this.ScaleX;
            rectangle.ScaleY = this.ScaleY;

            rectangle.Color =
                new Color(
                    (byte)(Red * 255),
                    (byte)(Green * 255),
                    (byte)(Blue * 255),
                    (byte)(Alpha * 255));

            rectangle.Name = this.Name;
        }
    }
}
