using FlatRedBall;
using FlatRedBall.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueControl.Editing
{
    internal class PolygonPointHandles
    {
        List<AxisAlignedRectangle> rectangles = new List<AxisAlignedRectangle>();


        int? PointIndexGrabbed;
        public int? PointIndexHighlighted { get; private set; }

        public bool Visible { get; set; }

        public void Destroy()
        {
            for (int i = 0; i < rectangles.Count; i++)
            {
                FlatRedBall.Screens.ScreenManager.PersistentAxisAlignedRectangles.Remove(rectangles[i]);
                rectangles[i].Visible = false;
            }
        }

        public void EveryFrameUpdate(PositionedObject item, SelectionMarker selectionMarker)
        {
            var itemAsPolygon = item as Polygon;
            Visible = selectionMarker.Visible && itemAsPolygon != null;

            if (itemAsPolygon == null)
            {
                UpdatePointCount(0);
            }

            ///////////Early Out//////////////
            if (!Visible || selectionMarker.CanMoveItem == false)
                return;
            ////////End Early Out/////////////

            var cursor = FlatRedBall.Gui.GuiManager.Cursor;

            UpdatePointsToItem(itemAsPolygon);

            if (cursor.PrimaryPush)
            {
                DoCursorPushActivity();
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
                DoCursorClickActivity();
            }
        }

        private void UpdatePointCount(int count)
        {
            while (count < rectangles.Count)
            {
                rectangles.Remove(rectangles.LastOrDefault());
            }
            while (count > rectangles.Count)
            {
                AddPointRectangle();
            }
        }

        private void UpdatePointsToItem(Polygon itemAsPolygon)
        {
            UpdatePointCount(itemAsPolygon.Points.Count);

            itemAsPolygon.ForceUpdateDependencies();

            for (int i = 0; i < itemAsPolygon.Points.Count; i++)
            {
                var position = itemAsPolygon.AbsolutePointPosition(i);

                rectangles[i].Position = position;
            }
        }

        public void AddPointRectangle()
        {
            var rectangle = new AxisAlignedRectangle();
            rectangle.Width = ResizeHandles.DefaultHandleDimension;
            rectangle.Height = ResizeHandles.DefaultHandleDimension;

            FlatRedBall.Screens.ScreenManager.PersistentAxisAlignedRectangles.Remove(rectangle);
            ShapeManager.AddToLayer(rectangle, SpriteManager.TopLayer, makeAutomaticallyUpdated: false);
            rectangle.Visible = true;
            rectangles.Add(rectangle);
        }


        private void DoCursorPushActivity()
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
                    break;
                }
            }
        }

        private void DoCursorDownActivity(Polygon polygon)
        {
            var cursor = FlatRedBall.Gui.GuiManager.Cursor;
            ///////////////Early Out////////////
            if (PointIndexGrabbed == null || (cursor.ScreenXChange == 0 && cursor.ScreenYChange == 0))
            {
                return;
            }
            /////////////End Early Out//////////

            var isEndpoint = PointIndexGrabbed == 0 || PointIndexGrabbed == polygon.Points.Count - 1;

            var areEndPointsOverlapping =
                polygon.AbsolutePointPosition(0) == polygon.AbsolutePointPosition(polygon.Points.Count - 1);

            // todo - move it!
            var absolutePointAtIndex = polygon.AbsolutePointPosition(PointIndexGrabbed.Value);

            absolutePointAtIndex.X += cursor.ScreenXChange;
            absolutePointAtIndex.Y += -cursor.ScreenYChange;

            if (isEndpoint && areEndPointsOverlapping)
            {
                polygon.SetPointFromAbsolutePosition(0, absolutePointAtIndex.X, absolutePointAtIndex.Y);
                polygon.SetPointFromAbsolutePosition(polygon.Points.Count - 1, absolutePointAtIndex.X, absolutePointAtIndex.Y);

            }
            else
            {
                polygon.SetPointFromAbsolutePosition(PointIndexGrabbed.Value, absolutePointAtIndex.X, absolutePointAtIndex.Y);
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

        private void DoCursorClickActivity()
        {
            PointIndexGrabbed = null;
        }
    }

}

