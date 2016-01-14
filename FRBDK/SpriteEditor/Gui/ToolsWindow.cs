using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;

using FlatRedBall.Graphics.Model;

using FlatRedBall.Gui;


using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Math;

using FlatRedBall.Instructions;
using FlatRedBall.Utilities;

using FlatRedBall.Collections;

using SpriteEditor.SEPositionedObjects;
using FlatRedBall.Graphics;

namespace SpriteEditor.Gui
{
    public class ToolsWindow : EditorObjects.Gui.ToolsWindow
    {
        #region Fields

        public ToggleButton MoveButton;
        public ToggleButton ScaleButton;
        public ToggleButton RotateButton;

        public ToggleButton attachSprite;
        public ComboBox brushSize;
        public ToggleButton constrainDimensions;
        public Button convertToSpriteFrame;
        public Button convertToSpriteGridButton;
        Button mDuplicateObject;
        public Button currentTextureDisplay;

        public Button detachSpriteButton;
        public ToggleButton eyedropper;

        public ToggleButton groupHierarchyControlButton;

        private GuiMessages messages;
        public ToggleButton paintButton;
        private SESpriteGridManager sesgMan;
        public Button setRootAsControlPoint;
        private ToggleButton mSnapSprite;
        public Window spriteToggleOptions;

        Button mDownZFreeRotateButton;

        #endregion

        #region Properties

        public ToggleButton SnapSprite
        {
            get { return mSnapSprite; }
        }

        public bool IsDownZ
        {
            get { return mDownZFreeRotateButton.ButtonPushedState != ButtonPushedState.Down; }
        }

        #endregion

        #region Event Methods

        public void CopyCurrentObjects(Window callingWindow)
        {
            if ((GameData.EditorLogic.CurrentSprites.Count != 0) ||
                (GameData.EditorLogic.CurrentSpriteFrames.Count != 0) ||
                (GameData.EditorLogic.CurrentPositionedModels.Count != 0) ||
                (GameData.EditorLogic.CurrentTexts.Count != 0))
            {
                int instructionNum = 0;

                #region Copy Sprites

                if (SpriteEditorSettings.EditingSprites)
                {

                    // remove the in-screen markers and axes so they don't get touched by the copying
                    GameData.EditorLogic.EditAxes.CurrentObject = null;

                    Sprite topSpriteAdded = null;
                    AttachableList<Sprite> oldestParents = AttachableList<Sprite>.GetTopParents<Sprite, Sprite>(GameData.EditorLogic.CurrentSprites);
                    SpriteList addedSprites = new SpriteList();

                    // The appendedNumbers variable is used for improving performance when copying large groups.  When a Sprite
                    // is copied, it is given a number at the end of its name or if there is already a number present,
                    // it is incremented.  However, copying large groups can result in a lot of string checking which
                    // can hurt performance.
                    // Consider copying 100 Sprites with the same texture.  Their names will likely be redball1, redball2,
                    // redball3, ... redball100.  Let's say redball1 gets copied first.  The SpriteEditor increments the
                    // number at the end to redball2, then checks to see if there is already a Sprite with that name.  If
                    // there is, it increments to redball3 and so on.  This must be conducted on average 10,000 times and each
                    // iteration requires string copying, concatenation, and parsing for integers.  To cut this down, the 
                    // appendedNumbers variable associates a given name (in this case redball) with the last integer appended
                    // to that particular name.  Therefore, when the first Sprite is created, it will loop through 1 - 100 and
                    // eventually end up at redball101.  It will then associate the number 101 with the name redball.  The next
                    // Sprite (named redball2) will know to begin at redball101.  This increases the performance of
                    // copying large groups from O(n^2) to O(n).

                    Dictionary<string, int> appendedNumbers = new Dictionary<string, int>();

                    foreach (Sprite s in oldestParents)
                    {
                        if (s is ISpriteEditorObject)
                        {
                            if (GuiData.ToolsWindow.groupHierarchyControlButton.IsPressed)
                            {
                                topSpriteAdded =
                                    GameData.copySpriteHierarchy(
                                        s, s.Parent as Sprite, GameData.EditorProperties.PixelSize, addedSprites, appendedNumbers);
                            }
                            else
                            {
                                topSpriteAdded =
                                    GameData.copySpriteHierarchy(
                                        s.TopParent as Sprite, null, GameData.EditorProperties.PixelSize, addedSprites, appendedNumbers);
                            }
                        }
                        SpriteList tempSpriteArray = new SpriteList();
                        topSpriteAdded.GetAllDescendantsOneWay(tempSpriteArray);
                        tempSpriteArray.Add(topSpriteAdded);

                    }

                    GameData.EditorLogic.EditAxes.CurrentObject =
                        GameData.EditorLogic.CurrentSprites[0];

                    GameData.Cursor.ClickSprite(null);
                    foreach (Sprite s in AttachableList<Sprite>.GetTopParents<Sprite, Sprite>(addedSprites))
                    {
                        // the 2nd true forces selection of all Sprites
                        GameData.Cursor.ClickObject<Sprite>(s, GameData.EditorLogic.CurrentSprites, true, true);
                    }

                    GameData.Cursor.VerifyAndUpdateGrabbedAgainstCurrent();
                }
                #endregion

                #region Copy SpriteGrids
                else if (SpriteEditorSettings.EditingSpriteGrids)
                {
                    SpriteGrid sg = SESpriteGridManager.CurrentSpriteGrid.Clone();


                    FlatRedBall.Utilities.StringFunctions.MakeNameUnique<SpriteGrid>(sg, GameData.Scene.SpriteGrids);


                    this.sesgMan.PopulateAndAddGridToEngine(sg, GameData.EditorLogic.CurrentSprites[0]);

                    sg.RefreshPaint();
                }
                #endregion

                #region Copy SpriteFrames
                else if (SpriteEditorSettings.EditingSpriteFrames)
                {
                    GameData.EditorLogic.EditAxes.CurrentObject = null;

                    AttachableList<SpriteFrame> oldestParents =
                        AttachableList<SpriteFrame>.GetTopParents<SpriteFrame, SpriteFrame>(GameData.EditorLogic.CurrentSpriteFrames);
                    foreach (SpriteFrame sf in oldestParents)
                    {
                        SpriteFrame topSpriteFrameAdded = GameData.CopySpriteFrameHierarchy(sf, null, GameData.EditorProperties.PixelSize);
                        GameData.Cursor.ClickSpriteFrame(topSpriteFrameAdded);
                    }
                }
                #endregion

                #region Copy Models

                else if (SpriteEditorSettings.EditingModels)
                {
                    GameData.EditorLogic.EditAxes.CurrentObject = null;

                    AttachableList<PositionedModel> oldestParents =
                        AttachableList<PositionedModel>.GetTopParents<PositionedModel, PositionedModel>(GameData.EditorLogic.CurrentPositionedModels);
                    foreach (PositionedModel model in oldestParents)
                    {

                        PositionedModel topSpriteFrameAdded =
                            GameData.CopyModelHierarchy(model, null, GameData.EditorProperties.PixelSize);

                        //         GameData.cursor.ClickSpriteFrame(topSpriteFrameAdded);
                    }
                }

                #endregion

                #region Copy Texts

                else if (SpriteEditorSettings.EditingTexts)
                {
                    GameData.EditorLogic.EditAxes.CurrentObject = null;

                    AttachableList<Text> oldestParents =
                        AttachableList<Text>.GetTopParents<Text, Text>(GameData.EditorLogic.CurrentTexts);
                    foreach (Text text in oldestParents)
                    {

                        Text topText =
                            GameData.CopyTextHierarchy(text, null, GameData.EditorProperties.PixelSize);

                        //         GameData.cursor.ClickSpriteFrame(topSpriteFrameAdded);
                    }

                }

                #endregion

            }
        }


        #endregion

        #region Methods

        public ToolsWindow()
            : base()
        {
            #region Set managers and UI references
            this.messages = GuiData.messages;
            this.sesgMan = GameData.sesgMan;
            #endregion

            #region Set "this" properties
            base.SetPositionTL(106f, 5.8f);
            base.HasCloseButton = true;
            #endregion

            #region Move Button
            this.MoveButton = AddToggleButton();
            this.MoveButton.Text = "Move";
            this.MoveButton.SetOverlayTextures(2, 0);
            #endregion

            #region Scale Button
            this.ScaleButton = AddToggleButton();
            this.ScaleButton.Text = "Scale";
            this.MoveButton.AddToRadioGroup(this.ScaleButton);
            this.ScaleButton.SetOverlayTextures(1, 0);
            #endregion

            #region Rotate Button
            this.RotateButton = AddToggleButton();
            this.RotateButton.Text = "Rotate";
            this.MoveButton.AddToRadioGroup(this.RotateButton);
            this.RotateButton.SetOverlayTextures(0, 0);
            #endregion

            #region Attach Sprite

            this.attachSprite = base.AddToggleButton();
            this.attachSprite.Text = "Attach";
            this.MoveButton.AddToRadioGroup(this.attachSprite);
            this.attachSprite.SetOverlayTextures(7, 0);

            #endregion

            #region Detach Sprite

            this.detachSpriteButton = AddButton();
            this.detachSpriteButton.Text = "Detach";
            this.detachSpriteButton.Enabled = false;
            this.detachSpriteButton.SetOverlayTextures(10, 0);
            this.detachSpriteButton.Click += new GuiMessage(this.detachSprite);

            #endregion

            #region SetRootAsControlPoint
            this.setRootAsControlPoint = AddButton();
            this.setRootAsControlPoint.Text = "Set Root As Control Point";
            this.setRootAsControlPoint.Enabled = false;
            this.setRootAsControlPoint.SetOverlayTextures(12, 2);
            this.setRootAsControlPoint.Click += new GuiMessage(this.SetRootAsControlPointClick);
            #endregion

            #region Duplicate Objects

            this.mDuplicateObject = AddButton();
            this.mDuplicateObject.Text = "Duplicate";
            this.mDuplicateObject.SetOverlayTextures(9, 0);
            this.mDuplicateObject.Click += new GuiMessage(this.CopyCurrentObjects);

            #endregion

            #region Convert to SpriteGrid
            this.convertToSpriteGridButton = AddButton();
            this.convertToSpriteGridButton.Text = "Convert Sprite to SpriteGrid";
            this.convertToSpriteGridButton.SetOverlayTextures(2, 1);
            this.convertToSpriteGridButton.Enabled = false;
            this.convertToSpriteGridButton.Click += new GuiMessage(SpriteGridGuiMessages.ConvertToSpriteGridButtonClick);
            #endregion

            #region Convert to SpriteFrame

            this.convertToSpriteFrame = AddButton();
            this.convertToSpriteFrame.Text = "Convert Sprite to SpriteFrame";
            this.convertToSpriteFrame.SetOverlayTextures(1, 3);
            this.convertToSpriteFrame.Enabled = false;
            this.convertToSpriteFrame.Click += new GuiMessage(GameData.sfMan.ConvertToSpriteFrameClick);

            #endregion

            #region Paint Button
            this.paintButton = AddToggleButton();
            this.paintButton.Text = "Paint";
            this.paintButton.SetOverlayTextures(5, 1);
            this.MoveButton.AddToRadioGroup(this.paintButton);
            this.paintButton.Click += new GuiMessage(this.PaintButtonClicked);

            #endregion

            #region Current Texture Display

            this.currentTextureDisplay = AddButton();
            this.currentTextureDisplay.Text = "";
            this.currentTextureDisplay.Click += new GuiMessage(FileButtonWindow.openFileWindowLoadTexture);

            #endregion

            #region eyedropper

            this.eyedropper = AddToggleButton();
            this.eyedropper.Text = "Eyedropper Tool";
            this.eyedropper.SetOverlayTextures(8, 1);
            this.MoveButton.AddToRadioGroup(this.eyedropper);

            #endregion

            #region Brush size

            this.brushSize = new ComboBox(mCursor);
            AddWindow(brushSize);
            this.brushSize.SetPositionTL(4.2f, 12.5f);
            this.brushSize.ScaleY = 1.3f;
            this.brushSize.ScaleX = 3.8f;
            this.brushSize.AddItem("1X1");
            this.brushSize.AddItem("3X3");
            this.brushSize.AddItem("5X5");
            this.brushSize.Text = "1X1";
            this.brushSize.ExpandOnTextBoxClick = true;

            #endregion

            this.constrainDimensions = base.AddToggleButton();
            this.constrainDimensions.SetPositionTL(constrainDimensions.X, 15.5f);
            this.constrainDimensions.Text = "Constrain Dim.";
            this.constrainDimensions.SetOverlayTextures(5, 0);

            #region Group/Hierarchy Button
            this.groupHierarchyControlButton = base.AddToggleButton();
            this.groupHierarchyControlButton.SetText("Group Control", "Hierarchy Control");
            this.groupHierarchyControlButton.SetOverlayTextures(3, 0, 4, 0);
            #endregion


            this.mSnapSprite = base.AddToggleButton();
            this.mSnapSprite.Text = "Sprite Snapping";
            this.mSnapSprite.SetOverlayTextures(6, 0);

            #region DownZFreeRotateButton

            this.mDownZFreeRotateButton = base.AddToggleButton();


            mDownZFreeRotateButton.SetOverlayTextures(
                FlatRedBallServices.Load<Texture2D>(@"Content\DownZ.png", FlatRedBallServices.GlobalContentManager),
                FlatRedBallServices.Load<Texture2D>(@"Content\FreeRotation.png", FlatRedBallServices.GlobalContentManager));
            

            #endregion

            this.MinimumScaleX = this.ScaleX;
            this.MinimumScaleY = this.ScaleY;
        }

        public void DuplicateClick()
        {
            mDuplicateObject.OnClick();
        }

        public void detachSprite(Window callingWindow)
        {
            GuiData.ToolsWindow.detachSpriteButton.Enabled = false;

            foreach (PositionedModel model in GameData.EditorLogic.CurrentPositionedModels)
            {
                model.Detach();
            }

            foreach (Sprite sprite in GameData.EditorLogic.CurrentSprites)
            {
                sprite.Detach();
            }
        }

        public void PaintButtonClicked(Window callingWindow)
        {
            if (this.paintButton.IsPressed)
            {
                if (GameData.EditorLogic.CurrentAnimationChainList != null)
                {
                    SpriteEditorSettings.ViewingAnimationChains = true;
                }
                else
                {
                    SpriteEditorSettings.EditingTextures = true;
                }
            }
        }

        public void SetRootAsControlPointClick(Window callingWindow)
        {
            Sprite topRoot;
            SpriteList allChildren;
            if (((Button)callingWindow).Text == "Set Root As Control Point")
            {
                topRoot = (Sprite)GameData.EditorLogic.CurrentSprites[0].TopParent;
                allChildren = new SpriteList();
                topRoot.GetAllDescendantsOneWay(allChildren);

                foreach (Sprite sprite in allChildren)
                {
                    if(sprite is EditorSprite)
                        ((EditorSprite)sprite).type = "Root Control";
                }

                ((EditorSprite)topRoot).type = "Top Root Control";
                ((Button)callingWindow).Text = "Clear Root Control Point";
            }
            else
            {
                topRoot = (Sprite)GameData.EditorLogic.CurrentSprites[0].TopParent;
                allChildren = new SpriteList();
                topRoot.GetAllDescendantsOneWay(allChildren);

                foreach (Sprite sprite in allChildren)
                {
                    ((EditorSprite)sprite).type = "Root Control";
                }
                
                ((EditorSprite)topRoot).type = "";
                ((Button)callingWindow).Text = "Set Root As Control Point";
            }
        }

        #endregion
    }
}
