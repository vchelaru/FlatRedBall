using FlatRedBall;
using FlatRedBall.Math.Geometry;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;

namespace GumCoreShared.FlatRedBall.Embedded
{
    /// <summary>
    /// A PositionedObject which can hold a reference to a Gum object (GraphicalUiElement) to position it in FlatRedBall coordinates. 
    /// This allows Gum objects to be positioned in world space, and if the FrbObject is attached to another FlatRedBall object, then
    /// the Gum object can move with the parent FlatRedBall object, enabling attachments.
    /// </summary>
    public class PositionedObjectGueWrapper : PositionedObject
    {
        PositionedObject frbObject;
        global::FlatRedBall.Math.Geometry.IReadOnlyScalable frbObjectAsScalable;
        global::FlatRedBall.Graphics.IVisible frbObjectAsIVisible;

        /// <summary>
        /// The FlatRedBall object controlling the position of the Gum object. This is typically an entity instance, and codegen in the FRB Editor
        /// assigns this automatically when adding a Gum object to a FlatRedBall entity.
        /// </summary>
        public PositionedObject FrbObject
        {
            get { return frbObject; }
            set
            {
                frbObject = value;
                frbObjectAsScalable = value as global::FlatRedBall.Math.Geometry.IReadOnlyScalable;
                frbObjectAsIVisible = value as global::FlatRedBall.Graphics.IVisible;
            }
        }

        GraphicalUiElement GumParent { get; set; }
        public GraphicalUiElement GumObject { get; private set; }

        public PositionedObjectGueWrapper(PositionedObject frbObject, GraphicalUiElement gumObject) : base()
        {
            // July 21, 2021
            // Why don't we attach
            // this to the frbObject.
            // This allows code which looks
            // through children (like the level
            // editor) to find this.
            this.AttachTo(frbObject);

            var renderable = new InvisibleRenderable();
            renderable.Visible = true;

            GumParent = new GraphicalUiElement();
            GumParent.SetContainedObject(renderable);
            GumParent.XUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
            GumParent.YUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;

            GumParent.XOrigin = HorizontalAlignment.Center;
            GumParent.YOrigin = VerticalAlignment.Center;


            this.FrbObject = frbObject;
            this.GumObject = gumObject;
            
            gumObject.Parent = GumParent;
        }

        public override void ForceUpdateDependencies()
        {
            base.ForceUpdateDependencies();
            UpdateGumObject();

        }

        public override void ForceUpdateDependenciesDeep()
        {
            base.ForceUpdateDependenciesDeep();
            UpdateGumObject();
        }

        /// <summary>
        /// Calls base.UpdateDependencies to update this object's position and rotation, then updates the Gum object.
        /// </summary>
        /// <param name="currentTime">The current game time, used to prevent multiple calls from updating this.</param>
        public override void UpdateDependencies(double currentTime)
        {
            base.UpdateDependencies(currentTime);
            UpdateGumObject();
        }

        private void UpdateGumObject()
        {

            // This is going to get positioned according to the FRB object. I guess we'll force update dependencies, which is expensive...
            FrbObject.ForceUpdateDependencies();

            // todo - need to support multiple cameras and layers
            var camera = global::FlatRedBall.Camera.Main;


            int screenXRelativeToDestinationRectangle = 0;
            int screenYRelativeToDestinationRectangle = 0;

            var worldPosition = FrbObject.Position;

            global::FlatRedBall.Math.MathFunctions.AbsoluteToWindow(
                worldPosition.X, worldPosition.Y, worldPosition.Z,
                ref screenXRelativeToDestinationRectangle, ref screenYRelativeToDestinationRectangle, camera);


            var zoom = 1.0f;
            if (camera.Orthogonal)
            {
                //var gumZoom = GumObject.Managers.Renderer.Camera.Zoom;
                //zoom = managers.Renderer.Camera.Zoom;
                // If we use the Gum zoom (managers.Renderer.Camera.Zoom), position will be accurate
                // but zooming of the objects in Gum won't change. What should happen is the Gum zoom 
                // should be zooming when the normal camera zooms too
                //zoom = camera.DestinationRectangle.Height / (managers.Renderer.Camera.Zoom * camera.OrthogonalHeight);
                // Update March 18, 2022
                // This is a huge mess, so
                // let's work this out:
                // screenX and screenY are
                // the pixel X and Y regardless
                // of any zoom. Therefore, on a 600
                // pixel wide screen, a value of 300
                // would be center of the screen. To convert
                // that to Gum coordinates, we need to set the
                // value to be the same ratio of the width and height.
                double ratioWidth = screenXRelativeToDestinationRectangle / (double)camera.DestinationRectangle.Width;
                double ratioHeight = screenYRelativeToDestinationRectangle / (double) camera.DestinationRectangle.Height;

                var managers = GumObject.Managers ?? SystemManagers.Default;
                var renderer = managers.Renderer;
                GumParent.X = (float)(GraphicalUiElement.CanvasWidth * ratioWidth);
                GumParent.Y = (float)(GraphicalUiElement.CanvasHeight * ratioHeight);
            }
            else
            {
                // todo - need to figure out 3D, but we'll worry about that later
                GumParent.X = screenXRelativeToDestinationRectangle / zoom;
                GumParent.Y = screenYRelativeToDestinationRectangle / zoom;
            }

            if(this.ParentRotationChangesRotation)
            {
                GumParent.Rotation = Microsoft.Xna.Framework.MathHelper.ToDegrees(this.FrbObject.RotationZ);
            }

            if (frbObjectAsScalable != null)
            {
                GumParent.Width = frbObjectAsScalable.ScaleX * 2;
                GumParent.Height = frbObjectAsScalable.ScaleY * 2;
            }
            else
            {
                // This allows the user to position things according to the top-left of the Gum canvas and
                // have that align with the center of the entity. Otherwise, positioning seems arbitrary.
                GumParent.Width = 0;
                GumParent.Height = 0;
            }
            if(frbObjectAsIVisible != null)
            {
                GumParent.Visible = frbObjectAsIVisible.AbsoluteVisible;
            }
        }

        /// <summary>
        /// Returns the absolute world position of the center of the argument graphicalUiElement.
        /// </summary>
        /// <remarks>
        /// This can be used to position FRB objects (such as collision shapes) according to the absolute
        /// position of the Glue object.</remarks>
        /// <param name="graphicalUiElement">The argument GraphicalUiElement.</param>
        /// <returns>The absolute position of the center of the GraphicalUiElement</returns>
        public Vector3 GetAbsolutePositionInFrbSpace(GraphicalUiElement graphicalUiElement)
        {
            var parentX = GumObject.GetAbsoluteX();
            var parentY = GumObject.GetAbsoluteY();

            var gumObjectAsIpso = GumObject as IPositionedSizedObject;

            var rectX = graphicalUiElement.GetAbsoluteX();
            var rectY = graphicalUiElement.GetAbsoluteY();

            var rectLeftOffset = rectX - parentX;
            var rectTopOffset = rectY - parentY;

            var toReturn = new Vector3();
            // Don't use Width and Height as those may have the wrong position values.
            //toReturn.X = FrbObject.X + gumObjectAsIpso.X + rectLeftOffset
            //    + graphicalUiElement.Width / 2.0f;
            //toReturn.Y = FrbObject.Y - gumObjectAsIpso.Y - rectTopOffset
            //    - graphicalUiElement.Height / 2.0f;
            toReturn.X = FrbObject.X + gumObjectAsIpso.X + rectLeftOffset
                + graphicalUiElement.GetAbsoluteWidth() / 2.0f;
            toReturn.Y = FrbObject.Y - gumObjectAsIpso.Y - rectTopOffset
                - graphicalUiElement.GetAbsoluteHeight() / 2.0f;

            toReturn.Z = FrbObject.Z;

            return toReturn;
        }
    }

    /// <summary>
    /// Static class containing extension methods for updating FlatRedBall objects from a GraphicalUiElement.
    /// </summary>
    public static class GraphicalUiElementExtensions
    {
        /// <summary>
        /// Updates the children shapes of the argument Parent to match the Gum object's shapes. This optionally
        /// creates new shapes.
        /// </summary>
        /// <param name="graphicalUiElement">The GraphicalUiElement from which to pull shapes.</param>
        /// <param name="shapeCollection">The ShapeCollection for the FlatRedBall shapes.</param>
        /// <param name="parent">The parent for the FlatRedBall shapes.</param>
        /// <param name="createMissingShapes">Whether to instantiate missing shapes, where the name is the property used to find matches.</param>
        public static void SetCollision(this GraphicalUiElement graphicalUiElement, ShapeCollection shapeCollection, PositionedObject parent, bool createMissingShapes = false)
        {
            // this will do it only at the element level. Instances must be of shape type to be applied
            if(graphicalUiElement.ElementSave != null)
            {

                for(int i = 0; i < graphicalUiElement.ElementSave.Instances.Count; i++)
                {
                    var instance = graphicalUiElement.ElementSave.Instances[i];

                    if (instance.BaseType == "Circle")
                    {
                        Circle frbMatch = null;

                        for(int j = 0; j < shapeCollection.Circles.Count; j++)
                        {
                            var candidate = shapeCollection.Circles[j];
                            if(candidate.Name == instance.Name)
                            {
                                frbMatch = candidate;
                                break;
                            }
                        }

                        if(frbMatch == null && createMissingShapes)
                        {
                            frbMatch = new Circle();
                            frbMatch.Name = instance.Name;
                            frbMatch.AttachTo(parent);
                            shapeCollection.Circles.Add(frbMatch);
                        }

                        if(frbMatch != null)
                        {
                            var gue = graphicalUiElement.GetGraphicalUiElementByName(instance.Name);

                            if(gue != null)
                            {
                                frbMatch.Radius = gue.GetAbsoluteWidth() / 2.0f;

                                SetFrbObjectWorldPosition(frbMatch, gue);
                                frbMatch.SetRelativeFromAbsolute();
                            }
                        }
                    }
                    
                    else if(instance.BaseType == "Rectangle")
                    {
                        AxisAlignedRectangle frbMatch = null;

                        for (int j = 0; j < shapeCollection.AxisAlignedRectangles.Count; j++)
                        {
                            var candidate = shapeCollection.AxisAlignedRectangles[j];
                            if (candidate.Name == instance.Name)
                            {
                                frbMatch = candidate;
                                break;
                            }
                        }

                        if(frbMatch == null && createMissingShapes)
                        {
                            frbMatch = new AxisAlignedRectangle();
                            frbMatch.AttachTo(parent);
                            frbMatch.Name = instance.Name;
                            shapeCollection.AxisAlignedRectangles.Add(frbMatch);
                        }

                        if(frbMatch != null)
                        {
                            var gue = graphicalUiElement.GetGraphicalUiElementByName(instance.Name);
                            
                            if(gue != null)
                            {
                                frbMatch.Width = gue.GetAbsoluteWidth();
                                frbMatch.Height = gue.GetAbsoluteWidth();

                                SetFrbObjectWorldPosition(frbMatch, gue);
                                frbMatch.SetRelativeFromAbsolute();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updates the children sprite of the argument Parent to match the Gum object's sprites. This optionally
        /// creates new Sprites.
        /// </summary>
        /// <param name="graphicalUiElement">The GraphicalUiElement from which to pull sprites.</param>
        /// <param name="parent">The parent of the FlatRedBall Sprites.</param>
        /// <param name="createMissingSprites">Whether to instantiate missing sprites, where the name is the property used to find matches.</param>
        public static void SetSprites(this GraphicalUiElement graphicalUiElement, PositionedObject parent, bool createMissingSprites = false)
        {
            if(graphicalUiElement.ElementSave != null)
            {
                for (int i = 0; i < graphicalUiElement.ElementSave.Instances.Count; i++)
                {
                    var instance = graphicalUiElement.ElementSave.Instances[i];

                    if(instance.BaseType == "Sprite")
                    {
                        global::FlatRedBall.Sprite frbMatch = null;

                        for(int j = 0; j < parent.Children.Count; j++)
                        {
                            var candidate = parent.Children[j];

                            if(candidate.Name == instance.Name && candidate is global::FlatRedBall.Sprite spriteCandidate)
                            {
                                frbMatch = spriteCandidate;
                                break;
                            }
                        }

                        if(frbMatch == null && createMissingSprites)
                        {
                            frbMatch = new global::FlatRedBall.Sprite();
                            global::FlatRedBall.SpriteManager.AddSprite(frbMatch);
                            frbMatch.Name = instance.Name;
                            frbMatch.AttachTo(parent); // todo - need to support positioned objects inbetween 
                        }

                        if(frbMatch != null)
                        {
                            var gue = graphicalUiElement.GetGraphicalUiElementByName(instance.Name);

                            if(gue != null)
                            {
                                frbMatch.Width = gue.GetAbsoluteWidth();
                                frbMatch.Height = gue.GetAbsoluteHeight();

                                SetFrbObjectWorldPosition(frbMatch, gue);
                                frbMatch.SetRelativeFromAbsolute();

                                var gumSprite = gue.RenderableComponent as RenderingLibrary.Graphics.Sprite;
                                frbMatch.Texture = gumSprite.Texture;
                                if (gumSprite.SourceRectangle == null)
                                {
                                    frbMatch.LeftTextureCoordinate = 0;
                                    frbMatch.TopTextureCoordinate = 0;
                                    frbMatch.RightTextureCoordinate = 1;
                                    frbMatch.BottomTextureCoordinate = 1;
                                }
                                else
                                {
                                    var sourceRect = gumSprite.SourceRectangle.Value;
                                    frbMatch.LeftTexturePixel = sourceRect.Left;
                                    frbMatch.RightTexturePixel = sourceRect.Right;
                                    frbMatch.TopTexturePixel = sourceRect.Top;
                                    frbMatch.BottomTexturePixel = sourceRect.Bottom;
                                }

                                frbMatch.FlipHorizontal = gumSprite.GetAbsoluteFlipHorizontal();
                            }
                        }
                    }
                }
            }
        }

        private static void SetFrbObjectWorldPosition(global::FlatRedBall.PositionedObject frbMatch, GraphicalUiElement gue)
        {
            var centerScreenX = gue.GetAbsoluteCenterX();
            var centerScreenY = gue.GetAbsoluteCenterY();

            var camera = global::FlatRedBall.Camera.Main;

            var xMultiple = camera.DestinationRectangle.Width / GraphicalUiElement.CanvasWidth;
            var yMultiple = camera.DestinationRectangle.Height / GraphicalUiElement.CanvasHeight;

            centerScreenX *= xMultiple;
            centerScreenY *= yMultiple;

            // 2 convert the screen to world
            var worldPosition = new Vector3();
            global::FlatRedBall.Math.MathFunctions.WindowToAbsolute(
                (int)centerScreenX,
                (int)centerScreenY,
                ref worldPosition);

            frbMatch.Position = worldPosition;
        }
    }
}
