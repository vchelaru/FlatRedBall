#if false
#define SUPPORTS_LIGHTS
#endif

using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.Xml.Serialization;

using FlatRedBall.Graphics;
using FlatRedBall.ManagedSpriteGroups;

using FlatRedBall.Content.Scene;
#if SUPPORTS_LIGHTS
using FlatRedBall.Content.Lighting;
using FlatRedBall.Graphics.Lighting;
using FlatRedBall.Content.Lighting;
#endif
using FlatRedBall.Content.SpriteFrame;
using FlatRedBall.Content.SpriteGrid;

using FileManager = FlatRedBall.IO.FileManager;

using FlatRedBall.Content.Saves;
using FlatRedBall.IO;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Math;
using FlatRedBall.Graphics.Texture;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using System.Globalization;
using Microsoft.Xna.Framework.Graphics;

namespace FlatRedBall.Content
{
    public enum SceneSettingOptions
    {
        None = 0,
        ForceLoadDefaultModelTextures = 1,
        ConvertZSeparatedSpritesIntoSpriteGrids = 2,
        SkipTextureLoading
    }

    /// <summary>
    /// This is the class that is serialized and deserialized to/from the XML file representing a scene.
    /// </summary>
    [Obsolete("Use FlatRedBall.Content.Scene.SceneSave instead")]
	[XmlInclude(typeof(SceneSave))]
    public class SpriteEditorScene : 
        SpriteEditorSceneBase<SpriteSave, SpriteGridSave, SpriteFrameSave, TextSave>,
        ISaveableContent
    {
#if ANDROID || IOS
        public static bool ManualDeserialization = true;
#else
        public static bool ManualDeserialization = false;
#endif
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
                foreach (SpriteSave spriteSave in SpriteList)
                {
                    if (!referencedTextures.Contains(spriteSave.Texture))
                    {
                        referencedTextures.Add(spriteSave.Texture);
                    }
                }

                foreach (SpriteSave spriteSave in DynamicSpriteList)
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

        [XmlIgnore]
        public bool IsEmpty
        {
            get
            {
                return this.DynamicSpriteList.Count == 0 &&
                    this.SpriteFrameSaveList.Count == 0 &&
                    this.SpriteGridList.Count == 0 &&
                    this.SpriteList.Count == 0 &&
                    this.TextSaveList.Count == 0;
            }
        }

        #endregion


        #region Public Methods

        public static SceneSave FromFile(string fileName)
        {
            SceneSave tempScene = null;
            if (ManualDeserialization)
            {
                tempScene = DeserializeManually(fileName);
            }
            else
            {
                tempScene = FileManager.XmlDeserialize<SceneSave>(fileName);
            }

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

        private static SceneSave DeserializeManually(string fileName)
        {

            SceneSave toReturn = new SceneSave();

            System.Xml.Linq.XDocument xDocument = null;

            using (var stream = FileManager.GetStreamForFile(fileName))
            {
                xDocument = System.Xml.Linq.XDocument.Load(stream);
            }

            System.Xml.Linq.XElement foundElement = null;

            foreach (var element in xDocument.Elements())
            {
                if (element.Name.LocalName == "SpriteEditorScene")
                {
                    foundElement = element;
                    break;
                }
            }

            LoadFromElement(toReturn, foundElement);

            return toReturn;
        }

        private static void LoadFromElement(SpriteEditorScene toReturn, System.Xml.Linq.XElement element)
        {
            foreach (var subElement in element.Elements())
            {
                switch (subElement.Name.LocalName)
                {
                    case "Sprite":
                        AddSpriteFromXElement(toReturn, subElement);

                        break;
                    case "SpriteFrame":
                        SpriteFrameSave sfs = SpriteFrameSave.FromXElement(subElement);
                        toReturn.SpriteFrameSaveList.Add(sfs);
                        break;
                    case "Camera":
                        AddCameraFromXElement(toReturn, subElement);

                        break;
                    case "Text":
                        AddTextFromXElement(toReturn, subElement);
                        break;
                    case "PixelSize":
                        toReturn.PixelSize = SceneSave.AsFloat(subElement);
                        break;
                    case "AssetsRelativeToSceneFile":
                        toReturn.AssetsRelativeToSceneFile = AsBool(subElement);
                        break;
                    case "CoordinateSystem":
                        toReturn.CoordinateSystem = (CoordinateSystem)Enum.Parse(typeof(CoordinateSystem), subElement.Value, true);
                        break;
                    case "Snapping":
                        toReturn.Snapping = AsBool(subElement);
                        break;
                    case "SpriteGrid":
                        SpriteGridSave spriteGridSave = SpriteGridSave.FromXElement(subElement);
                        toReturn.SpriteGridList.Add(spriteGridSave);
                        break;
                    case "FileName":

                        // do nothing with this, shouldn't be here anyway:
                        break;
                    default:
                        throw new NotImplementedException("Unexpected node in XML: " +
                            subElement.Name.LocalName);
                        //break;

                }
            }
        }

        private static void AddTextFromXElement(SpriteEditorScene toReturn, System.Xml.Linq.XElement element)
        {
            TextSave textSave = new TextSave();
            toReturn.TextSaveList.Add(textSave);

            foreach (var subElement in element.Elements())
            {
                switch (subElement.Name.LocalName)
                {
                    case "Blue":
                        textSave.Blue = SceneSave.AsFloat(subElement);
                        break;
                    case "ColorOperation":
                        textSave.ColorOperation = subElement.Value;
                        break;
                    case "CursorSelectable":
                        textSave.CursorSelectable = AsBool(subElement);
                        break;
                    case "DisplayText":
                        textSave.DisplayText = subElement.Value;
                        break;
                    case "FontFile":
                        textSave.FontFile = subElement.Value;
                        break;
                    case "FontTexture":
                        textSave.FontTexture = subElement.Value;
                        break;
                    case "Green":
                        textSave.Green = SceneSave.AsFloat(subElement);
                        break;
                    case "HorizontalAlignment":
                        textSave.HorizontalAlignment = (HorizontalAlignment)Enum.Parse(typeof(HorizontalAlignment), subElement.Value, true);
                        break;
                    case "MaxWidth":
                        textSave.MaxWidth = SceneSave.AsFloat(subElement);
                        break;
                    case "MaxWidthBehavior":
                        textSave.MaxWidthBehavior = (MaxWidthBehavior)Enum.Parse(typeof(MaxWidthBehavior), subElement.Value, true);
                        break;
                    case "Name":
                        textSave.Name = subElement.Value;
                        break;
                    case "NewLineDistance":
                        textSave.NewLineDistance = SceneSave.AsFloat(subElement);
                        break;
                    case "Red":
                        textSave.Red = SceneSave.AsFloat(subElement);
                        break;
                    case "RelativeX":
                        textSave.RelativeX = SceneSave.AsFloat(subElement);
                        break;
                    case "RelativeY":
                        textSave.RelativeY = SceneSave.AsFloat(subElement);
                        break;
                    case "RelativeZ":
                        textSave.RelativeZ = SceneSave.AsFloat(subElement);
                        break;
                    case "RelativeRotationX":
                        textSave.RelativeRotationX = SceneSave.AsFloat(subElement);
                        break;
                    case "RelativeRotationY":
                        textSave.RelativeRotationY = SceneSave.AsFloat(subElement);
                        break;
                    case "RelativeRotationZ":
                        textSave.RelativeRotationZ = SceneSave.AsFloat(subElement);
                        break;
                    
                    case "RotationX":
                        textSave.RotationX = SceneSave.AsFloat(subElement);
                        break;
                    case "RotationY":
                        textSave.RotationY = SceneSave.AsFloat(subElement);
                        break;
                    case "RotationZ":
                        textSave.RotationZ = SceneSave.AsFloat(subElement);
                        break;
                    case "Scale":
                        textSave.Scale = SceneSave.AsFloat(subElement);
                        break;
                    case "Spacing":
                        textSave.Spacing = SceneSave.AsFloat(subElement);
                        break;
                    case "VerticalAlignment":
                        textSave.VerticalAlignment = (VerticalAlignment)Enum.Parse(typeof(VerticalAlignment), subElement.Value, true);
                        break;
                    case "Visible":
                        textSave.Visible = AsBool(subElement);
                        break;
                    case "X":
                        textSave.X = SceneSave.AsFloat(subElement);
                        break;
                    case "Y":
                        textSave.Y = SceneSave.AsFloat(subElement);
                        break;
                    case "Z":
                        textSave.Z = SceneSave.AsFloat(subElement);
                        break;


                    default:
                        throw new NotImplementedException("Error trying to apply property " +
                            subElement.Name.LocalName + " on Text");

                        //break;
                }

            }
        }

        private static void AddCameraFromXElement(SpriteEditorScene toReturn, System.Xml.Linq.XElement element)
        {
            
            CameraSave cameraSave = new CameraSave();
            toReturn.Camera = cameraSave;

            foreach (var subElement in element.Elements())
            {
                switch (subElement.Name.LocalName)
                {
                    case "FarClipPlane":
                        cameraSave.FarClipPlane = SceneSave.AsFloat(subElement);
                        break;
                    case "Name":
                        //cameraSave. = subElement.Value;
                        break;
                    case "NearClipPlane":
                        cameraSave.NearClipPlane = SceneSave.AsFloat(subElement);
                        break;
                    case "Orthogonal":
                        cameraSave.Orthogonal = AsBool(subElement);
                        break;
                    case "OrthogonalHeight":
                        cameraSave.OrthogonalHeight = SceneSave.AsFloat(subElement);
                        break;
                    case "OrthogonalWidth":
                        cameraSave.OrthogonalWidth = SceneSave.AsFloat(subElement);
                        break;
                    case "X":
                        cameraSave.X = SceneSave.AsFloat(subElement);
                        break;
                    case "Y":
                        cameraSave.Y = SceneSave.AsFloat(subElement);
                        break;
                    case "Z":
                        cameraSave.Z = SceneSave.AsFloat(subElement);
                        break;

                    case "AspectRatio":
                        cameraSave.AspectRatio = SceneSave.AsFloat(subElement);
                        break;

                    default:

                        throw new NotImplementedException("Error trying to apply property " +
                            subElement.Name.LocalName + " on Camera");
                        //break;
                }
            }
        }

        private static void AddSpriteFromXElement(SpriteEditorScene toReturn, System.Xml.Linq.XElement element)
        {
            toReturn.SpriteList.Add(SpriteSave.FromXElement(element));
        }


        internal static bool AsBool(System.Xml.Linq.XElement subElement)
        {
            return bool.Parse(subElement.Value);
        }

        internal static string[][] AsStringArrayArray(System.Xml.Linq.XElement element)
        {
            List<List<string>> stringListList = new List<List<string>>();

            foreach (var subElement in element.Elements())
            {
                List<string> list = new List<string>();
                stringListList.Add(list);
                foreach(var subSubElement in subElement.Elements())
                {
                    list.Add(subSubElement.Value);
                }
            }

            string[][] toReturn = new string[stringListList.Count][];

            for(int i = 0; i < stringListList.Count; i++)
            {
                List<string> stringList = stringListList[i];

                toReturn[i] = stringListList[i].ToArray();
            }

            return toReturn;
        }


        internal static List<float> AsFloatList(System.Xml.Linq.XElement element)
        {
            List<float> toReturn = new List<float>();

            foreach (var subElement in element.Elements())
            {
                toReturn.Add(SceneSave.AsFloat(subElement));

            }

            return toReturn;
        }

        internal static char AsChar(System.Xml.Linq.XElement element)
        {
            return element.Value[0];
        }
        public SpriteSave FindSpriteByName(string nameOfSprite)
        {
            for (int i = 0; i < SpriteList.Count; i++)
            {
                SpriteSave spriteSave = SpriteList[i];

                if (spriteSave.Name == nameOfSprite)
                {
                    return spriteSave;
                }
            }

            return null;
        }

        public SpriteFrameSave FindSpriteFrameSaveByName(string nameOfSpriteFrame)
        {
            for (int i = 0; i < SpriteFrameSaveList.Count; i++)
            {
                SpriteFrameSave spriteFrameSave = SpriteFrameSaveList[i];

                if (spriteFrameSave.ParentSprite.Name == nameOfSpriteFrame)
                {
                    return spriteFrameSave;
                }
            }

            return null;

        }

        public void SetSceneDirectory()
        {
            if (string.IsNullOrEmpty(mFileName)) return;
            if (FileManager.IsRelative(mFileName))
            {
                mSceneDirectory = FileManager.GetDirectory(FileManager.RelativeDirectory + mFileName);
            }
            else
            {
                mSceneDirectory = FileManager.GetDirectory(mFileName);
            }

        }

        public void ValidateDependencies(SpriteList spritesToValidate)
        {
            SceneSave.ValidateDependencies(SpriteList, spritesToValidate);
        }

        public FlatRedBall.Scene ToScene(string contentManagerName)
        {
            FlatRedBall.Scene scene = ToScene<Sprite>(contentManagerName);
            return scene;
        }

        //public FlatRedBall.Scene ToScene(TextureAtlas textureAtlas)
        //{
        //    FlatRedBall.Scene scene = new FlatRedBall.Scene();

        //    SetScene<Sprite>(null, scene, SceneSettingOptions.None, textureAtlas);

        //    return scene;
        //}

        public FlatRedBall.Scene ToScene<SpriteType>
            (string contentManagerName) where SpriteType : Sprite, new()
        {
            FlatRedBall.Scene scene = new FlatRedBall.Scene();

            SetScene<SpriteType>(contentManagerName, scene);

            return scene;
        }

        public void SetScene<SpriteType>(string contentManagerName, FlatRedBall.Scene scene) where SpriteType : Sprite, new()
        {
            SetScene<SpriteType>(contentManagerName, scene, SceneSettingOptions.None);
        }

        public void SetScene<SpriteType>(string contentManagerName, FlatRedBall.Scene scene, SceneSettingOptions options) where SpriteType : Sprite, new()
        {
            #region Set the FileManager.RelativeDirectory if necessary

            string oldRelativeDirectory = FileManager.RelativeDirectory;
            if (AssetsRelativeToSceneFile && mSceneDirectory != null)
            {
                FileManager.RelativeDirectory = mSceneDirectory;
            }

            #endregion

            CreateSprites<SpriteType>(contentManagerName, scene, options);

            AddSpriteGridsToScene(contentManagerName, scene, options);

            #region Create the SpriteFrames

            foreach (SpriteFrameSave spriteFrameSave in SpriteFrameSaveList)
            {
                FlatRedBall.ManagedSpriteGroups.SpriteFrame spriteFrame =
                    spriteFrameSave.ToSpriteFrame(contentManagerName);

                scene.SpriteFrames.Add(spriteFrame);
            }

            // TODO:  Attach the SpriteFrames

            #endregion

            #region Create the Texts
            foreach (TextSave textSave in TextSaveList)
            {
                Text text = textSave.ToText(contentManagerName);

                scene.Texts.Add(text);
            }
            #endregion

            #region Invert Z if necessary
            if (CoordinateSystem == FlatRedBall.Math.CoordinateSystem.LeftHanded)
                scene.InvertHandedness();
            #endregion

            FileManager.RelativeDirectory = oldRelativeDirectory;

            // Set the name of the scene to the file name

            // Vic says - not sure why this is being made relative.  
            // It should be like a Texture - storing its full path name so it
            // can be re-saved easily.
            //if (!(string.IsNullOrEmpty(mFileName)))  //asdffdsa
            //    scene.Name = FileManager.RemovePath(this.mFileName);
            scene.Name = this.mFileName;
        }

        private void CreateSprites<SpriteType>(string contentManagerName, FlatRedBall.Scene scene, SceneSettingOptions options) where SpriteType : Sprite, new()
        {
            bool hasFlag = (options & SceneSettingOptions.ConvertZSeparatedSpritesIntoSpriteGrids) == SceneSettingOptions.ConvertZSeparatedSpritesIntoSpriteGrids;

            if (!hasFlag)
            {
                // Create the Sprites
                foreach (SpriteSave spriteSave in SpriteList)
                {
                    Sprite newSprite = null;

                    newSprite = spriteSave.ToSprite<SpriteType>(contentManagerName);

                    scene.Sprites.Add(newSprite);
                }

                // Attach the Sprites
                foreach (SpriteSave spriteSave in SpriteList)
                {
                    if (string.IsNullOrEmpty(spriteSave.Parent) == false)
                    {
                        scene.Sprites.FindByName(spriteSave.Name).AttachTo(
                            scene.Sprites.FindByName(spriteSave.Parent), false);
                    }
                }
            }
        }

        private void AddSpriteGridsToScene(string contentManagerName, FlatRedBall.Scene scene, SceneSettingOptions options)
        {
            foreach (SpriteGridSave spriteGridSave in SpriteGridList)
            {
                // for now just use the default camera and create a new Random - not sure if user ever needs
                // to specify the Random to use - perhaps for multiplayer games and SpriteGrids which are using AnimationChains
                FlatRedBall.ManagedSpriteGroups.SpriteGrid spriteGrid = spriteGridSave.ToSpriteGrid(
                    SpriteManager.Camera, contentManagerName);

                scene.SpriteGrids.Add(spriteGrid);
            }
            bool hasFlag = (options & SceneSettingOptions.ConvertZSeparatedSpritesIntoSpriteGrids) == SceneSettingOptions.ConvertZSeparatedSpritesIntoSpriteGrids;
            
            if (hasFlag)
            {

                Dictionary<int, FlatRedBall.ManagedSpriteGroups.SpriteGrid> spriteGrids = new Dictionary<int, FlatRedBall.ManagedSpriteGroups.SpriteGrid>();

                foreach (SpriteSave spriteSave in this.SpriteList)
                {
                    FloatRectangle floatRectangle = new FloatRectangle();
                    int zAsInt = MathFunctions.RoundToInt(spriteSave.Z);

                    if (spriteGrids.ContainsKey(zAsInt) == false)
                    {
                        var newGrid = new ManagedSpriteGroups.SpriteGrid(SpriteManager.Camera, ManagedSpriteGroups.SpriteGrid.Plane.XY, spriteSave.ToSprite(contentManagerName));
                        spriteGrids.Add(zAsInt, newGrid);
                        scene.SpriteGrids.Add(newGrid);
                    }

                    var spriteGrid = spriteGrids[zAsInt];

                    spriteGrid.XLeftBound = System.Math.Min(spriteGrid.XLeftBound, spriteSave.X - spriteSave.ScaleX * 2);
                    spriteGrid.YBottomBound = System.Math.Min(spriteGrid.YBottomBound, spriteSave.Y - spriteSave.ScaleY * 2);

                    spriteGrid.XRightBound = System.Math.Max(spriteGrid.XRightBound, spriteSave.X + spriteSave.ScaleX * 2);
                    spriteGrid.YTopBound = System.Math.Max(spriteGrid.YTopBound, spriteSave.Y + spriteSave.ScaleY * 2);

                    spriteGrid.PaintSprite(spriteSave.X, spriteSave.Y, spriteSave.Z, 
                        FlatRedBallServices.Load<Texture2D>(spriteSave.Texture));

                    floatRectangle.Left = spriteSave.LeftTextureCoordinate;
                    floatRectangle.Right = spriteSave.RightTextureCoordinate;
                    floatRectangle.Top = spriteSave.TopTextureCoordinate;
                    floatRectangle.Bottom = spriteSave.BottomTextureCoordinate;


                    spriteGrid.PaintSpriteDisplayRegion(spriteSave.X, spriteSave.Y, spriteSave.Z,
                        ref floatRectangle);
                }


            }
        }

        public List<string> GetMissingFiles()
        {
            List<string> texturesNotFound = new List<string>();

            // Get a list of all files that this .scnx references.  This won't return any duplicates
            List<string> allReferencedFiles = GetReferencedFiles(RelativeType.Relative);

            #region Set the FileManager.RelativeDirectory if necessary

            string oldRelativeDirectory = FileManager.RelativeDirectory;
            if (AssetsRelativeToSceneFile)
            {
                if(string.IsNullOrEmpty(ScenePath))
                {
                    throw new Exception("You must set ScenePath first before attempting to find missing files");
                }
                FileManager.RelativeDirectory = mSceneDirectory;
            }
            #endregion

            for (int i = 0; i < allReferencedFiles.Count; i++)
            {
                if (!FileManager.FileExists(allReferencedFiles[i]))
                {
                    texturesNotFound.Add(allReferencedFiles[i]);
                }
            }

            #region Set the FileManager.RelativeDirectory back to its old value

            FileManager.RelativeDirectory = oldRelativeDirectory;

            #endregion

            return texturesNotFound;
        }


        public List<string> GetReferencedFiles(RelativeType relativeType)
        {
            List<string> referencedFiles = new List<string>();

            foreach (SpriteSave spriteSave in SpriteList)
            {
                spriteSave.GetReferencedFiles(referencedFiles);
            }

            foreach (SpriteGridSave spriteGridSave in SpriteGridList)
            {
                if (!string.IsNullOrEmpty(spriteGridSave.BaseTexture) && !referencedFiles.Contains(spriteGridSave.BaseTexture))
                {
                    referencedFiles.Add(spriteGridSave.BaseTexture);
                }

                foreach (string[] stringArray in spriteGridSave.GridTexturesArray)
                {
                    foreach (string texture in stringArray)
                    {
                        if (!string.IsNullOrEmpty(texture) && !referencedFiles.Contains(texture))
                        {
                            referencedFiles.Add(texture);
                        }
                    }
                }
            }

            foreach (SpriteFrameSave spriteFrameSave in SpriteFrameSaveList)
            {
                if (!string.IsNullOrEmpty(spriteFrameSave.ParentSprite.Texture) &&
                    !referencedFiles.Contains(spriteFrameSave.ParentSprite.Texture))
                {
                    referencedFiles.Add(spriteFrameSave.ParentSprite.Texture);
                }

                if (!string.IsNullOrEmpty(spriteFrameSave.ParentSprite.AnimationChainsFile) &&
                    !referencedFiles.Contains(spriteFrameSave.ParentSprite.AnimationChainsFile))
                {
                    referencedFiles.Add(spriteFrameSave.ParentSprite.AnimationChainsFile);
                }
            }

            foreach (TextSave textSave in TextSaveList)
            {
                string fontFile = textSave.FontFile;
                bool isThereAFontFile = !string.IsNullOrEmpty(fontFile);
                if (isThereAFontFile &&
                    !referencedFiles.Contains(fontFile))
                {
                    referencedFiles.Add(fontFile);
                }


                string textureFile = textSave.FontTexture;
                if (!string.IsNullOrEmpty(textureFile))
                {
                    if (!referencedFiles.Contains(textureFile))
                    {
                        referencedFiles.Add(textureFile);
                    }
                }
                else if(isThereAFontFile)
                {
                    // This may be a multi-texture font, so let's check for that
                    string absoluteFontDirectory = this.mSceneDirectory + fontFile;

                    if (FileManager.FileExists(absoluteFontDirectory))
                    {
                        string fontDirectory = FileManager.GetDirectory(fontFile, FlatRedBall.IO.RelativeType.Relative);

                        string contents = FileManager.FromFileText(absoluteFontDirectory);

                        string[] textures = BitmapFont.GetSourceTextures(contents);

                        for (int i = 0; i < textures.Length; i++)
                        {
                            string textureWithFontFileDirectory = textures[i];

                            //make the texture hae the font file's directory
                            textureWithFontFileDirectory = fontDirectory + textureWithFontFileDirectory;


                            if (!referencedFiles.Contains(textureWithFontFileDirectory))
                            {
                                referencedFiles.Add(textureWithFontFileDirectory);
                            }
                        }
                    }
                }

            }

            if (relativeType == RelativeType.Absolute)
            {
                string directory = this.ScenePath;

                for (int i = 0; i < referencedFiles.Count; i++)
                {
                    if (FileManager.IsRelative(referencedFiles[i]))
                    {
                        referencedFiles[i] = directory + referencedFiles[i];
                    }
                }
            }

            return referencedFiles;
        }

        public List<SourceReferencingFile> GetSourceReferencingReferencedFiles(RelativeType relativeType)
        {
            List<SourceReferencingFile> referencedFiles = new List<SourceReferencingFile>();

            if (relativeType == RelativeType.Absolute)
            {
                string directory = this.ScenePath;

                string oldRelativeDirectory = FileManager.RelativeDirectory;
                FileManager.RelativeDirectory = directory;

                for (int i = 0; i < referencedFiles.Count; i++)
                {
                    referencedFiles[i].SourceFile = 
                        FileManager.MakeAbsolute(referencedFiles[i].SourceFile);

                    referencedFiles[i].DestinationFile =
                        FileManager.MakeAbsolute(referencedFiles[i].DestinationFile);
                }

                FileManager.RelativeDirectory = oldRelativeDirectory;
            }

            return referencedFiles;

        }

        public void SetCamera()
        {
            SetCamera(SpriteManager.Camera);
        }

        public void SetCamera(Camera camera)
        {
            this.Camera.SetCamera(camera);
            if (CoordinateSystem == FlatRedBall.Math.CoordinateSystem.LeftHanded)
            {
                camera.Z *= -1;
            }
        }

        public void Save(string fileName)
        {
            CoordinateSystem = FlatRedBall.Math.CoordinateSystem.RightHanded;

            if (AssetsRelativeToSceneFile)
            {
                MakeAssetsRelative(fileName);
            }

            AssignNamesIfEmptyOrNull();

            FileManager.XmlSerialize(this, fileName);
        }

        public static SpriteEditorScene FromScene(FlatRedBall.Scene scene)
        {
            SpriteEditorScene spriteEditorScene = new SpriteEditorScene();

            SetFromScene(scene, spriteEditorScene);

            return spriteEditorScene;
        }

        public static void SetFromScene(FlatRedBall.Scene scene, SpriteEditorScene spriteEditorScene)
        {
            foreach (Sprite sprite in scene.Sprites)
            {
                spriteEditorScene.SpriteList.Add(SpriteSave.FromSprite(sprite));
            }

            foreach (FlatRedBall.ManagedSpriteGroups.SpriteGrid spriteGrid in scene.SpriteGrids)
            {
                spriteEditorScene.SpriteGridList.Add(
                    FlatRedBall.Content.SpriteGrid.SpriteGridSave.FromSpriteGrid(spriteGrid));
            }

            foreach (FlatRedBall.ManagedSpriteGroups.SpriteFrame spriteFrame in scene.SpriteFrames)
            {
                spriteEditorScene.SpriteFrameSaveList.Add(
                    SpriteFrameSave.FromSpriteFrame(spriteFrame));
            }

            foreach (Text text in scene.Texts)
            {
                spriteEditorScene.TextSaveList.Add(
                    TextSave.FromText(text));
            }
        }
        
        #endregion

        #region Private Methods

        private void AssignNamesIfEmptyOrNull()
        {
            int currentIndex = 0;

            foreach (SpriteSave ss in SpriteList)
            {
                if (string.IsNullOrEmpty(ss.Name))
                {
                    ss.Name = currentIndex.ToString();
                    currentIndex++;
                }
            }

            foreach (SpriteGridSave sgs in SpriteGridList)
            {
                if (string.IsNullOrEmpty(sgs.Name))
                {
                    sgs.Name = currentIndex.ToString();
                    currentIndex++;
                }
            }

        }


        private void MakeAssetsRelative(string fileName)
        {
            string oldRelativeDirectory = FileManager.RelativeDirectory;

            if (FileManager.IsRelative(fileName))
            {
                fileName = FileManager.MakeAbsolute(fileName);
            }
            FileManager.RelativeDirectory = FileManager.GetDirectory(fileName);


            #region SpriteSave - Make textures relative
            foreach (SpriteSave ss in SpriteList)
            {
                ss.MakeRelative();
            }
            #endregion


            #region SpriteFrameSave - Make the textures relative

            foreach (SpriteFrameSave sfs in SpriteFrameSaveList)
            {
                sfs.ParentSprite.Texture = FileManager.MakeRelative(sfs.ParentSprite.Texture);

                if (string.IsNullOrEmpty(sfs.ParentSprite.AnimationChainsFile) == false)
                {
                    sfs.ParentSprite.AnimationChainsFile = 
                        FileManager.MakeRelative(sfs.ParentSprite.AnimationChainsFile);
                }
            }

            #endregion


            #region SpriteGridSave - Make textures realtive
            foreach (SpriteGridSave sgs in SpriteGridList)
            {
                sgs.BaseTexture = FileManager.MakeRelative(sgs.BaseTexture);
                sgs.Blueprint.Texture = FileManager.MakeRelative(sgs.Blueprint.Texture);
                foreach (string[] row in sgs.GridTexturesArray)
                {
                    for (int i = 0; i < row.Length; i++)
                    {
                        row[i] = FileManager.MakeRelative(row[i]);
                    }
                }

                if (sgs.AnimationChainGridSave != null)
                {
                    sgs.AnimationChainGridSave.MakeRelative();
                }

            }
            #endregion

            #region TextSaves - Make textures and .fnt files relative

            foreach (TextSave textSave in TextSaveList)
            {
                textSave.FontTexture = FileManager.MakeRelative(textSave.FontTexture);
                textSave.FontFile = FileManager.MakeRelative(textSave.FontFile);
            }

            #endregion

            FileManager.RelativeDirectory = oldRelativeDirectory;
        }


        #endregion


    }
}
