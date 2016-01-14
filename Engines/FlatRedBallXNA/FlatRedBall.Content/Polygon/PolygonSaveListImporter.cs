using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;

using TImport = FlatRedBall.Content.Polygon.PolygonSaveList;

namespace FlatRedBall.Content.Polygon
{
    /// <summary>
    /// This class will be instantiated by the XNA Framework Content Pipeline
    /// to import a file from disk into the specified type, TImport.
    /// 
    /// This should be part of a Content Pipeline Extension Library project.
    /// </summary>
    [ContentImporter(".plylstx", DisplayName = "FlatRedBall - PolygonSaveList", DefaultProcessor = "PolygonSaveListProcessor")]
    public class PolygonSaveListImporter : ContentImporter<TImport>
    {
        public override TImport Import(string filename, ContentImporterContext context)
        {
            PolygonSaveList polygonSaveList = PolygonSaveList.FromFile(filename);
            return polygonSaveList;
        }
    }
}
