using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Graphics;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Gui;
using FlatRedBall.Input;
using FlatRedBall;

namespace GlueViewTestPlugins.EntityControl.Handles
{
	/// <summary>
	/// A Circle handle
	/// </summary>
	public class Handle
    {
        #region Fields

        Layer mLayer;
		Circle mCircle;

        #endregion

        public float RelativeX
        {
            get { return mCircle.RelativeX; }
        }

        public float RelativeY
        {
            get { return mCircle.RelativeY; }
        }

		public float X
		{
			get { return mCircle.X; }
		}

		public float Y
		{
			get { return mCircle.Y; }
		}

		public float Z
		{
			get { return mCircle.Z; }
		}

		public float Radius
		{
			get { return mCircle.Radius; }
		}

		public float HandleSize
		{
			get;
			set;
		}

        /// <summary>
		/// The Coefficient of the XScale
		/// Default is 0
		/// </summary>
		public float ScaleXCoefficient
		{
			get;
			set;
		}

		/// <summary>
		/// The Coefficient of the YScale
		/// Default is 0
		/// </summary>
		public float ScaleYCoefficient
		{
			get;
			set;
		}

		public Handle(Layer layer, Circle circle)
		{
			mLayer = layer;
			mCircle = circle;

			ScaleXCoefficient = 0;
			ScaleYCoefficient = 0;

			ShapeManager.AddCircle(mCircle);
			ShapeManager.AddToLayer(mCircle, mLayer);
		}

		/// <summary>
		/// Checks to see if the cursor is over the handle
		/// </summary>
		/// <returns>Whether the cursor is over the handle</returns>
		public bool IsMouseOver()
		{
			if (InputManager.Mouse.IsOwnerFocused && GuiManager.Cursor.IsOn(mCircle))
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Removes the circle from the ShapeManager
		/// </summary>
		public void RemoveFromShapeManager()
		{
			ShapeManager.Remove(mCircle);
		}

        public void AdjustSizeToCamera()
        {
            mCircle.Radius = HandleSize / SpriteManager.Camera.PixelsPerUnitAt(mCircle.Z);
        }



	}
}
