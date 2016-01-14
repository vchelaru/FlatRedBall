using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;

// TODO: replace this with the type you want to import.
using TImport = FlatRedBall.Content.Math.Geometry.ShapeCollectionSave;

namespace FlatRedBall.Content.Math.Geometry
{
    /// <summary>
    /// This class will be instantiated by the XNA Framework Content Pipeline
    /// to import a file from disk into the specified type, TImport.
    /// 
    /// This should be part of a Content Pipeline Extension Library project.
    /// 
    /// TODO: change the ContentImporter attribute to specify the correct file
    /// extension, display name, and default processor for this importer.
    /// </summary>
    [ContentImporter(".shcx", DisplayName = "ShapeCollection - FlatRedBall", DefaultProcessor = "ShapeCollectionProcessor")]
    public class ShapeCollectionImporter : ContentImporter<TImport>
    {
        public override TImport Import(string filename, ContentImporterContext context)
        {
            ShapeCollectionSave shapeCollectionSave = ShapeCollectionSave.FromFile(filename);
            return shapeCollectionSave;
        }
    }
}
