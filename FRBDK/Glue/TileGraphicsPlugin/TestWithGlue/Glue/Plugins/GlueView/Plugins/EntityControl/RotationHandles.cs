using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.RuntimeObjects;
using FlatRedBall.Glue;
using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Glue.SaveClasses;
using GlueViewTestPlugins.EntityControl.Handles;
using Microsoft.Xna.Framework;

namespace GlueViewTestPlugins.EntityControl
{
	class RotationHandles : Highlight
	{
		protected override void CreatePolygonsForElement(ElementRuntime element)
		{
			//element.AssociatedNamedObjectSave.GetIsScalableEntity();

			//Positioned Objects
			if (element.DirectObjectReference != null && element.DirectObjectReference is PositionedObject)
			{
				//Sprite
				//CreateRotationHandleForPositionedObject((PositionedObject)element.DirectObjectReference);
				//Text

			}
			//Entities
			else if (element.AssociatedIElement != null)
			{
				//CreateRotationHandleForPositionedObject((PositionedObject)element);
			}
			
		}

		private void CreateRotationHandleForPositionedObject(PositionedObject posObj)
		{
			float size = 10;

			Circle circ = new Circle();
			circ.Radius = size / SpriteManager.Camera.PixelsPerUnitAt(posObj.Z);
			circ.Position = new Vector3(posObj.X, posObj.Y + 2, posObj.Z);
			circ.Color = Color.LightBlue;

			Handle handle = new Handle(Layer, circ);

			circ.AttachTo(posObj, true);
		}
	}
}
