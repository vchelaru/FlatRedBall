using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework;
using FlatRedBall.Graphics;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Graphics.Particle;
using System.Collections;

namespace FlatRedBall.Glue.RuntimeObjects
{
	public abstract class Highlight
	{
        #region Fields
        protected List<Polygon> mHighlightShapes;
        protected ElementRuntime mCurrentElement;
        #endregion

        #region Properties

        public ElementRuntime CurrentElement
        {
            get { return mCurrentElement; }

            set
            {
                mCurrentElement = value;
                
                RemoveHighlights();
                
                if( value != null)
                {
                    UpdateToCurrentElement();
                }
            }
        }

        public Color Color
        {
            get;
            set;
        }

        public Layer Layer
        {
            get;
            set;
        }
        #endregion

        #region Methods

        public Highlight()
        {
            Color = Color.White;
            Layer = SpriteManager.AddLayer();
            Layer.Name = "GlueView Highlight shapes layer";
            mHighlightShapes = new List<Polygon>();
        }

		protected virtual void UpdateToCurrentElement()
        {
            RemoveHighlights(); 
            CreatePolygonsForElement(mCurrentElement); 
        }

		protected virtual void CreatePolygonsForElement(ElementRuntime element)
		{

            bool createRectanglesForContainedElements = true;

            // We want to use coordinates
            // that match the coordinates of
            // whatever it is that we're viewing.
            // Graphical objects like Sprites use the
            // coordinates of the Layers that they're on,
            // or of their camera.  However Layers themselves
            // have a destination rectangle which is in pixel coordiantes
            // so the rectangles that we render need to be in the resolution
            // of the window.  Therefore for Layers we will want to MatchScreenResolution.
            LayerCoordinateType coordinateType = LayerCoordinateType.MatchCamera;


			if (element.DirectObjectReference != null)
			{
                object directObjectReference = element.DirectObjectReference;
                coordinateType = CreatePolygonsForObject(directObjectReference, coordinateType);


			}
            else if (element.AssociatedNamedObjectSave != null && element.AssociatedNamedObjectSave.GetIsScalableEntity())
            {
                // We used to not want to show contained object scales, but I think we want to now because we're not supporting scaling (yet) in GView.
                //createRectanglesForContainedElements = false;
                CreatePolygonForScalableEntity(element);
            }

            if (createRectanglesForContainedElements)
            {
                for (int i = 0; i < element.ContainedElements.Count; i++)
                {
                    ElementRuntime e = element.ContainedElements[i];

                    CreatePolygonsForElement(e);
                }

                for (int i = 0; i < element.ElementsInList.Count; i++)
                {
                    ElementRuntime e = element.ElementsInList[i];

                    CreatePolygonsForElement(e);
                }
            }

			SpriteManager.MoveToFront(Layer);


			if (element != null && element.Layer != null && element.Layer.LayerCameraSettings != null && element.Layer.LayerCameraSettings.Orthogonal)
			{
                // shared layer camera settings?  That seems BAD!
                //Layer.LayerCameraSettings = element.Layer.LayerCameraSettings;
                // I think we want to update to prevent modifications on the GlueView 
                // Layer (which can occur to render highlights) from screwing other layers.
                Layer.LayerCameraSettings = element.Layer.LayerCameraSettings.Clone();

			}
			else
			{
                if (coordinateType == LayerCoordinateType.MatchScreenResolution)
                {
                    Layer.UsePixelCoordinates();

                    Layer.LayerCameraSettings.LeftDestination = Camera.Main.LeftDestination;
                    Layer.LayerCameraSettings.RightDestination = Camera.Main.RightDestination;
                    Layer.LayerCameraSettings.TopDestination = Camera.Main.TopDestination;
                    Layer.LayerCameraSettings.BottomDestination = Camera.Main.BottomDestination;

                }
                else
                {
                    Layer.LayerCameraSettings = null;
                }
			}
		}

        private LayerCoordinateType CreatePolygonsForObject(object directObjectReference, LayerCoordinateType coordinateType)
        {
            if (directObjectReference is Scene)
            {
                CreatePolygonsForSceneElement(directObjectReference as Scene);
            }
            else if (directObjectReference is ShapeCollection)
            {
                CreatePolygonsForShapeCollectionElement(directObjectReference as ShapeCollection);
            }
            else if (directObjectReference is PositionedObject)
            {
                // Check the specifics before the general IReadOnlyScalable
                if (directObjectReference is Text)
                {
                    CreatePolygonForText((Text)directObjectReference);
                }
                else if (directObjectReference is Circle)
                {
                    CreatePolygonForCircle((Circle)directObjectReference);
                }
                else if (directObjectReference is AxisAlignedRectangle)
                {
                    CreatePolygonForAxisAlignedRectangle((AxisAlignedRectangle)directObjectReference);
                }
                else if(directObjectReference is Polygon)
                {
                    CreatePolygonForPolygon((Polygon)directObjectReference);

                }
                else if (directObjectReference is IReadOnlyScalable)
                {
                    CreatePolygonForIScalable(directObjectReference);
                }

            }
            else if (directObjectReference is Layer)
            {
                CreatePolygonForLayer((Layer)directObjectReference);
                coordinateType = LayerCoordinateType.MatchScreenResolution;
            }
            else if (directObjectReference is IEnumerable)
            {
                IEnumerable asEnumerable = directObjectReference as IEnumerable;

                foreach (var item in asEnumerable)
                {
                    CreatePolygonsForObject(item, coordinateType);
                }
            }
            return coordinateType;
        }



        private void CreatePolygonForAxisAlignedRectangle(AxisAlignedRectangle axisAlignedRectangle)
        {
            Polygon newPoly = CreatePolygonForIScalable(axisAlignedRectangle);

            const int pixelsOut = 2;

            GrowPolygonByPixels(pixelsOut, axisAlignedRectangle.Z, newPoly);
        }

        private void CreatePolygonForPolygon(Polygon polygon)
        {

            Polygon newPoly = new Polygon();

            Vector3 pos = polygon.Position;

            newPoly.Position = pos;

            mHighlightShapes.Add(newPoly);

            newPoly.AttachTo(polygon, true);

            newPoly.RelativeRotationZ = 0;

            newPoly.Points = polygon.Points;

            const int pixelsOut = 2;

            GrowPolygonByPixels(pixelsOut, polygon.Z, newPoly);
            InitializeNewPoly(newPoly);

        }

        private void GrowPolygonByPixels(int pixelsOut, float zValue, Polygon newPoly)
        {
            if (newPoly.Points != null)
            {
                // These used to always be rectangles, but now they can be irregular shapes because GView can show polys
                for (int i = 0; i < newPoly.Points.Count; i++)
                {
                    float oldX = (float)newPoly.Points[i].X;
                    float oldY = (float)newPoly.Points[i].Y;


                    float unitsOut;
                    if (Layer == null)
                    {
                        unitsOut = 1 / Camera.Main.PixelsPerUnitAt(zValue);
                    }
                    else
                    {
                        unitsOut = 1 / Layer.PixelsPerUnitAt(zValue);
                    }

                    float xSign = System.Math.Sign(oldX);
                    float ySign = System.Math.Sign(oldY);




                    newPoly.SetPoint(i,
                        oldX + xSign * unitsOut * pixelsOut,
                        oldY + ySign * unitsOut * pixelsOut);
                }
            }
        }

        protected virtual void CreatePolygonForScalableEntity(ElementRuntime element)
        {
            NamedObjectSave namedObjectSave = element.AssociatedNamedObjectSave;

            object scaleXAsObject = namedObjectSave.GetEffectiveValue("ScaleX");
            object scaleYAsObject = namedObjectSave.GetEffectiveValue("ScaleY");

            if (scaleXAsObject != null && scaleYAsObject != null)
            {
                float scaleX = (float)scaleXAsObject;
                float scaleY = (float)scaleYAsObject;

                Polygon newPoly = Polygon.CreateRectangle(scaleX, scaleY);

                InitializeNewPoly(newPoly);

                newPoly.Position = element.Position;

                mHighlightShapes.Add(newPoly);

                newPoly.AttachTo(element, true);

                newPoly.RelativeRotationZ = 0;
            }
        }

        public Color GetColorVisibleAgainst(Color otherColor)
        {

           int brightness = (int)System.Math.Sqrt(
              otherColor.R * otherColor.R * .241 +
              otherColor.G * otherColor.G * .691 +
              otherColor.B * otherColor.B * .068);

           if (brightness > 128)
           {
               return Color.Black;
           }
           else
           {
               return Color.White;
           }

        }

		protected virtual void CreatePolygonsForSceneElement(Scene sceneObject)
		{
			foreach (Sprite s in sceneObject.Sprites)
			{
				CreatePolygonForIScalable(s);
			}

			foreach (Text t in sceneObject.Texts)
			{
				CreatePolygonForText(t);
			}

			foreach (SpriteFrame spriteFrame in sceneObject.SpriteFrames)
			{
				CreatePolygonForIScalable(spriteFrame);
			}
		}

        private void CreatePolygonsForShapeCollectionElement(ShapeCollection shapeCollection)
        {
            foreach (AxisAlignedRectangle aar in shapeCollection.AxisAlignedRectangles)
            {
                CreatePolygonForAxisAlignedRectangle(aar);
            }
            foreach (Capsule2D capsule in shapeCollection.Capsule2Ds)
            {
                //CreatePolygonForCapsule(capsule);
            }
            foreach (Circle circle in shapeCollection.Circles)
            {
                CreatePolygonForCircle(circle);
            }
            foreach (Polygon polygon in shapeCollection.Polygons)
            {
                //CreatePolygonForPolygon(polygon);
            }
        }
		
        protected virtual Polygon CreatePolygonForIScalable(object obj)
		{

			Polygon newPoly = Polygon.CreateRectangle((IReadOnlyScalable)obj);

			InitializeNewPoly(newPoly);

			Vector3 pos = (Vector3)typeof(PositionedObject).GetField("Position").GetValue(obj);

			newPoly.Position = pos;

			mHighlightShapes.Add(newPoly);

			newPoly.AttachTo((PositionedObject)obj, true);

			newPoly.RelativeRotationZ = 0;

            return newPoly;
		}

		protected virtual void CreatePolygonForCircle(object o)
		{
			Circle asCircle = o as Circle;

			Polygon newPoly = Polygon.CreateRectangle(asCircle.Radius, asCircle.Radius);
			InitializeNewPoly(newPoly);

			Vector3 pos = asCircle.Position;

			newPoly.Position = pos;

			mHighlightShapes.Add(newPoly);

			newPoly.AttachTo(asCircle, true);

			newPoly.RelativeRotationZ = 0;

		}


		protected virtual void CreatePolygonForLayer(Layer layer)
		{
			if (layer.LayerCameraSettings != null && layer.LayerCameraSettings.Orthogonal)
			{
				// We divide by 2 here because we're converting width and height into Scale values
				Polygon newPolygon = Polygon.CreateRectangle(
					(layer.LayerCameraSettings.RightDestination - layer.LayerCameraSettings.LeftDestination) / 2.0f,
                    (layer.LayerCameraSettings.BottomDestination - layer.LayerCameraSettings.TopDestination) / 2.0f);
				InitializeNewPoly(newPolygon);

                // We used to use ortho width, 
                // but now we want to use the actual 
                // screen resolution because the polygon
                // will render on a pixel-perfect layer.
                //Vector3 pos = new Vector3(
                //    -SpriteManager.Camera.OrthogonalWidth / 2.0f + (layer.LayerCameraSettings.LeftDestination + layer.LayerCameraSettings.RightDestination) / 2.0f,
                //    SpriteManager.Camera.OrthogonalHeight / 2.0f - (layer.LayerCameraSettings.TopDestination + layer.LayerCameraSettings.BottomDestination) / 2.0f,
                //    0);
                Vector3 pos = new Vector3(
                    -SpriteManager.Camera.DestinationRectangle.Width / 2.0f + (layer.LayerCameraSettings.LeftDestination + layer.LayerCameraSettings.RightDestination) / 2.0f,
                    SpriteManager.Camera.DestinationRectangle.Height / 2.0f - (layer.LayerCameraSettings.TopDestination + layer.LayerCameraSettings.BottomDestination) / 2.0f,
                    0);


				newPolygon.Position = pos;

				mHighlightShapes.Add(newPolygon);


			}
		}

		protected virtual void CreatePolygonForText(Text t)
		{

			//PropertyInfo scaleXProperty = t.GetType().GetProperty("ScaleX");
			//PropertyInfo scaleYProperty = t.GetType().GetProperty("ScaleY");

            // Why aren't we multiplying this by 2?
            float width = t.ScaleX;// (float)scaleYProperty.GetValue(t, null);
            float height = t.ScaleY;// (float)scaleXProperty.GetValue(t, null);


			Polygon newPoly = Polygon.CreateRectangle(width, height);
			InitializeNewPoly(newPoly);
			mHighlightShapes.Add(newPoly);

			Vector3 pos = new Vector3(t.HorizontalCenter, t.VerticalCenter, t.Z);
			newPoly.Position = pos;

			newPoly.RotationMatrix = t.RotationMatrix;

            newPoly.Color = new Color(1, 1, 1,  .5f);
            newPoly.AttachTo((PositionedObject)t, true);

            if (!float.IsInfinity(t.MaxWidth))
            {
			    newPoly = Polygon.CreateRectangle(t.MaxWidth/2.0f, height);
			    InitializeNewPoly(newPoly);
			    mHighlightShapes.Add(newPoly);


                float x;
                if (t.HorizontalAlignment == HorizontalAlignment.Left)
                {
                    x = t.X + t.MaxWidth / 2.0f;
                }
                else if (t.HorizontalAlignment == HorizontalAlignment.Center)
                {
                    x = t.X;
                }
                else
                {
                    x = t.X - t.MaxWidth / 2.0f;
                }

			    pos = new Vector3(x, t.VerticalCenter, t.Z);
			    newPoly.Position = pos;

			    newPoly.RotationMatrix = t.RotationMatrix;

                newPoly.AttachTo((PositionedObject)t, true);

                newPoly.Color = new Color(1, .25f, 0, .5f);
            }

		}

		protected void InitializeNewPoly(Polygon polygon)
        {

            ShapeManager.AddPolygon(polygon);

            ShapeManager.AddToLayer(polygon, Layer);

            polygon.Color = Color;

        }

        public virtual void RemoveHighlights()
        {
            foreach (Polygon p in mHighlightShapes)
            {
                ShapeManager.Remove(p);
            }
           
            mHighlightShapes.Clear();
        }

        #endregion
	}
}
