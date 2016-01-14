using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using FlatRedBall.Content;
using FlatRedBall.Content.Scene;
using FlatRedBall.Content.SpriteFrame;
using FlatRedBall.Content.SpriteGrid;
using FlatRedBall.IO;

namespace FlatRedBall.Content
{
    [XmlRoot("SpriteEditorScene")]
    public class SpriteEditorSceneContent : SpriteEditorSceneBase<SpriteSaveContent, SpriteGridSaveContent, SpriteFrameSaveContent, TextSaveContent>
    {

        #region Properties
        [XmlIgnore]
        public string ScenePath
        {
            get { return mSceneDirectory; }
            set { mSceneDirectory = value; }
        }

        [XmlIgnore]
        public string[] ReferencedTextures
        {
            get
            {
                List<string> referencedTextures = new List<string>();
                foreach (SpriteSaveContent spriteSave in SpriteList)
                {
                    if (!referencedTextures.Contains(spriteSave.Texture))
                    {
                        referencedTextures.Add(spriteSave.Texture);
                    }
                }

                foreach (SpriteSaveContent spriteSave in DynamicSpriteList)
                {
                    if (!referencedTextures.Contains(spriteSave.Texture))
                    {
                        referencedTextures.Add(spriteSave.Texture);
                    }
                }

                return referencedTextures.ToArray();

            }

        }

        [XmlIgnore]
        public bool AllowLoadingModelsFromFile
        {
            get { return mAllowLoadingModelsFromFile; }
            set { mAllowLoadingModelsFromFile = value; }
        }

        /*
        [XmlIgnore]
        /// <summary>
        /// Returns an enumeration of all sprites in this scene
        /// </summary>
        public IEnumerable<SpriteSave> Sprites
        {
            get
            {
                foreach (SpriteSave s in this.DynamicSpriteList)
                {
                    yield return s;
                }

                foreach (SpriteSave s in this.SpriteList)
                {
                    yield return s;
                }
            }
        }
         */

        #endregion

        public SpriteEditorSceneContent() : base()
        {
        }

        public new static SpriteEditorSceneContent FromFile(string fileName)
        {

            if (FileManager.GetExtension(fileName) == "scn")
                throw new ArgumentException("Cannot load .scn files.  Convert file to .scnx in the SpriteEditor");

            SpriteEditorSceneContent tempScene =
                FileManager.XmlDeserialize<SpriteEditorSceneContent>(fileName);

            tempScene.mFileName = fileName;
            if (FileManager.IsRelative(fileName))
            {
                tempScene.mSceneDirectory = FileManager.GetDirectory(FileManager.RelativeDirectory + fileName);
            }
            else
            {
                tempScene.mSceneDirectory = FileManager.GetDirectory(fileName);
            }
            
            return tempScene;
        }
        
        
    }
}
