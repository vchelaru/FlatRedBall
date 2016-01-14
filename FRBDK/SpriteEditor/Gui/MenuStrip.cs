using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall;
using System.IO;
using FlatRedBall.Graphics;
using FlatRedBall.IO;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Content;
using EditorObjects.Gui;
using SpriteGrid = FlatRedBall.ManagedSpriteGroups.SpriteGrid;
using EditorObjects;
using Microsoft.DirectX;
using FlatRedBall.Content.Model;
using EditorObjects.Savers;
using FlatRedBall.Content.Scene;
using FlatRedBall.Content.SpriteRig;
using EditorObjects.EditorSettings;
using FlatRedBall.Content.SpriteGrid;
using FlatRedBall.Math;

namespace SpriteEditor.Gui
{
    public class MenuStrip : FlatRedBall.Gui.MenuStrip
    {
        #region Fields


        private string mLastFileName;

        private string mLastFileTypeLoaded;

        private SpriteList namesToChange;

        private List<Texture2D> mTexturesNotRelative = new List<Texture2D>();
        private List<AnimationChainList> mAnimationChainListsNotRelative = 
            new List<AnimationChainList>();
        private List<string> mFntFilesNotRelative = new List<string>();

        #endregion

        #region Properties

        public string LastFileTypeLoaded
        {
            get { return mLastFileTypeLoaded; }
            set { mLastFileTypeLoaded = value; }
        }

        #endregion

        #region Event Methods

        #region File events

        #region New
        public static void NewSceneClick(Window callingWindow)
        {
            OkCancelWindow tempWindow = GuiManager.AddOkCancelWindow();
            tempWindow.Name = "New Scene";
            tempWindow.ScaleX = 12f;
            tempWindow.ScaleY = 6f;
            tempWindow.Message = "Creating a new scene will delete the current scene.  Continue?";
            tempWindow.OkClick += new GuiMessage(newSceneOk);
            tempWindow.HasMoveBar = true;
            GuiManager.AddDominantWindow(tempWindow);
        }


        static void newSceneOk(Window callingWindow)
        {
            GameData.MakeNewScene();
        }

        #endregion

        #region Save


        private void SaveSceneClick(Window callingWindow)
        {
            SaveSceneClick(GameData.FileName);
        }

        void SaveSceneAs_Click(Window callingWindow)
        {
            SaveSceneClick("");
        }

        public void SaveSceneClick(string fileName)
        {


            if (!string.IsNullOrEmpty(fileName))
            {
                mLastFileName = fileName;
            }
            List<string> stringArray = new List<string>();
            namesToChange = new SpriteList();
            foreach (Sprite s in GameData.Scene.Sprites)
            {
                if (stringArray.Contains(s.Name))
                {
                    namesToChange.AddOneWay(s);
                }
                else
                {
                    stringArray.Add(s.Name);
                }
            }

            if (AskQuestionsAndDelaySaveIfNecessary(SaveSceneClick))
            {

                if (namesToChange.Count != 0)
                {
                    MultiButtonMessageBox mbmb = GuiManager.AddMultiButtonMessageBox();
                    mbmb.Name = "Duplicate Sprite names found";
                    mbmb.Text = "Duplicate names found in scene.  Duplicate names can alter attachment information.  What would you like to do?";
                    mbmb.AddButton("Leave names as they are and save.", new GuiMessage(OpenFileWindowSaveScene));
                    mbmb.AddButton("Automatically change Sprite names and save.", new GuiMessage(ChangeNamesAndSave));
                    mbmb.AddButton("Cancel save.", null);
                }
                else
                {
                    if (string.IsNullOrEmpty(fileName))
                    {
                        OpenFileWindowSaveScene(null);
                    }
                    else
                    {
                        SaveSceneFileWindowOk(null);
                    }

                }



                ShowWarningsAndMessagesBeforeSaving();
            }
        }

        private void SaveSceneFileWindowOk(Window callingWindow)
        {
            string fileName;
            if (callingWindow != null && callingWindow is FileWindow)
                fileName = ((FileWindow)callingWindow).Results[0];
            else
                fileName = mLastFileName;
            mLastFileName = fileName;
            SceneSaver.MakeSceneRelativeAndSave(GameData.Scene, fileName, GameData.SceneContentManager,
                GameData.SpriteEditorSceneProperties.FilesToMakeDotDotSlashRelative,
                GameData.ReplaceTexture, SaveScene);
        }

        public event EventHandler SavedSuccess;

        public bool SaveScene(string name, bool areAssetsRelativeToScene)
        {
            string oldRelativeDirectory = FileManager.RelativeDirectory;
            if (areAssetsRelativeToScene)
            {
                FileManager.RelativeDirectory = FileManager.GetDirectory(name);
            }

            // This will reduce the file size if we use a lot of SpriteGrids
            for (int i = 0; i < GameData.Scene.SpriteGrids.Count; i++)
            {
                GameData.Scene.SpriteGrids[i].TrimGrid();
            }

            SpriteEditorScene ses = SpriteEditorScene.FromScene(GameData.Scene);

            ses.AssetsRelativeToSceneFile = areAssetsRelativeToScene;



            ses.Save(name);

            // TODO:  Need to check if file saving worked properly.
            bool wasSceneSaved = true;

            if (wasSceneSaved)
            {
                #region create the SpriteEditorSceneProperties

                GameData.SpriteEditorSceneProperties.SetFromRuntime(
                    GameData.Camera,
                    GameData.BoundsCamera,
                    GameData.EditorProperties.PixelSize,
                    GuiData.CameraBoundsPropertyGrid.Visible
                    );


                GameData.SpriteEditorSceneProperties.WorldAxesVisible = GameData.EditorProperties.WorldAxesDisplayVisible;

                GameData.SpriteEditorSceneProperties.TextureDisplayRegionsList.Clear();

                for (int k = 0; k < GuiData.ListWindow.TextureListBox.Count; k++)
                {
                    TextureDisplayRegionsSave tdrs = new TextureDisplayRegionsSave();

                    CollapseItem item = GuiData.ListWindow.TextureListBox[k];

                    // The FileManager's relative directory has been set, so we just have to call MakeRelative and it should work fine
                    tdrs.TextureName =
                        FileManager.MakeRelative(((Texture2D)item.ReferenceObject).SourceFile());

                    tdrs.DisplayRegions = GuiData.ListWindow.CreateTextureReferences(item);

                    GameData.SpriteEditorSceneProperties.TextureDisplayRegionsList.Add(tdrs);    
                }


                GameData.SpriteEditorSceneProperties.Save(FileManager.RemoveExtension(name) + ".sep");

                #endregion

                FlatRedBallServices.Owner.Text = "SpriteEditor - Currently editing " + name;
                // As mentioned in the othe area where GameData.Filename is set, I'm not
                // sure why we were removing the extension.  I've commented out the extension
                // removal since it causes problems with CTRL+S
                // name = FileManager.RemoveExtension(name);
                GameData.FileName = name;
                if (SavedSuccess != null)
                    SavedSuccess(this, null);
            }

            FileManager.RelativeDirectory = oldRelativeDirectory;
            if (! wasSceneSaved)
                GuiManager.ShowMessageBox("Could not save " + GameData.FileName + ".  Is the file readonly?", "Error Saving");
            return wasSceneSaved;
        }

        #endregion

        #region Load

        public void LoadSceneClick(Window callingWindow)
        {
            FileWindow tempFileWindow = GuiManager.AddFileWindow();
            tempFileWindow.Filter = "SpriteEditor Binary Scene (*.scn)|*.scn|SpriteEditor XML Scene (*.scnx)|*.scnx";
            tempFileWindow.CurrentFileType = "scnx";
            tempFileWindow.OkClick += new GuiMessage(LoadSceneOk);
        }

        private void LoadSceneInsertClick(Window callingWindow)
        {
            PerformLoadScn(callingWindow.Name, false);
        }

        private void LoadSceneOk(Window callingWindow)
        {
            AskToReplaceOrInsertNewScene(((FileWindow)callingWindow).Results[0]);
        }

        private void LoadSceneReplaceClick(Window callingWindow)
        {
            PerformLoadScn(callingWindow.Name, true);
        }

        private void LoadShapeCollectionClick(Window callingWindow)
        {
            FileWindow tempFileWindow = GuiManager.AddFileWindow();
            tempFileWindow.Filter = "ShapeCollection XML File(*.shcx)|*.shcx";
            tempFileWindow.CurrentFileType = "shcx";
            tempFileWindow.OkClick += new GuiMessage(LoadShapeCollectionOk);
        }

        private void LoadShapeCollectionOk(Window callingWindow)
        {
            string fileName = ((FileWindow)callingWindow).Results[0];

            GameData.AddShapeCollection(fileName);
        }

        public static void LoadSpriteRigClick(Window callingWindow)
        {
            OkCancelWindow tempWindow = GuiManager.AddOkCancelWindow();
            tempWindow.ScaleX = 22f;
            tempWindow.ScaleY = 19f;
            tempWindow.Message = "The SpriteEditor does not fully support SpriteRig editing.  Currently, the SpriteEditor can only remember animations for one SpriteRig at a time.  Therefore, you should not load more than one SpriteRig per SpriteEditor session unless you do not plan on saving the SpriteRigs.  \n\nAlso, do not add or remove any Sprites from the SpriteRig as the animation data depends on having the same number of joints and body sprites when saving the SpriteRig.\n\nIf you attempt to save a SpriteRig which does not match the animation information, the SpriteEditor will warn you.\n\nLoad SpriteRig?";
            tempWindow.OkText = "Yes";
            tempWindow.CancelText = "No";
            tempWindow.OkClick += new GuiMessage(OpenFileWindowLoadSpriteRig);
        }

        public static void LoadSpriteRigOk(Window callingWindow)
        {
            #region Load the SpriteRig

            EditorSprite es;
            string fileName = ((FileWindow)callingWindow).Results[0];

            SpriteRigSave srs = SpriteRigSave.FromFile(fileName);
            SpriteList loadedSprites = new SpriteList();

            SpriteRig spriteRig = srs.ToSpriteRig(GameData.SceneContentManager);

            #endregion

            #region Play some Pose so that the SpriteRig has a proper pose and texture coordinates

            // Play and stop an animation to get the texture coordinates set up in case
            // the SpriteRig has texture coords defined in its AnimationChains
            if (spriteRig.PoseChains.Count != 0)
            {
                spriteRig.SetPoseChain(spriteRig.PoseChains[0]);
                spriteRig.Animate = true;
                spriteRig.SetPositionAtTimeFromAnimationStart(0);
                spriteRig.Animate = false;

            }
            #endregion

            GuiData.srSaveOptions.joints = new SpriteList();
            GuiData.srSaveOptions.bodySprites = new SpriteList();
            GuiData.srSaveOptions.joints.Clear();
            GuiData.srSaveOptions.bodySprites.Clear();

            string oldRelativeDirectory = FileManager.RelativeDirectory;
            FileManager.RelativeDirectory = FileManager.GetDirectory(fileName);


            foreach (Sprite regularSprite in spriteRig.Joints)
            {
                es = new EditorSprite();

                es.SetFromRegularSprite(regularSprite);
                GameData.Scene.Sprites.Add(es);
                SpriteManager.AddSprite(es);
                loadedSprites.AddOneWay(es);
                GuiData.srSaveOptions.joints.Add(es);
            }
            foreach (Sprite regularSprite in spriteRig.BodySprites)
            {
                es = new EditorSprite();
                es.SetFromRegularSprite(regularSprite);
                GameData.Scene.Sprites.Add(es);
                SpriteManager.AddSprite(es);
                loadedSprites.AddOneWay(es);
                GuiData.srSaveOptions.bodySprites.Add(es);
            }

            // The root is not part of the body Sprites, but it should be
            if (spriteRig.Root != null && GameData.Scene.Sprites.Contains(spriteRig.Root) == false)
            {
                es = new EditorSprite();
                es.SetFromRegularSprite(spriteRig.Root);
                GameData.Scene.Sprites.Add(es);
                SpriteManager.AddSprite(es);
                loadedSprites.AddOneWay(es);
                GuiData.srSaveOptions.bodySprites.Add(es);

            }

            if (spriteRig.Root != null)
            {
                GuiData.srSaveOptions.root = GuiData.srSaveOptions.bodySprites.FindByName(spriteRig.Root.Name);
                GuiData.srSaveOptions.bodySprites.Remove(GuiData.srSaveOptions.root);
            }
            GuiData.srSaveOptions.poseChains = spriteRig.PoseChains;
            if (srs.JointsVisible)
            {
                GuiData.srSaveOptions.jointsVisible.Press();
            }
            else
            {
                GuiData.srSaveOptions.jointsVisible.Unpress();
            }
            if (srs.RootVisible)
            {
                GuiData.srSaveOptions.rootVisible.Press();
            }
            else
            {
                GuiData.srSaveOptions.rootVisible.Unpress();
            }

            FileManager.RelativeDirectory = oldRelativeDirectory;

            string oldRelative = FileManager.RelativeDirectory;
            if (srs.AssetsRelativeToFile)
            {
                FileManager.RelativeDirectory = FileManager.GetDirectory(fileName);
            }

            FileManager.RelativeDirectory = oldRelative;
            foreach (SpriteSave ss in srs.Joints)
            {
                srs.BodySprites.Add(ss);
            }


            for (int i = 0; i < loadedSprites.Count; i++)
            {

                if (loadedSprites[i].PixelSize > 0f)
                {
                    ((EditorSprite)loadedSprites[i]).ConstantPixelSizeExempt = false;
                }
                else
                {
                    ((EditorSprite)loadedSprites[i]).ConstantPixelSizeExempt = true;
                }

                if (loadedSprites[i].Texture.texture != null)
                {
                    GuiData.ListWindow.Add(loadedSprites[i].Texture);
                }

                string parentName = "";

                Sprite matchingSprite = spriteRig.BodySprites.FindByName(loadedSprites[i].Name);

                if (matchingSprite == null)
                {
                    matchingSprite = spriteRig.Joints.FindByName(loadedSprites[i].Name);
                }
                // parent may be null if there is no root
                if (matchingSprite != null && matchingSprite.Parent != null)
                {
                    parentName = matchingSprite.Parent.Name;

                    loadedSprites[i].AttachTo(loadedSprites.FindByName(parentName), false);
                }
            }
            GameData.Scene.Sprites.SortZInsertionDescending();




            AskToSearchForReplacements(((FileWindow)callingWindow).Results[0]);
        }


        public void AskToReplaceOrInsertNewScene(string fileName)
        {
            if (FileManager.FileExists(fileName))
            {
                OkCancelWindow tempWindow = GuiManager.AddOkCancelWindow();
                tempWindow.Message = "Would you like to replace the current scene or insert " + fileName + " into the current scene?";
                tempWindow.ScaleX = 16f;
                tempWindow.ScaleY = 10f;
                tempWindow.Name = fileName;
                tempWindow.OkText = "Replace";
                tempWindow.OkClick += new GuiMessage(LoadSceneReplaceClick);
                tempWindow.CancelText = "Insert";
                tempWindow.CancelClick += new GuiMessage(LoadSceneInsertClick);
            }
            else
            {
                MessageBox messageBox = GuiManager.ShowMessageBox("Could not find the file " + fileName, "Error loading .scnx");
            }
        }

        public void PerformLoadScn(string fileName, bool replace)
        {
            // This method is public because this method is called if the user drags a
            // .scnx onto the SpriteEditor

            #region Mark how many objects before loading in case there is an insertion.
            // If there is an insertion, only the newly-added objects should have post load
            // logic performed on them

            int numSpritesBefore = GameData.Scene.Sprites.Count;
            int numOfSGsBefore = GameData.Scene.SpriteGrids.Count;
            int numOfSpriteFramesBefore = GameData.Scene.SpriteFrames.Count;
            int numberOfPositionedModels = GameData.Scene.PositionedModels.Count;

            #endregion

            SpriteEditorScene tempSES = SpriteEditorScene.FromFile(fileName);

            #region See if there are any Models that reference files that aren't on disk

            string sceneDirectory = FileManager.GetDirectory(fileName);
            for (int i = 0; i < tempSES.PositionedModelSaveList.Count; i++)
            {
                PositionedModelSave modelSave = tempSES.PositionedModelSaveList[i];

                if (!FileManager.FileExists(modelSave.ModelFileName))
                {
                    // See if there's a .x with that name

                    if (FileManager.FileExists(sceneDirectory + modelSave.ModelFileName + ".x"))
                    {
                        modelSave.ModelFileName = modelSave.ModelFileName + ".x";
                    }
                }
            }
            #endregion

            #region Now, see if there are any other files that haven't been found and create the error window

            List<string> texturesNotFound = tempSES.GetMissingFiles();
            if (texturesNotFound.Count != 0)
            {
                OkListWindow okListWindow = new OkListWindow(
                    "There are files that the .scnx references which cannot be located.",
                    "Error loading .scnx");

                foreach (string file in texturesNotFound)
                {
                    okListWindow.AddItem(file);
                }

                return;
            }
            #endregion

            #region if replacing, clear the old scene out
            if (replace)
            {
                SpriteManager.RemoveScene(GameData.Scene, true);
                FlatRedBallServices.Unload(GameData.SceneContentManager);

                tempSES.SetCamera(GameData.Camera);

#if FRB_MDX
                if (tempSES.CoordinateSystem == FlatRedBall.Math.CoordinateSystem.RightHanded)
                {
                    GameData.Camera.Z *= -1;
                }
#endif

                GameData.EditorProperties.PixelSize = tempSES.PixelSize;

                GuiData.EditorPropertiesGrid.Refresh();

                // 4/16/2011:  The following line of code
                // was causing errors when saving the .scnx
                // file through CTRL+S.  Taking it out because
                // I don't see why we need this anyway.
                // GameData.FileName = FileManager.RemoveExtension(fileName);
                GameData.FileName = fileName;
                FlatRedBallServices.Owner.Text = "SpriteEditor - Currently editing " + GameData.FileName;

                GuiData.MenuStrip.LastFileTypeLoaded = FileManager.GetExtension(fileName);

            }
            #endregion


            Scene newlyLoadedScene = tempSES.ToScene<EditorSprite>(GameData.SceneContentManager);

            GameData.Scene.AddToThis(newlyLoadedScene);
            // This caused
            // a double-add.
            // Not sure why we're
            // adding GameData.Scene.
            //GameData.Scene.AddToManagers();
            newlyLoadedScene.AddToManagers();

            GuiData.ListWindow.RefreshListsShown();

            #region Add the used Textures to the texture ListBox

            for (int i = numSpritesBefore; i < GameData.Scene.Sprites.Count; i++)
            {
                GuiData.ListWindow.Add(GameData.Scene.Sprites[i].Texture);
                GuiData.ListWindow.Add(GameData.Scene.Sprites[i].AnimationChains);
            }

            for (int i = numOfSpriteFramesBefore; i < GameData.Scene.SpriteFrames.Count; i++)
            {
                GuiData.ListWindow.Add(GameData.Scene.SpriteFrames[i].Texture);
                GuiData.ListWindow.Add(GameData.Scene.SpriteFrames[i].AnimationChains);
            }

            for (int i = numOfSGsBefore; i < GameData.Scene.SpriteGrids.Count; i++)
            {
                SpriteGrid sg = GameData.Scene.SpriteGrids[i];
                sg.PopulateGrid(GameData.Camera.X, GameData.Camera.Y, 0f);

                sg.RefreshPaint();
                List<Texture2D> texturesToAdd = sg.GetUsedTextures();
                foreach (Texture2D tex in texturesToAdd)
                {
                    GuiData.ListWindow.Add(tex);
                }
            }
            #endregion

            CheckForExtraFiles(FileManager.RemoveExtension(fileName));
            GameData.Scene.Sprites.SortZInsertionDescending();


            if (tempSES.AssetsRelativeToSceneFile)
            {
                FileManager.ResetRelativeToCurrentDirectory();
            }
        }

        #endregion


        private static void AskToSearchForReplacements(string fileName)
        {
            //throw new NotImplementedException("Not implemented");
            /*
            if (SpriteManager.texturesNotFound.Count != 0)
            {
                MultiButtonMessageBox mbmb = GuiManager.AddMultiButtonMessageBox();
                mbmb.Text = SpriteManager.texturesNotFound.Count + " textures not found.";
                mbmb.AddButton("Search in folders relative to SpriteEditor.", new GuiMessage(this.SpriteEditorRelativeSearch));
                mbmb.AddButton("Search in folders relative to file.", new GuiMessage(this.FileRelativeSearch));
                mbmb.AddButton("Don't search for replacements.", null);
                this.mSpriteRigFileName = fileName;
            }
             */
        }

        private void ChangeNamesAndSave(Window callingWindow)
        {
            foreach (Sprite s in namesToChange)
            {
                s.Name = GameData.GetUniqueNameForObject<Sprite>(s.Name, s);
            }

            OpenFileWindowSaveScene(null);
        }

        private void CopyAssetsToFileFolder(Window callingWindow)
        {
            string directory = FileManager.GetDirectory(mLastFileName);

            #region Copy Texture2Ds and replace references

            foreach (Texture2D texture in mTexturesNotRelative)
            {
                if (!System.IO.File.Exists(directory + FileManager.RemovePath(texture.Name)))
                {
                    System.IO.File.Copy(texture.Name, directory + FileManager.RemovePath(texture.Name));
                }
                GameData.ReplaceTexture(texture,
                    FlatRedBallServices.Load<Texture2D>(directory + FileManager.RemovePath(texture.Name), GameData.SceneContentManager));
            }
            #endregion

            #region Copy AnimationChainLists and replace references

            foreach (AnimationChainList animationChainList in mAnimationChainListsNotRelative)
            {
                if (!System.IO.File.Exists(directory + FileManager.RemovePath(animationChainList.Name)))
                {
                    System.IO.File.Copy(animationChainList.Name, directory + FileManager.RemovePath(animationChainList.Name));
                }

                animationChainList.Name = directory + FileManager.RemovePath(animationChainList.Name);
            }

            #endregion

            #region Copy Fnt Files and replace references

            foreach (string oldFnt in mFntFilesNotRelative)
            {
                if (!System.IO.File.Exists(directory + FileManager.RemovePath(oldFnt)))
                {
                    System.IO.File.Copy(
                        oldFnt,
                        directory + FileManager.RemovePath(oldFnt));

                    foreach (Text text in GameData.Scene.Texts)
                    {
                        if (text.Font.FontFile == oldFnt)
                        {
                            // A little inefficient because it hits the file for the same information.
                            // Revisit this if it's a performance problem.
                            text.Font.SetFontPatternFromFile(directory + FileManager.RemovePath(oldFnt));
                        }
                    }
                }

                
            }


            #endregion

            SaveSceneFileWindowOk(callingWindow);
        }


        public static void OpenFileWindowLoadSpriteRig(Window callingWindow)
        {
            FileWindow tempFileWindow = GuiManager.AddFileWindow();
            tempFileWindow.SetFileType("srgx");
            tempFileWindow.OkClick += new GuiMessage(LoadSpriteRigOk);
        }

        public void OpenFileWindowSaveScene(Window callingWindow)
        {
            FileWindow tempFileWindow = GuiManager.AddFileWindow();
            //tempFileWindow.activityToExecute = "loadScene";
            tempFileWindow.SetToSave();
            tempFileWindow.Filter = "SpriteEditor XML Scene (*.scnx)|*.scnx";
            tempFileWindow.CurrentFileType = "scnx";

            tempFileWindow.saveNameText = FileManager.RemoveExtension(FileManager.RemovePath(GameData.FileName));
            tempFileWindow.OkClick += new GuiMessage(SaveSceneFileWindowOk);
        }

        public static bool AskQuestionsAndDelaySaveIfNecessary(GuiMessage guiMessage)
        {
            if (SESpriteGridManager.CurrentSpriteGrid != null && SESpriteGridManager.HasBlueprintChanged())
            {
                SESpriteGridManager.AskIfChangesShouldBeApplied(guiMessage);
                return false;
            }
            else
            {
                return true;
            }

        }

        public static void ShowWarningsAndMessagesBeforeSaving()
        {
            SESpriteGridManager.ShowMessageBoxIfBoundsAreInvalid();
        }



        #endregion

        #region Edit events

        private void ShiftSceneClick(Window callingWindow)
        {
            Vector3OkWindow v3ok = new Vector3OkWindow(GuiManager.Cursor);
            GuiManager.AddWindow(v3ok);

            v3ok.OkClick += ShiftSceneOk;

        }

        private void ShiftSceneOk(Window callingWindow)
        {
            Vector3OkWindow v3ok = 
                callingWindow as Vector3OkWindow;

            Vector3 amountToShiftBy = v3ok.Vector3Value;

            GameData.Scene.Shift(amountToShiftBy);

            GuiManager.RemoveWindow(v3ok);

        }

        private void DeleteSelected(Window callingWindow)
        {
            GameData.EditorLogic.DeleteSelectedObject();
        }

        void ScalePositionOnly(Window callingWindow)
        {
            Vector3OkWindow v3ok = new Vector3OkWindow(GuiManager.Cursor);
            GuiManager.AddWindow(v3ok);

            v3ok.Vector3Value = new Vector3(1, 1, 1);

            v3ok.OkClick += ScalePositionsOnlyOk;            
        }

        void ScalePositionAndScaleOnly(Window callingWindow)
        {
            TextInputWindow tiw = GuiManager.ShowTextInputWindow(
                "Enter scale amount.  Scale will be applied on X, Y, and Z", "Scale Scene");

            tiw.Text = "1.0";
            tiw.Format = TextBox.FormatTypes.Decimal;
            tiw.OkClick += ScalePositionsAndScaleOk;            
        }

        void ScalePositionsAndScaleOk(Window callingWindow)
        {
            TextInputWindow window =
                callingWindow as TextInputWindow;

            float value = float.Parse(window.Text);

            Vector3 amountToShiftBy = new Vector3(value, value, value);


            Scene scene = GameData.Scene;

            for (int i = 0; i < scene.PositionedModels.Count; i++)
            {
                scene.PositionedModels[i].X *= amountToShiftBy.X;
                scene.PositionedModels[i].Y *= amountToShiftBy.Y;
                scene.PositionedModels[i].Z *= amountToShiftBy.Z;

                scene.PositionedModels[i].ScaleX *= amountToShiftBy.X;
                scene.PositionedModels[i].ScaleY *= amountToShiftBy.Y;
                scene.PositionedModels[i].ScaleZ *= amountToShiftBy.Z;
            }

            for (int i = 0; i < scene.SpriteFrames.Count; i++)
            {
                scene.SpriteFrames[i].X *= amountToShiftBy.X;
                scene.SpriteFrames[i].Y *= amountToShiftBy.Y;
                scene.SpriteFrames[i].Z *= amountToShiftBy.Z;

                scene.SpriteFrames[i].ScaleX *= amountToShiftBy.X;
                scene.SpriteFrames[i].ScaleY *= amountToShiftBy.Y;
            }

            for (int i = 0; i < scene.Sprites.Count; i++)
            {
                scene.Sprites[i].X *= amountToShiftBy.X;
                scene.Sprites[i].Y *= amountToShiftBy.Y;
                scene.Sprites[i].Z *= amountToShiftBy.Z;

                scene.Sprites[i].ScaleX *= amountToShiftBy.X;
                scene.Sprites[i].ScaleY *= amountToShiftBy.Y;
            }

            for (int i = 0; i < scene.Texts.Count; i++)
            {
                scene.Texts[i].X *= amountToShiftBy.X;
                scene.Texts[i].Y *= amountToShiftBy.Y;
                scene.Texts[i].Z *= amountToShiftBy.Z;

                scene.Texts[i].Scale *= amountToShiftBy.X;
                scene.Texts[i].Spacing *= amountToShiftBy.X;
                scene.Texts[i].NewLineDistance *= amountToShiftBy.X;
            }

            GuiManager.RemoveWindow(window);
        }

        void ScalePositionsOnlyOk(Window callingWindow)
        {
            Vector3OkWindow v3ok =
                callingWindow as Vector3OkWindow;

            Vector3 amountToShiftBy = v3ok.Vector3Value;

            Scene scene = GameData.Scene;

            for (int i = 0; i < scene.PositionedModels.Count; i++)
            {
                scene.PositionedModels[i].X *= amountToShiftBy.X;
                scene.PositionedModels[i].Y *= amountToShiftBy.Y;
                scene.PositionedModels[i].Z *= amountToShiftBy.Z;
            }

            for (int i = 0; i < scene.SpriteFrames.Count; i++)
            {
                scene.SpriteFrames[i].X *= amountToShiftBy.X;
                scene.SpriteFrames[i].Y *= amountToShiftBy.Y;
                scene.SpriteFrames[i].Z *= amountToShiftBy.Z;
            }

            for (int i = 0; i < scene.Sprites.Count; i++)
            {
                scene.Sprites[i].X *= amountToShiftBy.X;
                scene.Sprites[i].Y *= amountToShiftBy.Y;
                scene.Sprites[i].Z *= amountToShiftBy.Z;
            }

            for (int i = 0; i < scene.Texts.Count; i++)
            {
                scene.Texts[i].X *= amountToShiftBy.X;
                scene.Texts[i].Y *= amountToShiftBy.Y;
                scene.Texts[i].Z *= amountToShiftBy.Z;
            }

            GuiManager.RemoveWindow(v3ok);

        }

        #endregion

        #region Add events


        //private void AddAnimationChainClick(Window callingWindow)
        //{
        //    FileWindow fileWindow = GuiManager.AddFileWindow();
        //    fileWindow.SetFileType("achx");
        //    fileWindow.SetToLoad();
        //    fileWindow.OkClick += AddAnimationChainOk;
        //}

        private void AddAnimationChainOk(Window callingWindow)
        {
            string fileName = ((FileWindow)callingWindow).Results[0];

            AnimationChainList acl = GameData.AddAnimationChainList(fileName);

            foreach (AnimationChain animationChain in acl)
            {
                foreach (AnimationFrame animationFrame in animationChain)
                {
                    WarnAboutNonPowerOfTwoTexture(animationFrame.Texture);
                }
            }
        }

        private void AddSpriteClick(Window callingWindow)
        {

            FileWindow tempFileWindow = GuiManager.AddFileWindow();
            tempFileWindow.SetFileType("graphic and animation");
            tempFileWindow.ShowLoadDirButton();
            tempFileWindow.betweenLoadFolder += new BetweenLoadFolder(MoveCameraToTheRight);
            tempFileWindow.OkClick += new GuiMessage(this.AddSpriteOk);
        }

        private void AddSpriteOk(Window callingWindow)
        {
            SpriteEditorSettings.EditingSprites = true;
            Sprite tempSprite = null;
            try
            {
                tempSprite = GameData.AddSprite(((FileWindow)callingWindow).Results[0], "");
                WarnAboutNonPowerOfTwoTexture(tempSprite.Texture);

            }
            catch (InvalidDataException)
            {
                GuiManager.ShowMessageBox("Could not read " + ((FileWindow)callingWindow).Results[0] + ".  Sprite not created.", "Error Creating Sprite");
                return;
            }

            GuiData.ToolsWindow.paintButton.Unpress();
            GameData.Cursor.ClickSprite(tempSprite);
        }

        public static void WarnAboutNonPowerOfTwoTexture(Texture2D texture)
        {
            if (texture != null && 
                (!MathFunctions.IsPowerOfTwo(texture.Width) ||
                !MathFunctions.IsPowerOfTwo(texture.Height)))
            {
                GuiManager.ShowMessageBox("The texture " + texture.Name + " has dimensions that are not a " +
                    "power of two.  They are " + texture.Width + " x " + texture.Height, "Texture not power of two");
            }
        }

        private void AddUntexturedSprite(Window callingWindow)
        {
            Sprite tempSprite = GameData.AddSprite(null, "UntexturedSprite");
            GuiData.ToolsWindow.paintButton.Unpress();
            GameData.Cursor.ClickSprite(tempSprite);
        }

        private void MoveCameraToTheRight(string fileNameOfSpriteLoaded)
        {
            GameData.Camera.X += 2f;
        }

        private void AddSpriteFrameClick(Window callingWindow)
        {
            FileWindow tempFileWindow = GuiManager.AddFileWindow();
            tempFileWindow.SetFileType("graphic and animation");
            tempFileWindow.ShowLoadDirButton();
            tempFileWindow.betweenLoadFolder += new BetweenLoadFolder(MoveCameraToTheRight);
            tempFileWindow.OkClick += new GuiMessage(this.AddSpriteFrameOk);
        }

        private void AddSpriteFrameOk(Window callingWindow)
        {
            string texture = ((FileWindow)callingWindow).Results[0];

            SpriteFrame spriteFrame = GameData.AddSpriteFrame(texture);

            WarnAboutNonPowerOfTwoTexture(spriteFrame.Texture);
      }

        private void AddSpriteGridClick(Window callingWindow)
        {
            FileWindow tempFileWindow = GuiManager.AddFileWindow();
            tempFileWindow.SetFileType("graphic");
            //tempFileWindow.ShowAddButton();
            //tempFileWindow.ShowLoadDirButton();
            //tempFileWindow.betweenLoadFolder += new BetweenLoadFolder(MoveCameraToTheRight);
            tempFileWindow.OkClick += new GuiMessage(this.AddSpriteGridOk);
        }

        private void AddSpriteGridOk(Window callingWindow)
        {
            string textureFileName = ((FileWindow)callingWindow).Results[0];

            GameData.AddSpriteGrid(textureFileName);
        }


        private void AddTextClick(Window callingWindow)
        {
            SpriteEditorSettings.EditingTexts = true;
            Text newText = GameData.CreateText();

        }

        #endregion

        #region Performance

        void MakeAllSpriteGridsCreateParticleSpriteClick(Window callingWindow)
        {
            int numberConverted = 0;

            for (int i = 0; i < GameData.Scene.SpriteGrids.Count; i++)
            {
                SpriteGrid spriteGrid = GameData.Scene.SpriteGrids[i];

                if (!spriteGrid.CreatesParticleSprites)
                {
                    spriteGrid.CreatesParticleSprites = true;
                    numberConverted++;
                }
            }

            GuiManager.ShowMessageBox("Converted " + numberConverted + " SpriteGrids", "Conversion Complete");
        }

        #endregion

        #region Window events

        private void ShowCameraBounds(Window callingWindow)
        {
            GuiData.CameraBoundsPropertyGrid.Visible = true;

			GuiManager.BringToFront(GuiData.CameraBoundsPropertyGrid);
            GuiData.CameraBoundsPropertyGrid.UpdateDisplayedProperties();

        }

        private void toggleCameraPropertiesVisibility(Window callingWindow)
        {
            GuiData.CameraPropertyGrid.Visible = true;
            GuiManager.BringToFront(GuiData.CameraPropertyGrid);
        }

        private void toggleEditorPropertiesVisibility(Window callingWindow)
        {
            GuiData.EditorPropertiesGrid.Visible = true;
            GuiManager.BringToFront(GuiData.EditorPropertiesGrid);
        }

        private void toggleSceneColorOperationVisibility(Window callingWindow)
        {
            /*
            GameData.guiData.sceneColorOperationWindow.Visible = GameData.guiData.sceneColorOperationWindowVisibility.IsPressed;
            GuiManager.BringToFront(GameData.guiData.sceneColorOperationWindow);
             */
        }

        private void toggleSpriteListVisibility(Window callingWindow)
        {
            GuiData.ListWindow.Visible = true;
            GuiManager.BringToFront(GuiData.ListWindow);
        }

        private void toggleToolsVisibility(Window callingWindow)
        {
            GuiData.ToolsWindow.Visible = true;
            GuiManager.BringToFront(GuiData.ToolsWindow);
        }

        //private void ToggleAttributesVisibility(Window callingWindow)
        //{
        //    GuiData.AttributesWindow.Visible = true;
        //}

        #endregion

        #endregion

        #region Methods

        #region Constructor

        public MenuStrip()
            : base(GuiManager.Cursor)
        {

            GuiManager.AddWindow(this);

            MenuItem menuItem = null;
            MenuItem subMenuItem = null;

            #region File

            menuItem = AddItem("File");
            menuItem.AddItem("New Scene").Click += NewSceneClick;
            menuItem.AddItem("---------------------");
            menuItem.AddItem("Load Scene").Click += LoadSceneClick;
            menuItem.AddItem("Load SpriteRig").Click += LoadSpriteRigClick;
            menuItem.AddItem("Load Shape Collection").Click += LoadShapeCollectionClick;
            menuItem.AddItem("---------------------");
            menuItem.AddItem("Save Scene        Ctrl+S").Click += SaveSceneClick;
            menuItem.AddItem("Save Scene As...").Click += SaveSceneAs_Click;
            menuItem.AddItem("Save SpriteRig").Click += FileButtonWindow.SaveSpriteRigClick;
            #endregion

            #region Edit

            menuItem = AddItem("Edit");

            menuItem.AddItem("Delete Selected").Click += DeleteSelected;

            menuItem.AddItem("---------------");
            
            menuItem.AddItem("Shift Scene").Click += ShiftSceneClick;
            MenuItem scaleScene = menuItem.AddItem("Scale Scene->");
            scaleScene.AddItem("Position Only").Click += new GuiMessage(ScalePositionOnly);
            scaleScene.AddItem("Position and Scale").Click += new GuiMessage(ScalePositionAndScaleOnly);

            #endregion

            #region Add

            menuItem = AddItem("Add");

            subMenuItem = menuItem.AddItem("Sprite ->");
            
            subMenuItem.AddItem("From File...").Click += AddSpriteClick;
            subMenuItem.AddItem("Untextured").Click += AddUntexturedSprite;




            menuItem.AddItem("SpriteGrid").Click += AddSpriteGridClick;
            menuItem.AddItem("SpriteFrame").Click += AddSpriteFrameClick;
            menuItem.AddItem("Text").Click += AddTextClick;

            menuItem.AddItem("Texture").Click += FileButtonWindow.openFileWindowLoadTexture;

            // We no longer support adding raw AnimationChains - this was confusing
            // to users and not really useful
            // menuItem.AddItem("Animation Chains").Click += AddAnimationChainClick;

            #endregion

            #region Performance

            menuItem = AddItem("Performance");

            menuItem.AddItem("Make all SpriteGrids CreateParticleSprite").Click += MakeAllSpriteGridsCreateParticleSpriteClick;

            #endregion

            #region Window

            menuItem = AddItem("Window");

            menuItem.AddItem("Tools").Click += toggleToolsVisibility;
            menuItem.AddItem("List Box").Click += toggleSpriteListVisibility;
            menuItem.AddItem("Camera Properties").Click += toggleCameraPropertiesVisibility;
            menuItem.AddItem("Camera Bounds").Click += ShowCameraBounds;
            menuItem.AddItem("Editor Properties").Click += toggleEditorPropertiesVisibility;
            menuItem.AddItem("Show Undos").Click += UndoManager.ShowListDisplayWindow;
            //menuItem.AddItem("Attributes").Click += ToggleAttributesVisibility;

            #endregion
        }

        



        #endregion

        #region Private Methods

        private void CheckForExtraFiles(string fileLoaded)
        {
            #region Try loading the .sep file

            try
            {
                if (System.IO.File.Exists(FileManager.RemoveExtension(fileLoaded) + ".sep"))
                {
                    GameData.SpriteEditorSceneProperties = SpriteEditorSceneProperties.Load(FileManager.RemoveExtension(fileLoaded) + ".sep");

                    SpriteEditorSceneProperties sesp = GameData.SpriteEditorSceneProperties;

                    if (sesp != null)
                    {
                        string directory = FileManager.GetDirectory(fileLoaded);

                        sesp.SetCameras(GameData.Camera, GameData.BoundsCamera);

                        GameData.EditorProperties.WorldAxesDisplayVisible = sesp.WorldAxesVisible;
                        GameData.EditorProperties.PixelSize = sesp.PixelSize;

                        if (sesp.BoundsVisible)
                        {
                            GuiData.CameraBoundsPropertyGrid.Visible = true;
                        }


                        // add the textures from the SpriteEditorSceneProperties
                        if (sesp.TextureDisplayRegionsList != null)
                        {
                            foreach (TextureDisplayRegionsSave textureReference in sesp.TextureDisplayRegionsList)
                            {
                                string fileName = directory + textureReference.TextureName;
                                if (System.IO.File.Exists(fileName))
                                {
                                    Texture2D texture = FlatRedBallServices.Load<Texture2D>(fileName, GameData.SceneContentManager);

                                    foreach (DisplayRegion displayRegion in textureReference.DisplayRegions)
                                    {
                                        GuiData.ListWindow.AddDisplayRegion(displayRegion, texture);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // do nothing, it's ok.
            }

            #endregion

        }

        #endregion

        #endregion
    }
}
