// This will eventually go away because we wouldn't
// even include the CameraViewWindow in projects that
// don't support FRB Drawn GUI - but for now I don't want to 
// redo all of FRB XNA 4.
#if FRB_MDX || XNA3
#define SUPPORTS_FRB_DRAWN_GUI
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Graphics;
using FlatRedBall.Input;

#if XNA4
using Color = Microsoft.Xna.Framework.Color;
#else
using Color = Microsoft.Xna.Framework.Graphics.Color;
#endif

#if FRB_MDX
using Keys = Microsoft.DirectX.DirectInput.Key;
#elif FRB_XNA
using Keys = Microsoft.Xna.Framework.Input.Keys;
#endif

namespace FlatRedBall.Gui
{
    #region XML Docs
    /// <summary>
    /// A Window that can show what a Camera is viewing.
    /// </summary>
    #endregion
    public class CameraViewWindow : Window
    {
        #region Fields

        Camera mRenderCamera;

        #endregion

        #region Properties

        public Camera Camera
        {
            get { return mRenderCamera; }
        }

        public override float ScaleX
        {
            get
            {
                return base.ScaleX;
            }
            set
            {
                if (value != base.ScaleX)
                {
                    base.ScaleX = value;
                    ResizeEndMethod(null);
                }
            }
        }

        public override float ScaleY
        {
            get
            {
                return base.ScaleY;
            }
            set
            {
                if (value != base.ScaleY)
                {
                    base.ScaleY = value;
                    ResizeEndMethod(null);
                }
            }
        }

        #endregion

		#region Event

		public event GuiMessage CameraPan;
		public event GuiMessage CameraZoom;

		#endregion

		#region Event Methods

		private void ResizeEndMethod(Window callingWindow)
        {
            int windowWidth = FlatRedBallServices.GraphicsOptions.ResolutionWidth;
            int windowHeight = FlatRedBallServices.GraphicsOptions.ResolutionHeight;

            float ratioX = (this.ScaleX - BorderWidth) / SpriteManager.Camera.XEdge;
            float ratioY = (this.ScaleY - BorderWidth) / SpriteManager.Camera.YEdge;

            this.X += .001f;
            this.Y += .001f;
            int width = (int)(System.Math.Round(((float)windowWidth * ratioX)) + .1f);
            int height = (int)(System.Math.Round(((float)windowHeight * ratioY)) + .1f);

            mRenderCamera.DestinationRectangle = new Microsoft.Xna.Framework.Rectangle(0,0, width, height);
        }

        private void RollingOverEvent(Window callingWindow)
        {
#if SUPPORTS_FRB_DRAWN_GUI
            if (mCursor.WindowMiddleButtonPushed == this && mCursor.MiddleDown && !mCursor.MiddlePush)
            {
                float xRatioToMove = InputManager.Mouse.XChange / (float)mRenderCamera.DestinationRectangle.Width;
                float yRatioToMove = InputManager.Mouse.YChange / (float)mRenderCamera.DestinationRectangle.Height;

                mRenderCamera.X -= xRatioToMove * 2 * mRenderCamera.RelativeXEdgeAt(0);
                mRenderCamera.Y += yRatioToMove * 2 * mRenderCamera.RelativeYEdgeAt(0);

				if (CameraPan != null)
				{
					CameraPan(this);
				}
            }
#endif
        }

        private void CheckForMouseWheel(Window callingWindow)
        {
            // mouse-wheel scrolling zooms in and out
			if (InputManager.Mouse.ScrollWheel != 0 && GuiManager.DominantWindowActive == false)
			{
				if (mRenderCamera.Orthogonal == false)
				{
					mRenderCamera.Z *= 1 + System.Math.Sign(InputManager.Mouse.ScrollWheel) * -.1f;
				}
				else
				{
					mRenderCamera.OrthogonalHeight *= 1 + System.Math.Sign(InputManager.Mouse.ScrollWheel) * -.1f;
					mRenderCamera.OrthogonalWidth *= 1 + System.Math.Sign(InputManager.Mouse.ScrollWheel) * -.1f;
				}

				if (CameraZoom != null)
				{
					CameraZoom(this);
				}
			}
#if FRB_MDX
			else if (InputManager.Keyboard.KeyDown(Keys.Equals))
#elif FRB_XNA
			else if (InputManager.Keyboard.KeyDown(Keys.OemPlus))
#endif
			{
				if (mRenderCamera.Orthogonal == false)
				{
					mRenderCamera.Z *= .98f;
				}
				else
				{
					mRenderCamera.OrthogonalWidth *= .98f;
					mRenderCamera.OrthogonalHeight *= .98f;
				}
			}
#if FRB_MDX
				else if (InputManager.Keyboard.KeyDown(Keys.Minus))
#elif FRB_XNA
                else if(InputManager.Keyboard.KeyDown(Keys.OemMinus))
#endif
                {
                    if (mRenderCamera.Orthogonal == false)
				{
					mRenderCamera.Z *= 1.02f;
				}
				else
				{
					mRenderCamera.OrthogonalWidth *= 1.02f;
					mRenderCamera.OrthogonalHeight *= 1.02f;
				}
                }
        }

        #endregion

        #region Methods

        #region Constructor

        public CameraViewWindow(Cursor cursor, string contentManagerName)
            : base(cursor)
        {
			MinimumScaleY = .5f;

            mRenderCamera = new Camera(contentManagerName);
            mRenderCamera.BackgroundColor = Color.Black;
            mRenderCamera.Name = "CameraViewWindow Camera";
            //mRenderCamera.DestinationRectangle = new Microsoft.Xna.Framework.Rectangle(0, 0, 100, 100);
            SpriteManager.Cameras.Add(mRenderCamera);
            mRenderCamera.DrawsToScreen = false;

#if !XNA4
            mRenderCamera.RenderOrder.Clear();
            mRenderCamera.RenderOrder.Add(RenderMode.Default);
#endif

            ResizeEnd += ResizeEndMethod;
            CursorOver += CheckForMouseWheel;

            this.RollingOver += RollingOverEvent;
        }

        #endregion

        #region Public Methods

        public void RefreshTexture()
        {
            mRenderCamera.RefreshTexture();
        }

        public void UpdateDisplayResolution()
        {
            ResizeEndMethod(null);
        }

        public float WorldXAt(float absoluteZ)
        {
            float displayWidth = (this.ScaleX - Window.BorderWidth) * 2;

            float distanceFromLeft = 
                mCursor.XForUI - (this.WorldUnitX - this.ScaleX) - Window.BorderWidth;


            float cameraLeft = 0;
            float viewWidth = 0;

            if (mRenderCamera.Orthogonal)
            {
                cameraLeft = mRenderCamera.X - mRenderCamera.OrthogonalWidth / 2.0f;
                viewWidth = mRenderCamera.OrthogonalWidth;
            }
            else
            {
                cameraLeft = mRenderCamera.X - mRenderCamera.RelativeXEdgeAt(absoluteZ);
                viewWidth = 2 * mRenderCamera.RelativeXEdgeAt(absoluteZ);
            }
            float ratio = distanceFromLeft / displayWidth;

            return cameraLeft + ratio * viewWidth;

        }

        public float WorldYAt(float absoluteZ)
        {
            float displayHeight = (this.ScaleY - Window.BorderWidth) * 2;

            float distanceFromBottom = 
                (-this.WorldUnitY + (this.ScaleY - Window.BorderWidth)) + mCursor.YForUI;

            float ratio = distanceFromBottom / displayHeight;

            float cameraBottom = 0;
            float viewHeight = 0;

            if (mRenderCamera.Orthogonal)
            {
                cameraBottom = mRenderCamera.Y - mRenderCamera.OrthogonalHeight / 2.0f;
                viewHeight = mRenderCamera.OrthogonalHeight;
            }
            else
            {
                cameraBottom = mRenderCamera.Y - mRenderCamera.RelativeYEdgeAt(absoluteZ);
                viewHeight = 2 * mRenderCamera.RelativeYEdgeAt(absoluteZ);
            }

            return cameraBottom + ratio * viewHeight;
        }

        #endregion

        public override void Activity(Camera camera)
        {
#if XNA4
            // do nothing
#else
            if (mRenderCamera.RenderTargetTextures.Count != 0)
            {
                this.BaseTexture = mRenderCamera.GetRenderTexture(FlatRedBall.Graphics.RenderMode.Default);
            }
#endif
            base.Activity(camera);
        }

        #endregion
    }
}
