using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.Graphics;

using EditorObjects;
using EditorObjects.Gui;
using FlatRedBall.Graphics.Animation;
namespace SpriteEditor.Gui
{
    public static class GuiData
    {
        #region Fields
        private static Camera camera;
        public static CameraPropertyGrid mCameraPropertyGrid;

        public static FileButtonWindow fileButtonWindow;

        public static ListWindow ListWindow;
        //private static AttributesWindow mAttributesWindow;
        public static GuiMessages messages = new GuiMessages();

        private static TextureCoordinatesSelectionWindow mTextureCoordinateSelectionWindow;
 
        public static SpriteGridCreationPropertiesWindow spriteGridPropertiesWindow;
        public static SpriteRigSaveOptions srSaveOptions;
        public static ToolsWindow ToolsWindow;

        private static SpriteEditor.Gui.MenuStrip mMenuStrip;
        static InfoBarWindow mInfoBar;

        static EditorPropertiesGrid mEditorPropertiesGrid;

        static SpritePropertyGrid mSpritePropertyGrid;
        static SpriteGridPropertyGrid mSpriteGridPropertyGrid;
        static SpriteFramePropertyGrid mSpriteFramePropertyGrid;
        static PositionedModelPropertyGrid mPositionedModelPropertyGrid;
        static GraphicsOptionsPropertyGrid mGraphicsOptionsPropertyGrid;
        static TextPropertyGrid mTextPropertyGrid;
        static CameraBoundsPropertyGrid mCameraBoundsPropertyGrid;

        static WindowArray mVisibleToggleWindows = new WindowArray();
        static WindowArray mWindowsToMakeVisible = new WindowArray();
        #endregion

        #region Properties
        //public static AttributesWindow AttributesWindow
        //{
        //    get
        //    {
        //        return mAttributesWindow;
        //    }
        //}

        public static CameraBoundsPropertyGrid CameraBoundsPropertyGrid
        {
            get { return mCameraBoundsPropertyGrid; }
        }

        public static CameraPropertyGrid CameraPropertyGrid
        {
            get { return mCameraPropertyGrid; }
        }

        public static EditorPropertiesGrid EditorPropertiesGrid
        {
            get { return mEditorPropertiesGrid; }
        }

        public static MenuStrip MenuStrip
        {
            get { return mMenuStrip; }
        }

        public static SpriteGridPropertyGrid SpriteGridPropertyGrid
        {
            get { return mSpriteGridPropertyGrid; }
        }

        public static SpritePropertyGrid SpritePropertyGrid
        {
            get { return mSpritePropertyGrid; }
        }

        public static TextureCoordinatesSelectionWindow TextureCoordinatesSelectionWindow
        {
            get
            {
                return mTextureCoordinateSelectionWindow;
            }
        }




        public static SpriteFramePropertyGrid SpriteFramePropertyGrid
        {
            get { return mSpriteFramePropertyGrid; }
        }

        public static PositionedModelPropertyGrid PositionedModelPropertyGrid
        {
            get { return mPositionedModelPropertyGrid; }
        }

        public static TextPropertyGrid TextPropertyGrid
        {
            get { return mTextPropertyGrid; }
        }

        public static GraphicsOptionsPropertyGrid GraphicsOptionsPropertyGrid
        {
            get { return mGraphicsOptionsPropertyGrid; }

        }

        #endregion

        #region Delegate and Event Methods

        private static void ChangeTextureThroughPropertyGrid(Window callingWindow)
        {
            // If the texture is set through the PropertyGrid on a
            // Sprite that's part of a PropertyGrid, then the PropertyGrid
            // needs to be painted at the Sprite's position.
            if (GameData.EditorLogic.CurrentSpriteGrid != null)
            {
                // There's a SpriteGrid selected, so paint the
                // grid at the Sprite's position
                Sprite sprite = mSpritePropertyGrid.SelectedObject;

                GameData.EditorLogic.CurrentSpriteGrid.PaintSprite(
                    sprite.X, sprite.Y, sprite.Z, sprite.Texture);

            }
        }

        private static void CreateAnimationChainListDisplayWindow(Window callingWindow)
        {
            ListDisplayWindow listDisplayWindow = callingWindow as ListDisplayWindow;

            listDisplayWindow.ShowPropertyGridOnStrongSelect = false;

            listDisplayWindow.ListBox.StrongSelect += SetAnimationChainOnCurrentSprite;
        }

        private static void SetAnimationChainOnCurrentSprite(Window callingWindow)
        {
            ListBoxBase listBoxBase = callingWindow as ListBoxBase;

            AnimationChain animationChain = listBoxBase.GetFirstHighlightedObject() as AnimationChain;

            if (GameData.EditorLogic.CurrentSprites.Count != 0)
            {
                GameData.EditorLogic.CurrentSprites[0].SetAnimationChain(animationChain);
            }
        }

        #endregion

        #region Methods

        #region Public Methods

        private static void CreateInfoBar()
        {
            mInfoBar = new InfoBarWindow(GuiManager.Cursor);
            GuiManager.AddWindow(mInfoBar);
        }

        public static void Initialize()
        {

            Window.KeepWindowsInScreen = false;

            #region Assign engine references

            camera = GameForm.camera;

            #endregion


            fileButtonWindow = new FileButtonWindow(GuiManager.Cursor);

            ToolsWindow = new ToolsWindow();
            mVisibleToggleWindows.Add(ToolsWindow);
            
            ListWindow = new ListWindow(ToolsWindow);
            mVisibleToggleWindows.Add(ListWindow);

            //#region Create AttributesWindow
            //mAttributesWindow = new AttributesWindow(messages);
            //mAttributesWindow.SetPositionTL(84.5f, 25.8f);
            //mAttributesWindow.Visible = false;
            //GuiManager.AddWindow(mAttributesWindow);
            //#endregion

            spriteGridPropertiesWindow = new SpriteGridCreationPropertiesWindow(GuiManager.Cursor);
            srSaveOptions = new SpriteRigSaveOptions(messages, GuiManager.Cursor);

            #region TextureCoordinateSelectionWindow

            mTextureCoordinateSelectionWindow = new TextureCoordinatesSelectionWindow();
            GuiManager.AddWindow(mTextureCoordinateSelectionWindow);
            // set AddToListButtonShown to true before setting the visibility to false so that it updates correclty
            mTextureCoordinateSelectionWindow.AddToListButtonShown = true;
            mTextureCoordinateSelectionWindow.Visible = false;
            mTextureCoordinateSelectionWindow.HasCloseButton = true;
            mTextureCoordinateSelectionWindow.AddToListClickEventAdd(messages.AddDisplayRegion);
            mVisibleToggleWindows.Add(mTextureCoordinateSelectionWindow);

            #endregion

            #region MenuStrip

            mMenuStrip = new SpriteEditor.Gui.MenuStrip();
            mMenuStrip.SavedSuccess += new EventHandler(mMenuStrip_SavedSuccess);
            
            #endregion

            GameData.EditorLogic.NodeNetworkEditorManager.AddNodeNetworkMenus(MenuStrip);


            CreateInfoBar();

            CreatePropertyGrids();

            CreateListDisplayWindows();

            List<string> windowsToExclude = new List<string>();
            windowsToExclude.Add(mMenuStrip.Name);

            LayoutManager.LoadWindowLayout(windowsToExclude);

            Window.KeepWindowsInScreen = true;

        }

        static void mMenuStrip_SavedSuccess(object sender, EventArgs e)
        {
            mInfoBar.ResetSaveTime();
        }

        public static void ToggleWindowVisibility()
        {
            bool areAnyWindowsVisible = false;

            foreach (Window window in mVisibleToggleWindows)
            {
                if (window.Visible)
                {
                    areAnyWindowsVisible = true;
                    break;
                }
            }

            if (areAnyWindowsVisible)
            {
                mWindowsToMakeVisible.Clear();

                foreach (Window window in mVisibleToggleWindows)
                {
                    if (window.Visible)
                    {
                        window.Visible = false;
                        mWindowsToMakeVisible.Add(window);
                    }
                }
            }
            else
            {
                foreach(Window window in mWindowsToMakeVisible)
                {
                    window.Visible = true;
                }
            
            }
        }

        public static void Update()
        {
            ListWindow.Update();

            UpdatePropertyGrids();

            UpdateToolsWindow();
            mInfoBar.Activity();
        }

        #endregion

        #region Private Methods

        private static void CreateListDisplayWindows()
        {
            PropertyGrid.SetNewWindowEvent(typeof(AnimationChainList), CreateAnimationChainListDisplayWindow);
        }

        private static void CreatePropertyGrids()
        {
            PropertyGrid.SetPropertyGridTypeAssociation(typeof(Sprite), typeof(SpritePropertyGrid));

            #region EditorPropertiesGrid

            mEditorPropertiesGrid = new EditorPropertiesGrid();
            mEditorPropertiesGrid.Visible = false;
            mVisibleToggleWindows.Add(mEditorPropertiesGrid);
            #endregion

            #region SpriteFramePropertyGrid

            mSpriteFramePropertyGrid = new SpriteFramePropertyGrid(GuiManager.Cursor);
            GuiManager.AddWindow(mSpriteFramePropertyGrid);
            mSpriteFramePropertyGrid.Visible = false;
            mSpriteFramePropertyGrid.X = 19.3f;
            mSpriteFramePropertyGrid.Y = 61.2f;
            mSpriteFramePropertyGrid.ContentManagerName = GameData.SceneContentManager;
            mSpriteFramePropertyGrid.UndoInstructions = UndoManager.Instructions;

            mSpriteFramePropertyGrid.ShowWarningOnNonPowerOfTwoTexture = true;

            mVisibleToggleWindows.Add(mSpriteFramePropertyGrid);
            #endregion

            #region SpritePropertyGrid

            mSpritePropertyGrid = new SpritePropertyGrid(GuiManager.Cursor);
            GuiManager.AddWindow(mSpritePropertyGrid);
            mSpritePropertyGrid.Visible = false;
            mSpritePropertyGrid.X = 17.8f;
            mSpritePropertyGrid.Y = 61.2f;
            mSpritePropertyGrid.ContentManagerName = GameData.SceneContentManager;
            mSpritePropertyGrid.UndoInstructions = UndoManager.Instructions;
            mVisibleToggleWindows.Add(mSpritePropertyGrid);
            mSpritePropertyGrid.SetMemberChangeEvent("Texture", ChangeTextureThroughPropertyGrid);
            mSpritePropertyGrid.ShowWarningOnNonPowerOfTwoTexture = true;

			mSpritePropertyGrid.EnableSplittingSprite(GameData.Scene.Sprites);

            SpritePropertyGrid.ExtraCamerasForScale.Add(GameData.BoundsCamera);
            #endregion

            #region SpriteGridPropertyGrid

            mSpriteGridPropertyGrid = new SpriteGridPropertyGrid(GuiManager.Cursor);
            GuiManager.AddWindow(mSpriteGridPropertyGrid);
            mSpriteGridPropertyGrid.Visible = false;
            mSpriteGridPropertyGrid.X = 17.8f;
            mSpriteGridPropertyGrid.Y = 61.2f;
            mSpriteGridPropertyGrid.ContentManagerName = GameData.SceneContentManager;
            mSpriteGridPropertyGrid.UndoInstructions = UndoManager.Instructions;
            mVisibleToggleWindows.Add(mSpriteGridPropertyGrid);


            #endregion

            #region PositionedModelPropertyGrid

            mPositionedModelPropertyGrid = new PositionedModelPropertyGrid(GuiManager.Cursor);
            GuiManager.AddWindow(mPositionedModelPropertyGrid);
            mPositionedModelPropertyGrid.Visible = false;
            mPositionedModelPropertyGrid.X = 17.8f;
            mPositionedModelPropertyGrid.Y = 61.2f;
            mPositionedModelPropertyGrid.ContentManagerName = GameData.SceneContentManager;
            mPositionedModelPropertyGrid.UndoInstructions = UndoManager.Instructions;
            mVisibleToggleWindows.Add(mPositionedModelPropertyGrid);
            #endregion

            #region TextPropertyGrid

            mTextPropertyGrid = new TextPropertyGrid(GuiManager.Cursor);
            GuiManager.AddWindow(mTextPropertyGrid);
            mTextPropertyGrid.Visible = false;
            mTextPropertyGrid.X = 17.8f;
            mTextPropertyGrid.Y = 61.2f;
            mTextPropertyGrid.ContentManagerName = GameData.SceneContentManager;
            mTextPropertyGrid.UndoInstructions = UndoManager.Instructions;
            mVisibleToggleWindows.Add(mTextPropertyGrid);
            TextPropertyGrid.ExtraCamerasForScale.Add(GameData.BoundsCamera);
            #endregion

            #region GraphicsOptionsPropertyGrid

            mGraphicsOptionsPropertyGrid = new GraphicsOptionsPropertyGrid(GuiManager.Cursor);
            GuiManager.AddWindow(mGraphicsOptionsPropertyGrid);
            mGraphicsOptionsPropertyGrid.Visible = false;
            mGraphicsOptionsPropertyGrid.X = 17.8f;
            mGraphicsOptionsPropertyGrid.Y = 61.2f;
            mGraphicsOptionsPropertyGrid.UndoInstructions = UndoManager.Instructions;
            mVisibleToggleWindows.Add(mGraphicsOptionsPropertyGrid);
            #endregion

            #region Camera bounds PropertyGrid

            mCameraBoundsPropertyGrid = new EditorObjects.Gui.CameraBoundsPropertyGrid(GameData.BoundsCamera);
            mCameraBoundsPropertyGrid.SelectedObject = GameData.BoundsCamera;
            mCameraBoundsPropertyGrid.Visible = false;
            mVisibleToggleWindows.Add(mCameraBoundsPropertyGrid);
            #endregion

            #region Camera PropertyGrid
            mCameraPropertyGrid = new CameraPropertyGrid(GuiManager.Cursor);
            GuiManager.AddWindow(mCameraPropertyGrid);
            mCameraPropertyGrid.MakeFieldOfViewAndAspectRatioReadOnly();
            mCameraPropertyGrid.ShowDestinationRectangle(false);
            mCameraPropertyGrid.X = mCameraPropertyGrid.ScaleX;
            mCameraPropertyGrid.Y = 61.2f;
            mVisibleToggleWindows.Add(mCameraPropertyGrid);
            #endregion
        }

        private static void UpdatePropertyGrids()
        {
            if (GameData.EditorLogic.CurrentSpriteFrames.Count == 0)
            {
                GuiData.SpriteFramePropertyGrid.Visible = false;
            }

            if(GameData.EditorLogic.CurrentSprites.Count == 0)
            {
                GuiData.SpritePropertyGrid.Visible = false;
            }

            if (SESpriteGridManager.CurrentSpriteGrid == null)
            {
                GuiData.SpriteGridPropertyGrid.Visible = false;
            }

            if (GameData.EditorLogic.CurrentPositionedModels.Count == 0)
            {
                GuiData.PositionedModelPropertyGrid.Visible = false;
            }

            if (GameData.EditorLogic.CurrentTexts.Count == 0)
            {
                GuiData.TextPropertyGrid.Visible = false;
            }

            if (GameData.EditorLogic.CurrentSpriteFrames.Count != 0)
            {
                GuiData.SpriteFramePropertyGrid.Visible = true;
                GuiData.SpriteFramePropertyGrid.SelectedObject =
                    GameData.EditorLogic.CurrentSpriteFrames[0];
            }
            else if (SESpriteGridManager.CurrentSpriteGrid != null)
            {
                GuiData.SpriteGridPropertyGrid.SelectedObject = SESpriteGridManager.CurrentSpriteGrid;
            }
            else if (GameData.EditorLogic.CurrentPositionedModels.Count != 0)
            {
                GuiData.PositionedModelPropertyGrid.SelectedObject =
                    GameData.EditorLogic.CurrentPositionedModels[0];
            }
            else if (GameData.EditorLogic.CurrentTexts.Count != 0)
            {
                GuiData.TextPropertyGrid.SelectedObject =
                    GameData.EditorLogic.CurrentTexts[0];
            }
            else if (GameData.EditorLogic.CurrentSprites.Count != 0)
            {
                GuiData.SpritePropertyGrid.Visible = true;
                GuiData.SpritePropertyGrid.SelectedObject =
                    GameData.EditorLogic.CurrentSprites[0];
            }

            if (mTextPropertyGrid.Visible)
            {
                mTextPropertyGrid.UpdateDisplayedProperties();
            }
            if (mCameraPropertyGrid.Visible)
            {
                mCameraPropertyGrid.SelectedObject = GameData.Camera;
            }
            if (mCameraBoundsPropertyGrid.Visible)
            {
                mCameraBoundsPropertyGrid.SelectedObject = GameData.BoundsCamera;
            }
            if (mEditorPropertiesGrid.Visible)
            {
                mEditorPropertiesGrid.UpdateDisplayedProperties();
            }
        }

        private static void UpdateToolsWindow()
        {
            if (ToolsWindow.attachSprite.IsPressed && 
                GameData.EditorLogic.CurrentPositionedModels.Count == 0 &&
                GameData.EditorLogic.CurrentSpriteFrames.Count == 0 &&
                GameData.EditorLogic.CurrentSprites.Count == 0 &&
                GameData.EditorLogic.CurrentTexts.Count == 0)
            {
                ToolsWindow.attachSprite.Unpress();
            }

        }
        #endregion

        #endregion
    }
}
