{CompilerDirectives}


using FlatRedBall;
using FlatRedBall.Entities;
using FlatRedBall.Graphics;
using FlatRedBall.Gui;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Utilities;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GlueControl.Managers;

namespace GlueControl.Editing
{
    public static class SelectionLogic
    {
        #region Fields/Properties

        static List<PositionedObject> tempPunchThroughList = new List<PositionedObject>();
        static Vector2? PushStartLocation;

        public static float? LeftSelect { get; private set; }
        public static float? RightSelect;
        public static float? TopSelect;
        public static float? BottomSelect;


        public static bool PerformedRectangleSelection { get; private set; }

        #endregion

        public static void GetItemsOver(List<INameable> currentEntities, List<INameable> itemsOverToFill, List<ISelectionMarker> currentSelectionMarkers,
            bool punchThrough, ElementEditingMode elementEditingMode)
        {
            if (GuiManager.Cursor.SecondaryPush)
            {
                int m = 3;
            }
            if (itemsOverToFill.Count > 0)
            {
                itemsOverToFill.Clear();
            }

            INameable objectOver = null;

            var ray = GuiManager.Cursor.GetRay();

            if (currentEntities.Count > 0 && punchThrough == false)
            {
                // Vic asks - why do we use the the current entities rather than the markers?
                var currentObjectOver = currentEntities.FirstOrDefault(item =>
                {
                    return item is PositionedObject asPositionedObject && IsCursorOver(item as PositionedObject, ray);
                });
                if (currentObjectOver == null)
                {
                    var markerOver = currentSelectionMarkers.FirstOrDefault(item => item.IsCursorOverThis());
                    if (markerOver != null)
                    {
                        var index = currentSelectionMarkers.IndexOf(markerOver);
                        currentObjectOver = currentEntities[index];
                    }
                }
                objectOver = currentObjectOver;
            }

            if (punchThrough)
            {
                tempPunchThroughList.Clear();
            }

            IEnumerable<PositionedObject> availableItems = null;

            if (objectOver == null)
            {
                availableItems = GetAvailableObjects(elementEditingMode);

                if (availableItems != null)
                {
                    // here we sort every frame. This could be slow if we have a lot of objects so we may need to cache this somehow
                    foreach (var objectAtI in availableItems.OrderByDescending(item => item.Z))
                    {
                        if (IsSelectable(objectAtI))
                        {
                            if (IsCursorOver(objectAtI, ray))
                            {
                                var nos =
                                    GlueState.Self.CurrentElement?.AllNamedObjects.FirstOrDefault(
                                        item => item.InstanceName == objectAtI.Name);

                                // don't select it if it is locked
                                var isLocked = nos?.IsEditingLocked == true;

                                var isEditingLocked = nos != null &&
                                    ObjectFinder.Self.GetPropertyValueRecursively<bool>(
                                        nos, nameof(nos.IsEditingLocked));
                                if (!isEditingLocked)
                                {
                                    if (punchThrough)
                                    {
                                        tempPunchThroughList.Add(objectAtI);
                                    }
                                    else
                                    {
                                        objectOver = objectAtI;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (punchThrough)
            {
                if (tempPunchThroughList.Count == 0)
                {
                    objectOver = null;
                }
                else if (tempPunchThroughList.Count == 1)
                {
                    objectOver = tempPunchThroughList[0];
                }
                else if (tempPunchThroughList.Any(item => currentEntities.Contains(item)) == false)
                {
                    // just pick the first
                    objectOver = tempPunchThroughList[0];
                }
                else
                {
                    var index = tempPunchThroughList.IndexOf(currentEntities.FirstOrDefault() as PositionedObject);
                    if (index < tempPunchThroughList.Count - 1)
                    {
                        objectOver = tempPunchThroughList[index + 1];
                    }
                    else
                    {
                        objectOver = tempPunchThroughList[0];
                    }
                }
            }

            if (PerformedRectangleSelection && availableItems != null)
            {
                foreach (var item in availableItems)
                {
                    if (IsSelectable(item) && IsRectangleSelectionOver(item))
                    {
                        itemsOverToFill.Add(item);
                    }
                }
            }
            // If doing a rectangle selection, no need to also add the item over, it will be part of the rectangle:
            else if (objectOver != null)
            {
                itemsOverToFill.Add(objectOver);
            }
        }

        internal static void DoDragSelectLogic()
        {
            var cursor = GuiManager.Cursor;

            PerformedRectangleSelection = false;

            if (cursor.PrimaryDown == false && !cursor.PrimaryClick)
            {
                LeftSelect = null;
                RightSelect = null;
                TopSelect = null;
                BottomSelect = null;
            }

            if (cursor.PrimaryPush)
            {
                if (FlatRedBallServices.Game.IsActive)
                {
                    PushStartLocation = cursor.WorldPosition;
                    LeftSelect = null;
                    RightSelect = null;
                    TopSelect = null;
                    BottomSelect = null;
                }
                else
                {
                    PushStartLocation = null;
                }
            }
            if (cursor.PrimaryDown && PushStartLocation != null)
            {
                LeftSelect = Math.Min(PushStartLocation.Value.X, cursor.WorldX);
                RightSelect = Math.Max(PushStartLocation.Value.X, cursor.WorldX);

                TopSelect = Math.Max(PushStartLocation.Value.Y, cursor.WorldY);
                BottomSelect = Math.Min(PushStartLocation.Value.Y, cursor.WorldY);

                var centerX = (LeftSelect.Value + RightSelect.Value) / 2.0f;
                var centerY = (TopSelect.Value + BottomSelect.Value) / 2.0f;

                var width = RightSelect.Value - LeftSelect.Value;
                var height = TopSelect.Value - BottomSelect.Value;

                Color selectionColor = Color.LightBlue;

                EditorVisuals.Rectangle(width, height, new Vector3(centerX, centerY, 0), selectionColor);
            }
            if (cursor.PrimaryClick)
            {
                if (FlatRedBallServices.Game.IsActive == false)
                {
                    PushStartLocation = null;
                }
                else
                {
                    // get all things within this rect...
                    PerformedRectangleSelection = LeftSelect != RightSelect && TopSelect != BottomSelect;
                }
            }
        }

        public static void DoInactiveWindowLogic()
        {
            PerformedRectangleSelection = false;
        }

        public static IEnumerable<PositionedObject> GetAvailableObjects(ElementEditingMode elementEditingMode)
        {
            IEnumerable<PositionedObject> availableItems = null;

            if (elementEditingMode == ElementEditingMode.EditingScreen)
            {
                // is it slow to do this every frame?
                availableItems = SpriteManager.ManagedPositionedObjects
                    // We check for null parents so we don't grab an object that is embedded inside an entity instance.
                    .Where(item => item is CameraControllingEntity == false && item.Parent == null)
                    .Concat(SpriteManager.AutomaticallyUpdatedSprites.Where(item => item.Parent == null))
                    .Concat(TextManager.AutomaticallyUpdatedTexts.Where(item => item.Parent == null))
                    .Concat(ShapeManager.VisibleRectangles.Where(item => item.Parent == null))
                    .Concat(ShapeManager.VisibleCircles.Where(item => item.Parent == null))
                    .Concat(ShapeManager.VisiblePolygons.Where(item => item.Parent == null))
                    ;
            }
            else if (elementEditingMode == ElementEditingMode.EditingEntity)
            {
                var screen = FlatRedBall.Screens.ScreenManager.CurrentScreen as Screens.EntityViewingScreen;
                var entity = (screen.CurrentEntity as PositionedObject);
                if (entity != null)
                {
                    availableItems = entity.Children;
                }
                else if (SpriteManager.ManagedPositionedObjects.Count > 0)
                {
                    availableItems = SpriteManager.ManagedPositionedObjects[0].Children;
                }
            }

            return availableItems;
        }

        public static bool IsSelectable(INameable nameable)
        {
#if SupportsEditMode
            if(nameable is PositionedObject positionedObject)
            {
                return positionedObject.CreationSource == "Glue" && 
                    (positionedObject is FlatRedBall.TileGraphics.LayeredTileMap) == false ;
            }
            else if(nameable is NameableWrapper)
            {
                return true;
            }
            // Do we want to make all INameables supported? I dont' know yet, so for now let's add one-by-one
            else if(nameable is FlatRedBall.Math.Paths.Path)
            {
                return true;
            }
            else
            {
                return nameable is FlatRedBall.TileCollisions.TileShapeCollection;
            }
#else
            return false;
#endif
        }

        static PolygonFast polygonForCursorOver = new PolygonFast();

        private static bool IsCursorOver(PositionedObject positionedObject, Ray ray)
        {
            if (IsOverSpecificItem(positionedObject, ray))
            {
                return true;
            }
            else
            {
                for (int i = 0; i < positionedObject.Children.Count; i++)
                {
                    var child = positionedObject.Children[i];

                    var shouldConsiderChild = true;

                    if (child is IVisible asIVisible)
                    {
                        shouldConsiderChild = asIVisible.Visible;
                    }

                    if (shouldConsiderChild)
                    {
                        var isOverChild = IsCursorOver(child, ray);
                        if (isOverChild)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private static bool IsOverSpecificItem(IStaticPositionable collisionObject, Ray ray)
        {
            GetShapeFor(collisionObject, out PolygonFast polygon, out Circle circle);
            if (polygon != null)
            {
                //return polygon.IsMouseOver(GuiManager.Cursor);
                Matrix inverseRotation = polygon.RotationMatrix;

                Matrix.Invert(ref inverseRotation, out inverseRotation);

                Plane plane = new Plane(polygon.Position, polygon.Position + polygon.RotationMatrix.Up,
                    polygon.Position + polygon.RotationMatrix.Right);

                float? result = ray.Intersects(plane);

                if (!result.HasValue)
                {
                    return false;
                }

                Vector3 intersection = ray.Position + ray.Direction * result.Value;


                return polygon.IsPointInside(ref intersection);
            }
            else if (circle != null)
            {
                return circle.IsMouseOver(GuiManager.Cursor);
            }
            else
            {
                return false;
            }
        }

        public class PolygonFast
        {
            public List<FlatRedBall.Math.Geometry.Point> Points = new List<FlatRedBall.Math.Geometry.Point>();
            public Matrix RotationMatrix;
            public Vector3 Position;

            static List<Vector3> pointsForIsPointInside = new List<Vector3>();

            public bool IsPointInside(ref Vector3 vector)
            {
                while (Points.Count > pointsForIsPointInside.Count)
                {
                    pointsForIsPointInside.Add(new Vector3());
                }

                for (int i = 0; i < Points.Count; i++)
                {
                    pointsForIsPointInside[i] = Position +
                        (RotationMatrix.Right * (float)Points[i].X) +
                        (RotationMatrix.Up * (float)Points[i].Y);
                }

                bool b = false;
                for (int i = 0, j = Points.Count - 1; i < Points.Count; j = i++)
                {

                    if ((((pointsForIsPointInside[i].Y <= vector.Y) && (vector.Y < pointsForIsPointInside[j].Y)) || ((pointsForIsPointInside[j].Y <= vector.Y) && (vector.Y < pointsForIsPointInside[i].Y))) &&
                    (vector.X < (pointsForIsPointInside[j].X - pointsForIsPointInside[i].X) * (vector.Y - pointsForIsPointInside[i].Y) / (pointsForIsPointInside[j].Y - pointsForIsPointInside[i].Y) + pointsForIsPointInside[i].X)) b = !b;
                }
                return b;
            }
        }

        public static void GetShapeFor(IStaticPositionable collisionObject, out PolygonFast polygon, out Circle circle)
        {
            circle = null;
            polygon = null;

            void MakePolygonRectangle(float width, float height)
            {
                if (polygonForCursorOver.Points?.Count != 5)
                {
                    var points = new List<FlatRedBall.Math.Geometry.Point>();
                    points.Add(new FlatRedBall.Math.Geometry.Point(-width / 2, height / 2));
                    points.Add(new FlatRedBall.Math.Geometry.Point(width / 2, height / 2));
                    points.Add(new FlatRedBall.Math.Geometry.Point(width / 2, -height / 2));
                    points.Add(new FlatRedBall.Math.Geometry.Point(-width / 2, -height / 2));
                    points.Add(new FlatRedBall.Math.Geometry.Point(-width / 2, height / 2));
                    polygonForCursorOver.Points = points;
                }
                else
                {
                    polygonForCursorOver.Points[0] = new FlatRedBall.Math.Geometry.Point(-width / 2, height / 2);
                    polygonForCursorOver.Points[1] = new FlatRedBall.Math.Geometry.Point(width / 2, height / 2);
                    polygonForCursorOver.Points[2] = new FlatRedBall.Math.Geometry.Point(width / 2, -height / 2);
                    polygonForCursorOver.Points[3] = new FlatRedBall.Math.Geometry.Point(-width / 2, -height / 2);
                    polygonForCursorOver.Points[4] = new FlatRedBall.Math.Geometry.Point(-width / 2, height / 2);
                }
            }

            void MakePolygonRectangleMinMax(float minXInner, float maxXInner, float minYInner, float maxYInner)
            {
                MakePolygonRectangle(maxXInner - minXInner, maxYInner - minYInner);
                polygonForCursorOver.Position.X = (maxXInner + minXInner) / 2.0f;
                polygonForCursorOver.Position.Y = (maxYInner + minYInner) / 2.0f;
            }

            float minX = 0;
            float maxX = 0;
            float minY = 0;
            float maxY = 0;
            if (collisionObject is Circle asCircle)
            {
                circle = asCircle;
            }
            else if (collisionObject is IMinMax minMax)
            {
                minX = minMax.MinXAbsolute;
                maxX = minMax.MaxXAbsolute;

                minY = minMax.MinYAbsolute;
                maxY = minMax.MaxYAbsolute;

                MakePolygonRectangleMinMax(minX, maxX, minY, maxY);
                polygon = polygonForCursorOver;

            }
            else if (collisionObject is Text asText)
            {
                switch (asText.HorizontalAlignment)
                {
                    case HorizontalAlignment.Left:
                        minX = asText.X;
                        maxX = asText.X + asText.Width;

                        break;
                    case HorizontalAlignment.Right:
                        minX = asText.X - asText.Width;
                        maxX = asText.X;

                        break;
                    case HorizontalAlignment.Center:
                        minX = asText.X - asText.Width / 2.0f;
                        maxX = asText.X + asText.Width / 2.0f;
                        break;
                }

                // todo - support alignment
                minY = asText.Y - asText.Height / 2.0f;
                maxY = asText.Y + asText.Height / 2.0f;

                MakePolygonRectangleMinMax(minX, maxX, minY, maxY);
                polygon = polygonForCursorOver;

                polygon.RotationMatrix = asText.RotationMatrix;
            }
            else if (collisionObject is IReadOnlyScalable asScalable)
            {
                minX = collisionObject.X - asScalable.ScaleX;
                maxX = collisionObject.X + asScalable.ScaleX;

                minY = collisionObject.Y - asScalable.ScaleY;
                maxY = collisionObject.Y + asScalable.ScaleY;

                MakePolygonRectangleMinMax(minX, maxX, minY, maxY);
                polygon = polygonForCursorOver;

                if (collisionObject is PositionedObject positionedObject &&
                    // Not if it's an AARect - that can't rotate
                    collisionObject is AxisAlignedRectangle == false)
                {
                    polygon.RotationMatrix = positionedObject.RotationMatrix;
                }
                else
                {
                    polygon.RotationMatrix = Matrix.Identity;
                }
            }
            else if (collisionObject is Line asLine)
            {
                minX = asLine.X + (float)asLine.RelativePoint1.X;
                maxX = asLine.X + (float)asLine.RelativePoint1.X;

                minY = asLine.Y - (float)asLine.RelativePoint1.Y;
                maxY = asLine.Y + (float)asLine.RelativePoint1.Y;

                minX = Math.Min(minX, asLine.X + (float)asLine.RelativePoint2.X);
                maxX = Math.Max(maxX, asLine.X + (float)asLine.RelativePoint2.X);

                minY = Math.Min(minY, asLine.Y - (float)asLine.RelativePoint2.Y);
                maxY = Math.Max(maxY, asLine.Y + (float)asLine.RelativePoint2.Y);

                MakePolygonRectangleMinMax(minX, maxX, minY, maxY);
                polygon = polygonForCursorOver;

                polygon.RotationMatrix = asLine.RotationMatrix;
            }
#if HasGum
            else if (collisionObject is GumCoreShared.FlatRedBall.Embedded.PositionedObjectGueWrapper gumWrapper)
            {
                var gue = gumWrapper.GumObject;

                if (gue.Visible)
                {
                    var absoluteOrigin = gumWrapper.GetAbsolutePositionInFrbSpace(gue);

                    // assume top left origin for now
                    minX = absoluteOrigin.X - gue.GetAbsoluteWidth() / 2.0f;
                    maxX = absoluteOrigin.X + gue.GetAbsoluteWidth() / 2.0f;

                    minY = absoluteOrigin.Y - gue.GetAbsoluteHeight() / 2.0f;
                    maxY = absoluteOrigin.Y + gue.GetAbsoluteHeight() / 2.0f;

                    MakePolygonRectangleMinMax(minX, maxX, minY, maxY);
                    polygon = polygonForCursorOver;

                    polygon.RotationMatrix = gumWrapper.RotationMatrix;
                }
            }

#endif
            else if (collisionObject is Polygon asPolygon)
            {
                polygon = polygon ?? new PolygonFast();

                polygon.Points = asPolygon.Points.ToList();
                polygon.Position = asPolygon.Position;
                polygon.RotationMatrix = asPolygon.RotationMatrix;
            }
        }

        private static bool IsRectangleSelectionOver(PositionedObject item)
        {
            GetDimensionsFor(item, out float minX, out float maxX, out float minY, out float maxY);

            return RightSelect != null &&
                    RightSelect.Value >= minX &&
                    LeftSelect.Value <= maxX &&
                    TopSelect.Value >= minY &&
                    BottomSelect.Value <= maxY;
        }

        #region Get Dimensions

        static void UpdateMinsAndMaxes(PolygonFast polygon,
            ref float minX, ref float maxX, ref float minY, ref float maxY)
        {
            var pointCount = polygon.Points.Count;
            var right = polygon.RotationMatrix.Right;
            var up = polygon.RotationMatrix.Up;
            var absolutePoint = polygon.Position;

            for (int i = 0; i < pointCount; i++)
            {
                var relativePoint = polygon.Points[i];

                absolutePoint = polygon.Position;
                absolutePoint.X += (float)relativePoint.X * right.X;
                absolutePoint.Y += (float)relativePoint.X * right.Y;

                absolutePoint.X += (float)relativePoint.Y * up.X;
                absolutePoint.Y += (float)relativePoint.Y * up.Y;

                minX = Math.Min(minX, absolutePoint.X);
                maxX = Math.Max(maxX, absolutePoint.X);
                minY = Math.Min(minY, absolutePoint.Y);
                maxY = Math.Max(maxY, absolutePoint.Y);
            }
        }

        static void UpdateMinsAndMaxes(Circle circle,
            ref float minX, ref float maxX, ref float minY, ref float maxY)
        {
            minX = Math.Min(minX, circle.X - circle.Radius);
            maxX = Math.Max(maxX, circle.X + circle.Radius);
            minY = Math.Min(minY, circle.Y - circle.Radius);
            maxY = Math.Max(maxY, circle.Y + circle.Radius);
        }


        internal static void GetDimensionsFor(IStaticPositionable itemOver,
            out float minX, out float maxX, out float minY, out float maxY)
        {
            // We used to use the position as part of the min and max bounds, but this causes problems
            // if some objects are only visible when the cursor is over them. Therefore, always use half dimension
            // width for selection:
            minX = itemOver.X;
            maxX = itemOver.X;
            minY = itemOver.Y;
            maxY = itemOver.Y;

            GetDimensionsForInner(itemOver, ref minX, ref maxX, ref minY, ref maxY);

            float minDimension = 0;

            // if it's scalable, then we should show the 
            // bounds exactly as is, so it can be resized.
            // Otherwise, give it a min dimension in case it's
            // empty or really small.
            var isScalable = itemOver is IReadOnlyScalable;
            if (!isScalable)
            {
                var multiplier = Camera.Main.OrthogonalHeight / Camera.Main.DestinationRectangle.Height;
                minDimension = 16 * multiplier;
            }

            if (maxX - minX < minDimension)
            {
                var extraToAdd = minDimension - (maxX - minX);

                minX -= extraToAdd / 2.0f;
                maxX += extraToAdd / 2.0f;
            }

            if (maxY - minY < minDimension)
            {
                var extraToAdd = minDimension - (maxY - minY);

                minY -= extraToAdd / 2.0f;
                maxY += extraToAdd / 2.0f;
            }


        }

        private static void GetDimensionsForInner(IStaticPositionable itemOver,
            ref float minX, ref float maxX, ref float minY, ref float maxY)
        {
            GetShapeFor(itemOver, out PolygonFast polygon, out Circle circle);

            if (polygon != null)
            {
                UpdateMinsAndMaxes(polygon, ref minX, ref maxX, ref minY, ref maxY);
            }
            else if (circle != null)
            {
                UpdateMinsAndMaxes(circle, ref minX, ref maxX, ref minY, ref maxY);
            }

            if (itemOver is PositionedObject positionedObject)
            {
                for (int i = 0; i < positionedObject.Children.Count; i++)
                {
                    var child = positionedObject.Children[i];

                    var shouldConsiderChild = true;

                    if (child is IVisible asIVisible)
                    {
                        shouldConsiderChild = asIVisible.Visible;
                    }

                    if (shouldConsiderChild)
                    {
                        GetDimensionsForInner(child, ref minX, ref maxX, ref minY, ref maxY);
                    }
                }
            }
        }

        #endregion

    }


    #region IMinMax

    public interface IMinMax
    {
        float MinXAbsolute { get; }
        float MaxXAbsolute { get; }
        float MinYAbsolute { get; }
        float MaxYAbsolute { get; }
    }

    #endregion

}
