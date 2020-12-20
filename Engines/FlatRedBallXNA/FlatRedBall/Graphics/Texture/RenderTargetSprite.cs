using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Graphics.Texture
{
    public class RenderTargetSprite : Sprite
    {
        RenderTargetRenderer mRenderer;

        string mContentManagerName;

        public Layer DefaultInputLayer => mRenderer.Layer;

        int originalDestinationWidth;
        int originalDestinationHeight;

        /// <summary>
        /// Instantiates a new RenderTargetSprite with a render target matching the spriteName belonging
        /// to the argument ContentManagerName
        /// </summary>
        /// <param name="contentManagerName">The content manager name which the RenderTarget should belong to. This
        /// is used to dispose the RenderTarget, and will usually match the current screen's content manager name.</param>
        /// <param name="spriteName">The name of the Sprite, which is shared with the render target. This is required </param>
        public RenderTargetSprite(string contentManagerName, string spriteName)
        {
            mContentManagerName = contentManagerName;

            this.Name = spriteName;

            CreateRenderer();

            this.AttachTo(Camera.Main, false);
            this.RelativeX = 0;
            this.RelativeY = 0;
            this.RelativeZ = -40;
        }

        void CreateRenderer()
        {
            originalDestinationWidth = Camera.Main.DestinationRectangle.Width;
            originalDestinationHeight = Camera.Main.DestinationRectangle.Height;

            mRenderer = new RenderTargetRenderer(originalDestinationWidth,
                originalDestinationHeight);
        }

        public void Refresh()
        {
            // use the texture height, not the current ortho values:

            if(originalDestinationWidth != Camera.Main.DestinationRectangle.Width ||
                originalDestinationHeight != Camera.Main.DestinationRectangle.Height)
            {
                mRenderer.Dispose();

                var oldLayer = mRenderer.Layer;
                mRenderer.Camera.RemoveLayer(mRenderer.Camera.Layer);

                CreateRenderer();

                // preserve all added objects:
                mRenderer.Camera.RemoveLayer(mRenderer.Camera.Layer);
                mRenderer.Camera.AddLayer(oldLayer);
            }

            Camera.Main.ForceUpdateDependencies();
            mRenderer.Camera.Position = Camera.Main.Position;
            mRenderer.Camera.Orthogonal = Camera.Main.Orthogonal;
            mRenderer.Camera.AspectRatio = Camera.Main.AspectRatio;
            mRenderer.Camera.FieldOfView = Camera.Main.FieldOfView;

            mRenderer.Camera.OrthogonalWidth = Camera.Main.OrthogonalWidth;
            mRenderer.Camera.OrthogonalHeight = Camera.Main.OrthogonalHeight;

            if (mRenderer.HasRendered)
            {
                mRenderer.ReRender();
            }
            else
            {
                mRenderer.PerformRender(mContentManagerName, mName);
                Texture = mRenderer.Texture;
            }

            this.Width = mRenderer.Camera.OrthogonalWidth;
            this.Height = mRenderer.Camera.OrthogonalHeight;
        }

        public void AddInputLayer(Layer layer)
        {
            if(SpriteManager.Layers.Contains(layer))
            {
                SpriteManager.RemoveLayer(layer);
            }
            mRenderer.Camera.AddLayer(layer);
        }
    }
}
