using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Graphics.Texture
{
    /// <summary>
    /// A Sprite-inheriting object which can reference one or more Layers to use as "input". The input
    /// will be rendered and stored on a Texture2D for the Sprite. This provides the ability to improve rendering
    /// speed of complex objects by rendering to a Texutre2D one time. It also provides additional support for transformations,
    /// such as rotating a collection of objects.
    /// </summary>
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

        /// <summary>
        /// Renders all objects contained in this instance's DefaultInputLayer and additionally added input layers
        /// to the internally referenced Sprite. This can be called every frame, or it can be called only when the internal
        /// objects change. Refresh should be called at least one time, or the render target may contain unexpected content.
        /// </summary>
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

        /// <summary>
        /// Adds a Layer to be rendered by the RenderTargetSprite above all previously added Layers and above the DefaultInputLayer.
        /// If the argument layer is part of the SpriteManager, it will be removed.
        /// </summary>
        /// <param name="layer">The Layer to use as an input layer on this RenderTargetSprite.</param>
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
