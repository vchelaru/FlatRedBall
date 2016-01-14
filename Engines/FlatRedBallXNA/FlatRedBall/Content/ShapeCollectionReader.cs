using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

using TRead = FlatRedBall.Math.Geometry.ShapeCollection;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Content.Math.Geometry;

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
    public class ShapeCollectionReader : ContentTypeReader<TRead>
    {
        protected override TRead Read(ContentReader input, TRead existingInstance)
        {
            if (existingInstance != null)
            {
                return existingInstance;
            }

            ShapeCollectionSave shapeCollectionSave = ObjectReader.ReadObject<FlatRedBall.Content.Math.Geometry.ShapeCollectionSave>(input);

            shapeCollectionSave.FileName = input.AssetName;

            return shapeCollectionSave.ToShapeCollection() ;
        }
    }
}
