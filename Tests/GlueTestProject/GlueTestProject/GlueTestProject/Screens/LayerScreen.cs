using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Particle;

using FlatRedBall.Math.Geometry;
using FlatRedBall.Math.Splines;

using Cursor = FlatRedBall.Gui.Cursor;
using GuiManager = FlatRedBall.Gui.GuiManager;
using FlatRedBall.Localization;
using GlueTestProject.Entities;
using FlatRedBall.Graphics;

#if FRB_XNA || SILVERLIGHT
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
#endif

namespace GlueTestProject.Screens
{
	public partial class LayerScreen
	{
        DrawableEntity on3DInstance;
        Layer layer2D;
        Layer layer3D;

        void CustomInitialize()
		{
            if (!Layer2D.Texts.Contains(EntireScene.Texts[0]))
            {
                throw new Exception("Texts in entire Scenes which come from files in Screens are not properly added to Layers");
            }

            // The Layer defined by the Entity should be under the Layer defined by the screen.
            int indexOfEntityLayer = SpriteManager.Layers.IndexOf(this.LayerOwnerInstance.InternalLayer);
            int indexOfScreenLayer = SpriteManager.Layers.IndexOf(this.Layer2D);

            if (indexOfScreenLayer < indexOfEntityLayer)
            {
                throw new Exception("Unlayered Entity instances are placing their layers above the Layers defined by Screens");
            }


            if (Layer3DIndependentOfCamera.LayerCameraSettings == null)
            {
                throw new Exception("The 3D Layer needs its own LayerCameraSettings to be independent from the Camera");
            }
   
            if(Layer3DIndependentOfCamera.LayerCameraSettings.Orthogonal)
            {
                throw new Exception("The 3D Layer should not be orthogonal - it should be 3D 3D");
            }

            int right = FlatRedBall.Math.MathFunctions.RoundToInt(Layer2DPercentage.LayerCameraSettings.RightDestination);
            if (right != FlatRedBall.Math.MathFunctions.RoundToInt(SpriteManager.Camera.DestinationRectangle.Right * .4f))
            {
                throw new Exception("Percentage LayerCoordinateUnit is not working properly");
            }

            int left = FlatRedBall.Math.MathFunctions.RoundToInt(Layer2DPercentage.LayerCameraSettings.LeftDestination);
            if (left != FlatRedBall.Math.MathFunctions.RoundToInt(SpriteManager.Camera.DestinationRectangle.Right * .1f))
            {
                throw new Exception("Percentage LayerCoordinateUnit is not working properly - Left is wrong");
            }

            float orthoWidth = Layer2DPercentage.LayerCameraSettings.OrthogonalWidth;
            if ((orthoWidth - .3f * SpriteManager.Camera.OrthogonalWidth) > .1f)
            {
                throw new Exception("Percentage-based ortho widths are not working properly");
            }


            Sprite spriteThatShouldntBeOnMultipleLayers = this.LayerOwnerInstanceOnLayer.BearFromLayeredScene;
            int numberFound = 0;
            for (int i = 0; i < SpriteManager.Layers.Count; i++)
            {
                if (SpriteManager.Layers[i].Sprites.Contains(spriteThatShouldntBeOnMultipleLayers))
                {
                    numberFound++;
                }
            }
            if (numberFound > 1)
            {
                throw new Exception("Sprites which are on Layers in Entities which themselves are put on Layers are being rendered twice");
            }

            spriteThatShouldntBeOnMultipleLayers = this.LayerOwnerInstanceOnLayer.LayeredBear;
            numberFound = 0;
            for (int i = 0; i < SpriteManager.Layers.Count; i++)
            {
                if (SpriteManager.Layers[i].Sprites.Contains(spriteThatShouldntBeOnMultipleLayers))
                {
                    numberFound++;
                }
            }
            if (numberFound > 1)
            {
                throw new Exception("Sprites which are on Layers in Entities, which come from Scenes which are not on Layers, and have their Entity put on a Layer are being rendered twice.");
            }

            MoveToLayerEntityInstance.MoveToLayer(Layer2D);
            if (Layer2D.Circles.Contains(MoveToLayerEntityInstance.CircleInstance) == false)
            {
                throw new Exception("Circles on entities are not moved to a layer when calling MoveToLayer");
            }

            if(SpriteManager.TopLayer.Sprites.Contains(TopLayerSprite) == false)
            {
                throw new Exception($"The {nameof(TopLayerSprite)} should be on the SpriteManager's TopLayer, but it's not!");
            }

            // set a 3D camera to make sure layers with null LayerCameraSettings are also 3d:
            Camera.Main.Orthogonal = false;

            layer2D = Camera.Main.AddLayer();
            layer2D.Name = "layer2D created in code";
            layer2D.UsePixelCoordinates();

            layer3D = Camera.Main.AddLayer();
            layer3D.Name = "layer3D created in code";

            on3DInstance = new DrawableEntity(this.ContentManagerName, false);
            on3DInstance.AddToManagers(layer3D);
		}

        void CustomActivity(bool firstTimeCalled)
		{
            if(HasDrawBeenCalled)
            {
                if(on3DInstance.WasDrawn3D != true)
                {
                    throw new Exception("This instance shoud have been drawn on a 3D camera.");
                }
            }

            if (!firstTimeCalled && HasDrawBeenCalled)
            {
                IsActivityFinished = true;
            }
		}

		void CustomDestroy()
		{
            on3DInstance.Destroy();
            Camera.Main.RemoveLayer(layer2D);
            Camera.Main.RemoveLayer(layer3D);
        }

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

	}
}
