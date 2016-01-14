using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Gui;

using FlatRedBall.AI.Pathfinding;

#if FRB_MDX
using Color = System.Drawing.Color;
#else
using Color = Microsoft.Xna.Framework.Graphics.Color;
#endif

using CameraPropertyGrid = EditorObjects.Gui.CameraPropertyGrid;
using EditorObjects.Gui;
using EditorObjects;

namespace AIEditor.Gui
{
    public static class GuiData
    {
        #region Fields

        static int mFramesSinceLastExpensiveGuiUpdate = 0;

        static Menu mMenuStrip;

        static CameraPropertyGrid mCameraPropertyGrid;
        
        static NodeNetworkPropertyGrid mNodeNetworkPropertyGrid;

        static ToolsWindow mToolsWindow;

        static CommandDisplay mCommandDisplay;

        static ScenePropertyGrid mScenePropertyGrid;

        static ShapeCollectionPropertyGrid mShapeCollectionPropertyGrid;

        public static EditorPropertiesGrid mEditorPropertiesGrid;

        #endregion

        #region Properties

        public static CameraPropertyGrid CameraPropertyGrid
        {
            get { return mCameraPropertyGrid; }
        }

        public static CommandDisplay CommandDisplay
        {
            get { return mCommandDisplay; }
        }

        public static EditorPropertiesGrid EditorPropertiesGrid
        {
            get { return mEditorPropertiesGrid; }
        }

        public static NodeNetworkPropertyGrid NodeNetworkPropertyGrid
        {
            get { return mNodeNetworkPropertyGrid; }
            set { mNodeNetworkPropertyGrid = value; }
        }

        public static ScenePropertyGrid ScenePropertyGrid
        {
            get { return mScenePropertyGrid; }
        }

        public static ShapeCollectionPropertyGrid ShapeCollectionPropertyGrid
        {
            get { return mShapeCollectionPropertyGrid; }
        }

        public static ToolsWindow ToolsWindow
        {
            get { return mToolsWindow; }
        }

        #endregion

        #region Events

        private static void CreateColorPropertyGrid(Window callingWindow)
        {
            ((PropertyGrid<Color>)callingWindow).ExcludeAllMembers();

            ((PropertyGrid<Color>)callingWindow).IncludeMember("A");

            ((PropertyGrid<Color>)callingWindow).IncludeMember("R");

            ((PropertyGrid<Color>)callingWindow).IncludeMember("G");

            ((PropertyGrid<Color>)callingWindow).IncludeMember("B");

            callingWindow.Y = 40;


        }

        private static void CreatePositionedNodePropertyGrid(Window callingWindow)
        {
            PropertyGrid<PositionedNode> asPropertyGrid = callingWindow as PropertyGrid<PositionedNode>;

            asPropertyGrid.ExcludeMember("CostToGetHere");
            asPropertyGrid.ExcludeMember("Links");
            asPropertyGrid.ExcludeMember("X");
            asPropertyGrid.ExcludeMember("Y");
            asPropertyGrid.ExcludeMember("Z");

            asPropertyGrid.Name = "Positioned Node";
        }

        
        #endregion

        #region Methods

        #region Public Methods

        public static void Initialize()
        {
            mMenuStrip = new Menu();

            mToolsWindow = new ToolsWindow();

            CreatePropertyGrids();



            mCommandDisplay = new CommandDisplay();

            CreateListDisplayWindows();

        }

        public static void Update()
        {

            if (EditorData.Scene != mScenePropertyGrid.SelectedObject)
            {
                mScenePropertyGrid.SelectedObject = EditorData.Scene;
            }

            mScenePropertyGrid.UpdateDisplayedProperties();

            mNodeNetworkPropertyGrid.Update();

            mCameraPropertyGrid.UpdateDisplayedProperties();

            // This can be slow.  We can speed it up by only doing it every X frames
            const int updateEveryXFrames = 30;
            mFramesSinceLastExpensiveGuiUpdate++;
            if (mFramesSinceLastExpensiveGuiUpdate >= updateEveryXFrames)
            {
                mNodeNetworkPropertyGrid.UpdateDisplayedProperties();
                mFramesSinceLastExpensiveGuiUpdate = 0;
            }

            #region Update the ShapeCollection PropertyGrid

            if (mShapeCollectionPropertyGrid.Visible)
            {
                if (mShapeCollectionPropertyGrid.SelectedObject != EditorData.ShapeCollection)
                {
                    mShapeCollectionPropertyGrid.SelectedObject = EditorData.ShapeCollection;
                }

                mShapeCollectionPropertyGrid.UpdateDisplayedProperties();


            }


            #endregion

        }

        #endregion

        #region Private Methods

        private static void CreateListDisplayWindows()
        {

        }

        private static void CreatePropertyGrids()
        {
            #region CamerPropertyGrid
            mCameraPropertyGrid = new CameraPropertyGrid(GuiManager.Cursor);
            GuiManager.AddWindow(mCameraPropertyGrid);
            mCameraPropertyGrid.SelectedObject = SpriteManager.Camera;
            mCameraPropertyGrid.X = mCameraPropertyGrid.ScaleX;
            mCameraPropertyGrid.Y = 40;
            mCameraPropertyGrid.HasCloseButton = true;
            mCameraPropertyGrid.UndoInstructions =
                UndoManager.Instructions;
            #endregion

            #region NodeNetwork PropertyGrid

            mNodeNetworkPropertyGrid = new NodeNetworkPropertyGrid();
            mNodeNetworkPropertyGrid.SelectedObject = EditorData.NodeNetwork;
            mNodeNetworkPropertyGrid.X = mNodeNetworkPropertyGrid.ScaleX;
            mNodeNetworkPropertyGrid.Y = 61;
            mNodeNetworkPropertyGrid.HasCloseButton = true;
            mNodeNetworkPropertyGrid.UndoInstructions =
                UndoManager.Instructions;

            #endregion

            #region ScenePropertyGrid

            mScenePropertyGrid = new ScenePropertyGrid(GuiManager.Cursor);
            GuiManager.AddWindow(mScenePropertyGrid);
            mScenePropertyGrid.X = mScenePropertyGrid.ScaleX;
            mScenePropertyGrid.Y = 75.7f;
            mScenePropertyGrid.ShowPropertyGridOnStrongSelect = true;
            mScenePropertyGrid.HasCloseButton = true;
            mScenePropertyGrid.Visible = false;
            mScenePropertyGrid.UndoInstructions = UndoManager.Instructions;
            #endregion

            #region ShapeCollectionPropertyGrid

            mShapeCollectionPropertyGrid = new ShapeCollectionPropertyGrid(GuiManager.Cursor);
            GuiManager.AddWindow(mShapeCollectionPropertyGrid);
            mShapeCollectionPropertyGrid.ShowPropertyGridOnStrongSelectAxisAlignedCube = true;
            mShapeCollectionPropertyGrid.ShowPropertyGridOnStrongSelectAxisAlignedRectangle = true;
            mShapeCollectionPropertyGrid.ShowPropertyGridOnStrongSelectCircle = true;
            mShapeCollectionPropertyGrid.ShowPropertyGridOnStrongSelectPolygon = true;
            mShapeCollectionPropertyGrid.ShowPropertyGridOnStrongSelectSphere = true;
            mShapeCollectionPropertyGrid.HasCloseButton = true;
            mShapeCollectionPropertyGrid.Visible = false;
            mShapeCollectionPropertyGrid.UndoInstructions = UndoManager.Instructions;
            #endregion

            PropertyGrid.SetNewWindowEvent<FlatRedBall.AI.Pathfinding.PositionedNode>(CreatePositionedNodePropertyGrid);
            PropertyGrid.SetNewWindowEvent<Color>(CreateColorPropertyGrid);

            #region EditorPropertiesGrid
            mEditorPropertiesGrid = new EditorPropertiesGrid();
            mEditorPropertiesGrid.Visible = false;
            #endregion
        }

        #endregion

        #endregion
    }
}
