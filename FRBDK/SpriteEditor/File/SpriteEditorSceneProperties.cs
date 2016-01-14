using System;
using System.Collections.Generic;
using System.Text;

using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using FlatRedBall.Content.Scene;

using FlatRedBall;
using FlatRedBall.IO;
namespace SpriteEditor.File
{
    [Serializable]
    public class SpriteEditorSceneProperties
    {
        #region Fields

        public CameraSave BoundsCamera;
        public CameraSave Camera;

        [OptionalField (VersionAdded = 2)]
        public List<TextureReference> TextureReference;

        public bool WorldAxesVisible = true;

		public float PixelSize;

        #endregion

        #region Methods

        // For loading.
        public SpriteEditorSceneProperties()
        {
            // do nothing
        }

        // For saving.
        public SpriteEditorSceneProperties(Camera camera, Camera boundsCamera, float pixelSize)
        {
            Camera = CameraSave.FromCamera(camera);
            BoundsCamera = CameraSave.FromCamera(boundsCamera);
            TextureReference = new List<TextureReference>();
			PixelSize = pixelSize;
//            Gui.GuiData.listWindow
        }

        

        public static SpriteEditorSceneProperties Load(string fileName)
        {
            return FileManager.XmlDeserialize<SpriteEditorSceneProperties>(fileName);
        }

        public void Save(string fileName)
        {
            FileManager.XmlSerialize(this, fileName);
        }

        #endregion

    }
}
