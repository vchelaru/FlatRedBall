using FlatRedBall;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Graphics.Texture
{
    public class RenderTargetRenderer
    {
        int mWidth;
        int mHeight;

        RenderTarget2D mRenderTarget;

        string contentManagerName;

        public Texture2D Texture
        {
            get
            {
                if(!HasRendered)
                {
                    throw new InvalidOperationException("You must first call PerformRender");
                }
                return mRenderTarget;
            }
        }

        public Camera Camera
        {
            get;
            private set;
        }

        public FlatRedBall.Graphics.Layer Layer
        {
            get =>Camera.Layer;
        }

        public bool HasRendered
        {
            get;
            private set;
        }

        public RenderTargetRenderer(int width, int height, bool generateMipMaps = false)
        {
            mWidth = width;
            mHeight = height;

            var device = FlatRedBallServices.GraphicsDevice;
            mRenderTarget = new RenderTarget2D(device, mWidth, mHeight);

            Camera = new Camera(null, mWidth, mHeight);
            Camera.UsePixelCoordinates();
            Camera.Z = 40;
            Camera.DrawsWorld = false;

        }

        public RenderTargetRenderer(Camera camera, bool generateMipMaps = false)
        {
            // The camera may be off-center. Even though the area to the left and above the camera are not used, 
            // we need to pad the render target renderer so that internally FRB doesn't crash:
            mWidth = camera.DestinationRectangle.Left + camera.DestinationRectangle.Width;
            mHeight = camera.DestinationRectangle.Top + camera.DestinationRectangle.Height;

            var device = FlatRedBallServices.GraphicsDevice;
            mRenderTarget = new RenderTarget2D(device, mWidth, mHeight);
            // Monogame does not like these extra parameters. They cause some kind of "imbalance the stack" exception
            //,
              //  generateMipMaps, device.DisplayMode.Format, DepthFormat.Depth24);

            this.Camera = camera;
        }

        public void ReRender()
        {
            if(!HasRendered)
            {
                throw new InvalidOperationException("You must first call PerformRender");
            }

            // This updates the internal rendering variables according to what the user has set.
            Camera.ForceUpdateDependencies();


            var device = FlatRedBallServices.GraphicsDevice;

            // Default alpha blend
            // No wait, this could be causing problems internally
            // when sprites try to change blend state! We should always
            // use the renderer:
            //device.BlendState = BlendState.AlphaBlend;
            FlatRedBall.Graphics.Renderer.BlendOperation = BlendOperation.Regular;


            device.Viewport = Camera.GetViewport();
            device.SetRenderTarget(mRenderTarget);
            device.Clear(Microsoft.Xna.Framework.Color.Transparent);
            FlatRedBall.Graphics.Renderer.DrawCamera(Camera, null);
            device.SetRenderTarget(null);

        }

        public void PerformRender(string contentManagerName, string textureName)
        {
            if (HasRendered)
            {
                throw new InvalidOperationException("This has already rendered once, call ReRender instead.");
            }
            HasRendered = true;

            ReRender();

            mRenderTarget.Name = textureName;

            var contentManager = FlatRedBallServices.GetContentManagerByName(contentManagerName);
            this.contentManagerName = contentManagerName;
            contentManager.AddDisposable(textureName, mRenderTarget);

           
        }

        public void Dispose()
        {
            var contentManager = FlatRedBallServices.GetContentManagerByName(contentManagerName);
            contentManager.RemoveDisposable(mRenderTarget);
            mRenderTarget.Dispose();
            HasRendered = false;
        }
    }
}
