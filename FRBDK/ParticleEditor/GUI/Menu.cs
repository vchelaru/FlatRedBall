using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall.IO;
using FlatRedBall.Content.Particle;
using FlatRedBall;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Particle;
using EditorObjects.Gui;
using EditorObjects.Savers;

#if FRB_XNA
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using FlatRedBall.Content;
using FlatRedBall.Math;

#endif

namespace ParticleEditor.GUI
{
    public class Menu : MenuStrip
    {
        #region Fields
        
        private static List<Texture2D> mTexturesNotRelative = new List<Texture2D>();
        private static List<AnimationChainList> mAnimationChainListsNotRelative = new List<AnimationChainList>();

        private static string mLastFileName;

        #endregion

        #region Event Methods

        #region Save Methods

        private void CopyAssetsToFileFolder(Window callingWindow)
        {
            string directory = FileManager.GetDirectory(mLastFileName);

            List<string> filesToMakeDotDot = EditorData.EditorProperties.FilesToMakeDotDotRelative;

            foreach (Texture2D texture in mTexturesNotRelative)
            {
                if (!filesToMakeDotDot.Contains(texture.Name))
                {
                    if (!System.IO.File.Exists(directory + FileManager.RemovePath(texture.Name)))
                    {
                        System.IO.File.Copy(texture.Name, directory + FileManager.RemovePath(texture.Name));
                    }

                    FlatRedBallServices.ReplaceFromFileTexture2D(texture,
                        FlatRedBallServices.Load<Texture2D>(directory + FileManager.RemovePath(texture.Name)),
                        AppState.Self.PermanentContentManager);
                }

            }

            foreach (AnimationChainList animationChainList in mAnimationChainListsNotRelative)
            {
                if (!filesToMakeDotDot.Contains(animationChainList.Name))
                {
                    if (!System.IO.File.Exists(directory + FileManager.RemovePath(animationChainList.Name)))
                    {
                        System.IO.File.Copy(animationChainList.Name, directory + FileManager.RemovePath(animationChainList.Name));
                    }

                    animationChainList.Name = directory + FileManager.RemovePath(animationChainList.Name);
                }
            }

            PerformSave(callingWindow);
        }

        private void SaveEmitterListZipClick(Window callingWindow)
        {
            FileWindow tempWindow = GuiManager.AddFileWindow();
            tempWindow.Filter = "Emitter List Zip (*.emiz)|*.emiz";
            tempWindow.CurrentFileType = "emiz";
            tempWindow.saveNameText = FileManager.RemovePath(EditorData.CurrentEmixFileName);

			if (!string.IsNullOrEmpty(EditorData.CurrentEmixFileName))
			{
				tempWindow.SetDirectory(FileManager.GetDirectory(EditorData.CurrentEmixFileName));
			}

            tempWindow.SetToSave();
            tempWindow.OkClick += SaveEmitterZipOk;

        }

        private void SaveEmitterZipOk(Window callingWindow)
        {
            EmitterSaveList esl = EmitterSaveList.FromEmitterList(
                EditorData.Emitters);

            string fileName = ((FileWindow)callingWindow).Results[0];
            esl.Name = fileName;

            if (((ISaveableContent)esl).AreAllFilesRelativeTo(fileName) == false)
            {
                GuiManager.ShowMessageBox("Could not save the Emitter Save Zip file " +
                    "because not all files are relative to the desired location.", "Error saving");
            }
            else
            {
                esl.MakeAssetsRelative(fileName);

                esl.SaveZipFile(fileName);
            }
        }

        private void SaveEmitterArraysClick(FlatRedBall.Gui.Window callingWindow)
        {
            FileWindow tempWindow = GuiManager.AddFileWindow();
            tempWindow.Filter = "XML Emitter (*.emix)|*.emix";
            tempWindow.CurrentFileType = "emix";
            tempWindow.saveNameText = FileManager.RemovePath(EditorData.CurrentEmixFileName);

			if (!string.IsNullOrEmpty(EditorData.CurrentEmixFileName))
			{
				tempWindow.SetDirectory(FileManager.GetDirectory(EditorData.CurrentEmixFileName));
			}

            tempWindow.SetToSave();
            tempWindow.OkClick += SaveEmitterOk;
        }

        private void SaveEmitterOk(FlatRedBall.Gui.Window callingWindow)
        {
            string fileName = "";

            if (callingWindow is FileWindow)
            {
                fileName = ((FileWindow)callingWindow).Results[0];
                mLastFileName = fileName;
            }
            else
            {
                fileName = mLastFileName;
            }

            #region Get all of the not-relative textures

            mTexturesNotRelative.Clear();
            mAnimationChainListsNotRelative.Clear();

            string pathOfFile = FileManager.GetDirectory(fileName);

            foreach (Emitter emitter in EditorData.Emitters)
            {
                if (emitter.Texture != null &&
                    !FileManager.IsRelativeTo(emitter.Texture.Name, pathOfFile) && 
                    !mTexturesNotRelative.Contains(emitter.Texture))
                {
                    mTexturesNotRelative.Add(emitter.Texture);
                }

                if (emitter.ParticleBlueprint.AnimationChains != null &&
                    string.IsNullOrEmpty(emitter.ParticleBlueprint.AnimationChains.Name) == false &&
                    !FileManager.IsRelativeTo(emitter.ParticleBlueprint.AnimationChains.Name, pathOfFile) &&
                    !mAnimationChainListsNotRelative.Contains(emitter.ParticleBlueprint.AnimationChains))
                {
                    mAnimationChainListsNotRelative.Add(emitter.ParticleBlueprint.AnimationChains);
                }
            }

            #endregion

            mLastFileName = fileName;

            string message = "The following file has the following non-relative references\n" + fileName;


            CopyTexturesMultiButtonMessageBox mbmb = new CopyTexturesMultiButtonMessageBox();
            mbmb.Text = message;


            mbmb.SaveClick += CopyAssetsToFileFolder;
            //mbmb.AddButton("Cancel Save", null);
            //mbmb.AddButton("Copy textures to relative and reference copies.", new GuiMessage(CopyAssetsToFileFolder));

            foreach (Texture2D texture in mTexturesNotRelative)
            {
                mbmb.AddItem(texture.Name);
            }

            foreach (AnimationChainList animationChainList in mAnimationChainListsNotRelative)
            {
                mbmb.AddItem(animationChainList.Name);
            }
            if (EditorData.EditorProperties.FilesToMakeDotDotRelative.Count != mbmb.ItemsCount)
            {
                mbmb.FilesMarkedDotDotRelative = EditorData.EditorProperties.FilesToMakeDotDotRelative;
                GuiManager.AddDominantWindow(mbmb);
            }
            else
            {
                PerformSave(null);
            }
        }

        private static void PerformSave(Window callingWindow)
        {
            string fileName = mLastFileName;
            PositionedObjectList<Emitter> emitters = EditorData.Emitters;

            AppCommands.Self.File.SaveEmitters(emitters, fileName);
        }
        #endregion



        private void ShowActivityWindow(Window callingWindow)
        {
            GuiData.ActivityWindow.Visible = true;
        }

        private void ShowCameraBounds(Window callingWindow)
        {
            GuiData.ShowCameraBounds();
        }

        private void ShowEditorCameraProperties(Window callingWindow)
        {
            GuiData.EditorCameraPropertyGrid.Visible = true;
            GuiManager.BringToFront(GuiData.EditorCameraPropertyGrid);
        }

        private void ShowEditorPropertiesWindow(Window callingWindow)
        {
            GuiData.EditorPropertiesGrid.Visible = true;
            GuiManager.BringToFront(GuiData.EditorPropertiesGrid);
        }

        private void ShowListWindow(Window callingWindow)
        {
            GuiData.EmitterListBoxWindow.Visible = true;
        }

        private void ShowSceneProperties(Window callingWindow)
        {
            GuiData.ScenePropertyGrid.Visible = true;
            GuiManager.BringToFront(GuiData.ScenePropertyGrid);
        }

        private void ShowToolsWindow(Window callingWindow)
        {
            GuiData.ToolsWindow.Visible = true;
        }

        private void ViewDefault2D(Window callingWindow)
        {
            Camera camera = SpriteManager.Camera;

            camera.UsePixelCoordinates(false);

            Camera boundsCamera = GuiData.CameraBoundsPropertyGrid.ObjectDisplaying;

            boundsCamera.OrthogonalWidth = 800;
            boundsCamera.OrthogonalHeight = 600;

            boundsCamera.Orthogonal = true;

        }




        private void ViewDefault3D(Window callingWindow)
        {
            SpriteManager.Camera.Orthogonal = false;
            Camera boundsCamera = GuiData.CameraBoundsPropertyGrid.ObjectDisplaying;


            boundsCamera.Orthogonal = false;
            boundsCamera.Z = -40;
            boundsCamera.FieldOfView = (float)Math.PI / 4.0f;

        }

        #endregion

        #region Methods

        public Menu()
            : base(GuiManager.Cursor)
        {
            GuiManager.AddWindow(this);

            #region File
            MenuItem menuItem = AddItem("File");
            menuItem.AddItem("New").Click += FileMenuWindow.NewWorkspace;
            menuItem.AddItem("-----------------");
            menuItem.AddItem("Load .emix").Click += FileMenuWindow.LoadEmittersClick;
            menuItem.AddItem("Load .scnx").Click += FileMenuWindow.LoadScnxButtonClick;
            menuItem.AddItem("-----------------");
            menuItem.AddItem("Save .emix (Emitter List XML)").Click += SaveEmitterArraysClick;
            menuItem.AddItem("Save .emiz (Emitter List ZIP)").Click += SaveEmitterListZipClick;
            #endregion

            #region View

            menuItem = AddItem("View");
            menuItem.AddItem("Default 3D").Click += ViewDefault3D;
            menuItem.AddItem("Default 2D").Click += ViewDefault2D;

            #endregion

            #region Window

            menuItem = AddItem("Window");
            menuItem.AddItem("List Box").Click += ShowListWindow;
            menuItem.AddItem("Tools").Click += ShowToolsWindow;
            menuItem.AddItem("Emitter Actions").Click += ShowActivityWindow;
            menuItem.AddItem("Editor Properties").Click += ShowEditorPropertiesWindow;
            menuItem.AddItem("Scene Properties").Click += ShowSceneProperties;
            menuItem.AddItem("----------------------");

            menuItem.AddItem("Camera Properties").Click += ShowEditorCameraProperties;
            menuItem.AddItem("Camera Bounds").Click += ShowCameraBounds;
            #endregion
        }
        #endregion


    }
}
