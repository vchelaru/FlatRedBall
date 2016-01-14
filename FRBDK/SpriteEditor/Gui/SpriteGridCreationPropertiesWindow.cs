using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Collections;
using FlatRedBall.IO;
using FlatRedBall.Utilities;
using SpriteEditor.SpriteGridObjects;


namespace SpriteEditor.Gui
{
    public class SpriteGridCreationPropertiesWindow : PropertyGrid<SpriteGridCreationOptions>
    {
        #region Fields

        private Camera camera;

        private TextDisplay mResizeWarning;
        private SESpriteGridManager sesgMan;
        public Button spriteGridCancel;
        public Button spriteGridOk;
        public WindowArray spriteGridOkCancelGui;
        public Window spriteGridPropertiesWindow;

        Sprite mBlueprintSprite;

        bool mCreatingNewSpriteGrid;

        #endregion

        #region Methods

        public SpriteGridCreationPropertiesWindow(Cursor cursor)
            : base(cursor)
        {
            #region Create "this" properties
            this.camera = GameData.Camera;
            this.sesgMan = GameData.sesgMan;
            GuiManager.AddWindow(this);
            base.HasCloseButton = true;
            base.HasMoveBar = true;
            base.SetPositionTL(18.1f, 33f);
            this.Visible = false;
            #endregion

            SelectedObject = new SpriteGridCreationOptions();


            this.spriteGridOkCancelGui = new WindowArray();
            this.spriteGridOk = new Button(mCursor);
            this.spriteGridOk.Text = "Ok";
            this.spriteGridOk.ScaleX = 4f;
            this.spriteGridOk.ScaleY = 1.2f;
            this.spriteGridOk.Click += new GuiMessage(this.spriteGridOkClick);
            AddWindow(spriteGridOk);

            this.spriteGridCancel = new Button(mCursor);
            this.spriteGridCancel.Text = "Cancel";
            this.spriteGridCancel.ScaleX = 4f;
            this.spriteGridCancel.SetPositionTL(19f, 22f);
            this.spriteGridCancel.ScaleY = 1.2f;
            this.spriteGridCancel.Click += new GuiMessage(SpriteGridGuiMessages.spriteGridCancelClick);
            AddWindow(spriteGridCancel);


            this.mResizeWarning = new TextDisplay(mCursor);
            this.mResizeWarning.Text = "Changing grid spacing clears painted\ntextures.";
            this.mResizeWarning.SetPositionTL(0.2f, 1.7f);
            this.mResizeWarning.Visible = false;
            AddWindow(mResizeWarning);
        }

        public void Show(bool creatingNewSpriteGrid)
        {
            Show(creatingNewSpriteGrid, GameData.EditorLogic.CurrentSprites[0]);
        }

        public void Show(bool creatingNewSpriteGrid, Sprite spriteToUse)
        {
            mCreatingNewSpriteGrid = creatingNewSpriteGrid;

            if (GameData.Camera.Orthogonal)
            {
                Texture2D texture = spriteToUse.Texture;

                float pixelsPerUnit = camera.PixelsPerUnitAt(spriteToUse.Z);


                spriteToUse.ScaleX = .5f * texture.Width / pixelsPerUnit;
                spriteToUse.ScaleY = .5f * texture.Height / pixelsPerUnit;

                SelectedObject.GridSpacing = 2 * spriteToUse.ScaleX;
            }
            else
            {
                SelectedObject.GridSpacing = 2;
            }

            SelectedObject.XLeftBound = -SelectedObject.GridSpacing;
            SelectedObject.XRightBound = SelectedObject.GridSpacing;
            SelectedObject.YTopBound = SelectedObject.GridSpacing;
            SelectedObject.YBottomBound = -SelectedObject.GridSpacing;
            SelectedObject.ZCloseBound = -SelectedObject.GridSpacing;
            SelectedObject.ZFarBound = SelectedObject.GridSpacing;

            SelectedObject.Plane = SpriteGrid.Plane.XY;
            UpdateDisplayedProperties();

            if (mCreatingNewSpriteGrid)
            {
                mBlueprintSprite = spriteToUse;

                this.Name = "New SpriteGrid Properties";
                this.Visible = true;
                this.mResizeWarning.Visible = false;
                //this.yOrZTextDisplay.Visible = true;
                //this.yOrZComboBox.Visible = true;
                GuiManager.AddDominantWindow(this);



            }
            else
            {
                this.Visible = true;
                this.mResizeWarning.Visible = true;
                GuiManager.AddDominantWindow(this);
            }
        }

        public void spriteGridOkClick(Window callingWindow)
        {
            if ( SelectedObject.GridSpacing <= 0f)
            {
                GuiManager.ShowMessageBox("GridSpacing must be greater than 0.", "SpriteGrid error");
            }
            else if ((SelectedObject.Plane == SpriteGrid.Plane.XZ && 
                (10000f < ( SelectedObject.XRightBound - SelectedObject.XLeftBound) * (SelectedObject.ZFarBound - SelectedObject.ZCloseBound))) && (callingWindow.GetType() != typeof(OkCancelWindow)))
            {
                OkCancelWindow tempWindow = GuiManager.ShowOkCancelWindow("The grid you are creating is large and may result in sluggish performance or freezing of the SE.  What would you like to do?", "Large Grid");
                tempWindow.ScaleX = 15f;
                tempWindow.OkText = "Create anyway";
                tempWindow.CancelText = "Cancel conversion";
                tempWindow.OkClick += new GuiMessage(this.spriteGridOkClick);
            }
            else if (mCreatingNewSpriteGrid)
            {
                SpriteGrid tempGrid = null;
                if ( SelectedObject.Plane == SpriteGrid.Plane.XY)
                {
                    tempGrid = new SpriteGrid(this.camera, SpriteGrid.Plane.XY, mBlueprintSprite, null);
                }
                else
                {
                    tempGrid = new SpriteGrid(this.camera, SpriteGrid.Plane.XZ, mBlueprintSprite, null);
                }

                if (string.IsNullOrEmpty(mBlueprintSprite.Name))
                {
                    tempGrid.Name = 
                        FileManager.RemovePath(mBlueprintSprite.Texture.Name);
                }
                else
                {
                    tempGrid.Name = mBlueprintSprite.Name;
                }

                tempGrid.XLeftBound = SelectedObject.XLeftBound;
                tempGrid.XRightBound = SelectedObject.XRightBound;
                tempGrid.YTopBound = SelectedObject.YTopBound;
                tempGrid.YBottomBound = SelectedObject.YBottomBound;
                tempGrid.ZCloseBound = SelectedObject.ZCloseBound;
                tempGrid.ZFarBound = SelectedObject.ZFarBound;
                tempGrid.GridSpacing = SelectedObject.GridSpacing;

                this.sesgMan.PopulateAndAddGridToEngine(tempGrid, mBlueprintSprite);

                StringFunctions.MakeNameUnique<SpriteGrid>(tempGrid, GameData.Scene.SpriteGrids);

                //tempGrid.InitializeTextureGrid();
                tempGrid.RefreshPaint();
                this.Visible = false;
                SpriteEditorSettings.EditingSpriteGrids = true;
                GuiData.ToolsWindow.attachSprite.Enabled = false;
            }
            else
            {
                GameData.Cursor.ClickSprite(null);
                SESpriteGridManager.CurrentSpriteGrid.XLeftBound = SelectedObject.XLeftBound;
                SESpriteGridManager.CurrentSpriteGrid.XRightBound = SelectedObject.XRightBound;
                SESpriteGridManager.CurrentSpriteGrid.YTopBound = SelectedObject.YTopBound;
                SESpriteGridManager.CurrentSpriteGrid.YBottomBound = SelectedObject.YBottomBound;
                SESpriteGridManager.CurrentSpriteGrid.ZCloseBound = SelectedObject.ZCloseBound;
                SESpriteGridManager.CurrentSpriteGrid.ZFarBound = SelectedObject.ZFarBound;
                if (SESpriteGridManager.CurrentSpriteGrid.GridSpacing != SelectedObject.GridSpacing)
                {
                    SESpriteGridManager.CurrentSpriteGrid.GridSpacing = SelectedObject.GridSpacing;
                    SESpriteGridManager.CurrentSpriteGrid.ResetTextures();
                    SESpriteGridManager.CurrentSpriteGrid.PopulateGrid(this.camera.X, this.camera.Y, 0f);
                }
                SESpriteGridManager.CurrentSpriteGrid.Manage();
                SESpriteGridManager.CurrentSpriteGrid.RefreshPaint();
                this.Visible = false;
            }
        }

        public void yOrZComboBoxItemClick(Window callingWindow)
        {
            //if (((ComboBox)callingWindow).Text == "XY Plane")
            //{
            //    this.xYPlaneGui.Visible = true;
            //    this.xZPlaneGui.Visible = false;
            //    this.xRightBoundTextBox.NextInTabSequence = this.yBottomBoundTextBox;
            //}
            //else
            //{
            //    this.xRightBoundTextBox.NextInTabSequence = this.zCloseBoundTextBox;
            //    this.xYPlaneGui.Visible = false;
            //    this.xZPlaneGui.Visible = true;
            //}
        }

        #endregion

    }


}
