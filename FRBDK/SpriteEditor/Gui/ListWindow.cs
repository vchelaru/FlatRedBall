using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Gui;

using FlatRedBall.Input;

using FlatRedBall.Math;
using FlatRedBall.Texture;
using FlatRedBall.Collections;
using FlatRedBall.Graphics;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Model;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.IO;
using FlatRedBall.Math.Geometry;
using EditorObjects.EditorSettings;


namespace SpriteEditor.Gui
{
    public class ListWindow : CollapseWindow
    {
        #region Fields

        private GuiMessages messages;

        private ToggleButton editGrids;
        private ToggleButton editModels;
        private ToggleButton editSpriteFrames;
        private ToggleButton editSprites;
        private ToggleButton mEditTexts;

        private Button mViewTexturesButton;
        private Button mViewAnimationChainsButton;

        public ListDisplayWindow SpriteListBox;
        private ListDisplayWindow mModelListBox;
        public ListDisplayWindow SpriteFrameListBox;
        public ListDisplayWindow SpriteGridListBox;
        private ListDisplayWindow mTextListBox;


        private CollapseListBox textureListBox;
        private ListDisplayWindow mAnimationChainListWindow;

        // reference to the ToolsWindow.  The ToolsWindow also
        // exists in the GuiManager
        private ToolsWindow mToolsWindow;

        #endregion

        #region Properties

        public CollapseListBox TextureListBox
        {
            get { return textureListBox; }
        }


        public bool EditingModels
        {
            get
            {
                return this.editModels.IsPressed;
            }
            set
            {
                if (value)
                {
                    this.editModels.Press();
                }
                else
                {
                    this.editModels.Unpress();
                }
            }
        }

        public bool EditingSpriteFrames
        {
            get
            {
                return this.editSpriteFrames.IsPressed;
            }
            set
            {
                if (value)
                {
                    this.editSpriteFrames.Press();
                }
                else
                {
                    this.editSpriteFrames.Unpress();
                }
            }
        }

        public bool EditingSpriteGrids
        {
            get
            {
                return this.editGrids.IsPressed;
            }
            set
            {
                if (value)
                {
                    this.editGrids.Press();
                }
                else
                {
                    this.editGrids.Unpress();
                }
            }
        }

        public bool EditingSprites
        {
            get
            {
                return this.editSprites.IsPressed;
            }
            set
            {
                if (value)
                {
                    this.editSprites.Press();
                }
                else
                {
                    this.editSprites.Unpress();
                }
            }
        }
        
        public bool EditingTextures
        {
            get
            {
                return textureListBox.Visible;
            }
            
            set
            {
                if (value)
                {
                    this.mViewTexturesButton.OnClick();
                }
            }
             
        }

        public bool EditingTexts
        {
            get
            {
                return this.mEditTexts.IsPressed;
            }

            set
            {
                if (value)
                {
                    this.mEditTexts.Press();
                }
                else
                {
                    this.mEditTexts.Unpress();
                }
            }
        }

        public AnimationChain HighlightedAnimationChain
        {
            get
            {
                CollapseItem item = mAnimationChainListWindow.GetFirstHighlightedItem();

                object highlightedObject = mAnimationChainListWindow.GetFirstHighlightedObject();

                while (item != null && !(item.ReferenceObject is AnimationChain))
                {
                    item = item.ParentItem;
                }

                if (item != null)
                    return item.ReferenceObject as AnimationChain;
                else
                    return null;
            }
        }

        public AnimationChainList HighlightedAnimationChainList
        {
            get
            {
                CollapseItem item = mAnimationChainListWindow.GetFirstHighlightedItem();

                object highlightedObject = mAnimationChainListWindow.GetFirstHighlightedObject();

                while (item != null && !(item.ReferenceObject is AnimationChainList))
                {
                    item = item.ParentItem;
                }

                if (item != null)
                    return item.ReferenceObject as AnimationChainList;
                else
                    return null;
            }
        }

        public Texture2D HighlightedTexture
        {
            get
            {
                if (this.textureListBox.GetFirstHighlightedObject() is Texture2D)
                    return (this.textureListBox.GetFirstHighlightedObject() as Texture2D);
                else if (this.textureListBox.GetFirstHighlightedParentObject() is Texture2D)
                    return (this.textureListBox.GetFirstHighlightedParentObject() as Texture2D);
                else
                    return null;
            }
        }

        public DisplayRegion HighlightedDisplayRegion
        {
            get { return textureListBox.GetFirstHighlightedObject() as DisplayRegion; }
        }

        public ListDisplayWindow ModelListBox
        {
            get { return mModelListBox; }
        }

        public ListDisplayWindow TextListBox
        {
            get{ return mTextListBox;}
        }

        public bool ViewingAnimationChains
        {
            get { return mAnimationChainListWindow.Visible; }
            set
            {
                if (value)
                {
                    this.mViewAnimationChainsButton.OnClick();
                }
            }
        }
        #endregion

        #region Delegate and Event Methods

        private void AnimationChainListBoxClick(Window callingWindow)
        {
            // do whatever's necessary to select here
            GameData.EditorLogic.CurrentAnimationChainList = HighlightedAnimationChainList;

            if (GameData.EditorLogic.CurrentAnimationChainList != null)
            {
                textureListBox.HighlightObjectNoCall(null, false);

                GuiData.TextureCoordinatesSelectionWindow.Visible = false;


            }
        }

        private void EditFramesClick(Window callingWindow)
        {
            GameData.DeselectCurrentSprites();
            GuiData.ToolsWindow.convertToSpriteFrame.Text = "Convert To Sprite";
            this.EditModeSelect(callingWindow);
        }

        private void EditGridsClick(Window callingWindow)
        {
            GuiData.ToolsWindow.attachSprite.Enabled = false;
            if ((GameData.EditorLogic.CurrentSprites.Count != 0) && (SESpriteGridManager.CurrentSpriteGrid == null))
            {
                GameData.DeselectCurrentSprites();
            }
            this.EditModeSelect(callingWindow);
        }

        private void EditModelsClick(Window callingWindow)
        {
            this.EditModeSelect(callingWindow);
        }

        private void EditTextsClick(Window callingWindow)
        {
            this.EditModeSelect(callingWindow);
        }

        private void EditModeSelect(Window callingWindow)
        {
            MakeAllListBoxesInvisible();


            if (SpriteEditorSettings.EditingSprites)
            {
                GameData.DeselectCurrentObjects<SpriteFrame>(GameData.EditorLogic.CurrentSpriteFrames);
                GameData.DeselectCurrentObjects<PositionedModel>(GameData.EditorLogic.CurrentPositionedModels);
                GameData.DeselectCurrentTexts();

                this.SpriteListBox.Visible = true;
            }
            else if (SpriteEditorSettings.EditingSpriteGrids)
            {
                GameData.DeselectCurrentSprites();
                GameData.DeselectCurrentObjects<SpriteFrame>(GameData.EditorLogic.CurrentSpriteFrames);
                GameData.DeselectCurrentObjects<PositionedModel>(GameData.EditorLogic.CurrentPositionedModels);
                GameData.DeselectCurrentTexts();

                this.SpriteGridListBox.Visible = true;
            }
            else if (SpriteEditorSettings.EditingSpriteFrames)
            {
                GameData.DeselectCurrentSprites();
                GameData.DeselectCurrentObjects<PositionedModel>(GameData.EditorLogic.CurrentPositionedModels);
                GameData.DeselectCurrentTexts();

                this.SpriteFrameListBox.Visible = true;
            }
            else if (SpriteEditorSettings.EditingModels)
            {
                GameData.DeselectCurrentSprites();
                GameData.DeselectCurrentObjects<SpriteFrame>(GameData.EditorLogic.CurrentSpriteFrames);
                GameData.DeselectCurrentTexts();

                this.mModelListBox.Visible = true;
            }
            else if(SpriteEditorSettings.EditingTexts)
            {
                GameData.DeselectCurrentSprites();
                GameData.DeselectCurrentObjects<SpriteFrame>(GameData.EditorLogic.CurrentSpriteFrames);
                GameData.DeselectCurrentObjects<PositionedModel>(GameData.EditorLogic.CurrentPositionedModels);
                this.mTextListBox.Visible = true;
            }
        }

        private void EditSpritesClick(Window callingWindow)
        {
            GameData.DeselectCurrentSpriteFrames();
            if (SESpriteGridManager.CurrentSpriteGrid != null)
            {
                GameData.sesgMan.ClickGrid(null, null);
            }
            GuiData.ToolsWindow.attachSprite.Enabled = true;
            GuiData.ToolsWindow.convertToSpriteGridButton.Text = "Convert To SpriteGrid";
            GuiData.ToolsWindow.convertToSpriteFrame.Text = "Convert To SpriteFrame";
            this.EditModeSelect(callingWindow);
        }

        private void EditTexturesClick(Window callingWindow)
        {
            MakeAllListBoxesInvisible();
            
            //this.EditModeSelect(callingWindow);
            this.textureListBox.Visible = true;

        }

        public void FrameListBoxClick(Window callingWindow)
        {
            SpriteFrame tempFrameGrabbed = this.SpriteFrameListBox.GetFirstHighlightedObject() as SpriteFrame;
            GameData.Cursor.ClickSpriteFrame(tempFrameGrabbed);
            if (tempFrameGrabbed != null)
            {
                SpriteEditorSettings.EditingSpriteFrames = true;
            }
        }

        public void spriteListBoxClick(Window callingWindow)
        {
            if (SpriteEditorSettings.EditingSprites)
            {
                List<CollapseItem> items = GuiData.ListWindow.SpriteListBox.ListBox.GetHighlightedItems();

                // Calling ClickSprite when the paint button is down results
                // in the Sprite being painted.  Therefore remporarily unpress the
                // paint button and push it back down after selecting the Sprite.
                bool painting = GuiData.ToolsWindow.paintButton.IsPressed;
                GuiData.ToolsWindow.paintButton.Unpress();

                //GameData.EditorLogic.CurrentSprites.Clear();


                foreach (Sprite sprite in GameData.EditorLogic.CurrentSprites)
                {
                    for (int i = items.Count - 1; i > -1; i--)
                    {
                        if (items[i].ReferenceObject == sprite)
                        {
                            items.RemoveAt(i);
                            break;
                        }
                    }
                }

                foreach (CollapseItem item in items)
                {
                    GameData.Cursor.ClickObject<Sprite>(
                        item.ReferenceObject as Sprite,
                        GameData.EditorLogic.CurrentSprites,
                        false);
                }
                if (painting)
                {
                    GuiData.ToolsWindow.paintButton.Press();
                }
            }
        }

        public void spriteListBoxDoubleClick(Window callingWindow)
        {
            if (SpriteEditorSettings.EditingSprites)
            {
                Sprite tempSprite = GuiData.ListWindow.SpriteListBox.GetFirstHighlightedObject() as Sprite ;

                if (tempSprite != null)
                {
                    if (GameData.EditorLogic.CurrentSprites.Count != 0)
                    {
                        GameData.EditorLogic.CurrentSprites[0] = tempSprite;
                    }
                    else
                    {
                        GameData.EditorLogic.CurrentSprites.Add(tempSprite);
                    }
                    SpriteManager.Cameras[0].X = GameData.EditorLogic.CurrentSprites[0].X;
                    SpriteManager.Cameras[0].Y = GameData.EditorLogic.CurrentSprites[0].Y;
                }
            }
        }

        private void TextureListBoxFocusUpdate(IInputReceiver inputReceiver)
        {
            if (InputManager.Keyboard.KeyPushed(Microsoft.DirectX.DirectInput.Key.Delete))
            {
                bool doAnyItemsReferenceTexture = false;

                if (!doAnyItemsReferenceTexture)
                {
                    object highlightedObject = textureListBox.GetFirstHighlightedObject();

                    if (highlightedObject != null && highlightedObject is Texture2D)
                    {
                        GameData.DeleteTexture(highlightedObject as Texture2D);
                    }
                }
            }
        }

        private void TextureListBoxClick(Window callingWindow)
        {
            object highlightedObject = textureListBox.GetFirstHighlightedObject();
            if (highlightedObject is Texture2D)
            {
                mAnimationChainListWindow.HighlightObject(null, false);

                Texture2D asTexture2D = 
                    highlightedObject as Texture2D;

                GuiData.ToolsWindow.currentTextureDisplay.SetOverlayTextures(highlightedObject as Texture2D, null);

                GuiData.ToolsWindow.currentTextureDisplay.SetTextureCoordinates(0, 1, 0, 1);

                GuiData.TextureCoordinatesSelectionWindow.Visible = true;
                GuiData.TextureCoordinatesSelectionWindow.DisplayedTexture = asTexture2D;


            }
            else if (highlightedObject is DisplayRegion)
            {
                mAnimationChainListWindow.HighlightObject(null, false); 
                
                DisplayRegion displayRegion = highlightedObject as DisplayRegion;
                Button button = GuiData.ToolsWindow.currentTextureDisplay;

                Texture2D parentTexture = textureListBox.GetFirstHighlightedParentObject() as Texture2D;

                // get the current CollapseItem's parent which is a texture
                button.SetOverlayTextures(parentTexture, null);

                button.SetTextureCoordinates(
                    displayRegion.Top,
                    displayRegion.Bottom,
                    displayRegion.Left,
                    displayRegion.Right);

                GuiData.TextureCoordinatesSelectionWindow.Visible = true;
                GuiData.TextureCoordinatesSelectionWindow.DisplayedTexture = parentTexture;

                GuiData.TextureCoordinatesSelectionWindow.LeftTU = displayRegion.Left;
                GuiData.TextureCoordinatesSelectionWindow.RightTU = displayRegion.Right;
                GuiData.TextureCoordinatesSelectionWindow.TopTV = displayRegion.Top;
                GuiData.TextureCoordinatesSelectionWindow.BottomTV = displayRegion.Bottom;


            }
            else
            {
                GuiData.ToolsWindow.currentTextureDisplay.SetOverlayTextures((Texture2D)null, (Texture2D)null);
            }
        }

        private void ModelListBoxClick(Window callingWindow)
        {
            PositionedModel model = this.mModelListBox.GetFirstHighlightedObject() as PositionedModel;
            GameData.SelectModel(model);
        }

        private void TextListBoxClick(Window callingWindow)
        {
            Text text = this.mTextListBox.GetFirstHighlightedObject() as Text;
            GameData.SelectText(text);
        }

        private void PositionContainedElements(Window callingWindow)
        {
            this.SpriteListBox.ScaleX = this.ScaleX - 0.5f;
            this.SpriteListBox.ScaleY = this.ScaleY - 1.7f;
            this.SpriteListBox.SetPositionTL(this.ScaleX, this.ScaleY + 1.2f);

            this.SpriteGridListBox.ScaleX = this.ScaleX - 0.5f;
            this.SpriteGridListBox.ScaleY = this.ScaleY - 1.7f;
            this.SpriteGridListBox.SetPositionTL(this.ScaleX, this.ScaleY + 1.2f);

            this.SpriteFrameListBox.ScaleX = this.ScaleX - 0.5f;
            this.SpriteFrameListBox.ScaleY = this.ScaleY - 1.7f;
            this.SpriteFrameListBox.SetPositionTL(this.ScaleX, this.ScaleY + 1.2f);

            this.textureListBox.ScaleX = this.ScaleX - 0.5f;
            this.textureListBox.ScaleY = this.ScaleY - 1.7f;
            this.textureListBox.SetPositionTL(this.ScaleX, this.ScaleY + 1.2f);

            this.mModelListBox.ScaleX = this.ScaleX - 0.5f;
            this.mModelListBox.ScaleY = this.ScaleY - 1.7f;
            this.mModelListBox.SetPositionTL(this.ScaleX, this.ScaleY + 1.2f);

            this.mTextListBox.ScaleX = this.ScaleX - 0.5f;
            this.mTextListBox.ScaleY = this.ScaleY - 1.7f;
            this.mTextListBox.SetPositionTL(this.ScaleX, this.ScaleY + 1.2f);

            mAnimationChainListWindow.ScaleX = this.ScaleX - 0.5f;
            mAnimationChainListWindow.ScaleY = this.ScaleY - 1.7f;
            mAnimationChainListWindow.SetPositionTL(this.ScaleX, this.ScaleY + 1.2f);
        }

        private void ViewAnimationChainsClick(Window callingWindow)
        {
            MakeAllListBoxesInvisible();

            mAnimationChainListWindow.Visible = true;
        }

        private void SpriteGridListFocusUpdate(IInputReceiver inputReceiver)
        {
            if (InputManager.Keyboard.KeyPushed(Microsoft.DirectX.DirectInput.Key.Delete))
            {
                GameData.DeleteCurrentSpriteGrid();
            }
        }

        private void SpriteListFocusUpdate(IInputReceiver inputReceiver)
        {
            if (InputManager.Keyboard.KeyPushed(Microsoft.DirectX.DirectInput.Key.Delete))
            {
                GameData.DeleteCurrentSprites();
            }
        }

        #endregion

        #region Methods

        #region Constructor

        public ListWindow(ToolsWindow toolsWindow)
            : base(GuiManager.Cursor)
        {
            #region Set engine and common SpriteEditor references

            this.messages = GuiData.messages;
            this.mToolsWindow = toolsWindow;

            #endregion

            #region Set "this" properties

            GuiManager.AddWindow(this);
            this.ScaleX = 13f;
            this.ScaleY = 20f;
            base.SetPositionTL(this.ScaleX, 25.7f);
            base.HasMoveBar = true;
            base.HasCloseButton = true;
            base.mName = "Object List";
            base.Resizing += new GuiMessage(this.PositionContainedElements);
            base.Resizable = true;
            base.MinimumScaleX = 10f;
            base.MinimumScaleY = 5f;

            #endregion

            #region spriteListBox

            SpriteListBox = new ListDisplayWindow(GuiManager.Cursor);
            SpriteListBox.DrawBorders = false;
            SpriteListBox.ListBox.TakingInput = false; // allows input like delete, keyboard camera movement to fall through
            SpriteListBox.ListBox.ShiftClickOn = true;
            SpriteListBox.ListBox.CtrlClickOn = true;
            this.SpriteListBox.ListBox.Push += new GuiMessage(spriteListBoxClick);
            this.SpriteListBox.ListBox.DoubleClick += new GuiMessage(spriteListBoxDoubleClick);
            this.SpriteListBox.Lined = true;
            this.AddWindow(SpriteListBox);
            SpriteListBox.ListBox.FocusUpdate += SpriteListFocusUpdate;
            
            SpriteListBox.ConsiderAttachments = true;
            
            #endregion

            #region gridListBox property setting

            SpriteGridListBox = new ListDisplayWindow(GuiManager.Cursor);
            SpriteGridListBox.DrawBorders = false;
            this.SpriteGridListBox.Visible = false;
            this.SpriteGridListBox.ListBox.Push += new GuiMessage(this.GridListBoxClick);
            this.SpriteGridListBox.ListBox.DoubleClick += new GuiMessage(this.messages.GridListBoxDoubleClick);
            SpriteGridListBox.ListBox.FocusUpdate += SpriteGridListFocusUpdate;
            AddWindow(SpriteGridListBox);
            #endregion

            #region SpriteFrameListBox

            SpriteFrameListBox = new ListDisplayWindow(GuiManager.Cursor);
            SpriteFrameListBox.DrawBorders = false;
			SpriteFrameListBox.ListBox.TakingInput = false; // allows input like delete, keyboard camera movement to fall through

            this.SpriteFrameListBox.Visible = false;
            this.SpriteFrameListBox.ListBox.Push += new GuiMessage(this.FrameListBoxClick);
            AddWindow(SpriteFrameListBox);
            SpriteFrameListBox.ConsiderAttachments = true;

            #endregion

            #region ModelListBox
            mModelListBox = new ListDisplayWindow(GuiManager.Cursor);
            mModelListBox.DrawBorders = false;
            this.mModelListBox.Visible = false;
            this.mModelListBox.ListBox.Push += new GuiMessage(this.ModelListBoxClick);
            this.AddWindow(ModelListBox);
            mModelListBox.ConsiderAttachments = true;
            #endregion

            #region TextListBox

            mTextListBox = new ListDisplayWindow(GuiManager.Cursor);
            mTextListBox.DrawBorders = false;
            mTextListBox.Visible = false;
            mTextListBox.ListBox.Push += TextListBoxClick;
            this.AddWindow(mTextListBox);
            mTextListBox.ConsiderAttachments = true;


            #endregion

            #region TextureListBox
            this.textureListBox = new CollapseListBox(mCursor);
            AddWindow(textureListBox);
            this.textureListBox.Visible = false;
            this.textureListBox.Highlight += new GuiMessage(TextureListBoxClick);
            this.textureListBox.FocusUpdate += TextureListBoxFocusUpdate;
            #endregion

            #region AnimationChain List Box

            mAnimationChainListWindow = new ListDisplayWindow(GuiManager.Cursor);
            mAnimationChainListWindow.DrawBorders = false;
            mAnimationChainListWindow.Visible = false;
            mAnimationChainListWindow.ListBox.Highlight += AnimationChainListBoxClick;
            this.AddWindow(mAnimationChainListWindow);

            #endregion

            this.PositionContainedElements(this);

            float buttonScale = 1.3f;

            #region Create the EditSprites ToggleButton

            this.editSprites = new ToggleButton(mCursor);
            AddWindow(editSprites);
            this.editSprites.ScaleX = this.editSprites.ScaleY = buttonScale;
            this.editSprites.SetPositionTL(buttonScale + .4f, 1.5f);
            this.editSprites.Text = "Edit Sprites";
            this.editSprites.SetOverlayTextures(3, 1);
            this.editSprites.Press();
            this.editSprites.Click += new GuiMessage(this.EditSpritesClick);

            #endregion

            #region Edit SpriteGrids

            this.editGrids = new ToggleButton(mCursor);
            AddWindow(editGrids);
            this.editGrids.ScaleX = this.editGrids.ScaleY = buttonScale;
            this.editGrids.SetPositionTL(3 * buttonScale + .4f, 1.5f);
            this.editGrids.Text = "Edit Grids";
            this.editGrids.SetOverlayTextures(4, 1);
            this.editSprites.AddToRadioGroup(this.editGrids);
            this.editGrids.Click += new GuiMessage(this.EditGridsClick);
            this.editGrids.SetOneAlwaysDown(true);

            #endregion

            #region Edit SpriteFrames toggle button

            this.editSpriteFrames = new ToggleButton(mCursor);
            AddWindow(editSpriteFrames);
            this.editSpriteFrames.ScaleX = this.editSpriteFrames.ScaleY = buttonScale;
            this.editSpriteFrames.SetPositionTL(5 * buttonScale + .4f, 1.5f);
            this.editSpriteFrames.Text = "Edit SpriteFrames";
            this.editSpriteFrames.SetOverlayTextures(0, 3);
            this.editSprites.AddToRadioGroup(this.editSpriteFrames);
            this.editSpriteFrames.Click += new GuiMessage(this.EditFramesClick);
            this.editSpriteFrames.SetOneAlwaysDown(true);

            #endregion

            #region editModels
            this.editModels = new ToggleButton(mCursor);
            AddWindow(editModels);
            this.editModels.ScaleX = this.editModels.ScaleY = buttonScale;
            this.editModels.SetPositionTL(7 * buttonScale + .4f, 1.5f);
            this.editModels.Text = "Edit Models";
            this.editModels.SetOverlayTextures(4, 3);
            this.editSprites.AddToRadioGroup(this.editModels);
            this.editModels.Click += new GuiMessage(this.EditModelsClick);
            this.editModels.SetOneAlwaysDown(true);
            #endregion

            #region EditTexts
            this.mEditTexts = new ToggleButton(mCursor);
            AddWindow(mEditTexts);
            this.mEditTexts.ScaleX = this.mEditTexts.ScaleY = buttonScale;
            this.mEditTexts.SetPositionTL(9 * buttonScale + .4f, 1.5f);
            this.mEditTexts.Text = "Edit Texts";
            this.mEditTexts.SetOverlayTextures(6, 3);
            this.editSprites.AddToRadioGroup(this.mEditTexts);
            this.mEditTexts.Click += new GuiMessage(this.EditTextsClick);
            this.mEditTexts.SetOneAlwaysDown(true);

            #endregion

            #region View Textures icon
            this.mViewTexturesButton = new Button(mCursor);
            AddWindow(mViewTexturesButton);
            this.mViewTexturesButton.ScaleX = this.mViewTexturesButton.ScaleY = buttonScale;
            this.mViewTexturesButton.SetPositionTL(11 * buttonScale + .4f + .7f, 1.5f);
            this.mViewTexturesButton.Text = "View Textures";
            this.mViewTexturesButton.SetOverlayTextures(2, 3);
            // no longer a ToggleButton and it shouldn't be part of the group
            //this.editSprites.AddToRadioGroup(this.editTextures);
            //this.editTextures.SetOneAlwaysDown(true);
            this.mViewTexturesButton.Click += new GuiMessage(this.EditTexturesClick);
            #endregion

            #region Vew AnimationChains icon
            this.mViewAnimationChainsButton = new Button(mCursor);
            AddWindow(mViewAnimationChainsButton);
            this.mViewAnimationChainsButton.ScaleX = this.mViewAnimationChainsButton.ScaleY = buttonScale;
            this.mViewAnimationChainsButton.SetPositionTL(13 * buttonScale + .4f + .7f, 1.5f);
            this.mViewAnimationChainsButton.Text = "View AnimationChains";
            this.mViewAnimationChainsButton.SetOverlayTextures(7, 3);
            // no longer a ToggleButton and it shouldn't be part of the group
            //this.editSprites.AddToRadioGroup(this.editTextures);
            //this.editTextures.SetOneAlwaysDown(true);
            this.mViewAnimationChainsButton.Click += new GuiMessage(this.ViewAnimationChainsClick);

            #endregion

            RefreshListsShown();
        }

        #endregion

        #region Public Methods

        public void Add(AnimationChainList animationChainArray)
        {
            if (animationChainArray != null)
            {
                foreach (AnimationChain animationChain in animationChainArray)
                {
                    foreach (AnimationFrame frame in animationChain)
                    {
                        this.textureListBox.AddItemUnique(FileManager.MakeRelative(frame.Texture.Name, FileManager.RelativeDirectory), frame.Texture);
                    }
                }
            }
        }

        public void Add(Texture2D texture)
        {
            if (texture != null)
            {
                this.textureListBox.AddItemUnique(FileManager.MakeRelative(texture.Name, FileManager.RelativeDirectory), texture);
            }
        }

        public void AddDisplayRegion(DisplayRegion displayRegion, Texture2D texture)
        {
            CollapseItem item = textureListBox.GetItem(texture);

            if (item != null)
            {
                item.AddItem(displayRegion.Name, displayRegion);
            }
        }

        public void ClearTextures()
        {
            this.textureListBox.Clear();
        }

        public List<DisplayRegion> CreateTextureReferences(CollapseItem item)
        {
            List<DisplayRegion> toReturn = new List<DisplayRegion>();

            for (int i = 0; i < item.Count; i++)
            {
                toReturn.Add(item[i].ReferenceObject as DisplayRegion);
            }

            return toReturn;
        }

        public void DeselectObject(object objectToDeselect)
        {
            if (objectToDeselect is SpriteFrame)
            {
                this.SpriteFrameListBox.DeselectObject(objectToDeselect);
            }
            else if (objectToDeselect is Sprite)
            {
                this.SpriteListBox.DeselectObject(objectToDeselect);
            }
            else if (objectToDeselect is PositionedModel)
            {
                this.mModelListBox.DeselectObject(objectToDeselect);
            }
            else if(objectToDeselect is Text)
            {
                this.mTextListBox.DeselectObject(objectToDeselect);
            }
        }

        public List<Texture2D> GetTextures()
        {
            List<Texture2D> textures = new List<Texture2D>();
            foreach (CollapseItem item in this.textureListBox.Items)
            {
                textures.Add(item.ReferenceObject as Texture2D);
            }
            return textures;
        }

        public void GridListBoxClick(Window callingWindow)
        {
            SpriteGrid tempGridGrabbed = this.SpriteGridListBox.GetFirstHighlightedObject() as SpriteGrid;
            GameData.sesgMan.ClickGrid(tempGridGrabbed, null);
            if (tempGridGrabbed != null)
            {
                SESpriteGridManager.CurrentSpriteGrid = tempGridGrabbed;

                SpriteEditorSettings.EditingSpriteGrids = true;
            }
        }

        public void Highlight(object objectToHighlight)
        {
            if (objectToHighlight is Texture2D)
            {
                this.textureListBox.HighlightObject(objectToHighlight, false);
                EditingTextures = true;
            }
            else if (objectToHighlight is FloatRectangle)
            {
                this.textureListBox.HighlightObject(objectToHighlight, false);
                EditingTextures = true;
            }
            else if (objectToHighlight is AnimationChainList)
            {
                this.mAnimationChainListWindow.HighlightObject(objectToHighlight, false);
                ViewingAnimationChains = true;
            }
        }

        public void HighlightAnimationChainListByName(string nameOfAnimationChainList)
        {
            foreach (AnimationChainList list in GameData.AnimationChains)
            {
                if (list.Name == nameOfAnimationChainList)
                {
                    mAnimationChainListWindow.HighlightObject(list, false);
                    return;
                }
            }

            // If we got here then nothing has been found so highlight nothing
            mAnimationChainListWindow.HighlightObject(null, false);

        }

        public void RefreshListsShown()
        {
            SpriteFrameListBox.ListShowing = GameData.Scene.SpriteFrames;
            mModelListBox.ListShowing = GameData.Scene.PositionedModels;
            mTextListBox.ListShowing = GameData.Scene.Texts;
            this.SpriteListBox.ListShowing = GameData.Scene.Sprites;
            SpriteGridListBox.ListShowing = GameData.Scene.SpriteGrids;
            mAnimationChainListWindow.ListShowing = GameData.AnimationChains;
        }

        public void Remove(Texture2D texture)
        {
            this.textureListBox.RemoveItemByObject(texture);
        }

        public void Remove(DisplayRegion displayRegion)
        {
            this.TextureListBox.RemoveItemByObject(displayRegion);
        }

        public void ReplaceTexture(Texture2D oldTexture, Texture2D newTexture)
        {
            CollapseItem collapseItem = textureListBox.GetItem(oldTexture);

            if (collapseItem != null)
            {
                collapseItem.ReferenceObject = newTexture;
                collapseItem.Text = newTexture.Name;
            }

        }

        public void Update()
        {
            #region Update SpriteListBox
            if (SpriteListBox.Visible)
            {
                SpriteListBox.UpdateToList();

                if (SpriteListBox.AreHighlightsMatching(GameData.EditorLogic.CurrentSprites) == false)
                {
                    SpriteListBox.HighlightItem(null, false);
                    foreach (Sprite sprite in GameData.EditorLogic.CurrentSprites)
                    {
                        SpriteListBox.HighlightObject(sprite, true);
                    }
                }
            }
            #endregion

            #region Update ModelListBox

            if (mModelListBox.Visible)
            {
                mModelListBox.UpdateToList();

                if (mModelListBox.AreHighlightsMatching(GameData.EditorLogic.CurrentPositionedModels) == false)
                {
                    mModelListBox.HighlightItem(null, false);
                    foreach (PositionedModel model in GameData.EditorLogic.CurrentPositionedModels)
                    {
                        mModelListBox.HighlightObject(model, true);
                    }
                }
            }
            #endregion

            #region Update TextListBox

            if (mTextListBox.Visible)
            {
                mTextListBox.UpdateToList();

                if (mTextListBox.AreHighlightsMatching(GameData.EditorLogic.CurrentTexts) == false)
                {
                    mTextListBox.HighlightItem(null, false);
                    foreach (Text text in GameData.EditorLogic.CurrentTexts)
                    {
                        mTextListBox.HighlightObject(text, true);
                    }
                }
            }

            #endregion

            #region Update SpriteFrameListBox

            if (SpriteFrameListBox.Visible)
            {
                SpriteFrameListBox.UpdateToList();

                if (SpriteFrameListBox.AreHighlightsMatching(GameData.EditorLogic.CurrentSpriteFrames) == false)
                {
                    SpriteFrameListBox.HighlightItem(null, false);
                    foreach (SpriteFrame spriteFrame in GameData.EditorLogic.CurrentSpriteFrames)
                    {
                        SpriteFrameListBox.HighlightObject(spriteFrame, true);
                    }
                }
            }

            #endregion

            #region Update SpriteGridListBox

            if (SpriteGridListBox.Visible)
            {
                SpriteGridListBox.UpdateToList();

                if (SpriteGridListBox.GetFirstHighlightedObject() != SESpriteGridManager.CurrentSpriteGrid)
                {
                    // Update this if multiple SpriteGrids can ever be selected at once.
                    SpriteGridListBox.HighlightObject(SESpriteGridManager.CurrentSpriteGrid, false);
                }
            }

            #endregion

            #region Update AnimationChainListBox

            if (mAnimationChainListWindow.Visible)
            {
                mAnimationChainListWindow.UpdateToList();
            }

            #endregion
        }

        public void UpdateItemName<T>(T objectReference) where T : IAttachable
        {
            if (objectReference is Texture2D)
            {
                this.textureListBox.GetItem(objectReference).Text = objectReference.Name;
            }
        }


        #endregion

        #region Private Methods

        private void MakeAllListBoxesInvisible()
        {

            this.SpriteListBox.Visible = false;
            this.SpriteGridListBox.Visible = false;
            this.textureListBox.Visible = false;
            this.SpriteFrameListBox.Visible = false;
            this.mModelListBox.Visible = false;
            this.mTextListBox.Visible = false;
            this.mAnimationChainListWindow.Visible = false;
        }

        #endregion

        #endregion

    }
}
