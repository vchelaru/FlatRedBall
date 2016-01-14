using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Math;
using Microsoft.Xna.Framework;
using FlatRedBall;

namespace EditingControls
{
    public class ObjectHighlight
    {
        #region Fields

        // The number of pixels between the object and the highlight.
        // We use this so that objects 
        int mPixelsBetweenOutlineAndObject = 2;

        // Eventually change this to a polygon
        AxisAlignedRectangle mHighlightRectangle;

        object mHighlightedObject;

        public Color Color = Color.White;

        #endregion

        #region Properties

        float Z
        {
            get
            {
                return mHighlightRectangle.Z;
            }
        }

        public object HighlightedObject
        {
            set
            {
                mHighlightedObject = value;

                UpdateToHighlightedObject();

            }
        }

        #endregion

        public ObjectHighlight()
        {
            mHighlightRectangle = ShapeManager.AddAxisAlignedRectangle();

        }

        public void Activity()
        {
            this.mHighlightRectangle.Color = Color;
            UpdateToHighlightedObject();
        }

        private void UpdateToHighlightedObject()
        {
            mHighlightRectangle.Visible = mHighlightedObject != null;

            if (mHighlightedObject != null)
            {
                UpdatePosition();

                UpdateScale();
            }
        }

        private void UpdatePosition()
        {
            if (mHighlightedObject is IStaticPositionable)
            {
                IStaticPositionable positionable = mHighlightedObject as IStaticPositionable;

                mHighlightRectangle.X = positionable.X;
                mHighlightRectangle.Y = positionable.Y;
                mHighlightRectangle.Z = positionable.Z;
            }
        }

        private void UpdateScale()
        {
            float extraScale = 0;

            if (mPixelsBetweenOutlineAndObject != 0)
            {
                float units = 1 / SpriteManager.Camera.PixelsPerUnitAt(this.Z);

                extraScale = mPixelsBetweenOutlineAndObject * units;
            }

            if (mHighlightedObject is IScalable3D)
            {

            }
            else if (mHighlightedObject is IScalable)
            {
                IScalable scalable = mHighlightedObject as IScalable;

                mHighlightRectangle.ScaleX = scalable.ScaleX + extraScale;
                mHighlightRectangle.ScaleY = scalable.ScaleY + extraScale;
            }
        }

    }
}
