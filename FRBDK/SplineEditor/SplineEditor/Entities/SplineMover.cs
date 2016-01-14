using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall;
using FlatRedBall.Math.Splines;
using FlatRedBall.Input;
using FlatRedBall.Gui;

namespace ToolTemplate.Entities
{
    public class SplineMover : PositionedObject
    {
        #region Fields

        Sprite mVisibleRepresentation;
        Spline mSplineMoving;

        bool mIsMouseOver = false;

        bool mIsGrabbed;
        float mGrabbedXOffset;
        float mGrabbedYOffset;

        #endregion

        #region Properties

        public bool IsMouseOver
        {
            get { return mIsMouseOver; }
        }

        public Spline SplineMoving
        {
            get
            {
                return mSplineMoving;
            }
            set
            {
                mSplineMoving = value;
            }
        }

        #endregion

        #region Methods

        public SplineMover()
        {

            mVisibleRepresentation = SpriteManager.AddSprite(@"Content\Hud\MoveEntireSpline.png");
            SpriteManager.AddToLayer(mVisibleRepresentation, SpriteManager.TopLayer);
            mVisibleRepresentation.AttachTo(this, false);

            SpriteManager.AddPositionedObject(this);
        }

        public void Activity()
        {

            mSplineMoving = EditorData.EditorLogic.CurrentSpline;

            mIsMouseOver = 
                mVisibleRepresentation.Visible &&
                InputManager.Mouse.IsOn3D<Sprite>(mVisibleRepresentation, false);

            MouseControl();

            UpdatePosition();

            UpdateScale();

        }

        private void MouseControl()
        {
            Cursor cursor = GuiManager.Cursor;

            if (cursor.PrimaryPush)
            {
                mIsGrabbed = mIsMouseOver;

                if (mIsGrabbed)
                {
                    float worldX = cursor.WorldXAt(0);
                    float worldY = cursor.WorldYAt(0);

                    mGrabbedXOffset = this.X - worldX;
                    mGrabbedYOffset = this.Y - worldY;
                }
            }

            if (cursor.PrimaryDown && mIsGrabbed)
            {
                //cursor.ActualXVelocityAt(0);
                //float worldX = cursor.WorldXAt(0) + mGrabbedXOffset;
                //float worldY = cursor.WorldYAt(0) + mGrabbedYOffset;

                //float differenceX = this.X - worldX;
                //float differenceY = this.Y - worldY;

                var changeX = cursor.WorldXChangeAt(0);
                var changeY = cursor.WorldYChangeAt(0);

                mSplineMoving.Shift(changeX, changeY, 0);

            }

            if (cursor.PrimaryClick)
            {
                mIsGrabbed = false;
            }
        }

        private void UpdatePosition()
        {
            SplinePoint furthestLeft = null;

            if (mSplineMoving != null)
            {
                foreach (SplinePoint sp in mSplineMoving)
                {
                    if (furthestLeft == null || sp.Position.X < furthestLeft.Position.X)
                    {
                        furthestLeft = sp;
                    }
                }
            }

            if (furthestLeft != null)
            {
                mVisibleRepresentation.Visible = true;
                this.Y = furthestLeft.Position.Y +
                    25 / SpriteManager.Camera.PixelsPerUnitAt(0);

                this.X = furthestLeft.Position.X -
                    25 / SpriteManager.Camera.PixelsPerUnitAt(0);

            }
            else
            {
                mVisibleRepresentation.Visible = false;
            }


        }

        private void UpdateScale()
        {
            const float scaleCoefficient = 14;
            mVisibleRepresentation.ScaleX = scaleCoefficient * 1 / SpriteManager.Camera.PixelsPerUnitAt(0);
            mVisibleRepresentation.ScaleY = mVisibleRepresentation.ScaleX;
        }

        #endregion
    }
}
