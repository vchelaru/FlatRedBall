using System;
using System.Collections.Generic;
using System.Text;

using EditorObjects.Data;
using FlatRedBall.IO;
using FlatRedBall.Content.Scene;

namespace EditorObjects.EditorSettings
{
    public class PolygonEditorSettings
    {

        public LineGridSave LineGridSave; 
        public bool UsePixelCoordinates = false;

		public CameraSave BoundsCameraSave;

        // for XML:
        public PolygonEditorSettings()
        {
            
        }

        public PolygonEditorSettings(LineGrid lineGrid)
        {

        }


        public static PolygonEditorSettings FromFile(string fileName)
        {
            PolygonEditorSettings savedInformation = FileManager.XmlDeserialize<PolygonEditorSettings>(fileName);

            return savedInformation;

        }

        public void Save(string fileName)
        {
            FileManager.XmlSerialize(this, fileName);
        }


    }
}
