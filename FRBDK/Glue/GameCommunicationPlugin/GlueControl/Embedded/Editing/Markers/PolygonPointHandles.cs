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
using Microsoft.Xna.Framework;
using GlueControl.Models;
using Point = FlatRedBall.Math.Geometry.Point;

namespace GlueControl.Editing
{
    internal class PolygonPointHandles
    {
        #region Fields/Properties

        List<AxisAlignedRectangle> pointRectangles = new List<AxisAlignedRectangle>();

        Microsoft.Xna.Framework.Vector3 UnsnappedPosition;

        public float? PointSnapSize { get; set; }

        int? PointIndexGrabbed;
        public int? PointIndexHighlighted { get; private set; }

        public bool Visible { get; set; }

        #endregion

        static string pointsVariableToSet = null;
        public void EveryFrameUpdate(PositionedObject item, SelectionMarker selectionMarker)
        {
            pointsVariableToSet = null;
            var itemAsPolygon = item as Polygon;
            // special case for derived polygons, this will handle capsule polygon.
            // If in the future this is a problem, then we will add a new Glux file version:
            var itemType = item?.GetType();
            if (itemType != typeof(Polygon))
            {
                itemAsPolygon = null;
            }

            var elementName = itemType == null
                ? (string)null
                : CommandReceiver.GameElementTypeToGlueElement(itemType?.FullName);

            if (elementName != null && itemAsPolygon == null)
            {
                var element = ObjectFinder.Self.GetElement(elementName);

                var pointsTunneled = element?.CustomVariables.FirstOrDefault(item => item.SourceObjectProperty == "Points");
                if (pointsTunneled != null)
                {
                    var namedObject = element.GetNamedObject(pointsTunneled.SourceObject);
                    if (namedObject.SourceType == SourceType.FlatRedBallType && (namedObject.SourceClassType == "Polygon" || namedObject.SourceClassType == "FlatRedBall.Math.Geometry.Polygon"))
                    {
                        pointsVariableToSet = pointsTunneled.Name;
                        var polygonProperty = item.GetType().GetProperty(namedObject.InstanceName);
                        itemAsPolygon = polygonProperty?.GetValue(item) as Polygon;
                    }
                }
            }

            var isVisible = selectionMarker.Visible && itemAsPolygon != null;

            Visible = isVisible;

            if (!isVisible)
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
                DoCursorHoverActivity(itemAsPolygon);
            }
            if (cursor.PrimaryClick)
            {
                DoCursorClickActivity(itemAsPolygon);
            }
        }

        private void UpdatePointCount(int count)
        {
            while (count < pointRectangles.Count)
            {
                RemovePointRectangle(pointRectangles.LastOrDefault());
            }
            while (count > pointRectangles.Count)
            {
                AddPointRectangle();
            }
        }

        private void UpdatePointsToItem(Polygon asPolygon, SelectionMarker selectionMarker)
        {
            IList<Point> points = null;

            points = asPolygon?.Points;

            if (!selectionMarker.CanMoveItem || points == null)
            {
                UpdatePointCount(0);
            }
            else
            {
                UpdatePointCount(points.Count);

                asPolygon.ForceUpdateDependencies();

                var handledLast = false;
                // for now assume polygons have the last point overlapping:
                for (int i = 0; i < points.Count; i++)
                {
                    var point = points[i];

                    var position = asPolygon.Position;
                    position += asPolygon.RotationMatrix.Right * (float)point.X;
                    position += asPolygon.RotationMatrix.Up * (float)point.Y;

                    var rectangle = pointRectangles[i];

                    rectangle.Position = position;

                    if (i < points.Count - 1 || !handledLast)
                    {
                        if (i == PointIndexHighlighted)
                        {
                            rectangle.Width = ResizeHandles.HighlightedHandleDimension / CameraLogic.CurrentZoomRatio;
                            rectangle.Height = ResizeHandles.HighlightedHandleDimension / CameraLogic.CurrentZoomRatio;
                            if (i == 0)
                            {
                                rectangle = pointRectangles.LastOrDefault();
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
            pointRectangles.Add(rectangle);
        }

        Microsoft.Xna.Framework.Input.Keys addKey = Microsoft.Xna.Framework.Input.Keys.OemPlus;

        private void DoCursorPushActivity(Polygon polygon)
        {
            var cursor = FlatRedBall.Gui.GuiManager.Cursor;
            if (FlatRedBall.Input.InputManager.Keyboard.KeyDown(addKey))
            {
                var points = polygon.Points.ToList();

                polygon.VectorFrom(cursor.WorldX, cursor.WorldY, out int pointIndexBefore);

                FlatRedBall.Math.Geometry.Point newPoint = new FlatRedBall.Math.Geometry.Point(cursor.WorldX, cursor.WorldY);
                newPoint.X -= polygon.X;
                newPoint.Y -= polygon.Y;

                points.Insert(pointIndexBefore + 1, newPoint);

                polygon.Points = points;

                PointIndexGrabbed = pointIndexBefore + 1;
                PointIndexHighlighted = PointIndexGrabbed;
                UnsnappedPosition = polygon.AbsolutePointPosition(PointIndexGrabbed.Value);
            }
            else
            {
                PointIndexGrabbed = null;
                for (int i = 0; i < pointRectangles.Count; i++)
                {
                    var rectangle = pointRectangles[i];
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

        private void DoCursorHoverActivity(Polygon polygon)
        {
            var cursor = FlatRedBall.Gui.GuiManager.Cursor;
            PointIndexHighlighted = null;
            for (int i = 0; i < pointRectangles.Count; i++)
            {
                var rectangle = pointRectangles[i];
                if (cursor.IsOn(rectangle))
                {
                    PointIndexHighlighted = i;
                    break;
                }
            }

            if (FlatRedBall.Input.InputManager.Keyboard.KeyDown(addKey) && cursor.IsOn(polygon))
            {
                var vector = polygon.VectorFrom(cursor.WorldX, cursor.WorldY);

                var rectanglePosition = cursor.WorldPosition.ToVector3() + vector.ToVector3();

                var rect = EditorVisuals.Rectangle(ResizeHandles.DefaultHandleDimension, ResizeHandles.DefaultHandleDimension, rectanglePosition);


            }
        }

        private async void DoCursorClickActivity(Polygon polygon)
        {
            if (polygon != null && PointIndexGrabbed != null)
            {
                PointIndexGrabbed = null;

                await SendPolygonPointsToGlue(polygon);
            }
        }


        private static async Task SendPolygonPointsToGlue(Polygon polygon)
        {
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
                    VariableName = pointsVariableToSet ?? nameof(Polygon.Points),
                    Value = newValue
                });

            await GlueCommands.Self.GluxCommands.SetVariableOnList(assignments, owner);
        }

        public void RemovePointRectangle(AxisAlignedRectangle rectangle)
        {
            FlatRedBall.Screens.ScreenManager.PersistentAxisAlignedRectangles.Remove(rectangle);
            ShapeManager.Remove(rectangle);
            rectangle.Visible = false;
            pointRectangles.Remove(rectangle);
        }

        public void Destroy()
        {
            for (int i = 0; i < pointRectangles.Count; i++)
            {
                FlatRedBall.Screens.ScreenManager.PersistentAxisAlignedRectangles.Remove(pointRectangles[i]);
                pointRectangles[i].Visible = false;
            }
        }

        public bool HandleDelete(Polygon polygon)
        {
            // for now we'll just delete the grabbed point
            if (PointIndexGrabbed != null)
            {
                var points = polygon.Points.ToList();
                points.RemoveAt(PointIndexGrabbed.Value);

                PointIndexGrabbed = null;
                PointIndexHighlighted = null;
                polygon.Points = points;

                SendPolygonPointsToGlue(polygon);

                return true;
            }

            return false;
        }
    }


}

