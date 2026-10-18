using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using FlatRedBall.IO;
using FlatRedBall.Math.Geometry;
using Point = FlatRedBall.Math.Geometry.Point;

namespace FlatRedBall.Content.Polygon
{
    public class PolygonSave
    {
        #region Fields

        [XmlElementAttribute("X")]
        public float X;

        [XmlElementAttribute("Y")]
        public float Y;

        [XmlElementAttribute("Z")]
        public float Z;

        [XmlElementAttribute("RotationZ")]
        public float RotationZ;

        //[XmlElementAttribute("Point")]
        public Point[] Points;

        [XmlElementAttribute("Name")]
        public string Name;

        public float Alpha = 1;
        public float Red = 1;
        public float Green = 1;
        public float Blue = 1;

        #region Keyframe interpolation (optional, only meaningful on a shape inside an animation frame)

        // Written only when set, and the curve names are kept as raw strings so names this engine doesn't
        // understand survive a save. See KeyframeEvaluator.EvaluateShape and EvaluateShapePoints.

        /// <summary>
        /// When true this polygon is not an offset keyframe in its frame: its own X/Y and offset curve are ignored,
        /// and the previous offset keyframe of the same-named polygon glides across it.
        /// </summary>
        public bool SkipOffsetKeyframe;
        public bool ShouldSerializeSkipOffsetKeyframe() => SkipOffsetKeyframe;

        /// <summary>Curve type name from this polygon's offset to the next offset keyframe's. Null, "Instant" or an unrecognized name snaps.</summary>
        public string OffsetInterpolation;
        public bool ShouldSerializeOffsetInterpolation() => OffsetInterpolation != null;

        /// <summary>In, Out or InOut for <see cref="OffsetInterpolation"/>. Null means Out.</summary>
        public string OffsetEasing;
        public bool ShouldSerializeOffsetEasing() => OffsetEasing != null;

        /// <summary>
        /// When true this polygon is not a points keyframe in its frame: its own Points and points curve are ignored,
        /// and the previous points keyframe of the same-named polygon glides across it. Independent of
        /// <see cref="SkipOffsetKeyframe"/>.
        /// </summary>
        public bool SkipPointsKeyframe;
        public bool ShouldSerializeSkipPointsKeyframe() => SkipPointsKeyframe;

        /// <summary>
        /// Curve type name from this polygon's Points to the next points keyframe's: point i glides to point i, and a
        /// changed point count snaps. See <see cref="OffsetInterpolation"/>.
        /// </summary>
        public string PointsInterpolation;
        public bool ShouldSerializePointsInterpolation() => PointsInterpolation != null;

        /// <summary>In, Out or InOut for <see cref="PointsInterpolation"/>. Null means Out.</summary>
        public string PointsEasing;
        public bool ShouldSerializePointsEasing() => PointsEasing != null;

        #endregion
        #endregion

        #region Methods

        public PolygonSave()
        { 
        
        }

        public static PolygonSave FromPolygon(FlatRedBall.Math.Geometry.Polygon polygon)
        {
            PolygonSave polygonSave = new PolygonSave();

            int pointCount = polygon.Points.Count;
            polygonSave.Points = new Point[pointCount];

            for (int i = 0; i < polygon.Points.Count; i++)
            {
                polygonSave.Points[i] = polygon.Points[i];
            }

            polygonSave.Name = polygon.Name;
            polygonSave.X = polygon.Position.X;
            polygonSave.Y = polygon.Position.Y;
            polygonSave.Z = polygon.Position.Z;

            polygonSave.RotationZ = polygon.RotationZ;

            polygonSave.Alpha = polygon.Color.A / 255.0f;
            polygonSave.Red = polygon.Color.R / 255.0f;
            polygonSave.Green = polygon.Color.G / 255.0f;
            polygonSave.Blue = polygon.Color.B / 255.0f;

            return polygonSave;
        }

        /// <summary>
        /// Returns a deep copy. Does not use XmlSerializer, so it is safe under Native AOT.
        /// </summary>
        public PolygonSave Clone()
        {
            // MemberwiseClone copies every value-type and string field, including ones added later.
            // Only reference types that must not be shared need an explicit copy.
            var toReturn = (PolygonSave)MemberwiseClone();
            toReturn.Points = (Point[])Points?.Clone();
            return toReturn;
        }

        public void Save(string fileName)
        {
            FileManager.XmlSerialize(this, fileName);
        }

        public FlatRedBall.Math.Geometry.Polygon ToPolygon()
        {
            FlatRedBall.Math.Geometry.Polygon polygon = new FlatRedBall.Math.Geometry.Polygon();

            SetValuesOn(polygon);

            return polygon;
        }

        public void SetValuesOn(FlatRedBall.Math.Geometry.Polygon polygon)
        {
            polygon.Points = Points;

            polygon.Position.X = X;
            polygon.Position.Y = Y;
            polygon.Position.Z = Z;

            polygon.RotationZ = RotationZ;

            polygon.Name = Name;

            polygon.Color =
                new Color(
                    (byte)(Red * 255),
                    (byte)(Green * 255),
                    (byte)(Blue * 255),
                    (byte)(Alpha * 255));

            polygon.FillVertexArray(false);
        }

        #endregion
    }
}
