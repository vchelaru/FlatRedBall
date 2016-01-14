using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Graphics;

using SpriteEditor.SEPositionedObjects;

using TextureOperation = Microsoft.DirectX.Direct3D.TextureOperation;
using FlatRedBall.IO;
using EditorObjects.EditorSettings;

namespace SpriteEditor.Gui
{
    public class GuiMessages
    {
        #region Fields
        private Camera camera = null;
        private Cursor cursor;
        private GameForm form;

        private Random random;
        private SESpriteGridManager sesgMan;

        EditorProperties mEditorProperties;
        #endregion


        public void AddDisplayRegion(Window callingWindow)
        {
            TextInputWindow textInputWindow = GuiManager.ShowTextInputWindow(
                "Enter name for new Display Region", "Enter Name");
            textInputWindow.Text = 
                FileManager.RemovePath(GuiData.TextureCoordinatesSelectionWindow.DisplayedTexture.Name) +
                " Sub" + (GuiData.ListWindow.TextureListBox.GetItem(GuiData.TextureCoordinatesSelectionWindow.DisplayedTexture).Count + 1);

            textInputWindow.OkClick += AddDisplayRegionOk;
        }

        private void AddDisplayRegionOk(Window callingWindow)
        {
            DisplayRegion displayRegion = new DisplayRegion(
                GuiData.TextureCoordinatesSelectionWindow.TopTV,
                GuiData.TextureCoordinatesSelectionWindow.BottomTV,
                GuiData.TextureCoordinatesSelectionWindow.LeftTU,
                GuiData.TextureCoordinatesSelectionWindow.RightTU);

            displayRegion.Name = ((TextInputWindow)callingWindow).Text;

            if (GuiData.TextureCoordinatesSelectionWindow.DisplayedTexture != null)
                GuiData.ListWindow.AddDisplayRegion(
                    displayRegion, 
                    GuiData.TextureCoordinatesSelectionWindow.DisplayedTexture);
        }


        public void closeSceneColorOperation(Window callingWindow)
        {
    //        GameData.guiData.sceneColorOperationWindowVisibility.Unpress();
        }

        public void GridListBoxDoubleClick(Window callingWindow)
        {
            SpriteGrid sg = ((CollapseListBox)callingWindow).GetFirstHighlightedObject() as SpriteGrid;
            if (sg != null)
            {
                this.camera.X = (sg.XLeftBound + sg.XRightBound) / 2f;
                if (sg.GridPlane == SpriteGrid.Plane.XY)
                {
                    this.camera.Y = (sg.YBottomBound + sg.YTopBound) / 2f;
                }
                else
                {
                    this.camera.Y = sg.Blueprint.Y;
                }
            }
        }


        public void Initialize(GameForm form)
        {
            this.form = form;
            this.camera = GameForm.camera;

            this.cursor = GameForm.cursor;
            this.sesgMan = GameData.sesgMan;
            SpriteGridGuiMessages.camera = this.camera;
            SpriteGridGuiMessages.random = this.random;
            SpriteGridGuiMessages.sesgMan = GameData.sesgMan;

            mEditorProperties = GameData.EditorProperties;
        }

        public void sceneAddColorOp(Window callingWindow)
        {
            foreach (Sprite sprite in GameData.Scene.Sprites)
                sprite.ColorOperation = TextureOperation.Add;
        }

        public void sceneAddSignedColorOp(Window callingWindow)
        {
            foreach (Sprite sprite in GameData.Scene.Sprites)
                sprite.ColorOperation = TextureOperation.AddSigned;
        }

        public void sceneModulateColorOp(Window callingWindow)
        {
            foreach (Sprite sprite in GameData.Scene.Sprites)
                sprite.ColorOperation = TextureOperation.Modulate;
        }

        public void sceneNoColorOp(Window callingWindow)
        {
            foreach (Sprite sprite in GameData.Scene.Sprites)
                sprite.ColorOperation = TextureOperation.SelectArg1;
        }

        public void sceneSubtractColorOp(Window callingWindow)
        {
            foreach (Sprite sprite in GameData.Scene.Sprites)
                sprite.ColorOperation = TextureOperation.Subtract;
        }

        public void sceneTintBlueChange(Window callingWindow)
        {
            foreach (Sprite sprite in GameData.Scene.Sprites)
                sprite.Blue = ((UpDown)callingWindow).CurrentValue;
        }

        public void sceneTintGreenChange(Window callingWindow)
        {
            foreach (Sprite sprite in GameData.Scene.Sprites)
                sprite.Green = ((UpDown)callingWindow).CurrentValue;
        }

        public void sceneTintRedChange(Window callingWindow)
        {
            foreach (Sprite sprite in GameData.Scene.Sprites)
                sprite.Red = ((UpDown)callingWindow).CurrentValue;
        }

    }
}
