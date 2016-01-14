using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;

using FlatRedBall.Gui;

using FlatRedBall.Input;

using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;

using PolygonEditor.Gui;
using EditorObjects;
using FlatRedBall.IO;
using FlatRedBall.Graphics;
using FlatRedBall.Content;
using EditorObjects.EditorSettings;

namespace PolygonEditor
{
    public static class EditorData
    {
        #region Fields

        public const string ContentManagerName = "PolygonEditor Content Manager";

        static ShapeCollection mShapeCollection = new ShapeCollection();

        static LineGrid mLineGrid = new LineGrid();

        static Scene mScene;

        static EditingLogic mEditingLogic;

        public static EditorProperties EditorProperties = new EditorProperties();

#if FRB_XNA
        static FormMethods mFormMethods = new FormMethods();
#endif

        static Camera mSceneCamera;

        static Text mDebugText;

        #endregion

        #region Properties

        public static PositionedObjectList<AxisAlignedCube> AxisAlignedCubes
        {
            get { return ShapeCollection.AxisAlignedCubes; }
        }

        public static PositionedObjectList<Capsule2D> Capsule2Ds
        {
            get { return ShapeCollection.Capsule2Ds; }
        }

        public static PositionedObjectList<AxisAlignedRectangle> AxisAlignedRectangles
        {
              get { return ShapeCollection.AxisAlignedRectangles;}
        }
        
        public static PositionedObjectList<Circle> Circles
        {
            get {return ShapeCollection.Circles;}
        }

        public static PositionedObjectList<Sphere> Spheres
        {
              get { return ShapeCollection.Spheres;}
        }

        public static PositionedObjectList<Polygon> Polygons
        {
            get { return ShapeCollection.Polygons; }
        }

        public static LineGrid LineGrid
        {
            get { return mLineGrid; }
        }

		public static string LastLoadedShapeCollection
		{
			get;
			set;
		}

		public static string LastLoadedPolygonList
		{
			get;
			set;
		}

        public static EditingLogic EditingLogic
        {
            get { return mEditingLogic; }
        }

        public static Scene Scene
        {
            get { return mScene; }
            set 
            {

                mScene = value;

				mScene.AddToManagers();
            
            }
        }

        public static ShapeCollection ShapeCollection
        {
            get { return mShapeCollection; }
            //set { mShapeCollection = value; }
        }

        public static Camera BoundsCamera
        {
            get { return mSceneCamera; }
        }

        #endregion

        #region Methods

        #region Initialize

        public static void Initialize(ShapeCollection shapeCollectionEditing)
        {
            //ColorDisplay colorDisplay = new ColorDisplay(GuiManager.Cursor);
            //GuiManager.AddWindow(colorDisplay);

            ShapeManager.ShapeDrawingOrder = ShapeDrawingOrder.OverEverything;
            ShapeManager.UseZTestingWhenDrawing = false;

            //Increase the number of particles available
            SpriteManager.AutoIncrementParticleCountValue = 500;
            SpriteManager.MaxParticleCount = 5000;

            if (shapeCollectionEditing == null)
            {
                mShapeCollection = new ShapeCollection();
            }

            mSceneCamera = new Camera(FlatRedBallServices.GlobalContentManager);

            mEditingLogic = new EditingLogic();

            mDebugText = TextManager.AddText("Hello");
            mDebugText.Visible = false;
        }

        #endregion

        #region Public Methods

        public static void LoadPolygonList(string fileName)
        {
            FlatRedBall.Content.Polygon.PolygonSaveList psl = FlatRedBall.Content.Polygon.PolygonSaveList.FromFile(fileName);

            PositionedObjectList<Polygon> polygonList = psl.ToPolygonList();

            // At a later time may want to support Insert and Replace.  For now, do Replace
            while (Polygons.Count != 0)
            {
                ShapeManager.Remove(Polygons[0]);
            }

            foreach (Polygon polygon in polygonList)
            {
                ShapeManager.AddPolygon(polygon);
                ShapeCollection.Polygons.Add(polygon);
                polygon.Color = EditorProperties.PolygonColor;
            }

#if FRB_MDX
            GameForm.TitleText = "PolygonEditor - Editing " + fileName;
#else
            FlatRedBallServices.Game.Window.Title = "PolygonEditor Editing - " + fileName;
#endif

			LastLoadedShapeCollection = null;
			LastLoadedPolygonList = fileName;

            #region Load the SavedInformation if available

            fileName = FileManager.RemoveExtension(fileName) + ".pesix";
            if (System.IO.File.Exists(fileName))
            {
                try
                {

                    PolygonEditorSettings savedInformation = PolygonEditorSettings.FromFile(fileName);

                    if (savedInformation.LineGridSave != null)
                    {
                        savedInformation.LineGridSave.ToLineGrid(EditorData.LineGrid);
                    }

                    if (savedInformation.UsePixelCoordinates)
                    {
                        SpriteManager.Camera.UsePixelCoordinates(false);
                    }
                }
                catch
                {
                    GuiManager.ShowMessageBox(
                        "Could not load the settings file " + fileName + ".  \nThe data file was loaded with no problems",
                        "Error");
                }
            }
            #endregion
        }

        public static void LoadScene(string fileName)
        {
            if (mScene != null)
            {
                FlatRedBallServices.Unload(ContentManagerName);
                mScene.RemoveFromManagers();
            }

            FlatRedBall.Content.SpriteEditorScene ses = FlatRedBall.Content.SpriteEditorScene.FromFile(fileName);

            EditorData.Scene = ses.ToScene(EditorData.ContentManagerName);
        }

        public static void DeleteForNew()
        {
            #region Delete the referenced Polygons

            while (Polygons.Count != 0)
            {
                ShapeManager.Remove(Polygons[0]);
            }

            #endregion

            #region Delete the referenced AxisAlignedRectangles
            
            while (AxisAlignedRectangles.Count != 0)
            {
                ShapeManager.Remove(AxisAlignedRectangles[0]);
            }

            #endregion

            #region Delete the referenced AxisAlignedCubes

            while (AxisAlignedCubes.Count != 0)
            {
                ShapeManager.Remove(AxisAlignedCubes[0]);
            }

            #endregion

            #region Delete the referenced Circles

            while (Circles.Count != 0)
            {
                ShapeManager.Remove(Circles[Circles.Count - 1]);
            }

            #endregion

            #region Delete the referenced Spheres

            while (Spheres.Count != 0)
            {
                ShapeManager.Remove(Spheres[Spheres.Count - 1]);
            }

            #endregion   

            #region Hide the Property Grids
            EditingLogic.DeselectAll();
            #endregion

            UnloadScene();

			LastLoadedPolygonList = null;
			LastLoadedShapeCollection = null;

#if FRB_MDX
            GameForm.TitleText = "PolygonEditor - untitled file";
#else
            FlatRedBallServices.Game.Window.Title = "PolygonEditor Editing - untitled file";
#endif
        }

        public static void SaveScene(string fileName)
        {
            if (mScene == null)
            {
                GuiManager.ShowMessageBox("There's no scene to save.", "Error Saving");
            }
            else
            {
                SpriteEditorScene spriteEditorScene = SpriteEditorScene.FromScene(mScene);

                spriteEditorScene.Save(fileName);
            }
        }

        public static void UnloadScene()
        {
            #region Delete the Scene

            if (Scene != null)
            {
                Scene.RemoveFromManagers();
            }

            SpriteManager.Camera.X = 0f;
            SpriteManager.Camera.Y = 0f;
            SpriteManager.Camera.Z = -40f;

            #endregion

            #region Remove ContentManager
            FlatRedBallServices.Unload(EditorData.ContentManagerName);
            #endregion
        }

        public static void Update()
        {
            mEditingLogic.Update();

            PerformMouseCameraControl();

            UpdateUI();

            mDebugText.DisplayText = UndoManager.ToString();
        }

        #endregion

        #region Private Methods

        private static void PerformMouseCameraControl()
        {
            EditorObjects.CameraMethods.MouseCameraControl(SpriteManager.Camera);
        }

        private static void UpdateUI()
        {
            GuiData.Update();
        }

        #endregion

        #endregion

    }
}
