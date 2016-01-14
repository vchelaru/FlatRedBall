using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Math;
using FlatRedBall;
using FlatRedBall.Gui;
#if FRB_MDX
using Microsoft.DirectX;
#elif FRB_XNA
using Microsoft.Xna.Framework;
#endif
using FlatRedBall.Input;


namespace EditorObjects
{
    public enum MovementStyle
    {
        Group,
        Hierarchy,
        IgnoreAttachments
    }    

    public static class PositionedObjectMover
    {
        #region Enums



        #endregion

        #region Fields

        private static Vector3 mOriginalGrabPosition;
        private static Vector3 mGrabOffset;

        private static bool mAllowZMovement = false;

        private static PositionedObjectList<PositionedObject> mObjectsToIgnore = new PositionedObjectList<PositionedObject>();


        #endregion

        #region Properties

        public static bool AllowZMovement
        {
            get { return mAllowZMovement; }
            set { mAllowZMovement = value; }
        }

        private static bool HasValidStartPosition
        {
            get
            {
                return !float.IsNaN(mOriginalGrabPosition.X);
            }
        }

        public static PositionedObjectList<PositionedObject> ObjectsToIgnore
        {
            get
            {
                return mObjectsToIgnore;
            }
        }

        #endregion

        #region Methods

        #region Constructor

        static PositionedObjectMover()
        {
            mOriginalGrabPosition = new Vector3(float.NaN, float.NaN, float.NaN);
        }

        #endregion

        #region Public Methods

        public static void MouseMoveObject<T>(T objectToMove) where T : IStaticPositionable
        {
            MouseMoveObject(objectToMove, MovementStyle.Hierarchy);
        }

        public static void MouseMoveObject<T>(T objectToMove, MovementStyle movementStyle) where T : IStaticPositionable
        {
            Cursor cursor = GuiManager.Cursor;

            if (cursor.PrimaryPush)
            {
                return;
            }

            if (GuiManager.Cursor.ActualXVelocityAt(objectToMove.Z) == 0 &&
               GuiManager.Cursor.ActualYVelocityAt(objectToMove.Z) == 0)
            {
                return;
            }
            #region Store the movement in the movementVector
            Vector3 movementVector = new Vector3();

            #region If doing shift movement, then consider original position

#if FRB_MDX
            bool isShiftDown =
                InputManager.Keyboard.KeyDown(Microsoft.DirectX.DirectInput.Key.LeftShift) ||
                InputManager.Keyboard.KeyDown(Microsoft.DirectX.DirectInput.Key.RightShift);
#elif FRB_XNA
            bool isShiftDown =
                InputManager.Keyboard.KeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) ||
                InputManager.Keyboard.KeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift);

#endif


            if (isShiftDown && HasValidStartPosition)
            {
                float xDistanceFromStart = Math.Abs((cursor.WorldXAt(0) - mGrabOffset.X) - mOriginalGrabPosition.X);
                float yDistanceFromStart = Math.Abs((cursor.WorldYAt(0) - mGrabOffset.Y) - mOriginalGrabPosition.Y);

                if (xDistanceFromStart > yDistanceFromStart)
                {
                    movementVector.X = (cursor.WorldXAt(0) - mGrabOffset.X) - objectToMove.X;
                    movementVector.Y = (mOriginalGrabPosition.Y) - objectToMove.Y;
                }
                else
                {
                    movementVector.X = (mOriginalGrabPosition.X) - objectToMove.X;
                    movementVector.Y = (cursor.WorldYAt(0) - mGrabOffset.Y) - objectToMove.Y;
                }
            }

            #endregion

            #region Else, just accumulate normally
            else
            {

                if (cursor.PrimaryDown)
                {
                    movementVector.X = GuiManager.Cursor.WorldXChangeAt(objectToMove.Z);
                    movementVector.Y = GuiManager.Cursor.WorldYChangeAt(objectToMove.Z);
                }
                else if (cursor.SecondaryDown && mAllowZMovement)
                {
                    movementVector.Z = GuiManager.Cursor.WorldYChangeAt(objectToMove.Z);
                    cursor.StaticPosition = true;
                }
            }
            #endregion

            #endregion

            #region Apply the movement vector according to the movementStyle
            switch (movementStyle)
            {
                case MovementStyle.Group:
                    if (objectToMove is PositionedObject)
                    {
                        PositionedObject topParent = (objectToMove as PositionedObject).TopParent;

                        topParent.X += movementVector.X;
                        topParent.Y += movementVector.Y;
                        topParent.Z += movementVector.Z;

                    }
                    else
                    {
                        objectToMove.X += movementVector.X;
                        objectToMove.Y += movementVector.Y;
                        objectToMove.Z += movementVector.Z;
                    }
                    break;

                case MovementStyle.Hierarchy:
                    objectToMove.X += movementVector.X;
                    objectToMove.Y += movementVector.Y;
                    objectToMove.Z += movementVector.Z;

                    if (objectToMove is PositionedObject && (objectToMove as PositionedObject).Parent != null)
                    {
                        (objectToMove as PositionedObject).SetRelativeFromAbsolute();
                    }
                    break;

                case MovementStyle.IgnoreAttachments:
                    objectToMove.X += movementVector.X;
                    objectToMove.Y += movementVector.Y;
                    objectToMove.Z += movementVector.Z;

                    if (objectToMove is PositionedObject)
                    {
                        PositionedObject asPositionedObject = objectToMove as PositionedObject;
                        if (asPositionedObject.Parent != null)
                        {
                            asPositionedObject.SetRelativeFromAbsolute();
                        }
                        foreach(PositionedObject child in asPositionedObject.Children)
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

            #endregion

        }

        public static void MouseMoveObjects<T>(PositionedObjectList<T> listToMove) where T : PositionedObject
        {
            MouseMoveObjects(listToMove, MovementStyle.Hierarchy);
        }

        public static void MouseMoveObjects<T>(PositionedObjectList<T> listToMove, MovementStyle movementStyle) where T : PositionedObject
        {
            if (movementStyle == MovementStyle.Group)
            {
                PositionedObjectList<T> topParents =
                    listToMove.GetTopParents();

                foreach (T positionedObject in topParents)
                {
                    MouseMoveObject(positionedObject, movementStyle);
                }
            }
            else
            {
                foreach (PositionedObject positionedObject in listToMove)
                {
                    MouseMoveObject(positionedObject, movementStyle);
                }
            }
        }

        public static void SetStartPosition(IStaticPositionable newlyGrabbedObject)
        {
            Cursor cursor = GuiManager.Cursor;

            mOriginalGrabPosition = new Vector3(newlyGrabbedObject.X, newlyGrabbedObject.Y, newlyGrabbedObject.Z);

            mGrabOffset = new Vector3(
                cursor.WorldXAt(newlyGrabbedObject.Z) - newlyGrabbedObject.X,
                cursor.WorldYAt(newlyGrabbedObject.Z) - newlyGrabbedObject.Y,
                0);
        }

        #endregion

        #endregion
    }
}
