using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;

using FlatRedBall.Gui;

using PolygonPropertyGrid = EditorObjects.Gui.PolygonPropertyGrid;
using CameraPropertyGrid = EditorObjects.Gui.CameraPropertyGrid;
using EditorObjects.Gui;
using EditorObjects;
using FlatRedBall.Math.Geometry;

#if FRB_XNA
using Keys = Microsoft.Xna.Framework.Input.Keys;
using FlatRedBall.Utilities;
#else
using Keys = Microsoft.DirectX.DirectInput.Key;
#endif


namespace PolygonEditor.Gui
{
    public static class GuiData
    {
        #region Fields

        static Menu mMenuStrip;
        static InfoBarWindow mInfoBar;
        static CameraPropertyGrid mCameraPropertyGrid;
        static CameraBoundsPropertyGrid mSceneCameraPropertyGrid;

        static ToolsWindow mToolsWindow;
        static GeometryWindow mGeometryWindow;

        static EditorObjects.Gui.LineGridPropertyGrid mLineGridPropertyGrid;
        static EditorObjects.Gui.ScenePropertyGrid mScenePropertyGrid;
        static EditorObjects.Gui.ShapeCollectionPropertyGrid mShapeCollectionPropertyGrid;

        private static EditorPropertiesGrid mEditorPropertiesGrid;

        #endregion

        #region Properties
        
        public static CameraBoundsPropertyGrid SceneCameraPropertyGrid
        {
            get { return mSceneCameraPropertyGrid; }
        }

        public static CameraPropertyGrid CameraPropertyGrid
        {
            get { return mCameraPropertyGrid; }
        }

        public static ToolsWindow ToolsWindow
        {
            get { return mToolsWindow; }
        }

        public static GeometryWindow GeometryWindow
        {
            get { return mGeometryWindow; }
        }

        public static EditorPropertiesGrid EditorPropertiesGrid
        {
            get { return mEditorPropertiesGrid; }
        }

        public static PropertyGrid<LineGrid> LineGridPropertyGrid
        {
            get { return mLineGridPropertyGrid; }
        }

        public static ScenePropertyGrid ScenePropertyGrid
        {
            get { return mScenePropertyGrid; }
        }

        public static ShapeCollectionPropertyGrid ShapeCollectionPropertyGrid
        {
            get { return mShapeCollectionPropertyGrid; }
        }

        #endregion

        #region Event Methods

        private static void SelectAxisAlignedRectangle(Window callingWindow)
        {
            EditorData.EditingLogic.SelectAxisAlignedRectangle(mShapeCollectionPropertyGrid.CurrentAxisAlignedRectangle);
        }

        private static void SelectAxisAlignedCube(Window callingWindow)
        {
            EditorData.EditingLogic.SelectAxisAlignedCube(mShapeCollectionPropertyGrid.CurrentAxisAlignedCube);
        }

        private static void SelectCircle(Window callingWindow)
        {
            EditorData.EditingLogic.SelectCircle(mShapeCollectionPropertyGrid.CurrentCircle);
        }

        private static void SelectSphere(Window callingWindow)
        {
            EditorData.EditingLogic.SelectSphere(mShapeCollectionPropertyGrid.CurrentSphere);
        }

        private static void SelectPolygon(Window callingWindow)
        {
            EditorData.EditingLogic.SelectPolygon(mShapeCollectionPropertyGrid.CurrentPolygon);
        }


        public static void MakeAxisAlignedRectangleNameUnique(Window callingWindow)
        {
            AxisAlignedRectangle rectangle = ((PropertyGrid<AxisAlignedRectangle>)callingWindow.Parent).SelectedObject;

            StringFunctions.MakeNameUnique<AxisAlignedRectangle>(rectangle, EditorData.ShapeCollection.AxisAlignedRectangles);
        }

        public static void MakeAxisAlignedCubeNameUnique(Window callingWindow)
        {
            AxisAlignedCube cube = ((PropertyGrid<AxisAlignedCube>)callingWindow.Parent).SelectedObject;

            StringFunctions.MakeNameUnique<AxisAlignedCube>(cube, EditorData.ShapeCollection.AxisAlignedCubes);
        }

        public static void MakeCircleNameUnique(Window callingWindow)
        {
            Circle circle = ((PropertyGrid<Circle>)callingWindow.Parent).SelectedObject;

            StringFunctions.MakeNameUnique<Circle>(circle, EditorData.ShapeCollection.Circles);
        }

        public static void MakeSphereNameUnique(Window callingWindow)
        {
            Sphere sphere = ((PropertyGrid<Sphere>)callingWindow.Parent).SelectedObject;

            StringFunctions.MakeNameUnique<Sphere>(sphere, EditorData.ShapeCollection.Spheres);
        }

        public static void MakePolygonNameUnique(Window callingWindow)
        {
            Polygon polygon = ((PropertyGrid<Polygon>)callingWindow.Parent).SelectedObject;

            StringFunctions.MakeNameUnique<Polygon>(polygon, EditorData.ShapeCollection.Polygons);
        }

        #endregion

        #region Methods

        #region Public Methods

        public static void Initialize()
        {
            mMenuStrip = new Menu();

            CreateInfoBar();

            mToolsWindow = new ToolsWindow();
            mToolsWindow.HasCloseButton = true;

            mGeometryWindow = new GeometryWindow();
            mGeometryWindow.HasCloseButton = true;

            CreatePropertyGrids();

            #region Scene PropertyGrid

            mScenePropertyGrid = new ScenePropertyGrid(GuiManager.Cursor);
            GuiManager.AddWindow(mScenePropertyGrid);
            mScenePropertyGrid.HasCloseButton = true;
            mScenePropertyGrid.Visible = false;
            mScenePropertyGrid.ShowPropertyGridOnStrongSelect = true;
            mScenePropertyGrid.UndoInstructions = UndoManager.Instructions;
            #endregion

            #region ShapeCollection PropertyGrid

            CreateShapeCollectionPropertyGrid();

            #endregion

            #region Set type/IObjectDisplayer associations

            PropertyGrid.SetPropertyGridTypeAssociation(typeof(Polygon), typeof(PolygonEditor.Gui.PolygonPropertyGrid));


            #endregion

            EditorData.EditingLogic.NodeNetworkEditorManager.AddNodeNetworkMenus(mMenuStrip);

			GuiManager.ObjectDisplayManager.NewWindowLimitation = NewWindowLimitation.ByRequestingWindow;
        }

        private static void CreatePropertyGrids()
        {
            #region Camera PropertyGrid
            mCameraPropertyGrid = new CameraPropertyGrid(GuiManager.Cursor);
            GuiManager.AddWindow(mCameraPropertyGrid);
            mCameraPropertyGrid.SelectedObject = SpriteManager.Camera;
            mCameraPropertyGrid.X = mCameraPropertyGrid.ScaleX;
            mCameraPropertyGrid.Y = 40;
            mCameraPropertyGrid.UndoInstructions = UndoManager.Instructions;
            mCameraPropertyGrid.Name = "Camera";
            mCameraPropertyGrid.Visible = false;
            #endregion

            #region Scene Camera PropertyGrid
            mSceneCameraPropertyGrid = new CameraBoundsPropertyGrid(EditorData.BoundsCamera);
            // This doesn't get added to the GuiManager because it was created before the pattern was established that PropertyGrids don't add themselves.
            mSceneCameraPropertyGrid.Visible = false;
            mSceneCameraPropertyGrid.UndoInstructions = UndoManager.Instructions;
            mSceneCameraPropertyGrid.Name = "Camera Bounds";
            #endregion

            #region EditorPropertiesGrid
            mEditorPropertiesGrid = new EditorPropertiesGrid();
            mEditorPropertiesGrid.Visible = false;
            #endregion

            #region LineGrid PropertyGrid
            mLineGridPropertyGrid = new LineGridPropertyGrid(EditorData.LineGrid);
            mLineGridPropertyGrid.Visible = false;

            #endregion
        }

        public static void Update()
        {
            mMenuStrip.Update();
            mCameraPropertyGrid.UpdateDisplayedProperties();
            mSceneCameraPropertyGrid.UpdateDisplayedProperties();
            
            mToolsWindow.Update();
            mGeometryWindow.Update();
            mInfoBar.Activity();

            #region Scene PropertyGrid Update
            if (mScenePropertyGrid.SelectedObject != EditorData.Scene)
            {
                mScenePropertyGrid.SelectedObject = EditorData.Scene;
            }
            else if (mScenePropertyGrid.SelectedObject != null)
            {
                mScenePropertyGrid.UpdateDisplayedProperties();
            }
            #endregion

            #region ShapeCollection PropertyGrid
            if (mShapeCollectionPropertyGrid.SelectedObject != EditorData.ShapeCollection)
            {
                mShapeCollectionPropertyGrid.SelectedObject = EditorData.ShapeCollection;
            }
            else if (mShapeCollectionPropertyGrid.SelectedObject != null)
            {
                mShapeCollectionPropertyGrid.UpdateDisplayedProperties();
            }
            #endregion
        }

        #endregion

        #region Private Methods

        private static void CreateInfoBar()
        {
            mInfoBar = new InfoBarWindow(GuiManager.Cursor);
            GuiManager.AddWindow(mInfoBar);
        }

        private static void CreateShapeCollectionPropertyGrid()
        {
            mShapeCollectionPropertyGrid = new ShapeCollectionPropertyGrid(GuiManager.Cursor);
            GuiManager.AddWindow(mShapeCollectionPropertyGrid);

            mShapeCollectionPropertyGrid.HasCloseButton = true;
            mShapeCollectionPropertyGrid.Visible = false;

            mShapeCollectionPropertyGrid.ShowPropertyGridOnStrongSelectAxisAlignedRectangle = true;
            mShapeCollectionPropertyGrid.ShowPropertyGridOnStrongSelectAxisAlignedCube = true;
            mShapeCollectionPropertyGrid.ShowPropertyGridOnStrongSelectCircle = true;
            mShapeCollectionPropertyGrid.ShowPropertyGridOnStrongSelectSphere = true;
            mShapeCollectionPropertyGrid.ShowPropertyGridOnStrongSelectPolygon = true;

            mShapeCollectionPropertyGrid.AxisAlignedCubeSelected += SelectAxisAlignedCube;
            mShapeCollectionPropertyGrid.AxisAlignedRectangleSelected += SelectAxisAlignedRectangle;
            mShapeCollectionPropertyGrid.CircleSelected += SelectCircle;
            mShapeCollectionPropertyGrid.SphereSelected += SelectSphere;
            mShapeCollectionPropertyGrid.PolygonSelected += SelectPolygon;

            #region Add ignored keys so that keyboard shortcuts still work here

            mShapeCollectionPropertyGrid.AddIgnoredKey(Keys.Delete);
            mShapeCollectionPropertyGrid.AddIgnoredKey(Keys.M);
            mShapeCollectionPropertyGrid.AddIgnoredKey(Keys.X);
            mShapeCollectionPropertyGrid.AddIgnoredKey(Keys.R);


            #endregion

            mShapeCollectionPropertyGrid.UndoInstructions = UndoManager.Instructions;

            mShapeCollectionPropertyGrid.X = mShapeCollectionPropertyGrid.ScaleX;
            mShapeCollectionPropertyGrid.Y = mShapeCollectionPropertyGrid.ScaleY + Window.MoveBarHeight +
                MenuStrip.MenuStripHeight;

        }

        #endregion

        #endregion
    }
}
