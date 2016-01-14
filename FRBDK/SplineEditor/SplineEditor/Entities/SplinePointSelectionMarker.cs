using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Gui;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Math.Splines;
using Microsoft.Xna.Framework;
using SplineEditor.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SplineEditor.Entities
{
    public class SplinePointSelectionMarker : IVisible, IClickable
    {
        #region Fields

        AxisAlignedRectangle mRectangle;
        Line mLine;

        Circle mEndpoint1;
        Circle mEndpoint2;

        #endregion

        public bool Visible
        {
            get
            {
                return mRectangle.Visible;
            }
            set
            {
                mRectangle.Visible = value;
                mLine.Visible = value;

                mEndpoint1.Visible = value;
                mEndpoint2.Visible = value;
            }
        }

        public SplinePointSelectionMarker()
        {
            mRectangle = ShapeManager.AddAxisAlignedRectangle();
            mLine = ShapeManager.AddLine();
            mRectangle.Color = Color.LightBlue;

            mEndpoint1 = ShapeManager.AddCircle();
            mEndpoint1.Color = Color.Yellow;
            mEndpoint2 = ShapeManager.AddCircle();
            mEndpoint2.Color = Color.Yellow;
        }

        public void UpdateToSplinePoint(SplinePoint splinePoint)
        {
            Visible = splinePoint != null;

            if (Visible)
            {
                mRectangle.Position = splinePoint.Position;

                mRectangle.ScaleX = AppState.Self.CurrentSpline.SplinePointVisibleRadius;
                mRectangle.ScaleY = mRectangle.ScaleX;

                mLine.RelativePoint1 = new Point3D(-splinePoint.Velocity / 2.0f);
                mLine.RelativePoint2 = new Point3D(splinePoint.Velocity / 2.0f);
                mLine.Position = splinePoint.Position;

                if (splinePoint.UseCustomVelocityValue)
                {
                    mEndpoint1.Visible = true;
                    mEndpoint2.Visible = true;

                    mEndpoint1.Position = mLine.AbsolutePoint1.ToVector3();
                    mEndpoint2.Position = mLine.AbsolutePoint2.ToVector3();

                    mEndpoint1.Radius = 8 / Camera.Main.PixelsPerUnitAt(splinePoint.Position.Z);
                    mEndpoint2.Radius = 8 / Camera.Main.PixelsPerUnitAt(splinePoint.Position.Z);
                }
                else
                {
                    mEndpoint1.Visible = false;
                    mEndpoint2.Visible = false;
                }



            }

        }

        #region IVisible implementation

        public IVisible Parent
        {
            get { return null; }
        }

        public bool AbsoluteVisible
        {
            get { return Visible; }
        }

        public bool IgnoresParentVisibility
        {
            get
            {
                return false ;
            }
            set
            {
                // do nothing
            }
        }
        #endregion

        public bool HasCursorOver(Cursor cursor)
        {
            int throwaway;

            return HasCursorOver(cursor, out throwaway);
        }

        public bool HasCursorOver(Cursor cursor, out int handleIndexOver0Base)
        {
            handleIndexOver0Base = -1;

            if (Visible && mEndpoint1.Visible)
            {
                if(cursor.IsOn3D(mEndpoint1))
                {
                    handleIndexOver0Base = 0;
                    return true;
                }
                else if (cursor.IsOn3D(mEndpoint2))
                {
                    handleIndexOver0Base = 1;
                    return true;
                }
            }
            return false;
        }
    }
}
