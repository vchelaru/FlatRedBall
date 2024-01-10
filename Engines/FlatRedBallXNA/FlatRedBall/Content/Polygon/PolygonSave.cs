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
