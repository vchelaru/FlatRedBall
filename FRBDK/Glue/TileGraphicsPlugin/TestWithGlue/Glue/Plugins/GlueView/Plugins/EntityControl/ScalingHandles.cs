using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue;
using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework;
using FlatRedBall.Graphics;
using FlatRedBall;
using FlatRedBall.Glue.RuntimeObjects;
using FlatRedBall.Graphics.Model;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Gui;
using FlatRedBall.Glue.SaveClasses;

namespace GlueViewTestPlugins.EntityControl.Handles
{
	public class ScalingHandles : Highlight
    {
        #region Fields

        List<Handle> mHandles;
		Handle mSelectedHandle;
		const float mHandleSize = 4f;
		float mOldCameraZ;
        Handle mOppositeHandle;

        ResizingLogic mResizingLogic = new ResizingLogic();

        #endregion

        #region Properties

        /// <summary>
        /// This value can be set from code outside of
        /// ScalingHandles - if this is true then the ScalingHandles
        /// will refresh themselves next update.  This is useful if someone
        /// (like a propertyGrid) changes a Scale value.
        /// </summary>
        public bool ShouldRefresh
        {
            get
            {
                if (mHandles != null && mHandles.Count > 0)
                {
                    float scaleX;
                    float scaleY;

                    GetCurrentElementScaleValues(out scaleX, out scaleY);

                    return mHandles[2].RelativeX != scaleX || mHandles[2].RelativeY != scaleY;
                }
                else
                {
                    return false;
                }
            }
        }

        #endregion

        public ScalingHandles()
		{
			Color = Color.Red;
			mHandles = new List<Handle>();

			mOldCameraZ = SpriteManager.Camera.PixelsPerUnitAt(0);
		}

		protected override void CreatePolygonForIScalable(object obj)
		{
			base.CreatePolygonForIScalable(obj);

			IScalable scalable = (IScalable)obj;
			PositionedObject pObj = (PositionedObject)obj;

            float scaleX = scalable.ScaleX;
            float scaleY = scalable.ScaleY;


            CreateHandles(pObj, scaleX, scaleY);
		}

        private void CreateHandles(PositionedObject pObj, float scaleX, float scaleY)
        {
			AddHandle(pObj.X - (scaleX), pObj.Y + (scaleY), pObj.Z, mHandleSize, -0.5f, 0.5f).AttachTo(pObj, true);
			AddHandle(pObj.X, pObj.Y + (scaleY), pObj.Z, mHandleSize, 0f, 0.5f).AttachTo(pObj, true);
			AddHandle(pObj.X + (scaleX), pObj.Y + (scaleY), pObj.Z, mHandleSize, 0.5f, 0.5f).AttachTo(pObj, true);
			AddHandle(pObj.X + (scaleX), pObj.Y, pObj.Z, mHandleSize, 0.5f, 0f).AttachTo(pObj, true);
			AddHandle(pObj.X + (scaleX), pObj.Y - (scaleY), pObj.Z, mHandleSize, 0.5f, -0.5f).AttachTo(pObj, true);
			AddHandle(pObj.X, pObj.Y - (scaleY), pObj.Z, mHandleSize, 0f, -0.5f).AttachTo(pObj, true);
			AddHandle(pObj.X - (scaleX), pObj.Y - (scaleY), pObj.Z, mHandleSize, -0.5f, -0.5f).AttachTo(pObj, true);
			AddHandle(pObj.X - (scaleX), pObj.Y, pObj.Z, mHandleSize, -0.5f, 0f).AttachTo(pObj, true);
        }

		protected override void CreatePolygonForCircle(object o)
		{
			base.CreatePolygonForCircle(o);

			Circle asCircle = o as Circle;

			AddHandle(asCircle.X - (asCircle.Radius), asCircle.Y + (asCircle.Radius), asCircle.Z, mHandleSize, -0.5f, 0.5f).AttachTo(asCircle, true);
			AddHandle(asCircle.X, asCircle.Y + (asCircle.Radius), asCircle.Z, mHandleSize, 0f, 0.5f).AttachTo(asCircle, true);
			AddHandle(asCircle.X + (asCircle.Radius), asCircle.Y + (asCircle.Radius), asCircle.Z, mHandleSize, 0.5f, 0.5f).AttachTo(asCircle, true);
			AddHandle(asCircle.X + (asCircle.Radius), asCircle.Y, asCircle.Z, mHandleSize, 0.5f, 0f).AttachTo(asCircle, true);
			AddHandle(asCircle.X + (asCircle.Radius), asCircle.Y - (asCircle.Radius), asCircle.Z, mHandleSize, 0.5f, -0.5f).AttachTo(asCircle, true);
			AddHandle(asCircle.X, asCircle.Y - (asCircle.Radius), asCircle.Z, mHandleSize, 0f, -0.5f).AttachTo(asCircle, true);
			AddHandle(asCircle.X - (asCircle.Radius), asCircle.Y - (asCircle.Radius), asCircle.Z, mHandleSize, -0.5f, -0.5f).AttachTo(asCircle, true);
			AddHandle(asCircle.X - (asCircle.Radius), asCircle.Y, asCircle.Z, mHandleSize, -0.5f, 0f).AttachTo(asCircle, true);
        }

		protected override void CreatePolygonForText(Text t)
		{
			base.CreatePolygonForText(t);
			//To do??
		}

        protected override void CreatePolygonForScalableEntity(ElementRuntime element)
        {
            base.CreatePolygonForScalableEntity(element);

            float scaleX = (float)element.AssociatedNamedObjectSave.GetEffectiveValue("ScaleX");
            float scaleY = (float)element.AssociatedNamedObjectSave.GetEffectiveValue("ScaleY");


            CreateHandles(element, scaleX, scaleY);
        }

		private Circle AddHandle(float x, float y, float z, float size, float ScaleXCoef, float ScaleYCoef)
		{
			Circle circ = new Circle();
			circ.Radius = size / SpriteManager.Camera.PixelsPerUnitAt(z);
			circ.Position = new Vector3(x, y, z);
			circ.Color = Color.LightBlue;
			circ.AttachTo((PositionedObject)mCurrentElement, true);

			Handle handle = new Handle(Layer, circ);
			handle.ScaleXCoefficient = ScaleXCoef;
			handle.ScaleYCoefficient = ScaleYCoef;
			handle.HandleSize = mHandleSize;
			//Add the handle to the lists
			mHandles.Add(handle);

			return circ;
		}

		public override void RemoveHighlights()
		{
			mSelectedHandle = null;

			foreach(Handle h in mHandles)
			{
				h.RemoveFromShapeManager();
			}
			mHandles.Clear();

			base.RemoveHighlights();
		}

        public bool IsElementRuntimeScalable(ElementRuntime elementRuntime)
        {
            if (elementRuntime != null)
            {
                if (elementRuntime.DirectObjectReference != null)
                {
                    return elementRuntime.DirectObjectReference is IScalable ||
                        elementRuntime.DirectObjectReference is Circle;
                }
                else if (elementRuntime.AssociatedNamedObjectSave != null && elementRuntime.AssociatedNamedObjectSave.GetIsScalableEntity())
                {
                    // Does this thing have ScaleX and ScaleY values?
                    return true;
                }
            }
            return false;

        }

		public bool IsMouseOnHandle()
		{
			foreach (Handle h in mHandles)
			{
				if (h.IsMouseOver())
				{
					mSelectedHandle = h;
                    mOppositeHandle = mHandles[(mHandles.IndexOf(h) + 4) % 8];
					return true;
				}
			}

			return false;
		}

		public void Scale()
		{
			if (mSelectedHandle != null && 
                (GuiManager.Cursor.WorldXChangeAt(CurrentElement.Z) != 0 ||
                GuiManager.Cursor.WorldYChangeAt(CurrentElement.Z) != 0)
                
                )
			{
                PositionedObject pObj = CurrentElement;

                if (CurrentElement.DirectObjectReference != null)
                {
                    pObj = (PositionedObject)CurrentElement.DirectObjectReference;
                }

                float xChange;
                float yChange;
                GetCursorPositionChange(out xChange, out yChange);


                float xChangeCoef;
                float yChangeCoef;
                GetChangeCoefficients(out xChangeCoef, out yChangeCoef);

                if (pObj is Circle)
                {
                    mResizingLogic.ApplyCircleResize(
                        ref xChange,
                        ref yChange,
                        ref xChangeCoef,
                        ref yChangeCoef,
                        mSelectedHandle.ScaleXCoefficient,
                        mSelectedHandle.ScaleYCoefficient,
                        mOppositeHandle.X,
                        mOppositeHandle.Y,
                        GuiManager.Cursor.WorldXAt(pObj.Z),
                        GuiManager.Cursor.WorldYAt(pObj.Z),
                        pObj as Circle,
						mSelectedHandle.X,
						mSelectedHandle.Y);
                }
                else
                {
                    mResizingLogic.ApplyChangeVariables(pObj, xChange, yChange, xChangeCoef, yChangeCoef);
                }
                UpdateHighlights();
			}
		}

        private void GetChangeCoefficients(out float xChangeCoef, out float yChangeCoef)
        {
            //For movement
            xChangeCoef = 1;
            yChangeCoef = 1;
            if (mSelectedHandle.ScaleXCoefficient < 0)
            {
                xChangeCoef = -1;
            }
            if (mSelectedHandle.ScaleYCoefficient < 0)
            {
                yChangeCoef = -1;
            }
        }


        private void GetCursorPositionChange(out float xChange, out float yChange)
        {
            xChange = mSelectedHandle.ScaleXCoefficient * GuiManager.Cursor.WorldXChangeAt(CurrentElement.Z);
            yChange = mSelectedHandle.ScaleYCoefficient * GuiManager.Cursor.WorldYChangeAt(CurrentElement.Z);
        }

        private void GetCurrentElementScaleValues(out float scaleX, out float scaleY)
        {
            if (CurrentElement != null)
            {
                //IScalable
                if (CurrentElement.DirectObjectReference is IScalable)
                {
                    IScalable scalable = (IScalable)CurrentElement.DirectObjectReference;
                    scaleX = scalable.ScaleX;
                    scaleY = scalable.ScaleY;
					return;
                }
                //Circle
                else if (CurrentElement.DirectObjectReference is Circle)
                {
                    Circle circle = (Circle)CurrentElement.DirectObjectReference;

                    scaleX = circle.Radius;
                    scaleY = circle.Radius;

					return;
                }
                else if (CurrentElement.AssociatedNamedObjectSave != null && CurrentElement.AssociatedNamedObjectSave.GetIsScalableEntity())
                {
                    scaleX = (float)CurrentElement.AssociatedNamedObjectSave.GetEffectiveValue("ScaleX");
                    scaleY = (float)CurrentElement.AssociatedNamedObjectSave.GetEffectiveValue("ScaleY");

					return;
                }
            }
            scaleX = 0;
            scaleY = 0;

        }

		public void UpdateHighlights()
		{
			int handleID = -1;

			if (mSelectedHandle != null)
			{
				handleID = mHandles.IndexOf(mSelectedHandle);
			}

			CurrentElement = mCurrentElement;
			if (handleID != -1)
			{
				mSelectedHandle = mHandles.ElementAt(handleID);
			}
		}

		public void Update()
		{
            if (ShouldRefresh)
            {
                UpdateHighlights();
            }
            else if (CurrentElement != null && mOldCameraZ != SpriteManager.Camera.PixelsPerUnitAt(CurrentElement.Z))
			{

                foreach (Handle handle in mHandles)
                {
                    handle.AdjustSizeToCamera();
                }

                //CurrentElement = mCurrentElement;
                //mOldCameraZ = SpriteManager.Camera.PixelsPerUnitAt(SpriteManager.Camera.Z);
			}
		}
	}
}
