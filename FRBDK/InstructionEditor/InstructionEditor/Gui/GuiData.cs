using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Gui;
using EditorObjects.Gui;
using FlatRedBall.Graphics.Model;
using FlatRedBall.ManagedSpriteGroups;
using ToolTemplate;
using FlatRedBall.Graphics;
using FlatRedBall.Instructions;
using EditorObjects;



namespace InstructionEditor.Gui
{

    public static class GuiData
    {
        #region Fields

        private static InstructionEditor.Gui.ToolsWindow mToolsWindow;

        private static ListBoxWindow mListBoxWindow;

        static TimeControlWindow mTimeControlWindow;

        public static TimeLineWindow TimeLineWindow;

        static InstructionEditorMenuStrip mFileMenu;

        //static SpritePropertyGrid mSpritePropertyGrid;
        //static SpriteFramePropertyGrid mSpriteFramePropertyGrid;
        //static PositionedModelPropertyGrid mPositionedModelPropertyGrid;
        //static TextPropertyGrid mTextPropertyGrid;

        static CameraPropertyGrid mSceneCameraPropertyGrid;
        static CameraPropertyGrid mEditorCameraPropertyGrid;

        static InstructionListPropertyGrid mKeyframePropertyGrid;

        static UsedPropertySelectionWindow mUsedPropertySelectionWindow = new UsedPropertySelectionWindow();
        static ScenePropertyGrid mScenePropertyGrid;

        static PropertyGrid<EditorOptions> mEditorOptionsPropertyGrid;

        #endregion

        #region Properties

        public static CameraPropertyGrid EditorCameraPropertyGrid
        {
            get { return mEditorCameraPropertyGrid; }
        }

        public static PropertyGrid<EditorOptions> EditorOptionsPropertyGrid
        {
            get { return mEditorOptionsPropertyGrid; }
        }
            

        public static InstructionListPropertyGrid KeyframePropertyGrid
        {
            get { return mKeyframePropertyGrid; }
        }

        public static ListBoxWindow ListBoxWindow
        {
            get { return mListBoxWindow; }
        }

        //public static PositionedModelPropertyGrid PositionedModelPropertyGrid
        //{
        //    get { return mPositionedModelPropertyGrid; }
        //}

        public static CameraPropertyGrid SceneCameraPropertyGrid
        {
            get { return mSceneCameraPropertyGrid; }
        }

        public static ScenePropertyGrid ScenePropertyGrid
        {
            get { return mScenePropertyGrid; }
        }

        //public static SpriteFramePropertyGrid SpriteFramePropertyGrid
        //{
        //    get { return mSpriteFramePropertyGrid; }
        //    //set { mSpriteFramePropertyGrid = value; }
        //}

        //public static SpritePropertyGrid SpritePropertyGrid
        //{
        //    get { return mSpritePropertyGrid; }
        //}

        //public static TextPropertyGrid TextPropertyGrid
        //{
        //    get { return mTextPropertyGrid; }
        //}

        public static InstructionEditor.Gui.ToolsWindow ToolsWindow
        {
            get { return mToolsWindow; }
        }

        public static UsedPropertySelectionWindow UsedPropertySelectionWindow
        {
            get { return mUsedPropertySelectionWindow; }
        }

        #endregion

        #region Event Methods

        private static void PositionedModelHighlight(Window callingWindow)
        {
            EditorData.EditorLogic.SelectObject<PositionedModel>(mScenePropertyGrid.CurrentPositionedModel, EditorData.EditorLogic.CurrentPositionedModels);
        }

        private static void SpriteHighlight(Window callingWindow)
        {
            EditorData.EditorLogic.SelectObject<Sprite>(mScenePropertyGrid.CurrentSprite, EditorData.EditorLogic.CurrentSprites);
        }

        private static void SpriteFrameHighlight(Window callingWindow)
        {
            EditorData.EditorLogic.SelectObject<SpriteFrame>(mScenePropertyGrid.CurrentSpriteFrame, EditorData.EditorLogic.CurrentSpriteFrames);
        }

        private static void TextHighlight(Window callingWindow)
        {
            EditorData.EditorLogic.SelectObject<Text>(mScenePropertyGrid.CurrentText, EditorData.EditorLogic.CurrentTexts);
        }

        private static void UpdateCurrentKeyframe(Window callingWindow)
        {
            if (EditorData.EditorLogic.CurrentKeyframe != null)
            {
                double timeToExecute = TimeLineWindow.CurrentValue;

                if (EditorData.EditorLogic.CurrentKeyframe.Count > 0)
                {
                    timeToExecute = EditorData.EditorLogic.CurrentKeyframe[0].TimeToExecute;
                }

                InstructionList instructionList = new InstructionList();

                EditorData.AddInstructionsToList(instructionList, timeToExecute);


                EditorData.EditorLogic.CurrentKeyframe.Clear();
                EditorData.EditorLogic.CurrentKeyframe.AddRange(instructionList);
            }
        }

        #endregion
        
        #region Methods

        #region Constructor

        static GuiData()
        {

            #region initialize static members of message classes
            FileMenuMessages.camera = SpriteManager.Camera;

            TimeLineMessages.camera = SpriteManager.Camera;

            PropertyWindowMessages.camera = SpriteManager.Camera;

            ToolsMessages.camera = SpriteManager.Camera;


            #endregion

            CreateToolsWindow();

            mListBoxWindow = new ListBoxWindow();

            mFileMenu = new InstructionEditorMenuStrip();

            mTimeControlWindow = new TimeControlWindow();

            #region timeLineWindow
            TimeLineWindow = new TimeLineWindow(GuiManager.Cursor);


            #endregion

            CreatePropertyGrids();

        }

        #endregion

        #region Public Methods

        public static void Update()
        {
            TimeLineWindow.Update();

            UpdateListDisplays();

            UpdatePropertyGrids();

            mToolsWindow.ListenForShortcuts();
        }

        #endregion

        #region Private Methods

        private static void CreateToolsWindow()
        {
            mToolsWindow = new InstructionEditor.Gui.ToolsWindow();
            mToolsWindow.Y = 50;
            mToolsWindow.HasCloseButton = true;
        }

        private static void CreatePropertyGrids()
        {
            //mSpritePropertyGrid = new SpritePropertyGrid(GuiManager.Cursor);
            //GuiManager.AddWindow(mSpritePropertyGrid);
            //mSpritePropertyGrid.Visible = false;
            //mSpritePropertyGrid.X = mSpritePropertyGrid.ScaleX;
            //mSpritePropertyGrid.Y = 53.8381f;
            //mSpritePropertyGrid.UndoInstructions = UndoManager.Instructions;

            mSceneCameraPropertyGrid = new CameraPropertyGrid(GuiManager.Cursor);
            GuiManager.AddWindow(mSceneCameraPropertyGrid);
            mSceneCameraPropertyGrid.Visible = false;
            mSceneCameraPropertyGrid.SelectedObject = EditorData.SceneCamera;
            mSceneCameraPropertyGrid.Name = "Camera Bounds";
            mSceneCameraPropertyGrid.UndoInstructions = UndoManager.Instructions;


            //mSpriteFramePropertyGrid = new SpriteFramePropertyGrid(GuiManager.Cursor);
            //GuiManager.AddWindow(mSpriteFramePropertyGrid);
            //mSpriteFramePropertyGrid.Visible = false;
            //mSpriteFramePropertyGrid.UndoInstructions = UndoManager.Instructions;

            //mPositionedModelPropertyGrid = new PositionedModelPropertyGrid(GuiManager.Cursor);
            //GuiManager.AddWindow(mPositionedModelPropertyGrid);
            //mPositionedModelPropertyGrid.Visible = false;
            //mPositionedModelPropertyGrid.UndoInstructions = UndoManager.Instructions;

            //mTextPropertyGrid = new TextPropertyGrid(GuiManager.Cursor);
            //GuiManager.AddWindow(mTextPropertyGrid);
            //mTextPropertyGrid.Visible = false;
            //mTextPropertyGrid.X = mSpritePropertyGrid.ScaleX;
            //mTextPropertyGrid.Y = 53.8381f;
            //mTextPropertyGrid.UndoInstructions = UndoManager.Instructions;

            mUsedPropertySelectionWindow.Visible = false;

            mKeyframePropertyGrid = new InstructionListPropertyGrid(GuiManager.Cursor);
            GuiManager.AddWindow(mKeyframePropertyGrid);
            mKeyframePropertyGrid.ShowInstructionPropertyGridOnStrongSelect = true;
            mKeyframePropertyGrid.Name = "Keyframe Properties";
            mKeyframePropertyGrid.HasCloseButton = true;
            mKeyframePropertyGrid.OverwriteInstructionList += UpdateCurrentKeyframe;
            mKeyframePropertyGrid.UndoInstructions = UndoManager.Instructions;

            #region Scene Property Grid
            mScenePropertyGrid = new ScenePropertyGrid(GuiManager.Cursor);
            GuiManager.AddWindow(mScenePropertyGrid);
            mScenePropertyGrid.SpriteSelected += SpriteHighlight;
            mScenePropertyGrid.SpriteFrameSelected += SpriteFrameHighlight;
            mScenePropertyGrid.PositionedModelSelected += PositionedModelHighlight;
            mScenePropertyGrid.TextSelected += TextHighlight;
            mScenePropertyGrid.X = 17.23276f;
            mScenePropertyGrid.Y = 22.30255f;
            mScenePropertyGrid.HasCloseButton = true;
            mScenePropertyGrid.ShowPropertyGridOnStrongSelect = true;
            mScenePropertyGrid.UndoInstructions = UndoManager.Instructions;
            #endregion

            #region Editor Camera Property Grid
            mEditorCameraPropertyGrid = new CameraPropertyGrid(GuiManager.Cursor);
            GuiManager.AddWindow(mEditorCameraPropertyGrid);
            mEditorCameraPropertyGrid.Visible = false;
            mEditorCameraPropertyGrid.MakeFieldOfViewAndAspectRatioReadOnly();
            mEditorCameraPropertyGrid.SelectedObject = SpriteManager.Camera;
            mEditorCameraPropertyGrid.Name = "Editor Camera";
            mEditorCameraPropertyGrid.UndoInstructions = UndoManager.Instructions;
            #endregion

            #region EditorOptions Property Grid
            mEditorOptionsPropertyGrid = GuiManager.AddPropertyGrid<EditorOptions>();
            mEditorOptionsPropertyGrid.Visible = false;
            mEditorOptionsPropertyGrid.SelectedObject = EditorData.EditorOptions;
            mEditorOptionsPropertyGrid.HasCloseButton = true;
            mEditorOptionsPropertyGrid.UndoInstructions = UndoManager.Instructions;
            #endregion
        }

        private static void UpdateListDisplays()
        {
            mListBoxWindow.UpdateLists();

            if (EditorData.EditorLogic.CurrentKeyframeList != null && EditorData.EditorLogic.CurrentKeyframe != null)
            {
                CollapseItem highlightedItem = mListBoxWindow.InstructionSetListBox.GetFirstHighlightedItem();

                if (highlightedItem != null)
                {
                    CollapseItem parentItem = highlightedItem.TopParent;

                    parentItem.ReorderToMatchList(EditorData.EditorLogic.CurrentKeyframeList);
                }
            }
        }

        private static void UpdatePropertyGrids()
        {
            #region Update the ScenePropertyGrid

            if (mScenePropertyGrid.SelectedObject != EditorData.BlockingScene)
            {
                mScenePropertyGrid.SelectedObject = EditorData.BlockingScene;
            }
            else if(mScenePropertyGrid.SelectedObject != null)
            {
                mScenePropertyGrid.UpdateDisplayedProperties();
            }

            #region Update SpritePropertyGrid and Scene UI
            if (EditorData.EditorLogic.CurrentSprites.Count != 0)
            {
                mScenePropertyGrid.CurrentSprite = EditorData.EditorLogic.CurrentSprites[0];
            }
            else
            {
                mScenePropertyGrid.CurrentSprite = null;

            }
            #endregion

            #region Update SpriteFramePropertyGrid and Scene UI

            if (EditorData.EditorLogic.CurrentSpriteFrames.Count != 0)
            {
                mScenePropertyGrid.CurrentSpriteFrame = EditorData.EditorLogic.CurrentSpriteFrames[0];
            }
            else
            {
                mScenePropertyGrid.CurrentSpriteFrame = null;
            }

            #endregion

            #region Update the PositionedModelPropertyGrid and Scene UI

            if (EditorData.EditorLogic.CurrentPositionedModels.Count != 0)
            {
                mScenePropertyGrid.CurrentPositionedModel = EditorData.EditorLogic.CurrentPositionedModels[0];
            }
            else
            {
                mScenePropertyGrid.CurrentPositionedModel = null;
            }

            #endregion

            #region Update the Text and Scene UI

            if (EditorData.EditorLogic.CurrentTexts.Count != 0)
            {
                mScenePropertyGrid.CurrentText = EditorData.EditorLogic.CurrentTexts[0];
            }
            else
            {
                mScenePropertyGrid.CurrentText = null;
            }

            #endregion

            #endregion

            mKeyframePropertyGrid.UpdateDisplayedProperties();


            #region Update the SceneCameraPropertyGrid
            if (mSceneCameraPropertyGrid.Visible)
            {
                mSceneCameraPropertyGrid.UpdateDisplayedProperties();
            }
            #endregion

            #region Update the EditorCameraPropertyGrid
            if (mEditorCameraPropertyGrid.Visible)
            {
                mEditorCameraPropertyGrid.UpdateDisplayedProperties();
            }
            #endregion
        }

        #endregion

        #endregion
    }
}
