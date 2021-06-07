using FlatRedBall;
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
        public static PositionedObject GetEntityOver()
        {
            for (int i = 0; i < SpriteManager.ManagedPositionedObjects.Count; i++)
            {
                var objectAtI = SpriteManager.ManagedPositionedObjects[i] as PositionedObject;

                if (IsCursorOver(objectAtI))
                {
                    return objectAtI;
                }
            }

            return null;
        }

        private static bool IsCursorOver(PositionedObject objectAtI)
        {
            var cursor = GuiManager.Cursor;
            var worldX = cursor.WorldX;
            var worldY = cursor.WorldY;

            if (objectAtI is IScalable asScalable)
            {
                if (worldX >= objectAtI.X - asScalable.ScaleX &&
                    worldX <= objectAtI.X + asScalable.ScaleX &&
                    worldY >= objectAtI.Y - asScalable.ScaleY &&
                    worldY <= objectAtI.Y + asScalable.ScaleY
                    )
                {
                    return true;
                }
            }

            for (int i = 0; i < objectAtI.Children.Count; i++)
            {
                var isOverChild = IsCursorOver(objectAtI.Children[i]);
                if (isOverChild)
                {
                    return true;
                }
            }

            return false;
        }

        internal static void GetDimensionsFor(PositionedObject itemOver,
            out float minX, out float maxX, out float minY, out float maxY)
        {
            minX = itemOver.X;
            maxX = itemOver.X;
            minY = itemOver.Y;
            maxY = itemOver.Y;
            GetDimensionsForInner(itemOver, ref minX, ref maxX, ref minY, ref maxY);
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
