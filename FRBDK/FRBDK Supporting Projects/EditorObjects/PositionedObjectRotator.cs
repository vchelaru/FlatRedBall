using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Math;
using FlatRedBall.Gui;
using FlatRedBall;

namespace EditorObjects
{
    public static class PositionedObjectRotator
    {
        #region Fields

        private static PositionedObjectList<PositionedObject> mObjectsToIgnore = new PositionedObjectList<PositionedObject>();

        #endregion

        #region Properties

        public static PositionedObjectList<PositionedObject> ObjectsToIgnore
        {
            get
            {
                return mObjectsToIgnore;
            }
        }

        #endregion

        #region Methods

        public static void MouseRotateObject<T>(T objectToMove, MovementStyle movementStyle) where T : IRotatable
        {
            Cursor cursor = GuiManager.Cursor;

            if (cursor.YVelocity != 0)
                cursor.StaticPosition = true;

            float movementAmount = cursor.YVelocity / 8.0f;

            switch (movementStyle)
            {
                case MovementStyle.Group:
                    objectToMove.RotationZ += movementAmount;
                    break;

                case MovementStyle.Hierarchy:
                    objectToMove.RotationZ += movementAmount;

                    if (objectToMove is PositionedObject && (objectToMove as PositionedObject).Parent != null)
                    {
                        (objectToMove as PositionedObject).SetRelativeFromAbsolute();
                    }
                    break;

                case MovementStyle.IgnoreAttachments:
                    objectToMove.RotationZ += movementAmount;

                    if (objectToMove is PositionedObject)
                    {
                        PositionedObject asPositionedObject = objectToMove as PositionedObject;
                        if (asPositionedObject.Parent != null)
                        {
                            asPositionedObject.SetRelativeFromAbsolute();
                        }
                        foreach (PositionedObject child in asPositionedObject.Children)
                        {
                            //child.Position -= movementVector;
                            if (mObjectsToIgnore.Contains(child) == false)
                            {
                                child.SetRelativeFromAbsolute();
                            }
                        }
                    }
                    break;

            }
        }

        public static void MouseRotateObjects<T>(PositionedObjectList<T> listToMove, MovementStyle movementStyle) where T : PositionedObject
        {
            if (movementStyle == MovementStyle.Group)
            {
                PositionedObjectList<T> topParents =
                    listToMove.GetTopParents();

                foreach (T positionedObject in topParents)
                {
                    MouseRotateObject(positionedObject, movementStyle);
                }
            }
            else
            {
                foreach (PositionedObject positionedObject in listToMove)
                {
                    MouseRotateObject(positionedObject, movementStyle);
                }
            }
        }

        #endregion
    }
}
