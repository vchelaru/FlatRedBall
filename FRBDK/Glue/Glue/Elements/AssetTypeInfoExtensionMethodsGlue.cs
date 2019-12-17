using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.IO;
using ModelEditor.SaveClasses;
using FlatRedBall.Glue.Managers;

namespace FlatRedBall.Glue.Elements
{
    public static class AssetTypeInfoExtensionMethodsGlue
    {
        public static void CreateCompanionSettingsFile(string fileName, bool make2D)
        {
            string extension = FileManager.GetExtension(fileName);
            string settingsFile = FileManager.RemoveExtension(fileName);

            bool set2D = ObjectFinder.Self.GlueProject.In2D || make2D;


            switch (extension)
            {
                case "shcx":
                    EditorObjects.EditorSettings.PolygonEditorSettings pes = new EditorObjects.EditorSettings.PolygonEditorSettings();
                    if (set2D)
                    {
                        pes.UsePixelCoordinates = true;
                    }

                    settingsFile += ".pesix";

                    FileManager.XmlSerialize(pes, settingsFile);

                    break;
                //case "splx":
                //    {
                //        EditorObjects.EditorSettings.SplineEditorSettingsSave ses =
                //            new EditorObjects.EditorSettings.SplineEditorSettingsSave();

                //        if (set2D)
                //        {
                //            ses.ViewCamera.Orthogonal = true;
                //            ses.ViewCamera.OrthogonalHeight = -1;

                //            ses.BoundsCamera.Orthogonal = true;
                //            ses.BoundsCamera.OrthogonalWidth = ObjectFinder.Self.GlueProject.OrthogonalWidth;
                //            ses.BoundsCamera.OrthogonalHeight = ObjectFinder.Self.GlueProject.OrthogonalHeight;
                //        }

                //        FileManager.XmlSerialize(ses, settingsFile + ".splsetx");
                //    }
                //    break;
                case "emix":
                    {

                    }

                    break;
            }

        }
    }
}
