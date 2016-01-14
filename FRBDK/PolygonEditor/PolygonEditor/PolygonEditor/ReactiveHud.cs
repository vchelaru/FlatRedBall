using System;
using System.Collections.Generic;
using System.Text;
using EditorObjects;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Math;
using FlatRedBall.Gui;

using PolygonEditor.Gui;
using FlatRedBall;

#if FRB_MDX
using Microsoft.DirectX;
#else
using Color = Microsoft.Xna.Framework.Graphics.Color;
using Microsoft.Xna.Framework;
using FlatRedBall.Graphics;
#endif

namespace PolygonEditor
{
    public class ReactiveHud
    {
        #region Fields

        CameraBounds mCameraBounds;

        AxisAlignedRectangle mCurrentAxisAlignedRectangleHighlight;
        AxisAlignedRectangle mCurrentCircleHighlight;

        AxisAlignedRectangle mCornerRectangleGrabbed;
        PositionedObjectList<AxisAlignedRectangle> mCorners = new PositionedObjectList<AxisAlignedRectangle>();
        List<Circle> mPolygonRadiusDisplay = new List<Circle>();

        AxisAlignedRectangle mCurrentAxisAlignedCubeHighlight;
        AxisAlignedRectangle mCurrentSphereHighlight;

        AxisAlignedRectangle mCornerCubeGrabbed;

        #region XML Docs
        /// <summary>
        /// Visible representation of where new point will be added.  Will only be visible when the user
        /// has the Add Point button down.
        /// </summary>
        #endregion
        Polygon mNewPointPolygon;
        float mNewPointPolygonScale = 1f;
        int mIndexBeforeNewPoint;

        Crosshair mCrossHair;

        int mCornerIndexGrabbed = -1;
        int mCornerIndexSelected = -1;


        Text mPointText;

        #endregion

        #region Properties

        public int CornerIndexSelected
        {
            get { return mCornerIndexSelected; }
            set { mCornerIndexSelected = value; }
        }

        private ShapeCollection CurrentShapeCollection
        {
            get { return EditorData.EditingLogic.CurrentShapeCollection; }
        }

        public bool HasPolygonEdgeGrabbed
        {
            get { return mCornerRectangleGrabbed != null; }
        }

        public int IndexBeforeNewPoint
        {
            get { return mIndexBeforeNewPoint; }
        }

        public Polygon NewPointPolygon
        {
            get { return mNewPointPolygon; }
        }

        public float NewPointPolygonScale
        {
            get { return mNewPointPolygonScale; }
            set
            {
                double diff = (double)value / (double)mNewPointPolygonScale;
                mNewPointPolygon.ScaleBy(diff);
                mNewPointPolygonScale = value;
            }
        }

        #endregion

        #region Methods

        #region Public Methods

        public ReactiveHud()
        {
            mCameraBounds = new CameraBounds(EditorData.BoundsCamera);

            mCurrentAxisAlignedRectangleHighlight = ShapeManager.AddAxisAlignedRectangle();
            mCurrentAxisAlignedRectangleHighlight.Visible = false;

            mCurrentAxisAlignedCubeHighlight = ShapeManager.AddAxisAlignedRectangle();
            mCurrentAxisAlignedCubeHighlight.Visible = false;

            mCurrentCircleHighlight = ShapeManager.AddAxisAlignedRectangle();
            mCurrentCircleHighlight.Visible = false;

            mCurrentSphereHighlight = ShapeManager.AddAxisAlignedRectangle();
            mCurrentSphereHighlight.Visible = false;

            mCrossHair = new Crosshair();
            mCrossHair.Visible = false;


            float screensize = 5f;
            Vector3 forwardVector = MathFunctions.ForwardVector3;
            Matrix rotationMatrix = SpriteManager.Camera.RotationMatrix;
            MathFunctions.TransformVector(ref forwardVector, ref rotationMatrix);


            float planeDistance = Vector3.Dot(forwardVector, -SpriteManager.Camera.Position);

            float planeScreenHeight = 2f * planeDistance * (float)Math.Tan((double)SpriteManager.Camera.FieldOfView);
            float planeScreenWidth = planeScreenHeight / SpriteManager.Camera.FieldOfView;


            float width = screensize * planeScreenWidth / (float)SpriteManager.Camera.DestinationRectangle.Width;
            float height = screensize * planeScreenHeight / (float)SpriteManager.Camera.DestinationRectangle.Height;


            mNewPointPolygon = Polygon.CreateEquilateral(3, Math.Min(width, height), 0); //.3f, 0);
            mNewPointPolygon.Color = EditorProperties.NewPointPolygonColor;

            mPointText = TextManager.AddText("");

            NewPointPolygonScale = 10 / SpriteManager.Camera.PixelsPerUnitAt(mNewPointPolygon.Z);

        }

        public void Activity()
        {
            mCameraBounds.UpdateBounds(0);

            #region Update the selected Polygon corner scales, positions, and scales

            UpdateCornerRectangleCount();

            UpdateRadiusCircles();

            UpdateCornerRectanglePositions();

            UpdateCornerRectangleScales();

            #endregion

            UpdateSelectedAxisAlignedCubeSelectionHud();

            UpdateSelectedAxisAlignedRectangleSelectionHud();

            UpdateSelectedCircleSelectionHud();

            UpdateSelectedSphereSelectionHud();

            UpdateNewPointDisplay();

            UpdateCrosshairs();

            UpdateUIColors();

            // do this last so the rectangles are already created.
            TestForPushingOnPolygonCorner();
        }



        private void UpdateNewPointDisplay()
        {
            if (EditorData.EditingLogic.EditingState == EditingState.AddingPoints)
            {
                Cursor cursor = GuiManager.Cursor;
                if (cursor.WindowOver == null)
                {
                    Polygon polygon = CurrentShapeCollection.Polygons[0];

                    float worldX = cursor.WorldXAt(0);
                    float worldY = cursor.WorldYAt(0);


                    Point3D vector = polygon.VectorFrom(worldX, worldY, out mIndexBeforeNewPoint);

                    mNewPointPolygon.Visible = true;
                    mNewPointPolygon.Position.X = (float)(vector.X + worldX);
                    mNewPointPolygon.Position.Y = (float)(vector.Y + worldY);

                    #region Update the Point Text
                    mPointText.X = mNewPointPolygon.Position.X;
                    mPointText.Y = mNewPointPolygon.Position.Y;
                    mPointText.SetPixelPerfectScale(SpriteManager.Camera);

                    mPointText.DisplayText = "  X: " + mPointText.X + ", Y: " + mPointText.Y + "";
                    mPointText.Visible = true;

                    #endregion
                }
            }
            else
            {
                mPointText.Visible = false;
                mNewPointPolygon.Visible = false;
            }
        }

        #endregion

        #region Private Methods

        private void UpdateSelectedAxisAlignedCubeSelectionHud()
        {
            if (CurrentShapeCollection.AxisAlignedCubes.Count == 0)
            {
                mCurrentAxisAlignedCubeHighlight.Visible = false;
                if (mCurrentAxisAlignedCubeHighlight.Parent != null)
                {
                    mCurrentAxisAlignedCubeHighlight.Detach();
                }
            }
            else
            {
                if (mCurrentAxisAlignedCubeHighlight.Visible == false)
                {
                    mCurrentAxisAlignedCubeHighlight.Visible = true;
                    mCurrentAxisAlignedCubeHighlight.AttachTo(CurrentShapeCollection.AxisAlignedCubes[0], false);

                }
                else if (mCurrentAxisAlignedCubeHighlight.Parent != CurrentShapeCollection.AxisAlignedCubes[0])
                {
                    mCurrentAxisAlignedCubeHighlight.AttachTo(CurrentShapeCollection.AxisAlignedCubes[0], false);
                }

                mCurrentAxisAlignedCubeHighlight.Position = CurrentShapeCollection.AxisAlignedCubes[0].Position;

                float extraScale = 4 / SpriteManager.Camera.PixelsPerUnitAt(0);


                mCurrentAxisAlignedCubeHighlight.ScaleX = CurrentShapeCollection.AxisAlignedCubes[0].ScaleX + extraScale;
                mCurrentAxisAlignedCubeHighlight.ScaleY = CurrentShapeCollection.AxisAlignedCubes[0].ScaleY + extraScale;
            }
        }

        private void UpdateSelectedAxisAlignedRectangleSelectionHud()
        {
            if (CurrentShapeCollection.AxisAlignedRectangles.Count == 0)
            {
                mCurrentAxisAlignedRectangleHighlight.Visible = false;
                if (mCurrentAxisAlignedRectangleHighlight.Parent != null)
                {
                    mCurrentAxisAlignedRectangleHighlight.Detach();
                }
            }
            else
            {
                if (mCurrentAxisAlignedRectangleHighlight.Visible == false)
                {
                    mCurrentAxisAlignedRectangleHighlight.Visible = true;
                    mCurrentAxisAlignedRectangleHighlight.AttachTo(CurrentShapeCollection.AxisAlignedRectangles[0], false);

                }
                else if (mCurrentAxisAlignedRectangleHighlight.Parent != CurrentShapeCollection.AxisAlignedRectangles[0])
                {
                    mCurrentAxisAlignedRectangleHighlight.AttachTo(CurrentShapeCollection.AxisAlignedRectangles[0], false);
                }

                mCurrentAxisAlignedRectangleHighlight.Position = CurrentShapeCollection.AxisAlignedRectangles[0].Position;

                float extraScale = 4 / SpriteManager.Camera.PixelsPerUnitAt(0);

                mCurrentAxisAlignedRectangleHighlight.ScaleX = CurrentShapeCollection.AxisAlignedRectangles[0].ScaleX + extraScale;
                mCurrentAxisAlignedRectangleHighlight.ScaleY = CurrentShapeCollection.AxisAlignedRectangles[0].ScaleY + extraScale;
            }
        }

        private void UpdateSelectedCircleSelectionHud()
        {
            if (CurrentShapeCollection.Circles.Count == 0)
            {
                mCurrentCircleHighlight.Visible = false;
                if (mCurrentCircleHighlight.Parent != null)
                {
                    mCurrentCircleHighlight.Detach();
                }
            }
            else
            {
                if (mCurrentCircleHighlight.Visible == false)
                {
                    mCurrentCircleHighlight.Visible = true;
                    mCurrentCircleHighlight.AttachTo(CurrentShapeCollection.Circles[0], false);
                }
                else if (mCurrentCircleHighlight.Parent != CurrentShapeCollection.Circles[0])
                {
                    mCurrentCircleHighlight.AttachTo(CurrentShapeCollection.Circles[0], false);
                }

                mCurrentCircleHighlight.Position = CurrentShapeCollection.Circles[0].Position;

                float extraScale = 4 / SpriteManager.Camera.PixelsPerUnitAt(0);

                mCurrentCircleHighlight.ScaleX = CurrentShapeCollection.Circles[0].Radius + extraScale;
                mCurrentCircleHighlight.ScaleY = CurrentShapeCollection.Circles[0].Radius + extraScale;
            }
        }

        private void UpdateSelectedSphereSelectionHud()
        {
            if (CurrentShapeCollection.Spheres.Count == 0)
            {
                mCurrentSphereHighlight.Visible = false;
                if (mCurrentSphereHighlight.Parent != null)
                {
                    mCurrentSphereHighlight.Detach();
                }
            }
            else
            {
                if (mCurrentSphereHighlight.Visible == false)
                {
                    mCurrentSphereHighlight.Visible = true;
                    mCurrentSphereHighlight.AttachTo(CurrentShapeCollection.Spheres[0], false);
                }
                else if (mCurrentSphereHighlight.Parent != CurrentShapeCollection.Spheres[0])
                {
                    mCurrentSphereHighlight.AttachTo(CurrentShapeCollection.Spheres[0], false);
                }

                mCurrentSphereHighlight.Position = CurrentShapeCollection.Spheres[0].Position;

                float extraScale = 4 / SpriteManager.Camera.PixelsPerUnitAt(0);

                mCurrentSphereHighlight.ScaleX = CurrentShapeCollection.Spheres[0].Radius + extraScale;
                mCurrentSphereHighlight.ScaleY = CurrentShapeCollection.Spheres[0].Radius + extraScale;
            }
        }

        private void UpdateRadiusCircles()
        {
            //while (CurrentShapeCollection.Polygons.Count > mPolygonRadiusDisplay.Count)
            //{
            //    mPolygonRadiusDisplay.Add(
            //}
        }

        private void UpdateCornerRectangleCount()
        {
            int numberOfEdges = 0;

            foreach (Polygon polygon in CurrentShapeCollection.Polygons)
            {
                // Polygons of 1 point should still draw their point

                if (polygon.Points.Count == 1)
                {
                    numberOfEdges++;
                }
                else
                {
                    numberOfEdges +=
                        polygon.Points.Count - 1; // assume the last point repeats
                }
            }

            while (mCorners.Count < numberOfEdges)
            {
                AxisAlignedRectangle newRectangle = ShapeManager.AddAxisAlignedRectangle();
                newRectangle.Color = Color.Red;

                newRectangle.ScaleX = newRectangle.ScaleY = .3f;

                mCorners.Add(newRectangle);
            }

            while (mCorners.Count > numberOfEdges)
            {
                ShapeManager.Remove(mCorners[mCorners.Count - 1]);

            }
        }

        private void UpdateCornerRectanglePositions()
        {
            int rectangleNumber = 0;

            foreach (Polygon polygon in CurrentShapeCollection.Polygons)
            {
                if (polygon.Points.Count == 1)
                {
                    if (mCorners[rectangleNumber].Parent != polygon)
                    {
                        mCorners[rectangleNumber].AttachTo(polygon, false);
                    }

                    mCorners[rectangleNumber].RelativeX = (float)(polygon.Points[0].X);
                    mCorners[rectangleNumber].RelativeY = (float)(polygon.Points[0].Y);

                    rectangleNumber++;
                }
                else
                {
                    for (int i = 0; i < polygon.Points.Count - 1; i++)
                    {
                        if (mCorners[rectangleNumber].Parent != polygon)
                        {
                            mCorners[rectangleNumber].AttachTo(polygon, false);
                        }

                        mCorners[rectangleNumber].RelativeX = (float)(polygon.Points[i].X);
                        mCorners[rectangleNumber].RelativeY = (float)(polygon.Points[i].Y);

                        rectangleNumber++;
                    }
                }
            }
        }

        private void UpdateCornerRectangleScales()
        {
            int rectangleNumber = 0;

            foreach (Polygon polygon in CurrentShapeCollection.Polygons)
            {
                for (int i = 0; i < polygon.Points.Count - 1; i++)
                {
                    // Set scale to be screensize when viewed at camera distance

                    float pixelsPerUnit = SpriteManager.Camera.PixelsPerUnitAt(mCorners[rectangleNumber].Z);

                    mCorners[rectangleNumber].ScaleX = 5 * (1 / pixelsPerUnit);
                    mCorners[rectangleNumber].ScaleY = 5 * (1 / pixelsPerUnit);

                    rectangleNumber++;
                }
            }
        }

        private void UpdateCrosshairs()
        {
            Polygon selectedPolygon = null;

            if (EditorData.EditingLogic.CurrentShapeCollection.Polygons.Count != 0)
            {
                selectedPolygon = EditorData.EditingLogic.CurrentShapeCollection.Polygons[0];
            }

            mCrossHair.Visible = selectedPolygon != null;

            if (selectedPolygon != null)
            {
                mCrossHair.Position = selectedPolygon.Position;
                mCrossHair.UpdateScale();
            }
        }

        private void UpdateUIColors()
        {
            for (int i = 0; i < mCorners.Count; i++)
            {
                mCorners[i].Color = Color.Red;
            }

            if (mCornerIndexSelected != -1 && CurrentShapeCollection.Polygons.Count != 0)
            {

                mCorners[mCornerIndexSelected].Color = EditorProperties.SelectedCornerColor;

            }
        }


        private void TestForPushingOnPolygonCorner()
        {
            Cursor cursor = GuiManager.Cursor;

            if (cursor.WindowOver != null)
                return;

            #region Primary Push
            if (cursor.PrimaryPush)
            {
                mCornerRectangleGrabbed = null;
                mCornerIndexGrabbed = -1;
                mCornerIndexSelected = -1;
                bool pushHandled = false;

                if (EditorData.EditingLogic.CurrentShapeCollection.Polygons.Count != 0)
                {
                    int rectangleNumber = 0;
                    // See if the user is over any of the corners
                    foreach (Polygon polygon in EditorData.EditingLogic.CurrentShapeCollection.Polygons)
                    {
                        for (int i = 0; i < polygon.Points.Count - 1; i++)
                        {
                            if (cursor.IsOn<AxisAlignedRectangle>(mCorners[rectangleNumber]))
                            {
                                mCornerIndexGrabbed = i;
                                mCornerIndexSelected = i;
                                mCornerRectangleGrabbed = mCorners[rectangleNumber];

                                pushHandled = true;
                                UndoManager.AddToWatch(polygon);
                                break;
                            }

                            rectangleNumber++;
                        }

                        if (pushHandled)
                            break;
                    }
                }
            }
            #endregion

            #region Primary Down

            if ( this.mCornerRectangleGrabbed != null)
            {
                if (GuiData.ToolsWindow.IsMoveButtonPressed)
                {
                    Polygon parentPolygon = mCornerRectangleGrabbed.Parent as Polygon;

                    // Get the position of the cursor in object space
                    Vector3 newRelativePosition = new Vector3(cursor.WorldXAt(0), cursor.WorldYAt(0), 0);

                    newRelativePosition -= parentPolygon.Position;

                    Matrix invertedMatrix = parentPolygon.RotationMatrix;
#if FRB_MDX
                    invertedMatrix.Invert();
                    Vector4 temporary = Vector3.Transform(newRelativePosition, invertedMatrix);
#else
                    Matrix.Invert(ref invertedMatrix, out invertedMatrix);

                    Vector3 temporary = newRelativePosition;

                    MathFunctions.TransformVector(ref temporary, ref invertedMatrix);
#endif


                    mCornerRectangleGrabbed.RelativePosition = new Vector3(temporary.X, temporary.Y, 0);

                    parentPolygon.SetPoint(mCornerIndexGrabbed, mCornerRectangleGrabbed.RelativePosition.X,
                        mCornerRectangleGrabbed.RelativePosition.Y);

                    if (mCornerIndexGrabbed == 0)
                    {
                        parentPolygon.SetPoint(
                            parentPolygon.Points.Count - 1,
                            mCornerRectangleGrabbed.RelativePosition.X,
                            mCornerRectangleGrabbed.RelativePosition.Y);
                    }
                }
            }

            #endregion

            #region Primary Release

            if (cursor.PrimaryClick)
            {
                if (mCornerRectangleGrabbed == null)
                {
                    mCornerIndexSelected = -1;
                }
                else
                {
                    UndoManager.RecordUndos<Polygon>();
                    UndoManager.ClearObjectsWatching<Polygon>();

                }

                mCornerRectangleGrabbed = null;
                mCornerIndexGrabbed = -1;



            }

            #endregion
        }

        #endregion

        #endregion
    }
}
