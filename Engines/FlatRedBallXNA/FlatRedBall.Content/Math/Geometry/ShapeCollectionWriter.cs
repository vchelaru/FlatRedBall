using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;

using TWrite = FlatRedBall.Content.Math.Geometry.ShapeCollectionSave;
#if XNA4
using TargetPlatform = Microsoft.Xna.Framework.Content.Pipeline.TargetPlatform;
#else
using TargetPlatform = Microsoft.Xna.Framework.TargetPlatform;
#endif


namespace FlatRedBall.Content.Math.Geometry
{
    /// <summary>
    /// This class will be instantiated by the XNA Framework Content Pipeline
    /// to write the specified data type into binary .xnb format.
    ///
    /// This should be part of a Content Pipeline Extension Library project.
    /// </summary>
    [ContentTypeWriter]
    public class ShapeCollectionWriter : ContentTypeWriter<TWrite>
    {
        protected override void Write(ContentWriter output, TWrite value)
        {
            ObjectWriter.WriteObject<ShapeCollectionSave>(output, value);
        }


        public override string GetRuntimeType(TargetPlatform targetPlatform)
        {
            return typeof(FlatRedBall.Math.Geometry.ShapeCollection).AssemblyQualifiedName;
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return typeof(FlatRedBall.Content.ShapeCollectionReader).AssemblyQualifiedName;
        }
    }
}
