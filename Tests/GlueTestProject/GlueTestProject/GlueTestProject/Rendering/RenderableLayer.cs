using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Graphics
{
    public class RenderableLayer
    {
        
        Sprite mSprite;

        RenderTargetRenderer mRenderer;

        string mContentManagerName;
        string mName;

        public Layer TargetLayer
        {
            get;
            private set;
        }

        public RenderableLayer (string contentManagerName, string thisName)
        {
            mContentManagerName = contentManagerName;
            mName = thisName;

            mRenderer = new RenderTargetRenderer(Camera.Main.DestinationRectangle.Width,
                Camera.Main.DestinationRectangle.Height);

            TargetLayer = new Layer();
            TargetLayer.UsePixelCoordinates();

            mSprite = new Sprite();
            mSprite.TextureScale = 1;
            SpriteManager.AddToLayer(mSprite, TargetLayer);
            mSprite.AttachTo(Camera.Main, false);
            mSprite.RelativeX = 0;
            mSprite.RelativeY = 0;
            mSprite.RelativeZ = -40;

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
                mSprite.Texture = mRenderer.Texture;
            }
        }

        public void AddLayerToRender(Layer layer)
        {
            if(SpriteManager.Layers.Contains(layer))
            {
                SpriteManager.RemoveLayer(layer);
            }
            mRenderer.Camera.AddLayer(layer);
        }
    }
}
