using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;

// TODO: replace this with the type you want to write out.
using TWrite = FlatRedBall.Content.Polygon.PolygonSaveList;
using System.Reflection;

namespace FlatRedBall.Content.Polygon
{
    /// <summary>
    /// This class will be instantiated by the XNA Framework Content Pipeline
    /// to write the specified data type into binary .xnb format.
    ///
    /// This should be part of a Content Pipeline Extension Library project.
    /// </summary>
    [ContentTypeWriter]
    public class PolygonSaveListWriter : ContentTypeWriter<TWrite>
    {
        protected override void Write(ContentWriter output, TWrite value)
        {
            // write out how many Polygons there are so the reader can create a loop
            output.Write(value.PolygonSaves.Count);

            for (int i = 0; i < value.PolygonSaves.Count; i++)
            {
                ObjectWriter.WriteObject<PolygonSave>(output, value.PolygonSaves[i]);

            }
        }


        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            // TODO: change this to the name of your ContentTypeReader
            // class which will be used to load this data.
            return typeof(FlatRedBall.Content.PolygonListReader).AssemblyQualifiedName;
        }



        internal static void WritePolygonSave(ContentWriter output, PolygonSave value)
        {
            Type t = typeof(TWrite);
            output.Write(value.X);
            output.Write(value.Y);
            output.Write(value.Z);

            output.Write(value.RotationZ);

            // write the number of points
            output.Write(value.Points.Length);

            for (int i = 0; i < value.Points.Length; i++)
            {
                output.Write(value.Points[i].X);
                output.Write(value.Points[i].Y);
            }

            // Crash occurs if trying to write out NULL string.
            if (value.Name == null)
            {
                output.Write("");
            }
            else
            {
                output.Write(value.Name);
            }
        }
    }
}
