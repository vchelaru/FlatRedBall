using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

using FlatRedBall;

using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Instructions.Reflection;


// TODO: replace this with the type you want to read.
using TRead = FlatRedBall.Math.PositionedObjectList<FlatRedBall.Math.Geometry.Polygon>;
using System.Reflection;

namespace FlatRedBall.Content
{
    /// <summary>
    /// This class will be instantiated by the XNA Framework Content
    /// Pipeline to read the specified data type from binary .xnb format.
    /// 
    /// Unlike the other Content Pipeline support classes, this should
    /// be a part of your main game project, and not the Content Pipeline
    /// Extension Library project.
    /// </summary>
    public class PolygonListReader : ContentTypeReader<TRead>
    {
        protected override TRead Read(ContentReader input, TRead existingInstance)
        {
            if (existingInstance != null)
            {
                return existingInstance;
            }

            int count = input.ReadInt32();

            existingInstance = new TRead(count);

            for (int i = 0; i < count; i++)
            {
                existingInstance.Add(ObjectReader.ReadObject<FlatRedBall.Content.Polygon.PolygonSave>(input).ToPolygon());
            }

            return existingInstance;
        }




        internal static FlatRedBall.Math.Geometry.Polygon ReadPolygon(ContentReader input)
        {
            FlatRedBall.Math.Geometry.Polygon polygon = new FlatRedBall.Math.Geometry.Polygon();

            polygon.Position = new Vector3(input.ReadSingle(), input.ReadSingle(), input.ReadSingle());

            polygon.RotationZ = input.ReadSingle();

            int numberOfPoints = input.ReadInt32();

            FlatRedBall.Math.Geometry.Point[] pointList = new FlatRedBall.Math.Geometry.Point[numberOfPoints];

            for (int i = 0; i < numberOfPoints; i++)
            {
                pointList[i] = new FlatRedBall.Math.Geometry.Point(input.ReadDouble(), input.ReadDouble());
            }

            polygon.Points = pointList;

            polygon.Name = input.ReadString();

            return polygon;
        }

    }
}
