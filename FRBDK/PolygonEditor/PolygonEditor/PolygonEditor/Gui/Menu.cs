using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;

using FlatRedBall.Content;
using FlatRedBall.Content.Polygon;

using FlatRedBall.Graphics;

using FlatRedBall.Gui;

using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;

using PolygonEditor;
using FlatRedBall.Content.Math.Geometry;
using FlatRedBall.IO;
using EditorObjects.Gui;

using EditorObjects.EditorSettings;
using FlatRedBall.Content.Scene;

#if FRB_MDX
using Microsoft.DirectX;
#else
using Microsoft.Xna.Framework;
using PolygonEditorXna.IO;
#endif

namespace PolygonEditor.Gui
{
    public class Menu : MenuStrip
    {
        #region Fields

        MenuItem mFlipHorizontally;
        MenuItem mFlipVertically;

        #endregion

        #region Event Methods

        #region Add

        void AddAxisAlignedCube(Window callingWindow)
        {
            EditorData.EditingLogic.AddAxisAlignedCube();
        }
        
        void AddCapsule2D(Window callingWindow)
        {
            EditorData.EditingLogic.AddCapsule2D();
        }

        void AddRectanglePolygon(Window callingWindow)
        {
            EditorData.EditingLogic.AddRectanglePolygon();
        }

        void AddAxisAlignedRectangle(Window callingWindow)
        {
            EditorData.EditingLogic.AddAxisAlignedRectangle();
        }

        void AddCircle(Window callingWindow)
        {
            EditorData.EditingLogic.AddCircle();
        }

        void AddSphere(Window callingWindow)
        {
            EditorData.EditingLogic.AddSphere();
        }

        #endregion

        #region Action
        void ScaleAllPolygons(Window callingWindow)
        {
            Window polyScaleWindow = GuiManager.AddWindow();
            polyScaleWindow.Name = "Scale All Polygons";
            polyScaleWindow.ScaleX = 10f;
            polyScaleWindow.ScaleY = 4f;
            polyScaleWindow.HasMoveBar = true;
            polyScaleWindow.HasCloseButton = true;

            UpDown scaleBox = new UpDown(mCursor);
            polyScaleWindow.AddWindow(scaleBox);
            scaleBox.Name = "scale";
            scaleBox.CurrentValue = 1f;
            scaleBox.ScaleX = 9f;
            scaleBox.X = 10f;
            scaleBox.Y = Window.MoveBarHeight;

            Button okButton = new Button(mCursor);
            polyScaleWindow.AddWindow(okButton);
            okButton.Text = "Set Scale";
            okButton.ScaleX = 9f;
            okButton.X = 10f;
            okButton.Y = scaleBox.Y + scaleBox.ScaleY * 2f;
            okButton.Click += ScalePolygonsOK;
        }

        void ScalePolygonsOK(Window callingWindow)
        {
            float scaleValue = 1f;
            UpDown scaleBox = ((Window)callingWindow.Parent).FindByName("scale") as UpDown;
            scaleValue = scaleBox.CurrentValue;

            foreach (Polygon poly in EditorData.Polygons)
            {
                poly.ScaleBy(scaleValue);
                poly.Position *= scaleValue;
            }

            callingWindow.Parent.CloseWindow();
        }

        void ShiftAllPolygons(Window callingWindow)
        {
            Vector3OkWindow v3ok = new Vector3OkWindow(GuiManager.Cursor);
            GuiManager.AddWindow(v3ok);

            v3ok.OkClick += ShiftAllPolygonsOk;
        }

        void ShiftAllPolygonsOk(Window callingWindow)
        {
            Vector3OkWindow v3ok = callingWindow as Vector3OkWindow;

            Vector3 amountToShiftBy = v3ok.Vector3Value;

            EditorData.ShapeCollection.Shift(amountToShiftBy);       
        }

        void FlipHorizontallyClick(Window callingWindow)
        {
            foreach (Polygon polygon in EditorData.EditingLogic.CurrentPolygons)
            {
                for (int i = 0; i < polygon.Points.Count; i++)
                {
                    polygon.SetPoint(i, -polygon.Points[i].X, polygon.Points[i].Y);

                }
            }
        }

        void FlipVerticallyClick(Window callingWindow)
        {
            foreach (Polygon polygon in EditorData.EditingLogic.CurrentPolygons)
            {
                for (int i = 0; i < polygon.Points.Count; i++)
                {
                    polygon.SetPoint(i, polygon.Points[i].X, -polygon.Points[i].Y);

                }
            }
        }

        private void UnloadScene(Window callingWindow)
        {
            EditorData.UnloadScene();
        }

        #endregion

        #region File

        public static void NewScene(Window callingWindow)
        {
            EditorData.DeleteForNew();
        }

        public static void PromptNew(Window callingWindow)
        {
            OkCancelWindow toNew = GuiManager.AddOkCancelWindow();
            toNew.Name = "New";
            toNew.Message = "Any unsaved data will be lost. Continue?";
            toNew.ScaleX = 9.4f;
            toNew.ScaleY = 5;
            toNew.OkClick += new GuiMessage(NewScene);
            toNew.HasMoveBar = true;
            GuiManager.AddDominantWindow(toNew);
        }

        void LoadSceneOk(Window callingWindow)
        {
            string fileName = ((FileWindow)callingWindow).Results[0];

            EditorData.LoadScene(fileName);
        }

        void LoadPolygonListOk(Window callingWindow)
        {
            string fileName = ((FileWindow)callingWindow).Results[0];

            EditorData.LoadPolygonList(fileName);
        }

        void LoadShapeListOk(Window callingWindow)
        {
            string fileName = ((FileWindow)callingWindow).Results[0];

            FileLoader.LoadShapeCollectionAskReplaceOrInsert(fileName);
        }

        void OpenFileWindowLoadScene(Window callingWindow)
        {
            FileWindow fileWindow = GuiManager.AddFileWindow();

            fileWindow.SetFileType("scnx");

            fileWindow.OkClick += LoadSceneOk;

			if (!string.IsNullOrEmpty(EditorData.LastLoadedShapeCollection))
			{
				fileWindow.SetDirectory(FileManager.GetDirectory(EditorData.LastLoadedShapeCollection));

			}
        }

        void OpenFileWindowLoadPolygonList(Window callingWindow)
        {
            FileWindow fileWindow = GuiManager.AddFileWindow();

            fileWindow.SetFileType("plylstx");

            fileWindow.OkClick += LoadPolygonListOk;

			if (!string.IsNullOrEmpty(EditorData.LastLoadedShapeCollection))
			{
				fileWindow.SetDirectory(FileManager.GetDirectory(EditorData.LastLoadedShapeCollection));

			}
        }

        void OpenFileWindowLoadShapeList(Window callingWindow)
        {
            FileWindow fileWindow = GuiManager.AddFileWindow();

            fileWindow.SetFileType("shcx");

            fileWindow.OkClick += LoadShapeListOk;

			if (!string.IsNullOrEmpty(EditorData.LastLoadedShapeCollection))
			{
				fileWindow.SetDirectory(FileManager.GetDirectory(EditorData.LastLoadedShapeCollection));

			}
        }        

        void OpenFileWindowSavePolygonList(Window callingWindow)
        {
            FileWindow fileWindow = GuiManager.AddFileWindow();
            fileWindow.SetToSave();
            fileWindow.SetFileType("plylstx");

			if (!string.IsNullOrEmpty(EditorData.LastLoadedPolygonList))
			{
				fileWindow.SetDirectory(FileManager.GetDirectory(EditorData.LastLoadedPolygonList));
				fileWindow.saveNameText = FileManager.RemoveExtension(FileManager.RemovePath(EditorData.LastLoadedPolygonList));

			}

            fileWindow.OkClick += SavePolygonListOk;

        }

        void OpenFileWindowSaveShapeList(Window callingWindow)
        {
            FileWindow fileWindow = GuiManager.AddFileWindow();
            fileWindow.SetToSave();
            fileWindow.SetFileType("shcx");

			if (!string.IsNullOrEmpty(EditorData.LastLoadedShapeCollection))
			{
				fileWindow.SetDirectory(FileManager.GetDirectory(EditorData.LastLoadedShapeCollection));
				fileWindow.saveNameText = FileManager.RemoveExtension(FileManager.RemovePath(EditorData.LastLoadedShapeCollection));

			}

            fileWindow.OkClick += SaveShapeListOk;
        }

        void SavePolygonListOk(Window callingWindow)
        {
            string fileName = ((FileWindow)callingWindow).Results[0];

            PolygonSaveList polygonSaveList = new PolygonSaveList();
            polygonSaveList.AddPolygonList(EditorData.Polygons);
            polygonSaveList.Save(fileName);

#if FRB_MDX
            GameForm.TitleText = "PolygonEditor - Editing " + fileName;
#else
            FlatRedBallServices.Game.Window.Title = "PolygonEditor Editing - " + fileName;
#endif

            fileName = FileManager.RemoveExtension(fileName);
            fileName += ".pesix";
            PolygonEditorSettings savedInformation = new PolygonEditorSettings(EditorData.LineGrid);
            savedInformation.Save(fileName);


        }

        void SaveSceneClick(Window callingWindow)
        {
            // See if the shape collection is empty.  If it's not, the user
            // might be trying to save a .scnx file which will include the shapes.
            // Just to amek sure they don't make that mistake, give them a warning.
            if (!EditorData.ShapeCollection.IsEmpty)
            {
                OkCancelWindow ocw = GuiManager.ShowOkCancelWindow(
                    "You are attempting to save a Scene.  Scenes cannot save any shape information (polygons, circles, etc)." + 
                    "  If you are trying to save shape information, save either a Polygon List (plylstx) or Shape Collection (shcx).", "Warning");
                ocw.HasMoveBar = true;
                ocw.OkClick += ShowFileWindowToSaveScene;

            }
            else
            {
                ShowFileWindowToSaveScene(null);
            }
        }

        void ShowFileWindowToSaveScene(Window callingWindow)
        {
            FileWindow fileWindow = GuiManager.AddFileWindow();

            fileWindow.SetFileType("scnx");
            fileWindow.SetToSave();

            fileWindow.OkClick += SaveSceneOk;
        }

        void SaveSceneOk(Window callingWindow)
        {
            string fileName = ((FileWindow)callingWindow).Results[0];

            EditorData.SaveScene(fileName);

        }

        void SaveShapeListOk(Window callingWindow)
        {
            string fileName = ((FileWindow)callingWindow).Results[0];

            ShapeCollectionSave shapeSaveList = new ShapeCollectionSave();

            shapeSaveList.AddPolygonList(EditorData.Polygons);
            shapeSaveList.AddAxisAlignedRectangleList(EditorData.AxisAlignedRectangles);
            shapeSaveList.AddCircleList(EditorData.Circles);
            shapeSaveList.AddAxisAlignedCubeList(EditorData.AxisAlignedCubes);
            shapeSaveList.AddSphereList(EditorData.Spheres);
            shapeSaveList.Save(fileName);

#if FRB_MDX
            GameForm.TitleText = "PolygonEditor - Editing " + fileName;
#else 
            FlatRedBallServices.Game.Window.Title = "PolygonEditor Editing - " + fileName;
#endif

            fileName = FileManager.RemoveExtension(fileName);
            fileName += ".pesix";
            PolygonEditorSettings savedInformation = new PolygonEditorSettings(EditorData.LineGrid);
			savedInformation.BoundsCameraSave = CameraSave.FromCamera(EditorData.BoundsCamera);
			savedInformation.UsePixelCoordinates = SpriteManager.Camera.Orthogonal;
            savedInformation.Save(fileName);


        }

        #endregion

        #region Window

        private void ShowEditorCameraProperties(Window callingWindow)
        {
            GuiData.CameraPropertyGrid.Visible = true;
            GuiManager.BringToFront(GuiData.CameraPropertyGrid);
        }

        private void ShowSceneCameraProperties(Window callingWindow)
        {
            GuiData.SceneCameraPropertyGrid.Visible = true;
        }

        private void ShowLineGridProperties(Window callingWindow)
        {
            GuiData.LineGridPropertyGrid.Visible = true;
            GuiManager.BringToFront(GuiData.LineGridPropertyGrid);
            //GuiData.LineGrid.Visible = true;
        }

        private void ShowEditorPropertiesWindow(Window callingWindow)
        {
            GuiData.EditorPropertiesGrid.Visible = true;
            GuiManager.BringToFront(GuiData.EditorPropertiesGrid);
        }

        private void ShowSceneProperties(Window callingWindow)
        {
            GuiData.ScenePropertyGrid.Visible = true;
            GuiManager.BringToFront(GuiData.ScenePropertyGrid);
        }

        private void ShowShapeCollectionProperties(Window callingWindow)
        {
            GuiData.ShapeCollectionPropertyGrid.Visible = true;
            GuiManager.BringToFront(GuiData.ShapeCollectionPropertyGrid);
        }

        #endregion

        #endregion

        #region Methods

        public Menu()
            : base(GuiManager.Cursor)
        {
            GuiManager.AddWindow(this);

            #region File

            MenuItem item = AddItem("File");

            item.AddItem("New").Click += PromptNew;
            item.AddItem("---------------");
            item.AddItem("Load Polygon List").Click += OpenFileWindowLoadPolygonList;
            item.AddItem("Load Shape Collection").Click += OpenFileWindowLoadShapeList;
            item.AddItem("Load Scene").Click += OpenFileWindowLoadScene;
            item.AddItem("---------------");
            item.AddItem("Save Polygon List").Click += OpenFileWindowSavePolygonList;
            item.AddItem("Save Shape Collection").Click += OpenFileWindowSaveShapeList;
            item.AddItem("Save Scene").Click += SaveSceneClick;
            #endregion

            #region Add
            item = AddItem("Add");

            item.AddItem("AxisAlignedRectangle").Click += AddAxisAlignedRectangle;
            item.AddItem("AxisAlignedCube").Click += AddAxisAlignedCube;
            item.AddItem("Capsule2D").Click += AddCapsule2D;
            item.AddItem("Circle").Click += AddCircle;
            item.AddItem("Polygon (Rectangle)").Click += AddRectanglePolygon;
            item.AddItem("Sphere").Click += AddSphere;
            #endregion

            #region Action
            item = AddItem("Action");
            item.AddItem("Scale All Polygons").Click += ScaleAllPolygons;

            mFlipHorizontally = item.AddItem("Flip Polygon Horizontally");
            mFlipHorizontally.Click += FlipHorizontallyClick;

            mFlipVertically = item.AddItem("Flip Polygon Vertically");
            mFlipVertically.Click += FlipVerticallyClick;

            item.AddItem("Unload Scene").Click += UnloadScene;
            #endregion

            #region Window
            item = AddItem("Window");
            item.AddItem("Editor Properties").Click += ShowEditorPropertiesWindow;
            item.AddItem("Line Grid Properties").Click += ShowLineGridProperties;
            item.AddItem("Bounds Properties").Click += ShowSceneCameraProperties;
            item.AddItem("Camera Properties").Click += ShowEditorCameraProperties;
            item.AddItem("Show Scene Properties").Click += ShowSceneProperties;
            item.AddItem("Show ShapeCollection Properties").Click += ShowShapeCollectionProperties;

            #endregion
        }

        public void Update()
        {
            mFlipHorizontally.Enabled = EditorData.EditingLogic.CurrentPolygons.Count != 0;
            mFlipVertically.Enabled = EditorData.EditingLogic.CurrentPolygons.Count != 0;
        }

        #endregion

    }
}
