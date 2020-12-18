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

            mRenderer = new RenderTargetRenderer(Camera.Main.DestinationRectangle.Width,
                Camera.Main.DestinationRectangle.Height);

            this.TextureScale = 1;
            this.AttachTo(Camera.Main, false);
            this.RelativeX = 0;
            this.RelativeY = 0;
            this.RelativeZ = -40;
        }

        public void Refresh()
        {
            Camera.Main.ForceUpdateDependencies();
            mRenderer.Camera.Position = Camera.Main.Position;
            mRenderer.Camera.Orthogonal = Camera.Main.Orthogonal;
            mRenderer.Camera.AspectRatio = Camera.Main.AspectRatio;
            mRenderer.Camera.FieldOfView = Camera.Main.FieldOfView;
            mRenderer.Camera.OrthogonalWidth = Camera.Main.OrthogonalWidth;
            mRenderer.Camera.OrthogonalHeight = Camera.Main.OrthogonalHeight;


            if(mRenderer.HasRendered)
            {
                mRenderer.ReRender();
            }
            else
            {
                mRenderer.PerformRender(mContentManagerName, mName);
                Texture = mRenderer.Texture;
            }
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
