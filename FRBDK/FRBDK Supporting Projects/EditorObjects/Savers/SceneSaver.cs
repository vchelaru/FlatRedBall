using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.IO;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Graphics;
using FlatRedBall.Gui;
using FlatRedBall.Content;
using FlatRedBall.Graphics.Model;

using FlatRedBall.Graphics.Texture;
using EditorObjects.Gui;

#if FRB_XNA
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using FlatRedBall.Content.Model.Helpers;
using FlatRedBall.Graphics.Animation3D;
#endif

namespace EditorObjects.Savers
{
    public delegate void ReplaceTextureDelegate(Texture2D oldTexture, Texture2D newTexture);
	//public delegate void ReplaceTextureDeletage(
    public delegate bool SaveSceneDelegate(string fileName, bool areAssetsRelativeToScene);

    public static class SceneSaver
    {
        #region Fields

        private static List<Texture2D> mTexturesNotRelative = new List<Texture2D>();
        private static List<AnimationChainList> mAnimationChainListsNotRelative =
            new List<AnimationChainList>();
        private static List<string> mFntFilesNotRelative = new List<string>();
		private static List<string> mModelsNotRelative = new List<string>();

        private static List<string> mExtraFilesToCopy = new List<string>();
		

        private static Scene SceneSaving;
        private static string mLastFileName;
        private static ReplaceTextureDelegate mReplaceTextureDelegate;

        private static string mContentManager;
        private static SaveSceneDelegate mSaveDelegate;

        #endregion

        #region Event and Delegate Methods

        static bool SaveScene(string name, bool assetsRelativeToScene)
        {
            string oldRelativeDirectory = FileManager.RelativeDirectory;

            assetsRelativeToScene = true;

            if (assetsRelativeToScene)
            {
                FileManager.RelativeDirectory = FileManager.GetDirectory(name);
            }

            SpriteEditorScene ses = SpriteEditorScene.FromScene(SceneSaving);

            ses.AssetsRelativeToSceneFile = assetsRelativeToScene;

            ses.Save(name);

            FileManager.RelativeDirectory = oldRelativeDirectory;
            return true;
        }

        static void MakeSceneRelativeWithDotDots(Window callingWindow)
        {
            mSaveDelegate(mLastFileName, true);
        }

        static void DefaultReplaceTexture(Texture2D oldTexture, Texture2D newTexture)
        {
            foreach (SpriteGrid sg in SceneSaving.SpriteGrids)
                sg.ReplaceTexture(oldTexture, newTexture);

            // If necessary replace textures on the UI
            //if (GuiData.ToolsWindow.currentTextureDisplay.UpOverlayTexture == oldTexture)
            //{
            //    GuiData.ToolsWindow.currentTextureDisplay.SetOverlayTextures(newTexture, null);
            //}

            FlatRedBallServices.ReplaceFromFileTexture2D(oldTexture, newTexture, mContentManager);

            //GuiData.ListWindow.Add(newTexture);
        }

        static void SaveClick(IWindow window)
        {

        }

        #endregion

        #region Methods

        public static void MakeSceneRelativeAndSave(Scene scene, string fileName, string contentManagerName, List<string> filesToMakeDotDotRelative)
        {
            MakeSceneRelativeAndSave(scene, fileName, contentManagerName, filesToMakeDotDotRelative, DefaultReplaceTexture, SaveScene);
        }

        public static void MakeSceneRelativeAndSave(Scene scene, string fileName, string contentManager, List<string> filesToMakeDotDotRelative,
            ReplaceTextureDelegate replaceTextureDelegate, SaveSceneDelegate saveDelegate)
        {
            if(replaceTextureDelegate == null)
            {
                replaceTextureDelegate = DefaultReplaceTexture;
            }

            mTexturesNotRelative.Clear();
            mAnimationChainListsNotRelative.Clear();
            mFntFilesNotRelative.Clear();
			mModelsNotRelative.Clear();
            mExtraFilesToCopy.Clear();

            mSaveDelegate = saveDelegate;
            mLastFileName = fileName;
            mContentManager = contentManager;

            string pathOfFile = FileManager.GetDirectory(fileName);

            #region Find any not-relative Sprites

            foreach (Sprite s in scene.Sprites)
            {
                if (s.Texture == null)
                    continue;

                string textureFileName = s.Texture.SourceFile();

                if (!FileManager.IsRelativeTo(textureFileName, pathOfFile) &&
                    !mTexturesNotRelative.Contains(s.Texture))
                {
                    mTexturesNotRelative.Add(s.Texture);
                }

                if (s.AnimationChains != null && string.IsNullOrEmpty(s.AnimationChains.Name) == false &&
                    !FileManager.IsRelativeTo(s.AnimationChains.Name, pathOfFile) &&
                    !mAnimationChainListsNotRelative.Contains(s.AnimationChains))
                {
                    mAnimationChainListsNotRelative.Add(s.AnimationChains);
                }
            }

            #endregion

            #region Find any not-relative SpriteGrids

            foreach (SpriteGrid sg in scene.SpriteGrids)
            {
                List<Texture2D> usedTextures = sg.GetUsedTextures();
                foreach (Texture2D texture in usedTextures)
                {
                    if (!FileManager.IsRelativeTo(texture.SourceFile(), pathOfFile) && !mTexturesNotRelative.Contains(texture))
                    {
                        mTexturesNotRelative.Add(texture);
                    }
                }
            }

            #endregion

            #region Find any not-relative SpriteFrames

            foreach (SpriteFrame sf in scene.SpriteFrames)
            {
                if (!FileManager.IsRelativeTo(sf.Texture.SourceFile(), pathOfFile) && !mTexturesNotRelative.Contains(sf.Texture))
                {
                    mTexturesNotRelative.Add(sf.Texture);
                }
            }

            #endregion

            #region Find any not-relative Fonts (Text objects)

            foreach (Text t in scene.Texts)
            {
                BitmapFont bitmapFont = t.Font;

                if (bitmapFont == TextManager.DefaultFont)
                    continue;

                foreach (Texture2D texture in bitmapFont.Textures)
                {
                    if (!FileManager.IsRelativeTo(texture.SourceFile(), pathOfFile) &&
                        !mTexturesNotRelative.Contains(texture))
                    {
                        mTexturesNotRelative.Add(texture);
                    }
                }
                if (!FileManager.IsRelativeTo(bitmapFont.FontFile, pathOfFile) &&
                    !mFntFilesNotRelative.Contains(bitmapFont.FontFile))
                {
                    mFntFilesNotRelative.Add(bitmapFont.FontFile);
                }

            }

            #endregion

			#region Find any non-relative PositionedModels

			foreach (PositionedModel positionedModel in scene.PositionedModels)
			{
				if (!FileManager.IsRelativeTo(positionedModel.ModelFileName, pathOfFile) &&
					!mModelsNotRelative.Contains(positionedModel.ModelFileName))
				{
					mModelsNotRelative.Add(positionedModel.ModelFileName);
				}

#if !FRB_MDX
                // Extra files which won't be re-referenced
                foreach (SourceReferencingFile extraFile in positionedModel.ExtraFiles)
                {
                    if (!FileManager.IsRelativeTo(extraFile.DestinationFile, pathOfFile) &&
                        !mExtraFilesToCopy.Contains(extraFile.DestinationFile))
                    {
                        mExtraFilesToCopy.Add(extraFile.DestinationFile);

                    }
                }
#endif

			}

			#endregion

            #region Loop through all AnimationChains and add any missing files

            foreach (AnimationChainList acl in mAnimationChainListsNotRelative)
            {
                foreach (AnimationChain chain in acl)
                {
                    foreach (AnimationFrame frame in chain)
                    {
                        Texture2D texture = frame.Texture;

                        if (!FileManager.IsRelativeTo(texture.SourceFile(), pathOfFile) &&
                            !mTexturesNotRelative.Contains(texture))
                        {
                            mTexturesNotRelative.Add(texture);
                        }
                    }
                }
            }

            #endregion

            mLastFileName = fileName;
            mContentManager = contentManager;
            mSaveDelegate = saveDelegate;


            #region If there are files which are not relative, show the multi button message box

            SceneSaving = scene;
            mReplaceTextureDelegate = replaceTextureDelegate;

            string message = "The file\n\n" + fileName +
                "\n\nreferences the following which are not relative:";


            CopyTexturesMultiButtonMessageBox mbmb = new CopyTexturesMultiButtonMessageBox();
            mbmb.Text = message;
            mbmb.SaveClick += CopyAssetsToFileFolder;

            //mbmb.AddButton("Make all references relative using \"..\\\"", MakeSceneRelativeWithDotDots);
            //mbmb.AddButton("Copy textures to relative and reference copies.", new GuiMessage(CopyAssetsToFileFolder));

            foreach (Texture2D texture in mTexturesNotRelative)
            {
                mbmb.AddItem(texture.SourceFile());
            }

            foreach (AnimationChainList animationChainList in mAnimationChainListsNotRelative)
            {
                mbmb.AddItem(animationChainList.Name);
            }
            foreach (string s in mFntFilesNotRelative)
            {
                mbmb.AddItem(s);
            }
			foreach (string s in mModelsNotRelative)
			{
				mbmb.AddItem(s);
			}

            foreach (string s in mExtraFilesToCopy)
            {
                mbmb.AddItem(s);
            }


            //mbmb.FilesToMakeDotDotRelative = filesToMakeDotDotRelative;
            if (filesToMakeDotDotRelative.Count != mbmb.ItemsCount) 
            {
                mbmb.FilesMarkedDotDotRelative = filesToMakeDotDotRelative;
                GuiManager.AddDominantWindow(mbmb);
            }
            else
            {
                saveDelegate(fileName, true);
            }

            #endregion

        }

        private static void CopyAssetsToFileFolder(Window callingWindow)
        {
            string targetDirectory = FileManager.GetDirectory(mLastFileName);

            CopyTexturesMultiButtonMessageBox mbmb = ((CopyTexturesMultiButtonMessageBox)callingWindow);
            List<string> filesToMakeDotDot = mbmb.FilesMarkedDotDotRelative;

            #region Copy Texture2Ds and replace references

            foreach (Texture2D texture in mTexturesNotRelative)
            {
                if (filesToMakeDotDot == null || !filesToMakeDotDot.Contains(texture.Name))
                {
                    if (!System.IO.File.Exists(targetDirectory + FileManager.RemovePath(texture.SourceFile())))
                    {
                        System.IO.File.Copy(texture.SourceFile(), targetDirectory + FileManager.RemovePath(texture.SourceFile()));
                    }

                    mReplaceTextureDelegate(texture,
                        FlatRedBallServices.Load<Texture2D>(targetDirectory + FileManager.RemovePath(texture.SourceFile()),
                        mContentManager));
                }
            }
            #endregion

            #region Copy AnimationChainLists and replace references

            foreach (AnimationChainList animationChainList in mAnimationChainListsNotRelative)
            {
                if (filesToMakeDotDot == null || !filesToMakeDotDot.Contains(animationChainList.Name))
                {
                    if (!System.IO.File.Exists(targetDirectory + FileManager.RemovePath(animationChainList.Name)))
                    {
                        System.IO.File.Copy(animationChainList.Name, targetDirectory + FileManager.RemovePath(animationChainList.Name));
                    }

                    animationChainList.Name = targetDirectory + FileManager.RemovePath(animationChainList.Name);
                }
            }

            #endregion

            #region Copy Fnt Files and replace references

            foreach (string oldFnt in mFntFilesNotRelative)
            {
                if (filesToMakeDotDot == null || !filesToMakeDotDot.Contains(oldFnt))
                {
                    if (!System.IO.File.Exists(targetDirectory + FileManager.RemovePath(oldFnt)))
                    {
                        System.IO.File.Copy(
                            oldFnt,
                            targetDirectory + FileManager.RemovePath(oldFnt));

                        foreach (Text text in SceneSaving.Texts)
                        {
                            if (text.Font.FontFile == oldFnt)
                            {
                                // A little inefficient because it hits the file for the same information.
                                // Revisit this if it's a performance problem.
                                text.Font.SetFontPatternFromFile(targetDirectory + FileManager.RemovePath(oldFnt));
                            }
                        }
                    }
                }

            }


            #endregion

			#region Copy model files and replace references

			foreach (string modelFile in mModelsNotRelative)
			{
                if (filesToMakeDotDot == null || !filesToMakeDotDot.Contains(modelFile))
                {
                    if (!System.IO.File.Exists(targetDirectory + FileManager.RemovePath(modelFile)))
                    {
                        System.IO.File.Copy(modelFile, targetDirectory + FileManager.RemovePath(modelFile));

#if FRB_XNA
                        if(FileManager.GetExtension(modelFile) == "wme")
                        {
                            string modelDirectory;

                            if (FileManager.IsRelative(modelFile))
                            {
                                modelDirectory = FileManager.GetDirectory(FileManager.MakeAbsolute(modelFile));
                            }
                            else
                            {
                                modelDirectory = FileManager.GetDirectory(modelFile);
                            }

                            CustomModel customModel = FlatRedBallServices.Load<CustomModel>(modelFile, mContentManager);

                            // copy files that this model references
                            foreach (string referencedFile in customModel.ReferencedFiles)
                            {
                                string fullFile = modelDirectory + referencedFile;

                                if (!System.IO.File.Exists(targetDirectory + referencedFile))
                                {
                                    System.IO.File.Copy(fullFile, targetDirectory + referencedFile);
                                }
                            }
                        }
#endif

                    }

                    // Vic says on 10/7/2010
                    // Right now we do a full file replacement.  We *could* just change the file names instead of loading from file,
                    // but I'm not sure if we'll need to do an actual replacement at some point in the future...this seems more bugproof
                    ModelManager.ReplaceSourceModel(modelFile, targetDirectory + FileManager.RemovePath(modelFile), mContentManager);
                }
			}

			#endregion

            #region Copy Extra Files

            for (int i = 0; i < mExtraFilesToCopy.Count; i++)
            {
                try
                {
#if !FRB_MDX
                    string oldFileName = mExtraFilesToCopy[i];
                    string newFileName = targetDirectory + FileManager.RemovePath(mExtraFilesToCopy[i]);

                    System.IO.File.Copy(
                        oldFileName,
                        newFileName, true);

                    ModelManager.ReplaceFileReference(oldFileName, newFileName);
#endif
                }
                catch(System.IO.IOException e)
                {
                    System.Windows.Forms.MessageBox.Show("Could not copy to " + targetDirectory + FileManager.RemovePath(mExtraFilesToCopy[i]));
                }
                // Don't do anything else with these files
            }


            #endregion

            mSaveDelegate(mLastFileName, mbmb.AreAssetsRelative);
        }

        #endregion


    }
}
