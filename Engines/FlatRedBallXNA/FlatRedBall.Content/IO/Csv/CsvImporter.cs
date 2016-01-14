using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;

// TODO: replace this with the type you want to import.
using TImport = FlatRedBall.Content.IO.Csv.BuildtimeCsvRepresentation;
using FlatRedBall.IO.Csv;

namespace FlatRedBall.Content.IO.Csv
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
    [ContentImporter(".csv", DisplayName = "CSV - FlatRedBall", DefaultProcessor = "CsvProcessor")]
    public class CsvImporter : ContentImporter<TImport>
    {
        public override TImport Import(string filename, ContentImporterContext context)
        {
            BuildtimeCsvRepresentation buildtimeCsvRepresentation = CsvFileManager.CsvDeserializeToRuntime<BuildtimeCsvRepresentation>(filename);
            return buildtimeCsvRepresentation;
        }
    }
}
