using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;

using FlatRedBall.Content;
using FlatRedBall.Content.Scene;

using FlatRedBall.Graphics;
using FlatRedBall.Gui;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Graphics.Model;
using FlatRedBall.Collections;
using FlatRedBall.IO;

using SpriteEditor.SEPositionedObjects;
using FlatRedBall.Math;

using Microsoft.DirectX;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Content.SpriteRig;
using EditorObjects.Gui;

namespace SpriteEditor.Gui
{
    public class FileButtonWindow
    {
        #region Fields

        private static Camera camera;

        private static bool mAssetsRelativeToSpriteRig;
        private GuiMessages messages;
        private static bool mSaveStoredSpriteRig;
        private string mSpriteRigFileName;
        
        private Random random;

        public Button saveSpriteRigButton;

        private static SESpriteGridManager sesgMan;

        #endregion

        #region Methods

        #region Constructor

        public FileButtonWindow(Cursor cursor)
        {
            camera = SpriteManager.Cameras[0];
            this.messages = GuiData.messages;
            sesgMan = GameData.sesgMan;

            mAssetsRelativeToSpriteRig = true;
        }

        #endregion

        #region Saving-related Methods



        public static void saveSpriteRig(Window callingWindow)
        {
            mAssetsRelativeToSpriteRig = (callingWindow.FindByName("scnRelativeAssets") as ToggleButton).IsPressed;
            string fileName = ((FileWindow)callingWindow).Results[0];
            if (mAssetsRelativeToSpriteRig)
            {
                MessageBox mb;
                string pathOfSrg = FileManager.GetDirectory(fileName);
                foreach (Sprite s in GuiData.srSaveOptions.joints)
                {
                    if (!((s.Texture.Name == null) || FileManager.IsRelativeTo(s.Texture.Name, pathOfSrg)))
                    {
                        mb = GuiManager.ShowMessageBox(string.Concat(new object[] { "Cannot save ", fileName, " because the texture ", s.Texture, " is not relative to the .srgx file." }), "Texture not relative.");
                        return;
                    }
                }
                foreach (Sprite s in GuiData.srSaveOptions.bodySprites)
                {
                    if (!FileManager.IsRelativeTo(s.Texture.Name, pathOfSrg))
                    {
                        mb = GuiManager.ShowMessageBox(string.Concat(new object[] { "Cannot save ", fileName, " because the texture ", s.Texture, " is not relative to the .srgx file." }), "Texture not relative.");
                        return;
                    }
                }
                if ((GuiData.srSaveOptions.root != null) && !FileManager.IsRelativeTo(GuiData.srSaveOptions.root.Texture.Name, pathOfSrg))
                {
                    mb = GuiManager.ShowMessageBox("Cannot save " + fileName + " because the texture " + GuiData.srSaveOptions.root.Texture.Name + " is not relative to the .srgx file.", "Texture not relative.");
                    return;
                }
            }

            SpriteRigSave srs = new SpriteRigSave(GuiData.srSaveOptions.joints, GuiData.srSaveOptions.bodySprites, GuiData.srSaveOptions.poseChains, GuiData.srSaveOptions.root, mAssetsRelativeToSpriteRig);
            srs.JointsVisible = GuiData.srSaveOptions.jointsVisible.IsPressed;
            srs.RootVisible = GuiData.srSaveOptions.rootVisible.IsPressed;
            GuiData.srSaveOptions.joints.Visible = true;

            if (GuiData.srSaveOptions.root != null)
            {
                GuiData.srSaveOptions.root.Visible = true;
            }

            srs.Save(fileName);

        }


        private static void SaveStoredSpriteRig(Window callingWindow)
        {
            mSaveStoredSpriteRig = true;
            spriteRigOptionsOK(callingWindow);
        }


        private static void SaveStoredSpriteRigCancel(Window callingWindow)
        {
            GuiData.srSaveOptions.Visible = true;
            GuiData.srSaveOptions.FillComboBoxes();
        }
        #endregion

        #region Loading-related Methods


        public static void loadTextureOk(Window callingWindow)
        {
            string file = ((FileWindow)callingWindow).Results[0];
            GameData.AddTexture(file);
        }


        public static void openFileWindowLoadTexture(Window callingWindow)
        {
            FileWindow tempFileWindow = GuiManager.AddFileWindow();
            tempFileWindow.SetFileType("graphic");
            tempFileWindow.OkClick += new GuiMessage(loadTextureOk);
        }


        #endregion


        public static void CreateTextureNotFoundOkCancel()
        {
            throw new NotImplementedException("Not implemented");
            /*
            while ((GameData.textureReplacements.Count > 0) && (GameData.textureReplacements[0] == ""))
            {
                GameData.textureReplacements.RemoveAt(0);
                SpriteManager.texturesNotFound.RemoveAt(0);
            }
            if (GameData.textureReplacements.Count > 0)
            {
                OkCancelWindow tempOkCancel = GuiManager.AddOkCancelWindow();
                GuiManager.SetDominantWindow(tempOkCancel);
                tempOkCancel.ScaleX = 23f;
                tempOkCancel.ScaleY = 14f;
                tempOkCancel.Name = SpriteManager.texturesNotFound[0];
                tempOkCancel.message = "Could not find\n\n " + SpriteManager.texturesNotFound[0] + ". \n\n" + GameData.textureReplacements[0] + "\n\nfound.  Replace?";
                tempOkCancel.OkClick += new GuiMessage(this.ReplaceTexture);
                tempOkCancel.CancelClick += new GuiMessage(this.DoNotReplaceTexture);
            }
             */
        }

        public static void DoNotReplaceTexture(Window callingWindow)
        {
            throw new NotImplementedException("Not Implemented");
            /*
            GameData.textureReplacements.RemoveAt(0);
            SpriteManager.texturesNotFound.RemoveAt(0);
            if (GameData.textureReplacements.Count > 0)
            {
                this.CreateTextureNotFoundOkCancel();
            }
             */
        }

        private static void FileRelativeSearch(Window callingWindow)
        {
                        throw new NotImplementedException("Not Implemented");
            /*

            for (int texNum = 0; texNum < SpriteManager.texturesNotFound.Count; texNum++)
            {
                GameData.textureReplacements.Add(FileManager.FindFileInDirectory(FileManager.RemovePath(SpriteManager.texturesNotFound[texNum]), FileManager.GetDirectory(this.mSpriteRigFileName)));
            }
            if (GameData.textureReplacements.Count > 0)
            {
                this.CreateTextureNotFoundOkCancel();
            }
             */
        }

        public static void FindTextureNotFoundReplacements()
        {
                        throw new NotImplementedException("Not Implemented");
            /*

            for (int texNum = 0; texNum < SpriteManager.texturesNotFound.Count; texNum++)
            {
                GameData.textureReplacements.Add(FileManager.FindFileInDirectory(FileManager.RemovePath(SpriteManager.texturesNotFound[texNum]), Directory.GetCurrentDirectory()));
            }
            if (GameData.textureReplacements.Count > 0)
            {
                this.CreateTextureNotFoundOkCancel();
            }
             */
        }

        public static void SaveSpriteRigClick(Window callingWindow)
        {
            if (GuiData.srSaveOptions.poseChains != null)
            {
                OkCancelWindow okCancel = GuiManager.ShowOkCancelWindow("The SpriteEditor has a SpriteRig in memory.  Save this SpriteRig?", "SpriteRig in memory");
                okCancel.OkClick += new GuiMessage(SaveStoredSpriteRig);
                okCancel.CancelClick += new GuiMessage(SaveStoredSpriteRigCancel);
            }
            else
            {
                GuiData.srSaveOptions.Visible = true;
                GuiData.srSaveOptions.FillComboBoxes();
            }
        }

        public static void ReplaceTexture(Window callingWindow)
        {
                        throw new NotImplementedException("Not Implemented");
            /*

            Texture2D textureToReplace = 
                FlatRedBallServices.Load<Texture2D>(SpriteManager.texturesNotFound[0], GameData.SceneContentManager);
            Texture2D replacementTexture = 
                FlatRedBallServices.Load<Texture2D>(GameData.textureReplacements[0], GameData.SceneContentManager);

            SpriteManager.ReplaceTexture(
                    textureToReplace, replacementTexture);

            foreach (SpriteGrid sg in GameData.Scene.SpriteGrids)
            {
                sg.ReplaceTexture(textureToReplace,
                    FlatRedBallServices.Load<Texture2D>(GameData.textureReplacements[0], GameData.SceneContentManager));
            }
            GuiData.ListWindow.Add(replacementTexture);
            GameData.textureReplacements.RemoveAt(0);
            SpriteManager.texturesNotFound.RemoveAt(0);
            if (GameData.textureReplacements.Count > 0)
            {
                this.CreateTextureNotFoundOkCancel();
            }
             * 
             */
        }

        private static void ShowSpriteRigOptionsFileWindow(Window callingWindow)
        {
            FileWindow tempFileWindow = GuiManager.AddFileWindow();
            tempFileWindow.Filter = "XML SpriteRig (*.srgx)|*.srgx";
            tempFileWindow.CurrentFileType = "srgx";
            tempFileWindow.SetToSave();
            tempFileWindow.OkClick += new GuiMessage(saveSpriteRig);
            ToggleButton relativeToggle = new ToggleButton(GuiManager.Cursor);
            tempFileWindow.AddWindow(relativeToggle);
            relativeToggle.Name = "scnRelativeAssets";
            relativeToggle.SetText("Not .scn-relative assets", ".scn-relative assets");
            relativeToggle.ScaleX = 9f;
            relativeToggle.SetPositionTL(22.5f, 6.5f);
            if (mAssetsRelativeToSpriteRig)
            {
                relativeToggle.Press();
            }
            else
            {
                relativeToggle.Unpress();
            }
        }

        private static void SpriteEditorRelativeSearch(Window callingWindow)
        {
                        throw new NotImplementedException("Not Implemented");
            /*

            for (int texNum = 0; texNum < SpriteManager.texturesNotFound.Count; texNum++)
            {
                GameData.textureReplacements.Add(FileManager.FindFileInDirectory(FileManager.RemovePath(SpriteManager.texturesNotFound[texNum]), Directory.GetCurrentDirectory()));
            }
            if (GameData.textureReplacements.Count > 0)
            {
                this.CreateTextureNotFoundOkCancel();
            }
             */
        }

        public static void spriteRigOptionsOK(Window callingWindow)
        {
            ShowSpriteRigOptionsFileWindow(callingWindow);
        }


        #endregion
    }
}
