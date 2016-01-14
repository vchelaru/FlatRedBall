using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;

using FlatRedBall.Gui;

using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;

using EditorObjects;

using PolygonEditor.Gui;
using FlatRedBall.Utilities;
using FlatRedBall.Input;
using FlatRedBall.Graphics;

#if FRB_MDX

using Microsoft.DirectX;
#else
using Microsoft.Xna.Framework;
using Point = FlatRedBall.Math.Geometry.Point;
using Keys = Microsoft.Xna.Framework.Input.Keys;
using EditorObjects.NodeNetworks;
#endif

namespace PolygonEditor
{
    #region Enums
    public enum EditingState
    {
        None,
        AddingPointsOnEnd,
        AddingPoints

    }
    #endregion

    public class EditingLogic
    {
        #region Fields
        PositionedObject mObjectGrabbed;

        EditingState mEditingState;

        ShapeCollection mCurrentShapeCollection = new ShapeCollection();

        NodeNetworkEditorManager mNodeNetworkEditorManager;


        ReactiveHud mReactiveHud;

        #endregion

        #region Properties

        #region Private Properties

        Polygon PolygonGrabbed
        {
            get { return mObjectGrabbed as Polygon; }
        }

        AxisAlignedRectangle AxisAlignedRectangleGrabbed
        {
            get { return mObjectGrabbed as AxisAlignedRectangle; }
        }

        Circle CircleGrabbed
        {
            get { return mObjectGrabbed as Circle; }
        }

        Sphere SphereGrabbed
        {
            get { return mObjectGrabbed as Sphere; }
        }

        AxisAlignedCube AxisAlignedCubeGrabbed
        {
            get { return mObjectGrabbed as AxisAlignedCube; }
        }

        #endregion

        public EditingState EditingState
        {
            get { return mEditingState; }
            set { mEditingState = value; }
        }

        public PositionedObjectList<Polygon> CurrentPolygons
        {
            get {return CurrentShapeCollection.Polygons; }
        }

        public NodeNetworkEditorManager NodeNetworkEditorManager
        {
            get
            {
                return mNodeNetworkEditorManager;
            }
        }

        public ShapeCollection CurrentShapeCollection
        {
            get { return mCurrentShapeCollection; }
        }

        #endregion

        #region Methods

        #region Constructors

        public EditingLogic()
        {
            mReactiveHud = new ReactiveHud();

            mNodeNetworkEditorManager = new NodeNetworkEditorManager();

			PrepareUndoManager();
        }

		void PrepareUndoManager()
		{
			UndoManager.AddAxisAlignedCubePropertyComparer();
		}


        #endregion

        #region Public Methods

        public Polygon AddRectanglePolygon()
        {
            Polygon polygon = Polygon.CreateRectangle(1, 1);
            ShapeManager.AddPolygon(polygon);
            polygon.Color = EditorProperties.PolygonColor;

            polygon.X = SpriteManager.Camera.X;
            polygon.Y = SpriteManager.Camera.Y;

            float scale = (float)Math.Abs(
                18 / SpriteManager.Camera.PixelsPerUnitAt(0));

            polygon.ScaleBy(scale);

            EditorData.ShapeCollection.Polygons.Add(polygon);

            polygon.Name = "Polygon" + EditorData.Polygons.Count;

            StringFunctions.MakeNameUnique<Polygon>(polygon, EditorData.Polygons);

            return polygon;
        }


        public void AddAxisAlignedCube()
        {
            AxisAlignedCube cube = new AxisAlignedCube();
            ShapeManager.AddAxisAlignedCube(cube);
            cube.Color = EditorProperties.AxisAlignedCubeColor;

            cube.X = SpriteManager.Camera.X;
            cube.Y = SpriteManager.Camera.Y;

            float scale = (float)Math.Abs(
                18 / SpriteManager.Camera.PixelsPerUnitAt(0));

            cube.ScaleX = scale;
            cube.ScaleY = scale;
            cube.ScaleZ = scale;

            EditorData.ShapeCollection.AxisAlignedCubes.Add(cube);
                
            cube.Name = "Cube" + EditorData.AxisAlignedCubes.Count;

            StringFunctions.MakeNameUnique<AxisAlignedCube>(cube, EditorData.AxisAlignedCubes);
        }


        public void AddCapsule2D()
        {
            Capsule2D capsule2D = new Capsule2D();
            ShapeManager.AddCapsule2D(capsule2D);
            capsule2D.Color = EditorProperties.Capsule2DColor;

            capsule2D.X = SpriteManager.Camera.X;
            capsule2D.Y = SpriteManager.Camera.Y;

            EditorData.ShapeCollection.Capsule2Ds.Add(capsule2D);

            capsule2D.Name = "Capsule" + EditorData.Capsule2Ds.Count;

            StringFunctions.MakeNameUnique<Capsule2D>(capsule2D, EditorData.Capsule2Ds);

        }


        public void AddAxisAlignedRectangle()
        {
            AxisAlignedRectangle rectangle = new AxisAlignedRectangle();
            ShapeManager.AddAxisAlignedRectangle(rectangle);
            rectangle.Color = EditorProperties.AxisAlignedRectangleColor;

            rectangle.X = SpriteManager.Camera.X;
            rectangle.Y = SpriteManager.Camera.Y;

            float scale = (float)Math.Abs(
                18 / SpriteManager.Camera.PixelsPerUnitAt(0));

            rectangle.ScaleX = scale;
            rectangle.ScaleY = scale;
            
            EditorData.ShapeCollection.AxisAlignedRectangles.Add(rectangle);

            rectangle.Name = "AxisAlignedRectangle" + EditorData.AxisAlignedRectangles.Count;

            StringFunctions.MakeNameUnique<AxisAlignedRectangle>(rectangle, EditorData.AxisAlignedRectangles);

        }
        

        public void AddCircle()
        {
            Circle circle = new Circle();
            ShapeManager.AddCircle(circle);
            circle.Color = EditorProperties.CircleColor;

            circle.X = SpriteManager.Camera.X;
            circle.Y = SpriteManager.Camera.Y;

            float scale = (float)Math.Abs(
                18 / SpriteManager.Camera.PixelsPerUnitAt(0));

            circle.Radius = scale;

            EditorData.ShapeCollection.Circles.Add(circle);

            circle.Name = "Circle" + EditorData.Circles.Count;

            StringFunctions.MakeNameUnique<Circle>(circle, EditorData.Circles);
        }


        public void AddSphere()
        {
            Sphere sphere = new Sphere();
            ShapeManager.AddSphere(sphere);
            sphere.Color = EditorProperties.SphereColor;

            sphere.X = SpriteManager.Camera.X;
            sphere.Y = SpriteManager.Camera.Y;

            float scale = (float)Math.Abs(
                18 / SpriteManager.Camera.PixelsPerUnitAt(0));

            sphere.Radius = scale;

            EditorData.ShapeCollection.Spheres.Add(sphere);

            sphere.Name = "Sphere" + EditorData.Spheres.Count;

            StringFunctions.MakeNameUnique<Sphere>(sphere, EditorData.Spheres);
        }


        public void CopyCurrentAxisAlignedCubes()
        {
            foreach (AxisAlignedCube cube in CurrentShapeCollection.AxisAlignedCubes)
            {
                AxisAlignedCube newCube = cube.Clone<AxisAlignedCube>();

                ShapeManager.AddAxisAlignedCube(newCube);

                EditorData.ShapeCollection.AxisAlignedCubes.Add(newCube);

                FlatRedBall.Utilities.StringFunctions.MakeNameUnique<AxisAlignedCube>(newCube, EditorData.AxisAlignedCubes);
            }
        }


        public void CopyCurrentAxisAlignedRectangles()
        {
            foreach (AxisAlignedRectangle rectangle in CurrentShapeCollection.AxisAlignedRectangles)
            {
                AxisAlignedRectangle newRectangle = rectangle.Clone<AxisAlignedRectangle>();

                ShapeManager.AddAxisAlignedRectangle(newRectangle);

                EditorData.ShapeCollection.AxisAlignedRectangles.Add(newRectangle);

                FlatRedBall.Utilities.StringFunctions.MakeNameUnique<AxisAlignedRectangle>(newRectangle, EditorData.AxisAlignedRectangles);
            }
        }


        public void CopyCurrentPolygons()
        {
            foreach (Polygon polygon in CurrentShapeCollection.Polygons)
            {
                Polygon newPolygon = polygon.Clone<Polygon>();

                ShapeManager.AddPolygon(newPolygon);

                EditorData.ShapeCollection.Polygons.Add(newPolygon);

                FlatRedBall.Utilities.StringFunctions.MakeNameUnique<Polygon>(newPolygon, EditorData.Polygons);
            }
        }


        public void CopyCurrentCircles()
        {
            foreach (Circle circle in CurrentShapeCollection.Circles)
            {
                Circle newCircle = circle.Clone<Circle>();

                ShapeManager.AddCircle(newCircle);

                EditorData.ShapeCollection.Circles.Add(newCircle);

                StringFunctions.MakeNameUnique<Circle>(newCircle, EditorData.Circles);

            }
        }


        public void CopyCurrentSpheres()
        {
            foreach (Sphere sphere in CurrentShapeCollection.Spheres)
            {
                Sphere newSphere = sphere.Clone<Sphere>();

                ShapeManager.AddSphere(newSphere);

                EditorData.ShapeCollection.Spheres.Add(newSphere);

                StringFunctions.MakeNameUnique<Sphere>(newSphere, EditorData.Spheres);

            }
        }


        public void SelectAxisAlignedCube(AxisAlignedCube axisAlignedCube)
        {
            CurrentShapeCollection.AxisAlignedCubes.Clear();

            GuiData.ShapeCollectionPropertyGrid.CurrentAxisAlignedCube = axisAlignedCube;

            if (axisAlignedCube != null)
            {
                bool isNewWindow;
				PropertyGrid propertyGrid = (PropertyGrid)GuiManager.ObjectDisplayManager.GetObjectDisplayerForObject(axisAlignedCube, GuiData.ShapeCollectionPropertyGrid, null, out isNewWindow);

                if (isNewWindow)
                {
                    propertyGrid.SetMemberChangeEvent("Name", GuiData.MakeAxisAlignedCubeNameUnique);
                }

                CurrentShapeCollection.AxisAlignedCubes.Add(axisAlignedCube);
                SelectPolygon(null);
                SelectCircle(null);
                SelectAxisAlignedRectangle(null);
                SelectSphere(null);
            }
        }


        public void SelectAxisAlignedRectangle(AxisAlignedRectangle axisAlignedRectangle)
        {
            CurrentShapeCollection.AxisAlignedRectangles.Clear();

            GuiData.ShapeCollectionPropertyGrid.CurrentAxisAlignedRectangle = axisAlignedRectangle;

            if (axisAlignedRectangle != null)
            {
                bool isNewWindow;
                PropertyGrid propertyGrid = (PropertyGrid)GuiManager.ObjectDisplayManager.GetObjectDisplayerForObject(axisAlignedRectangle, GuiData.ShapeCollectionPropertyGrid, null, out isNewWindow);
                if (isNewWindow)
                {
                    propertyGrid.SetMemberChangeEvent("Name", GuiData.MakeAxisAlignedRectangleNameUnique);
                }

                CurrentShapeCollection.AxisAlignedRectangles.Add(axisAlignedRectangle);
                SelectPolygon(null);
                SelectCircle(null);
                SelectAxisAlignedCube(null);
                SelectSphere(null);
            }
        }


        public void SelectCircle(Circle circle)
        {
            CurrentShapeCollection.Circles.Clear();

            GuiData.ShapeCollectionPropertyGrid.CurrentCircle = circle;

            if (circle != null)
            {
                bool isNewWindow;
                PropertyGrid propertyGrid = (PropertyGrid)GuiManager.ObjectDisplayManager.GetObjectDisplayerForObject(circle, GuiData.ShapeCollectionPropertyGrid, null, out isNewWindow);
                if (isNewWindow)
                {
                    propertyGrid.SetMemberChangeEvent("Name", GuiData.MakeCircleNameUnique);
                }

                CurrentShapeCollection.Circles.Add(circle);
                SelectPolygon(null);
                SelectAxisAlignedRectangle(null);
                SelectAxisAlignedCube(null);
                SelectSphere(null);
            }
        }


        public void SelectSphere(Sphere sphere)
        {
            CurrentShapeCollection.Spheres.Clear();

            GuiData.ShapeCollectionPropertyGrid.CurrentSphere = sphere;

            if (sphere != null)
            {
                bool isNewWindow;
                PropertyGrid propertyGrid = (PropertyGrid)GuiManager.ObjectDisplayManager.GetObjectDisplayerForObject(sphere, GuiData.ShapeCollectionPropertyGrid, null, out isNewWindow);
                if (isNewWindow)
                {
                    propertyGrid.SetMemberChangeEvent("Name", GuiData.MakeSphereNameUnique);
                }

                CurrentShapeCollection.Spheres.Add(sphere);
                SelectPolygon(null);
                SelectAxisAlignedRectangle(null);
                SelectAxisAlignedCube(null);
                SelectCircle(null);
            }
        }


        public void SelectPolygon(Polygon polygon)
        {
            CurrentShapeCollection.Polygons.Clear();

            GuiData.ShapeCollectionPropertyGrid.CurrentPolygon = polygon;


            if (polygon != null)
            {
                bool isNewWindow;
                PropertyGrid propertyGrid = (PropertyGrid)GuiManager.ObjectDisplayManager.GetObjectDisplayerForObject(polygon, GuiData.ShapeCollectionPropertyGrid, null, out isNewWindow);
                if (isNewWindow)
                {
                    propertyGrid.SetMemberChangeEvent("Name", GuiData.MakePolygonNameUnique);
                }

                CurrentShapeCollection.Polygons.Add(polygon);
                SelectAxisAlignedRectangle(null);
                SelectCircle(null);
                SelectAxisAlignedCube(null);
                SelectSphere(null);
            }
        }


        public void SelectPolygonCorner(int index)
        {
            mReactiveHud.CornerIndexSelected = index;
        }


        public void Update()
        {
            mReactiveHud.Activity();

            UpdateEditingState();

            UpdateBasedOnEditingState();

            PerformKeyboardShortcuts();
            
            UndoManager.EndOfFrameActivity();

            mNodeNetworkEditorManager.Activity();
        }

        #endregion

        #region Private Methods

        private void AddingPointsUpdate()
        {
            Cursor cursor = GuiManager.Cursor;
            if (cursor.WindowOver == null)
            {
                Polygon polygon = CurrentShapeCollection.Polygons[0];

                #region If cursor clicked, add new point
                if (cursor.PrimaryClick)
                {
                    Matrix inverseRotation = polygon.RotationMatrix;
#if FRB_MDX
                    inverseRotation.Invert();
#else
                    Matrix.Invert(ref inverseRotation, out inverseRotation);
#endif

                    Point newPoint = new Point(
                        mReactiveHud.NewPointPolygon.Position.X - polygon.Position.X,
                        mReactiveHud.NewPointPolygon.Position.Y - polygon.Position.Y);

                    FlatRedBall.Math.MathFunctions.TransformPoint(ref newPoint, ref inverseRotation);

                    // adding new point
                    polygon.Insert( mReactiveHud.IndexBeforeNewPoint + 1, newPoint);
                }

                #endregion
            }
        }


        private void AddingPointsOnEndUpdate()
        {
            Cursor cursor = GuiManager.Cursor;

            if(cursor.PrimaryClick)
            {

                if (CurrentShapeCollection.Polygons.Count == 0)
                {
                    Polygon polygon = AddRectanglePolygon();

                    SelectPolygon(polygon);
                    Point[] newPoints = new Point[1];
                    newPoints[0].X = cursor.WorldXAt(0) - polygon.X;
                    newPoints[0].Y = cursor.WorldYAt(0) - polygon.Y;
                    polygon.Points = newPoints;
                }
                else
                {

                    Polygon currentPolygon = CurrentShapeCollection.Polygons[0];

                    Point[] newPoints = new Point[currentPolygon.Points.Count + 1];

                    for (int i = 0; i < currentPolygon.Points.Count; i++)
                    {
                        newPoints[i] = currentPolygon.Points[i];
                    }

                    newPoints[currentPolygon.Points.Count].X = cursor.WorldXAt(0) - 
                        currentPolygon.X;
                    newPoints[currentPolygon.Points.Count].Y = cursor.WorldYAt(0) - 
                        currentPolygon.Y;

                    currentPolygon.Points = newPoints;
                }


            }


        }

        #region XML Docs
        /// <summary>
        /// Controls selecting new Shapes and performing move, scale, and rotate (when appropriate).
        /// </summary>
        #endregion
        private void CursorControlOverShapes()
        {
            if (GuiManager.DominantWindowActive)
                return;


            Cursor cursor = GuiManager.Cursor;

            #region Pushing selects and grabs a Shape or the corner of a Polygon
            if (cursor.PrimaryPush)
            {
                #region If the user has not interacted with the corners then see check for grabbing any Shapes

                if (mReactiveHud.HasPolygonEdgeGrabbed == false)
                {

                    mObjectGrabbed = GetShapeOver(cursor.PrimaryDoublePush);

                    ShowErrorIfObjectCanNotBeGrabbed();

                    // If the object is not null store its original position.  This will be used
                    // for SHIFT+drag which allows movement only on one axis.
                    if (mObjectGrabbed != null)
                    {
                        PositionedObjectMover.SetStartPosition(mObjectGrabbed);
                    }

                    cursor.SetObjectRelativePosition(mObjectGrabbed);

                    if (PolygonGrabbed != null)
                    {
                        UndoManager.AddToWatch(PolygonGrabbed);
                        SelectPolygon(PolygonGrabbed);
                    }

                    if (AxisAlignedCubeGrabbed != null)
                    {
                        UndoManager.AddToWatch(AxisAlignedCubeGrabbed);
                        SelectAxisAlignedCube(AxisAlignedCubeGrabbed);
                    }

                    if (AxisAlignedRectangleGrabbed != null)
                    {
                        UndoManager.AddToWatch(AxisAlignedRectangleGrabbed);
                        SelectAxisAlignedRectangle(AxisAlignedRectangleGrabbed);
                    }
                    if (CircleGrabbed != null)
                    {
                        UndoManager.AddToWatch(CircleGrabbed);
                        SelectCircle(CircleGrabbed);
                    }
                    if (SphereGrabbed != null)
                    {
                        UndoManager.AddToWatch(SphereGrabbed);
                        SelectSphere(SphereGrabbed);
                    }

                    if (mObjectGrabbed == null)
                    {
                        DeselectAll();
                    }
                }

                #endregion
            }
            #endregion

            #region Holding the button down can be used to drag objects
            if (cursor.PrimaryDown)
            {
                PerformDraggingUpdate();

            }
            #endregion

            #region Clicking (releasing) the mouse lets go of grabbed Polygons

            if (cursor.PrimaryClick)
            {
                if (PolygonGrabbed != null)
                {
                    UndoManager.RecordUndos<Polygon>();
                    UndoManager.ClearObjectsWatching<Polygon>();
                }

                if (AxisAlignedRectangleGrabbed != null)
                {
                    UndoManager.RecordUndos<AxisAlignedRectangle>();
                    UndoManager.ClearObjectsWatching<AxisAlignedRectangle>();
                }

                if (AxisAlignedCubeGrabbed != null)
                {
                    UndoManager.RecordUndos<AxisAlignedCube>();
                    UndoManager.ClearObjectsWatching<AxisAlignedCube>();
                }

                if (CircleGrabbed != null)
                {
                    UndoManager.RecordUndos<Circle>();
                    UndoManager.ClearObjectsWatching<Circle>();
                }

                if (SphereGrabbed != null)
                {
                    UndoManager.RecordUndos<Sphere>();
                    UndoManager.ClearObjectsWatching<Sphere>();
                }

                mObjectGrabbed = null;

                cursor.StaticPosition = false;

                cursor.ObjectGrabbed = null;
            }

            #endregion

        }


        private void DeleteCurrentAxisAlignedRectangles()
        {
            while (CurrentShapeCollection.AxisAlignedRectangles.Count != 0)
            {
                ShapeManager.Remove(CurrentShapeCollection.AxisAlignedRectangles[0]);
            }

        }


        private void DeleteCurrentAxisAlignedCubes()
        {
            while (CurrentShapeCollection.AxisAlignedCubes.Count != 0)
            {
                ShapeManager.Remove(CurrentShapeCollection.AxisAlignedCubes[0]);
            }

        }


        private void DeleteCurrentCircles()
        {
            while (CurrentShapeCollection.Circles.Count != 0)
            {
                ShapeManager.Remove(CurrentShapeCollection.Circles[0]);
            }
        }


        private void DeleteCurrentSpheres()
        {
            while (CurrentShapeCollection.Spheres.Count != 0)
            {
                ShapeManager.Remove(CurrentShapeCollection.Spheres[0]);
            }
        }


        private void DeleteCurrentPolygons()
        {
            while (CurrentShapeCollection.Polygons.Count != 0)
            {
                ShapeManager.Remove(CurrentShapeCollection.Polygons[0]);
            }
        }


        private void DeleteKeyPressed()
        {
            if (mReactiveHud.CornerIndexSelected == -1)
            {
                DeleteCurrentPolygons();
                DeleteCurrentAxisAlignedRectangles();
                DeleteCurrentCircles();
                DeleteCurrentAxisAlignedCubes();
                DeleteCurrentSpheres();
            }
            else
            {
                DeleteSelectedCorner();
            }
        }


        private void DeleteSelectedCorner()
        {
            if (CurrentShapeCollection.Polygons[0].Points.Count > 4) // 3 points (triangle) are the minimum + 1 for repeated last point.
            {
                // First, get the points of the polygon.  The deleted point will be removed from this
                List<Point> pointList = new List<Point>(CurrentShapeCollection.Polygons[0].Points);
                // Remove the point
                pointList.RemoveAt(mReactiveHud.CornerIndexSelected);

                if (mReactiveHud.CornerIndexSelected == 0)
                {
                    // removed corner index 0
                    pointList[pointList.Count - 1] = pointList[0];
                }
                else if (mReactiveHud.CornerIndexSelected == pointList.Count)
                {
                    pointList[0] = pointList[pointList.Count - 1];
                }

                // Since a point has been deleted, make the mCornerIndexSelected = -1
                mReactiveHud.CornerIndexSelected = -1;

                CurrentShapeCollection.Polygons[0].Points = pointList;
            }
            else
            {
                GuiManager.ShowMessageBox("Polygon cannot have fewer than 3 points.", "Error Deleting Point");
            }

        }


        private PositionedObject GetShapeOver(bool skipCurrent)
        {
            PositionedObject positionedObject = null;

            #region See if we're over any current objects
            if (!skipCurrent)
            {
                positionedObject = GetCurrentPolygonOver();
                
                if (positionedObject == null)
                {
                    positionedObject = GetCurrentAxisAlignedRectangleOver();
                }
                if (positionedObject == null)
                {
                    positionedObject = GetCurrentCircleOver();
                }
                if (positionedObject == null)
                {
                    positionedObject = GetCurrentAxisAlignedCubeOver();
                }
                if (positionedObject == null)
                {
                    positionedObject = GetCurrentSphereOver();
                }
            }
            #endregion

            #region We're not over any current shape, so see if we're over any shape that isn't selected

            if (positionedObject == null && GuiData.GeometryWindow.EditingPolygons)
            {
                positionedObject = GetAllPolygonOver(skipCurrent);
            }
            if (positionedObject == null && GuiData.GeometryWindow.EditingAxisAlignedRectangles)
            {
                positionedObject = GetAllAxisAlignedRectangleOver(skipCurrent);
            }
            if (positionedObject == null && GuiData.GeometryWindow.EditingCircles)
            {
                positionedObject = GetAllCircleOver(skipCurrent);
            }
            if (positionedObject == null && GuiData.GeometryWindow.EditingAxisAlignedCubes)
            {
                positionedObject = GetAllAxisAlignedCubeOver(skipCurrent);
            }
            if (positionedObject == null && GuiData.GeometryWindow.EditingSpheres)
            {
                positionedObject = GetAllSphereOver(skipCurrent);
            }

            #endregion

            return positionedObject;
        }


        private AxisAlignedCube GetAllAxisAlignedCubeOver(bool skipCurrent)
        {
            Cursor cursor = GuiManager.Cursor;

            foreach (AxisAlignedCube axisAlignedCube in EditorData.AxisAlignedCubes)
            {
                if (cursor.IsOn3D(axisAlignedCube))
                {
                    if (!skipCurrent || CurrentShapeCollection.AxisAlignedCubes.Contains(axisAlignedCube) == false)
                    {
                        return axisAlignedCube;
                    }
                }
            }

            return null;
        }


        private AxisAlignedRectangle GetAllAxisAlignedRectangleOver(bool skipCurrent)
        {
            Cursor cursor = GuiManager.Cursor;

            foreach (AxisAlignedRectangle axisAlignedRectangle in EditorData.AxisAlignedRectangles)
            {
                if (cursor.IsOn(axisAlignedRectangle))
                {
                    if (!skipCurrent || CurrentShapeCollection.AxisAlignedRectangles.Contains(axisAlignedRectangle) == false)
                    {
                        return axisAlignedRectangle;
                    }
                }
            }

            return null;
        }


        private Circle GetAllCircleOver(bool skipCurrent)
        {
            Cursor cursor = GuiManager.Cursor;

            foreach (Circle circle in EditorData.Circles)
            {
                if (cursor.IsOn(circle))
                {
                    if (!skipCurrent || CurrentShapeCollection.Circles.Contains(circle) == false)
                    {
                        return circle;
                    }
                }

            }

            return null;
        }


        private Sphere GetAllSphereOver(bool skipCurrent)
        {
            Cursor cursor = GuiManager.Cursor;

            if (InputManager.Keyboard.KeyPushed(Keys.D))
            {
                int m = 3;
            }

            // When fixing, also consider current
            for(int i = 0; i < EditorData.Spheres.Count; i++)
            {
                Sphere sphere = EditorData.Spheres[i];

                if (cursor.IsOn3D(sphere)) 
                {
                    if (!skipCurrent || CurrentShapeCollection.Spheres.Contains(sphere) == false)
                    {
                        return sphere;
                    }
                }

            }

            return null;
        }


        private Polygon GetAllPolygonOver(bool skipCurrent)
        {
            Cursor cursor = GuiManager.Cursor;

            foreach (Polygon polygon in EditorData.Polygons)
            {
                if (cursor.IsOn3D(polygon))
                {
                    if (!skipCurrent || CurrentShapeCollection.Polygons.Contains(polygon) == false)
                    {
                        return polygon;
                    }
                }
            }

            return null;
        }


        private AxisAlignedCube GetCurrentAxisAlignedCubeOver()
        {
            Cursor cursor = GuiManager.Cursor;
            // Return current shapes if over any of them
            if (CurrentShapeCollection.AxisAlignedCubes.Count != 0)
            {
                foreach (AxisAlignedCube axisAlignedCube in CurrentShapeCollection.AxisAlignedCubes)
                {
                    if (cursor.IsOn3D(axisAlignedCube))
                    {
                        return axisAlignedCube;
                    }
                }
            }



            return null;
        }


        private AxisAlignedRectangle GetCurrentAxisAlignedRectangleOver()
        {
            Cursor cursor = GuiManager.Cursor;
            // Return current shapes if over any of them
            if (CurrentShapeCollection.AxisAlignedRectangles.Count != 0)
            {
                foreach (AxisAlignedRectangle axisAlignedRectangle in CurrentShapeCollection.AxisAlignedRectangles)
                {
                    if (cursor.IsOn(axisAlignedRectangle))
                    {
                        return axisAlignedRectangle;
                    }
                }
            }


            return null;
        }


        private Circle GetCurrentCircleOver()
        {
            Cursor cursor = GuiManager.Cursor;
            // Return current shapes if over any of them
            if (CurrentShapeCollection.Circles.Count != 0)
            {
                foreach (Circle circle in CurrentShapeCollection.Circles)
                {
                    if (cursor.IsOn(circle))
                    {
                        return circle;
                    }
                }
            }

            return null;
        }


        private Sphere GetCurrentSphereOver()
        {
            Cursor cursor = GuiManager.Cursor;


            // When fixing, also consider current
            for(int i = 0; i < CurrentShapeCollection.Spheres.Count; i++)
            {
                Sphere sphere = CurrentShapeCollection.Spheres[i];
                if (cursor.IsOn3D(sphere))
                {
                    return sphere;
                }

            }

            return null;
        }


        private Polygon GetCurrentPolygonOver()
        {
            Cursor cursor = GuiManager.Cursor;
            // Return current shapes if over any of them
            if (CurrentPolygons.Count != 0)
            {
                foreach (Polygon polygon in CurrentPolygons)
                {
                    if (cursor.IsOn(polygon))
                    {
                        return polygon;
                    }
                }
            }

            return null;
        }


        private void PerformDraggingUpdate()
        {
            
            Cursor cursor = GuiManager.Cursor;
            #region If a Shape is grabbed

            if (mReactiveHud.HasPolygonEdgeGrabbed == false && mObjectGrabbed != null)
            {
                #region If Move button is down
                if (GuiData.ToolsWindow.IsMoveButtonPressed)
                {

                    PerformMoveDragging(cursor);

                }
                #endregion

                #region If Rotate Button is down

                else if (GuiData.ToolsWindow.IsRotateButtonPressed)
                {
                    PerformRotateDragging(cursor);
                }
                #endregion

                #region If Scale Button is down

                else if (GuiData.ToolsWindow.IsScaleButtonPressed)
                {
                    cursor.StaticPosition = true;


                    foreach (Polygon polygon in CurrentShapeCollection.Polygons)
                    {
                        polygon.ScaleBy(1 + cursor.XVelocity / 100.0f, 1 + cursor.YVelocity / 100.0f);
                    }

                    foreach (AxisAlignedRectangle rectangle in CurrentShapeCollection.AxisAlignedRectangles)
                    {
                        float newScaleX = rectangle.ScaleX * (1 + cursor.XVelocity / 100.0f);
                        float newScaleY = rectangle.ScaleY * (1 + cursor.YVelocity / 100.0f);
                        newScaleX = Math.Max(0, newScaleX);
                        newScaleY = Math.Max(0, newScaleY);
                        rectangle.ScaleX = newScaleX;
                        rectangle.ScaleY = newScaleY;
                    }

                    foreach (AxisAlignedCube cube in CurrentShapeCollection.AxisAlignedCubes)
                    {
                        float newScaleX = cube.ScaleX * (1 + cursor.XVelocity / 100.0f);
                        float newScaleY = cube.ScaleY * (1 + cursor.YVelocity / 100.0f);

                        newScaleX = Math.Max(0, newScaleX);
                        newScaleY = Math.Max(0, newScaleY);
                        

                        cube.ScaleX = newScaleX;
                        cube.ScaleY = newScaleY;
                    }

                    foreach (Circle circle in CurrentShapeCollection.Circles)
                    {
                        float newRadius = circle.Radius * (1 + cursor.YVelocity / 100.0f);
                        newRadius = Math.Max(0, newRadius);
                        circle.Radius = newRadius;
                    }

                    foreach (Sphere sphere in CurrentShapeCollection.Spheres)
                    {
                        float newRadius = sphere.Radius * ( 1 + cursor.YVelocity / 100.0f);
                        newRadius = Math.Max(0, newRadius);
                        sphere.Radius = newRadius;
                    }
                }

                #endregion
            }
            #endregion
        }


        private void PerformMoveDragging(Cursor cursor)
        {
            PositionedObjectMover.MouseMoveObject(mObjectGrabbed);
        }


        private void PerformRotateDragging(Cursor cursor)
        {
            if (mObjectGrabbed is Circle || mObjectGrabbed is AxisAlignedRectangle ||
                mObjectGrabbed is AxisAlignedCube || mObjectGrabbed is Sphere)
            {
                return;// GuiManager.ShowMessageBox("This shape cannot be rotated", "Error rotating");
            }

            PositionedObjectRotator.MouseRotateObject(mObjectGrabbed, MovementStyle.Hierarchy);
        }


        private void PerformKeyboardShortcuts()
        {

            #region Control the Camera with the Keyboard

            EditorObjects.CameraMethods.KeyboardCameraControl(SpriteManager.Camera);

            #endregion

            GuiData.ToolsWindow.ListenForShortcuts();
            GuiData.GeometryWindow.ListenForShortcuts();

            #region Delete Key - delete current Shapes
            if (InputManager.Keyboard.KeyPushedConsideringInputReceiver(Keys.Delete))
            {
                DeleteKeyPressed();

                // Since all current objects are gone the PropertyGrids should disappear
                // Update on 7/28/2010:
                // I don't think it's a good
                // idea to make this thing disappear.
                // Why do we want to do this?  It's annoying!
                // GuiData.ShapeCollectionPropertyGrid.Visible = false;
            }

            #endregion

            #region CTRL + C - copy current Polygons

            if (InputManager.ReceivingInput == null && InputManager.Keyboard.ControlCPushed())
            {
                CopyCurrentPolygons();
                CopyCurrentAxisAlignedRectangles();
                CopyCurrentAxisAlignedCubes();
                CopyCurrentCircles();
                CopyCurrentSpheres();
            }

            #endregion              

            #region T key - makes the current point a right-angle

            if (InputManager.ReceivingInput == null && InputManager.Keyboard.KeyDown(Keys.T) && mReactiveHud.CornerIndexSelected != -1 )
            {
                MakeCurrentPointRightAngle();
            }


            #endregion
        }

        private void MakeCurrentPointRightAngle()
        {
            Polygon currentPolygon = CurrentPolygons[0];

            int indexBefore = mReactiveHud.CornerIndexSelected - 1;

            if (indexBefore < 0)
            {
                indexBefore = CurrentPolygons[0].Points.Count - 2;
            }

            int indexAfter = (mReactiveHud.CornerIndexSelected + 1) % (CurrentPolygons[0].Points.Count - 1);

            int m = 3;

            Point grabbedPoint = currentPolygon.Points[mReactiveHud.CornerIndexSelected];
            Point pointBefore = currentPolygon.Points[indexBefore];
            Point pointAfter = currentPolygon.Points[indexAfter];

            Point toBefore = pointBefore - grabbedPoint;
            Point toAfter = pointAfter - grabbedPoint;

            toBefore.Normalize();
            toAfter.Normalize();

            double xToSet = 0;
            double yToSet = 0;

            if (Math.Abs(toBefore.X) < Math.Abs(toAfter.X))
            {
                xToSet = pointBefore.X;
                yToSet = pointAfter.Y;
            }
            else
            {
                xToSet = pointAfter.X;
                yToSet = pointBefore.Y;
            }

            grabbedPoint.X = xToSet;
            grabbedPoint.Y = yToSet;
            currentPolygon.SetPoint(mReactiveHud.CornerIndexSelected, grabbedPoint);

            if (mReactiveHud.CornerIndexSelected == 0)
            {
                currentPolygon.SetPoint(currentPolygon.Points.Count - 1, grabbedPoint);
            }
        }


        internal void DeselectAll()
        {
            SelectAxisAlignedCube(null);
            SelectAxisAlignedRectangle(null);
            SelectCircle(null);
            SelectSphere(null);
            SelectPolygon(null);
        }


        private void ShowErrorIfObjectCanNotBeGrabbed()
        {
            if (GuiData.ToolsWindow.IsRotateButtonPressed)
            {
                if (mObjectGrabbed is Circle || mObjectGrabbed is AxisAlignedRectangle ||
                    mObjectGrabbed is AxisAlignedCube || mObjectGrabbed is Sphere)
                {
                    GuiManager.ShowMessageBox("This shape cannot be rotated", "Error rotating");

                    mObjectGrabbed = null;
                }
            }
        }


        private void UpdateBasedOnEditingState()
        {
            //Moved from Update
            // Don't do any logic if the cursor is over a window
            Cursor cursor = GuiManager.Cursor;

            if (cursor.WindowOver == null)
            {
                switch (mEditingState)
                {
                    #region Case AddingPointsOnEnd

                    case EditingState.AddingPointsOnEnd:
                        AddingPointsOnEndUpdate();
                        break;
                    #endregion

                    #region Case AddingPoints

                    case EditingState.AddingPoints:
                        AddingPointsUpdate();
                        break;

                    #endregion

                    #region Case None

                    case EditingState.None:
                        CursorControlOverShapes();
                        break;

                    #endregion
                }
            }

            // It is possible that the user clicked on an object to scale it, then a 
            // Window appeared on top of it.  This put the cursor in a perment static
            // state.  Fix this:
            if (cursor.PrimaryClick)
            {
                cursor.StaticPosition = false;
            }
        }

        private void UpdateEditingState()
        {
            EditingState oldState = mEditingState;

            if (GuiData.ToolsWindow.IsAddPointButtonPressed)
            {
                mEditingState = EditingState.AddingPoints;
            }
            else if (GuiData.ToolsWindow.IsDrawingPolygonButtonPressed)
            {

                EditingState = EditingState.AddingPointsOnEnd;

                if (EditingState != oldState)
                {
                    SelectPolygon(null);

                }
            }
            else
            {
                mEditingState = EditingState.None;
            }

            #region If the user was adding point on end, then stopped, finish up by setting the last point and optimizing the radius
            if (oldState == EditingState.AddingPointsOnEnd && 
                oldState != mEditingState)
            {
                if (CurrentShapeCollection.Polygons.Count == 0)
                {
                    // the user never started the polygon, so exit
                }
                else
                {
                    // Finish up the polygon
                    Polygon currentPolygon = CurrentShapeCollection.Polygons[0];

                    currentPolygon.Insert(currentPolygon.Points.Count, currentPolygon.Points[0]);

                    currentPolygon.OptimizeRadius();
                }

            }

            #endregion
        }


        #endregion

        #endregion
    }
}
