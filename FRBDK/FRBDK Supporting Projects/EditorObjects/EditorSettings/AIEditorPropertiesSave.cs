using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Content.Scene;
using FlatRedBall;
using FlatRedBall.IO;

namespace EditorObjects.EditorSettings
{
    public class AIEditorPropertiesSave
    {
        public const string Extension = "aiep";

        public CameraSave BoundsCamera;
        public CameraSave Camera = new CameraSave();
        public bool BoundsVisible = false;

                
        // For loading.
        public AIEditorPropertiesSave()
        {
            Camera.Z = 40;
            // do nothing
        }

        public void SetFromRuntime(Camera camera, Camera boundsCamera, bool boundsVisible)
        {

            Camera = CameraSave.FromCamera(camera);
            if (boundsCamera != null)
            {
                BoundsCamera = CameraSave.FromCamera(boundsCamera, true);
            }
            BoundsVisible = boundsVisible;
            //            Gui.GuiData.listWindow
        }

        public static AIEditorPropertiesSave Load(string fileName)
        {
            AIEditorPropertiesSave toReturn = new AIEditorPropertiesSave();

            FileManager.XmlDeserialize(fileName, out toReturn);

            return toReturn;
        }

        public void Save(string fileName)
        {
            FileManager.XmlSerialize(this, fileName);
        }
    }
}
