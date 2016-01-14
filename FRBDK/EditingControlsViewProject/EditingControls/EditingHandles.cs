using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework;
using FlatRedBall.Math;
using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.Graphics;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Input;

namespace EditingControls
{
    public class EditingHandles
    {
        #region Fields

        // The order will be:
        // 0----1----2
        // |         |
        // |         |
        // 7         3
        // |         |
        // |         |
        // 6----5----4
        Circle[] mCircles = new Circle[8];

        Circle mCircleOver;
        Circle mCircleGrabbed;

        bool mOverEntireObject = false;
        bool mGrabbedEntireObject = false;

        object mSelectedObject;


        public Color Color = Color.White;

        UndoManager mUndoManager = new UndoManager();

        #endregion

        #region Properties

        public bool IsOnHandles
        {
            get
            {
                return mCircleOver != null || mCircleGrabbed != null;
            }
        }

        float Z
        {
            get
            {
                return mCircles[0].Z;
            }
        }

        public object SelectedObject
        {
            get { return mSelectedObject; }
            set
            {
                mSelectedObject = value;
                UpdateToSelectedObject();


            }
        }

        public bool UpdatesWindowsCursor
        {
            get;
            set;
        }

        #endregion

        public event EventHandler SelectedObjectChanged;

        public EditingHandles()
        {
            for (int i = 0; i < 8; i++)
            {
                mCircles[i] = new Circle();
            }

            UpdatesWindowsCursor = true;
        }

        public void Activity()
        {
            

            foreach (var circle in mCircles)
            {
                circle.Color = Color;
            }

            EditingActivity();

            UpdateToSelectedObject();

            UpdateWindowsCursor();
        }

        private void UpdateWindowsCursor()
        {
            if (UpdatesWindowsCursor)
            {
                int circleIndex = -1;
                if (mCircleGrabbed != null)
                {
                    circleIndex = IndexOf(mCircleGrabbed, mCircles);
                }
                else if (mCircleOver != null)
                {
                    circleIndex = IndexOf(mCircleOver, mCircles);
                }

                if (circleIndex != -1)
                {
                    switch (circleIndex)
                    {
                        case 0:
                        case 4:
                            System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.SizeNWSE;
                            break;
                        case 1:
                        case 5:
                            System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.SizeNS;
                            break;
                        case 2:
                        case 6:
                            System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.SizeNESW;
                            break;
                        case 3:
                        case 7:
                            System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.SizeWE;
                            break;
                    }
                }
                else if (mOverEntireObject)
                {
                    System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.SizeAll;
                }
                else
                {
                    System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Arrow;

                }

            }
        }

        private void EditingActivity()
        {
            NoDownActivity();

            PushActivity();

            DragActivity();

            ClickActivity();

            UndoActivity();
        }

        private void UndoActivity()
        {
            if (InputManager.Keyboard.ControlZPushed())
            {
                mUndoManager.PerformUndo();
            }
        }

        private void NoDownActivity()
        {
            if (mSelectedObject != null)
            {
                mCircleOver = GetCircleOver();
                mOverEntireObject = mCircleOver == null && GetIsCursorOverGrabbedObject();
            }
            else
            {
                mCircleOver = null;
                mOverEntireObject = false;
            }
        }

        private void PushActivity()
        {
            Cursor cursor = GuiManager.Cursor;

            if (cursor.PrimaryPush)
            {
                mCircleGrabbed = mCircleOver;
                mGrabbedEntireObject = mOverEntireObject;

                if (mSelectedObject != null)
                {
                    mUndoManager.SaveSnapshot(mSelectedObject);
                }
            }

        }

        bool GetIsCursorOverGrabbedObject()
        {
            Cursor cursor = GuiManager.Cursor;

            if (mSelectedObject is IClickable)
            {
                return ((IClickable)mSelectedObject).HasCursorOver(GuiManager.Cursor);
            }
            else if (mSelectedObject is Circle)
            {
                return cursor.IsOn3D(mSelectedObject as Circle);
            }
            else if (mSelectedObject is Polygon)
            {
                return cursor.IsOn3D(mSelectedObject as Polygon);
            }
            else if (mSelectedObject is AxisAlignedRectangle)
            {
                return cursor.IsOn3D(mSelectedObject as AxisAlignedRectangle);
            }
            else if (mSelectedObject is Text)
            {
                return cursor.IsOn3D(mSelectedObject as Text);
            }
            else if (mSelectedObject is Sprite)
            {
                return cursor.IsOn3D(mSelectedObject as Sprite);
            }
            else if (mSelectedObject is SpriteFrame)
            {
                return cursor.IsOn3D(mSelectedObject as SpriteFrame);
            }
            else if (mSelectedObject is IScalable && mSelectedObject is IStaticPositionable)
            {
                float cursorWorldX = cursor.WorldXAt(0);
                float cursorWorldY = cursor.WorldYAt(0);

                IStaticPositionable positionable = SelectedObject as IStaticPositionable;
                IScalable scalable = SelectedObject as IScalable;


                return cursorWorldX > positionable.X - scalable.ScaleX &&
                    cursorWorldX < positionable.X + scalable.ScaleX &&
                    cursorWorldY > positionable.Y - scalable.ScaleY &&
                    cursorWorldY < positionable.Y + scalable.ScaleY;

            }
            return false;

        }

        private Circle GetCircleOver()
        {
            Cursor cursor = GuiManager.Cursor;

            // Are we over any of the handles?
            foreach (Circle circle in mCircles)
            {
                if (cursor.IsOn3D(circle))
                {
                    return circle;
                }
            }
            return null;
        }

        private void DragActivity()
        {
            Cursor cursor = GuiManager.Cursor;

            if (mSelectedObject != null && cursor.PrimaryDown && 
                (mCircleGrabbed != null || mGrabbedEntireObject) )
            {
                float worldX = cursor.WorldXAt(Z);
                float worldY = cursor.WorldYAt(Z);

                float xChange = cursor.WorldXChangeAt(0);
                float yChange = cursor.WorldYChangeAt(0);

                ApplyHandleChange(xChange, yChange);

            }
        }

        private void ApplyHandleChange(float xChange, float yChange)
        {

            float xChangeMultiplier;
            float yChangeMultiplier;
            float scaleXMultiplier;
            float scaleYMultiplier;
            GetChangeAndScaleMultipliers(out xChangeMultiplier, out yChangeMultiplier, out scaleXMultiplier, out scaleYMultiplier);


            IStaticPositionable positionable = SelectedObject as IStaticPositionable;

            PositionedObject positionedObject = SelectedObject as PositionedObject;

            if (positionedObject != null && positionedObject.Parent != null && positionedObject.IgnoreParentPosition == false)
            {
                positionedObject.RelativeX += xChangeMultiplier * xChange;
                positionedObject.RelativeY += yChangeMultiplier * yChange;
            }
            else
            {
                positionable.X += xChangeMultiplier * xChange;
                positionable.Y += yChangeMultiplier * yChange;
            }

            ApplyScale(xChange, yChange, scaleXMultiplier, scaleYMultiplier);
        }

        private void ApplyScale(float xChange, float yChange, float scaleXMultiplier, float scaleYMultiplier)
        {
            IScalable scalable = SelectedObject as IScalable;
            if (scalable != null)
            {
                float newScaleX = scalable.ScaleX + scaleXMultiplier * xChange;
                float newScaleY = scalable.ScaleY + scaleYMultiplier * yChange;

                scalable.ScaleX = Math.Max(0, newScaleX);
                scalable.ScaleY = Math.Max(0, newScaleY);
            }
        }

        private void GetChangeAndScaleMultipliers(out float xChangeMultiplier, out float yChangeMultiplier, out float scaleXMultiplier, out float scaleYMultiplier)
        {






            if (mCircleGrabbed != null)
            {
                int index = IndexOf(mCircleGrabbed, mCircles);

                GetMultipliersForIndex(index, out xChangeMultiplier, out yChangeMultiplier, out scaleXMultiplier, out scaleYMultiplier);
            }
            else
            {
                xChangeMultiplier = 1;
                yChangeMultiplier = 1;
                scaleXMultiplier = 0;
                scaleYMultiplier = 0;
            }
        }

        private void GetMultipliersForIndex(int index, out float xChangeMultiplier, out float yChangeMultiplier, out float scaleXMultiplier, out float scaleYMultiplier)
        {
            // do X's
            switch (index)
            {
                case 0:
                case 6:
                case 7:
                    xChangeMultiplier = .5f;
                    scaleXMultiplier = -.5f;
                    break;
                case 1:
                case 5:
                    xChangeMultiplier = 0;
                    scaleXMultiplier = 0;
                    break;
                case 2:
                case 3:
                case 4:
                    xChangeMultiplier = .5f;
                    scaleXMultiplier = .5f;                    
                    break;
                default:
                    throw new ArgumentException();
            }

            // now X's
            switch (index)
            {
                case 0:
                case 1:
                case 2:
                    yChangeMultiplier = .5f;
                    scaleYMultiplier = .5f;
                    break;
                case 3:
                case 7:
                    yChangeMultiplier = 0;
                    scaleYMultiplier = 0;
                    break;
                case 4:
                case 5:
                case 6:
                    yChangeMultiplier = .5f;
                    scaleYMultiplier = -.5f;
                    break;
                default:
                    throw new ArgumentException();
            }
        }

        private int IndexOf(Circle circle, Circle[] allCircles)
        {
            for(int i = 0; i < allCircles.Length; i++)
            {
                if (allCircles[i] == circle)
                {
                    return i;
                }
            }
            return -1;
        }

        private void ClickActivity()
        {
            Cursor cursor = GuiManager.Cursor;
            if (cursor.PrimaryClick)
            {
                mCircleGrabbed = null;

                if (mSelectedObject != null)
                {
                    bool wasAnythingChanged = mUndoManager.SaveUndo(mSelectedObject);
                    if (wasAnythingChanged && SelectedObjectChanged != null)
                    {
                        SelectedObjectChanged(this, null);
                    }
                }
            }
        }

        private void UpdateToSelectedObject()
        {
            bool visible = mSelectedObject as IScalable != null;

            foreach (var circle in mCircles)
            {
                circle.Visible = visible;
            }

            if (visible)
            {
                UpdatePositions();

                UpdateRadii();
            }
        }

        private void UpdateRadii()
        {

            float units = 1 / SpriteManager.Camera.PixelsPerUnitAt(this.Z);
            int pixelRadius = 5;
            float radius = pixelRadius * units;

            foreach (var circle in mCircles)
            {
                circle.Radius = radius;
            }
        }

        private void UpdatePositions()
        {
            if (mSelectedObject is IScalable && mSelectedObject is IStaticPositionable)
            {
                float scaleX = ((IScalable)mSelectedObject).ScaleX;
                float scaleY = ((IScalable)mSelectedObject).ScaleY;

                float x = ((IStaticPositionable)mSelectedObject).X;
                float y = ((IStaticPositionable)mSelectedObject).Y;
                float z = ((IStaticPositionable)mSelectedObject).Z;

                mCircles[0].X = x - scaleX;
                mCircles[0].Y = y + scaleY;
                mCircles[0].Z = z;

                mCircles[1].X = x;
                mCircles[1].Y = y + scaleY;
                mCircles[1].Z = z;

                mCircles[2].X = x + scaleX;
                mCircles[2].Y = y + scaleY;
                mCircles[2].Z = z;

                mCircles[3].X = x + scaleX;
                mCircles[3].Y = y;
                mCircles[3].Z = z;

                mCircles[4].X = x + scaleX;
                mCircles[4].Y = y - scaleY;
                mCircles[4].Z = z;

                mCircles[5].X = x;
                mCircles[5].Y = y - scaleY;
                mCircles[5].Z = z;

                mCircles[6].X = x - scaleX;
                mCircles[6].Y = y - scaleY;
                mCircles[6].Z = z;

                mCircles[7].X = x - scaleX;
                mCircles[7].Y = y;
                mCircles[7].Z = z;
            }
        }

        public void ClearUndos()
        {
            mUndoManager.Clear();
        }
    }
}
