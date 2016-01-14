using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.Collections;
using FlatRedBall.IO;

using Microsoft.DirectX;


namespace SpriteEditor.Gui
{
    public class SpriteGridGuiMessages
    {
        // Fields
        public static Camera camera;

        public static Random random;
        public static SESpriteGridManager sesgMan;

        // Methods
        public static void blueprintEmbedClick(Window callingWindow)
        {
            throw new NotImplementedException("SetAnimationChainFromFile not implemented");
            //SESpriteGridManager.CurrentSpriteGrid.blueprint.SetAnimationChainFromFile(((OkCancelWindow)callingWindow).Name, 0, SpriteManager);
            SESpriteGridManager.CurrentSpriteGrid.Blueprint.Animate = true;
            throw new NotImplementedException();
//            GuiData.propertiesWindow.blueprintTextureAnimationButton.CurrentChain = GameData.EditorLogic.CurrentSprites[0].CurrentChain;
  //          GuiData.propertiesWindow.blueprintTextureAnimationButton.animate = true;
            SESpriteGridManager.CurrentSpriteGrid.RefreshPaint();
        }

        public static void blueprintReferenceClick(Window callingWindow)
        {
            throw new NotImplementedException("SetAnimationChainFromFile not implemented");

       //     SESpriteGridManager.CurrentSpriteGrid.blueprint.SetAnimationChainFromFile(((OkCancelWindow)callingWindow).Name, 0, SpriteManager, true);
            SESpriteGridManager.CurrentSpriteGrid.Blueprint.Animate = true;
    //        GuiData.propertiesWindow.blueprintTextureAnimationButton.CurrentChain = GameData.EditorLogic.CurrentSprites[0].CurrentChain;
      //      GuiData.propertiesWindow.blueprintTextureAnimationButton.animate = true;
            SESpriteGridManager.CurrentSpriteGrid.RefreshPaint();
        }

        public static void ConvertToSpriteGridButtonClick(Window callingWindow)
        {
            if (((Button)callingWindow).Text == "Modify Sprite Grid")
            {
                // There is already a SpriteGrid created.  The user
                // wants to modify the SpriteGrid's properties
                GuiData.spriteGridPropertiesWindow.Show(false);
            }
            else if (GameData.EditorLogic.CurrentSprites.Count != 0)
            {
                // The user is creating a new SpriteGrid.
                GuiData.spriteGridPropertiesWindow.Show(true);
            }
        }

        public static void gridTextureAnimationButtonClick(Window callingWindow)
        {
            if (SESpriteGridManager.CurrentSpriteGrid != null)
            {
                FileWindow tempFileWindow = GuiManager.AddFileWindow();
                List<string> fileTypes = new List<string>();
                fileTypes.Add("ach");
                fileTypes.Add("bmp");
                fileTypes.Add("jpg");
                fileTypes.Add("png");
                fileTypes.Add("tga");
                tempFileWindow.SetFileType(fileTypes);
                tempFileWindow.OkClick += new GuiMessage(SpriteGridGuiMessages.loadGridAnimationTextureOk);
            }
        }

        public static void loadGridAnimationTextureOk(Window callingWindow)
        {
            if (FileManager.GetExtension(((FileWindow)callingWindow).Results[0]) == "ach")
            {
                OkCancelWindow tempWindow = GuiManager.AddOkCancelWindow();
                tempWindow.ScaleX = 10f;
                tempWindow.ScaleY = 7f;
                tempWindow.OkText = "Embed";
                tempWindow.OkClick += new GuiMessage(SpriteGridGuiMessages.blueprintEmbedClick);
                tempWindow.CancelText = "Reference";
                tempWindow.CancelClick += new GuiMessage(SpriteGridGuiMessages.blueprintReferenceClick);
                tempWindow.Message = "Would you like to embed or reference the animation chain?";
                tempWindow.Name = ((FileWindow)callingWindow).Results[0];
            }
            else
            {
                SESpriteGridManager.CurrentSpriteGrid.Blueprint.Texture = 
                    FlatRedBallServices.Load<Texture2D>(((FileWindow)callingWindow).Results[0], GameData.SceneContentManager);
                SESpriteGridManager.CurrentSpriteGrid.SetBaseTexture(SESpriteGridManager.CurrentSpriteGrid.Blueprint.Texture);
                SESpriteGridManager.CurrentSpriteGrid.RefreshPaint();
                GuiData.ListWindow.Add(SESpriteGridManager.CurrentSpriteGrid.Blueprint.Texture);
            }
        }

        public static void resetGridTexturesClick(Window callingWindow)
        {
            if (SESpriteGridManager.CurrentSpriteGrid != null)
            {
                SESpriteGridManager.CurrentSpriteGrid.ResetTextures();
            }
        }

        public static void spriteGridCancelClick(Window callingWindow)
        {
            GuiData.spriteGridPropertiesWindow.Visible = false;
        }

        public static void updateSpriteGridCancel(Window callingWindow)
        {
            GameData.EditorLogic.CurrentSprites[0].X = SESpriteGridManager.oldPosition.X;
            GameData.EditorLogic.CurrentSprites[0].Y = SESpriteGridManager.oldPosition.Y;
            GameData.EditorLogic.CurrentSprites[0].Z = SESpriteGridManager.oldPosition.Z;
            GameData.EditorLogic.CurrentSprites[0].ScaleX = SESpriteGridManager.CurrentSpriteGrid.Blueprint.ScaleX;
            GameData.EditorLogic.CurrentSprites[0].ScaleY = SESpriteGridManager.CurrentSpriteGrid.Blueprint.ScaleY;
            GameData.EditorLogic.CurrentSprites[0].RotationX = SESpriteGridManager.CurrentSpriteGrid.Blueprint.RotationX;
            GameData.EditorLogic.CurrentSprites[0].RotationY = SESpriteGridManager.CurrentSpriteGrid.Blueprint.RotationY;
            GameData.EditorLogic.CurrentSprites[0].RotationZ = SESpriteGridManager.CurrentSpriteGrid.Blueprint.RotationZ;
        }

        public static void updateSpriteGridOk(Window callingWindow)
        {
            float xChange = GameData.EditorLogic.CurrentSprites[0].X - SESpriteGridManager.oldPosition.X;
            float yChange = GameData.EditorLogic.CurrentSprites[0].Y - SESpriteGridManager.oldPosition.Y;
            float zChange = GameData.EditorLogic.CurrentSprites[0].Z - SESpriteGridManager.oldPosition.Z;

            GameData.EditorLogic.CurrentSprites[0].X = SESpriteGridManager.oldPosition.X;
            GameData.EditorLogic.CurrentSprites[0].Y = SESpriteGridManager.oldPosition.Y;
            GameData.EditorLogic.CurrentSprites[0].Z = SESpriteGridManager.oldPosition.Z;
            SESpriteGridManager.CurrentSpriteGrid.Blueprint.ScaleX = GameData.EditorLogic.CurrentSprites[0].ScaleX;
            SESpriteGridManager.CurrentSpriteGrid.Blueprint.ScaleY = GameData.EditorLogic.CurrentSprites[0].ScaleY;
            SESpriteGridManager.CurrentSpriteGrid.Blueprint.RotationX = GameData.EditorLogic.CurrentSprites[0].RotationX;
            SESpriteGridManager.CurrentSpriteGrid.Blueprint.RotationY = GameData.EditorLogic.CurrentSprites[0].RotationY;
            SESpriteGridManager.CurrentSpriteGrid.Blueprint.RotationZ = GameData.EditorLogic.CurrentSprites[0].RotationZ;
            SESpriteGridManager.CurrentSpriteGrid.Shift(xChange, yChange, zChange);
            SESpriteGridManager.CurrentSpriteGrid.UpdateRotationAndScale();
            SESpriteGridManager.CurrentSpriteGrid.RefreshPaint();

            GameData.Cursor.ClickSprite(sesgMan.newlySelectedCurrentSprite);
            if (sesgMan.newlySelectedCurrentSprite == null)
            {
                SESpriteGridManager.oldPosition = Vector3.Empty;
                SESpriteGridManager.CurrentSpriteGrid = null;
            }
            else
            {
                SESpriteGridManager.oldPosition = sesgMan.newlySelectedCurrentSprite.Position;
                if (SESpriteGridManager.CurrentSpriteGrid == sesgMan.newlySelectedCurrentSpriteGrid)
                {
                    SESpriteGridManager.CurrentSpriteGrid.Manage();
                }
                else
                {
                    SESpriteGridManager.CurrentSpriteGrid = sesgMan.newlySelectedCurrentSpriteGrid;
                }
            }
            Vector3 newPosToSet = Vector3.Empty;
            if (sesgMan.newlySelectedCurrentSprite != null)
            {
                newPosToSet = sesgMan.newlySelectedCurrentSprite.Position;
            }
            sesgMan.ClickGrid(SESpriteGridManager.CurrentSpriteGrid, sesgMan.newlySelectedCurrentSprite);
            SESpriteGridManager.oldPosition = newPosToSet;
        }
    }


}
