{CompilerDirectives}

using FlatRedBall;
using FlatRedBall.Entities;
using FlatRedBall.Graphics;
using FlatRedBall.Gui;
using FlatRedBall.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace {ProjectNamespace}.GlueControl.Editing
{
    public static class SelectionLogic
    {
        static List<PositionedObject> tempPunchThroughList = new List<PositionedObject>();

        public static PositionedObject GetInstanceOver(PositionedObject currentEntity, SelectionMarker selectionMarker,
            bool punchThrough, ElementEditingMode elementEditingMode)
        {
            PositionedObject entityOver = null;
            if(currentEntity != null && punchThrough == false)
            {
                if(IsCursorOver(currentEntity) || selectionMarker.IsCursorOverThis())
                {
                    entityOver = currentEntity;
                }
            }

            if(punchThrough)
            {
                tempPunchThroughList.Clear();
            }

            if(entityOver == null)
            {
                IEnumerable<PositionedObject> availableItems = GetAvailableObjects(elementEditingMode);

                if (availableItems != null)
                {
                    foreach (PositionedObject objectAtI in availableItems)
                    {
                        if (IsSelectable(objectAtI) && IsCursorOver(objectAtI))
                        {
                            if (punchThrough)
                            {
                                tempPunchThroughList.Add(objectAtI);
                            }
                            else
                            {
                                entityOver = objectAtI;
                                break;
                            }
                        }
                    }
                }
            }

            if (punchThrough)
            {
                if(tempPunchThroughList.Count == 0)
                {
                    entityOver = null;
                }
                else if(tempPunchThroughList.Count == 1)
                {
                    entityOver = tempPunchThroughList[0];
                }
                else if(tempPunchThroughList.Contains(currentEntity) == false)
                {
                    // just pick the first
                    entityOver = tempPunchThroughList[0];
                }
                else
                {
                    var index = tempPunchThroughList.IndexOf(currentEntity);
                    if(index < tempPunchThroughList.Count - 1)
                    {
                        entityOver = tempPunchThroughList[index + 1];
                    }
                    else
                    {
                        entityOver = tempPunchThroughList[0];
                    }
                }
            }

            return entityOver;
        }

        public static IEnumerable<PositionedObject> GetAvailableObjects(ElementEditingMode elementEditingMode)
        {
            IEnumerable<PositionedObject> availableItems = null;

            if (elementEditingMode == ElementEditingMode.EditingScreen)
            {
                // is it slow to do this every frame?
                availableItems = SpriteManager.ManagedPositionedObjects
                    .Where(item => item is CameraControllingEntity == false)

                    .Concat(ShapeManager.VisibleRectangles.Where(item => item.Parent == null))
                    .Concat(ShapeManager.VisibleCircles.Where(item => item.Parent == null))
                    .Concat(ShapeManager.VisiblePolygons.Where(item => item.Parent == null))

                    ;
            }
            else if (elementEditingMode == ElementEditingMode.EditingEntity)
            {
                if (SpriteManager.ManagedPositionedObjects.Count > 0)
                {
                    availableItems = SpriteManager.ManagedPositionedObjects[0].Children;
                }
            }

            return availableItems;
        }

        private static bool IsSelectable(PositionedObject objectAtI)
        {
#if SupportsEditMode

            return objectAtI.CreationSource == "Glue";
#else
            return false;
#endif
        }

        private static bool IsCursorOver(PositionedObject objectAtI)
        {
            var cursor = GuiManager.Cursor;
            var worldX = cursor.WorldX;
            var worldY = cursor.WorldY;

            GetDimensionsFor(objectAtI, out float minX, out float maxX, out float minY, out float maxY);

            return worldX >= minX &&
                    worldX <= maxX &&
                    worldY >= minY &&
                    worldY <= maxY;
        }

        internal static void GetDimensionsFor(PositionedObject itemOver,
            out float minX, out float maxX, out float minY, out float maxY)
        {
            minX = itemOver.X;
            maxX = itemOver.X;
            minY = itemOver.Y;
            maxY = itemOver.Y;
            GetDimensionsForInner(itemOver, ref minX, ref maxX, ref minY, ref maxY);

            const float minDimension = 16;
            if(maxX - minX < minDimension)
            {
                var extraToAdd = minDimension - (maxX - minX);

                minX -= extraToAdd / 2.0f;
                maxX += extraToAdd / 2.0f;
            }

            if(maxY - minY < minDimension)
            {
                var extraToAdd = minDimension - (maxY - minY);

                minY -= extraToAdd / 2.0f;
                maxY += extraToAdd / 2.0f;
            }
        }

        private static void GetDimensionsForInner(PositionedObject itemOver,
            ref float minX, ref float maxX, ref float minY, ref float maxY)
        {
            if (itemOver is IScalable asScalable)
            {
                minX = Math.Min(minX, itemOver.X - asScalable.ScaleX);
                maxX = Math.Max(maxX, itemOver.X + asScalable.ScaleX);

                minY = Math.Min(minY, itemOver.Y - asScalable.ScaleY);
                maxY = Math.Max(maxY, itemOver.Y + asScalable.ScaleY);
            }
            else if(itemOver is Circle asCircle)
            {
                minX = Math.Min(minX, itemOver.X - asCircle.Radius);
                maxX = Math.Max(maxX, itemOver.X + asCircle.Radius);

                minY = Math.Min(minY, itemOver.Y - asCircle.Radius);
                maxY = Math.Max(maxY, itemOver.Y + asCircle.Radius);
            }
            else
            {
                for (int i = 0; i < itemOver.Children.Count; i++)
                {
                    var child = itemOver.Children[i];

                    GetDimensionsForInner(child, ref minX, ref maxX, ref minY, ref maxY);
                }
            }
        }
    }
}
