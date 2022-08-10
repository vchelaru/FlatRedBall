#region Usings

using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Particle;

using FlatRedBall.Math.Geometry;
using FlatRedBall.Math.Splines;

using Cursor = FlatRedBall.Gui.Cursor;
using GuiManager = FlatRedBall.Gui.GuiManager;
using FlatRedBall.Localization;

#if FRB_XNA || SILVERLIGHT
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using FlatRedBall.Math;
using FlatRedBall.Graphics;
using FlatRedBall.Graphics.Texture;
#endif
#endregion

namespace GlueTestProject.Screens
{
	public partial class TransparencyRenderingScreen
	{
        PositionedObjectList<Sprite> dynamicSprites = new PositionedObjectList<Sprite>();

        RenderTargetRenderer renderer;

        bool hasPerformedRenderingTest = false;

		const int SpriteTextureScale = 1;

		void CustomInitialize()
		{
            FlatRedBall.FlatRedBallServices.GraphicsOptions.TextureFilter = Microsoft.Xna.Framework.Graphics.TextureFilter.Point;
			SolidColorTransparencyTestSprite.TextureScale = SpriteTextureScale;

            SolidColorTransparencyTestSprite.Left = Camera.Main.AbsoluteLeftXEdgeAt(0);
            SolidColorTransparencyTestSprite.Top = Camera.Main.AbsoluteTopYEdgeAt(0);


            float nextLeft = SolidColorTransparencyTestSprite.Right;
            for(int i = 0; i <6; i++)
            {
                var newSprite = SolidColorTransparencyTestSprite.Clone();
                newSprite.Left = nextLeft;
                nextLeft = newSprite.Right;

                dynamicSprites.Add(newSprite);
                SpriteManager.AddSprite(newSprite);
                newSprite.Alpha = 1 - ((i+1) * .12f);
            }
		}

        void CustomActivity(bool firstTimeCalled)
		{
            if(!firstTimeCalled && !hasPerformedRenderingTest)
            {
                TestRendering();

                hasPerformedRenderingTest = true;
            }

            if(hasPerformedRenderingTest)
            {
                IsActivityFinished = true;
            }

		}

        private void TestRendering()
        {
            renderer = new RenderTargetRenderer(Camera.Main);
            renderer.PerformRender(this.ContentManagerName, "MainCameraTexture");

            ImageData imageData = ImageData.FromTexture2D(renderer.Texture);

            int xOffset = Camera.Main.DestinationRectangle.X;
            int yOffset = Camera.Main.DestinationRectangle.Y;

            // verify that colors are what they should be:
            if(imageData.GetPixelColor(1 + xOffset,1 + yOffset).R != 255)
            {
                throw new Exception("Control case failed - top-left pixel is not red");
            }


			var pixelColor = imageData.GetPixelColor (1 + xOffset, 11 + yOffset);
			if(pixelColor.R == 255)
            {
                throw new Exception("Transparency from PNGs is not rendering correctly");
            }

            if (imageData.GetPixelColor(SolidColorTransparencyTestSprite.Texture.Width +  1, 11).R == 255)
            {
                throw new Exception("Transparency from Sprite Alpha is not rendering correctly");
            }

        }

		void CustomDestroy()
		{
            SpriteManager.RemoveSpriteList(dynamicSprites);

		}

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

	}
}
