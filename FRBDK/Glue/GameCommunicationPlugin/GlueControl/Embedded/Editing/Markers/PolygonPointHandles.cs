using FlatRedBall;
using FlatRedBall.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GlueControl.Managers;
using FlatRedBall.Math;
using FlatRedBall.Gui;


namespace GlueControl.Editing
{
    internal class PolygonPointHandles
    {
        #region Fields/Properties

        List<AxisAlignedRectangle> rectangles = new List<AxisAlignedRectangle>();

        Microsoft.Xna.Framework.Vector3 UnsnappedPosition;

        public float? PointSnapSize { get; set; }

        int? PointIndexGrabbed;
        public int? PointIndexHighlighted { get; private set; }

        public bool Visible { get; set; }

        #endregion

        public void EveryFrameUpdate(PositionedObject item, SelectionMarker selectionMarker)
        {
            var itemAsPolygon = item as Polygon;
            // special case for derived polygons, this will handle capsule polygon.
            // If in the future this is a problem, then we will add a new Glux file version:
            if (item?.GetType() != typeof(Polygon))
            {
                itemAsPolygon = null;
            }

            Visible = selectionMarker.Visible && itemAsPolygon != null;

            if (itemAsPolygon == null)
            {
                UpdatePointCount(0);
            }

            UpdatePointsToItem(itemAsPolygon, selectionMarker);

            ///////////Early Out//////////////
            if (!Visible || selectionMarker.CanMoveItem == false || FlatRedBallServices.Game.IsActive == false)
                return;
            ////////End Early Out/////////////

            var cursor = FlatRedBall.Gui.GuiManager.Cursor;


            if (cursor.PrimaryPush)
            {
                DoCursorPushActivity(itemAsPolygon);
            }
            if (cursor.PrimaryDown)
            {
                DoCursorDownActivity(itemAsPolygon);
            }
            if (!cursor.PrimaryDown)
            {
                DoCursorHoverActivity();
            }
            if (cursor.PrimaryClick)
            {
                DoCursorClickActivity(itemAsPolygon);
            }
        }

        private void UpdatePointCount(int count)
        {
            while (count < rectangles.Count)
            {
                RemovePointRectangle(rectangles.LastOrDefault());
            }
            while (count > rectangles.Count)
            {
                AddPointRectangle();
            }
        }

        private void UpdatePointsToItem(Polygon itemAsPolygon, SelectionMarker selectionMarker)
        {
            if (!selectionMarker.CanMoveItem || itemAsPolygon == null)
            {
                UpdatePointCount(0);
            }
            else
            {
                UpdatePointCount(itemAsPolygon.Points.Count);

                itemAsPolygon.ForceUpdateDependencies();

                var handledLast = false;
                // for now assume polygons have the last point overlapping:
                for (int i = 0; i < itemAsPolygon.Points.Count; i++)
                {
                    var position = itemAsPolygon.AbsolutePointPosition(i);

                    var rectangle = rectangles[i];

                    rectangle.Position = position;

                    if (i < itemAsPolygon.Points.Count - 1 || !handledLast)
                    {
                        if (i == PointIndexHighlighted)
                        {
                            rectangle.Width = ResizeHandles.HighlightedHandleDimension / CameraLogic.CurrentZoomRatio;
                            rectangle.Height = ResizeHandles.HighlightedHandleDimension / CameraLogic.CurrentZoomRatio;
                            if (i == 0)
                            {
                                rectangle = rectangles.LastOrDefault();
                                rectangle.Width = ResizeHandles.HighlightedHandleDimension / CameraLogic.CurrentZoomRatio;
                                rectangle.Height = ResizeHandles.HighlightedHandleDimension / CameraLogic.CurrentZoomRatio;
                                handledLast = true;
                            }
                        }
                        else
                        {
                            rectangle.Width = ResizeHandles.DefaultHandleDimension / CameraLogic.CurrentZoomRatio;
                            rectangle.Height = ResizeHandles.DefaultHandleDimension / CameraLogic.CurrentZoomRatio;
                        }
                    }
                }
            }
        }

        public void AddPointRectangle()
        {
            var rectangle = new AxisAlignedRectangle();
            rectangle.Width = ResizeHandles.DefaultHandleDimension;
            rectangle.Height = ResizeHandles.DefaultHandleDimension;

            FlatRedBall.Screens.ScreenManager.PersistentAxisAlignedRectangles.Add(rectangle);
            ShapeManager.AddToLayer(rectangle, SpriteManager.TopLayer, makeAutomaticallyUpdated: false);
            rectangle.Visible = true;
            rectangles.Add(rectangle);
        }

        private void DoCursorPushActivity(Polygon polygon)
        {
            var cursor = FlatRedBall.Gui.GuiManager.Cursor;
            PointIndexGrabbed = null;
            for (int i = 0; i < rectangles.Count; i++)
            {
                var rectangle = rectangles[i];
                if (cursor.IsOn(rectangle))
                {
                    PointIndexGrabbed = i;
                    PointIndexHighlighted = i;
                    // rectangle and point should be at the same point, but let's go to the source just in case...
                    UnsnappedPosition = polygon.AbsolutePointPosition(i);
                    break;
                }
            }
        }

        private void DoCursorDownActivity(Polygon polygon)
        {
            var cursor = FlatRedBall.Gui.GuiManager.Cursor;
            ///////////////Early Out////////////
            if (PointIndexGrabbed == null)
            {
                return;
            }
            /////////////End Early Out//////////
            var circle = EditorVisuals.Circle(polygon.BoundingRadius, polygon.Position);
            circle.Color = new Microsoft.Xna.Framework.Color(.5f, .5f, .5f, .5f);

            var didMove = cursor.ScreenXChange != 0 || cursor.ScreenYChange != 0;
            if (didMove)
            {
                DoCursorDownMovement(polygon, cursor);
            }
        }

        private void DoCursorDownMovement(Polygon polygon, Cursor cursor)
        {
            var isEndpoint = PointIndexGrabbed == 0 || PointIndexGrabbed == polygon.Points.Count - 1;

            var areEndPointsOverlapping =
                polygon.AbsolutePointPosition(0) == polygon.AbsolutePointPosition(polygon.Points.Count - 1);


            UnsnappedPosition.X += cursor.ScreenXChange / CameraLogic.CurrentZoomRatio;
            UnsnappedPosition.Y += -cursor.ScreenYChange / CameraLogic.CurrentZoomRatio;

            float Snap(float value) =>
                PointSnapSize > 0
                ? MathFunctions.RoundFloat(value, PointSnapSize.Value)
                : value;

            var snappedX = Snap(UnsnappedPosition.X);
            var snappedY = Snap(UnsnappedPosition.Y);

            if (isEndpoint && areEndPointsOverlapping)
            {
                polygon.SetPointFromAbsolutePosition(0, snappedX, snappedY);
                polygon.SetPointFromAbsolutePosition(polygon.Points.Count - 1, snappedX, snappedY);

            }
            else
            {
                polygon.SetPointFromAbsolutePosition(PointIndexGrabbed.Value, snappedX, snappedY);
            }
        }

        private void DoCursorHoverActivity()
        {
            var cursor = FlatRedBall.Gui.GuiManager.Cursor;
            PointIndexHighlighted = null;
            for (int i = 0; i < rectangles.Count; i++)
            {
                var rectangle = rectangles[i];
                if (cursor.IsOn(rectangle))
                {
                    PointIndexHighlighted = i;
                    break;
                }
            }
        }

        private async void DoCursorClickActivity(Polygon polygon)
        {
            if (polygon != null && PointIndexGrabbed != null)
            {
                PointIndexGrabbed = null;

                var nos = GlueState.Self.CurrentNamedObjectSave;
                var owner = GlueState.Self.CurrentElement;

                var newValue = polygon.Points
                    .Select(item => new Microsoft.Xna.Framework.Vector2((float)item.X, (float)item.Y))
                    .ToList();

                var assignments = new List<NosVariableAssignment>();
                assignments.Add(
                    new NosVariableAssignment
                    {
                        NamedObjectSave = nos,
                        VariableName = nameof(Polygon.Points),
                        Value = newValue
                    });

                await GlueCommands.Self.GluxCommands.SetVariableOnList(assignments, owner);
            }
        }

        public void RemovePointRectangle(AxisAlignedRectangle rectangle)
        {
            FlatRedBall.Screens.ScreenManager.PersistentAxisAlignedRectangles.Remove(rectangle);
            ShapeManager.Remove(rectangle);
            rectangle.Visible = false;
            rectangles.Remove(rectangle);
        }

        public void Destroy()
        {
            for (int i = 0; i < rectangles.Count; i++)
            {
                FlatRedBall.Screens.ScreenManager.PersistentAxisAlignedRectangles.Remove(rectangles[i]);
                rectangles[i].Visible = false;
            }
        }
    }


}

