using System;
using System.Collections.Generic;
using System.Text;

using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using FlatRedBall.Content.Scene;

using FlatRedBall;
using FlatRedBall.IO;
namespace EditorObjects.EditorSettings
{
    [Serializable]
    public class SpriteEditorSceneProperties
    {
        #region Fields

        public CameraSave BoundsCamera;
        public CameraSave Camera = new CameraSave();
        public bool BoundsVisible = false;
        public bool WorldAxesVisible = true;

		public float PixelSize;

        public List<string> FilesToMakeDotDotSlashRelative = new List<string>();

        public List<TextureDisplayRegionsSave> TextureDisplayRegionsList = new List<TextureDisplayRegionsSave>();

        #endregion

        #region Methods

        // For loading.
        public SpriteEditorSceneProperties()
        {
            Camera.Z = -40;
            // do nothing
        }

        // For saving.
        public SpriteEditorSceneProperties(Camera camera, Camera boundsCamera, float pixelSize, bool boundsVisible)
        {
            SetFromRuntime(camera, boundsCamera, pixelSize, boundsVisible);
        }


        public void SetFromRuntime(Camera camera, Camera boundsCamera, float pixelSize, bool boundsVisible)
        {

            Camera = CameraSave.FromCamera(camera);
            BoundsCamera = CameraSave.FromCamera(boundsCamera, true);
            BoundsVisible = boundsVisible;
            
            PixelSize = pixelSize;
//            Gui.GuiData.listWindow
        }

        public void SetCameras(Camera camera, Camera boundsCamera)
        {

            bool isPixelPerfect2D = this.Camera.OrthogonalHeight <= 0;

            this.Camera.SetCamera(camera);
            this.BoundsCamera.SetCamera(boundsCamera);

            if (isPixelPerfect2D)
            {
                camera.UsePixelCoordinates();
            }
                        

        }

        public static SpriteEditorSceneProperties Load(string fileName)
        {
            SpriteEditorSceneProperties returnObject =  FileManager.XmlDeserialize<SpriteEditorSceneProperties>(fileName);

            string directory = FileManager.GetDirectory(fileName);
            string oldRelativeDirectory = FileManager.RelativeDirectory;
            FileManager.RelativeDirectory = directory;

            for (int i = 0; i < returnObject.FilesToMakeDotDotSlashRelative.Count; i++)
            {
                returnObject.FilesToMakeDotDotSlashRelative[i] = FileManager.MakeAbsolute(returnObject.FilesToMakeDotDotSlashRelative[i]);
            }

            FileManager.RelativeDirectory = oldRelativeDirectory;

            return returnObject;
        }

        public void Save(string fileName)
        {
            if (FileManager.IsRelative(fileName))
            {
                fileName = FileManager.MakeAbsolute(fileName);
            }

            string directory = FileManager.GetDirectory(fileName);
            string oldRelativeDirectory = FileManager.RelativeDirectory;
            FileManager.RelativeDirectory = directory;

            for (int i = 0; i < FilesToMakeDotDotSlashRelative.Count; i++)
            {
                FilesToMakeDotDotSlashRelative[i] = FileManager.MakeRelative(FilesToMakeDotDotSlashRelative[i]);
            }

            FileManager.XmlSerialize(this, fileName);

            for (int i = 0; i < FilesToMakeDotDotSlashRelative.Count; i++)
            {
                FilesToMakeDotDotSlashRelative[i] = FileManager.MakeAbsolute(FilesToMakeDotDotSlashRelative[i]);
            }

            FileManager.RelativeDirectory = oldRelativeDirectory;
        }

        #endregion

    }
}
