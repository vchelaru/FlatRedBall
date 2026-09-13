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

        // Every entity-attached Gum object shares this one layer, so the FRB Editor's live-edit zoom
        // can zoom world-space content (this layer) without touching HUD/screen-space content (which
        // never lands here) - see CameraLogic.UpdateCameraToZoomLevel.
        private static global::RenderingLibrary.Graphics.Layer entityAttachmentZoomLayer;
        private static global::FlatRedBall.Graphics.Layer entityAttachmentFrbLayer;
        private static bool hasSubscribedToScreenDestroyedForZoomLayerCleanup;

        public static global::RenderingLibrary.Graphics.Layer GetOrCreateEntityAttachmentZoomLayer()
        {
            if (entityAttachmentZoomLayer == null)
            {
                entityAttachmentZoomLayer = new global::RenderingLibrary.Graphics.Layer();
                entityAttachmentZoomLayer.Name = "FrbEntityAttachmentGumZoomLayer";
                entityAttachmentZoomLayer.LayerCameraSettings = new global::RenderingLibrary.Graphics.LayerCameraSettings
                {
                    IsInScreenSpace = false
                };

                // A Gum layer with no matching FRB Layer never actually gets drawn - FRB's render
                // dispatch (GumIdb.Draw) only ever renders MainLayer during the default pass, or Gum
                // layers explicitly registered against a named FRB Layer. Create a real FRB Layer and
                // register through GumIdb so FRB actually invokes a draw pass for it - the entity's own
                // sprite doesn't need to move, since the health bar's layer membership is independent
                // of whatever layer the entity itself renders on.
                entityAttachmentFrbLayer = global::FlatRedBall.SpriteManager.AddLayer();
                entityAttachmentFrbLayer.Name = "FrbEntityAttachmentZoomLayer";
                global::FlatRedBall.Gum.GumIdb.Self.AddGumLayerToFrbLayer(entityAttachmentZoomLayer, entityAttachmentFrbLayer);

                // FRB's ScreenManager warns (CheckAndWarnIfNotEmpty) if a screen doesn't clean up every
                // Layer it created before unloading - since this layer is a lazily-created singleton
                // that can outlive many screens, remove it whenever the CURRENT screen is destroyed and
                // let it be recreated fresh for whichever screen needs it next, rather than trying to
                // track which screen originally created it.
                if (!hasSubscribedToScreenDestroyedForZoomLayerCleanup)
                {
                    hasSubscribedToScreenDestroyedForZoomLayerCleanup = true;
                    global::FlatRedBall.Screens.ScreenManager.AfterScreenDestroyed += _ =>
                    {
                        if (entityAttachmentFrbLayer != null)
                        {
                            global::FlatRedBall.SpriteManager.RemoveLayer(entityAttachmentFrbLayer);
                            global::RenderingLibrary.SystemManagers.Default.Renderer.RemoveLayer(entityAttachmentZoomLayer);
                            entityAttachmentFrbLayer = null;
                            entityAttachmentZoomLayer = null;
                        }
                    };

                    // A statically-placed entity (and thus this layer's lazy creation) can happen
                    // BEFORE the screen's own AddLayer() calls for things like a "draw above
                    // darkness/lighting" layer, landing us before them in draw order. ScreenLoaded
                    // fires once, after Initialize() has fully finished (so every screen-created layer
                    // already exists) and is never invoked mid-draw, unlike UpdateGumObject - safe to
                    // reorder here.
                    global::FlatRedBall.Screens.ScreenManager.ScreenLoaded += _ => EnsureEntityAttachmentFrbLayerDrawsLast();
                }
            }
            return entityAttachmentZoomLayer;
        }

        // This layer is created lazily the first time an entity-attached Gum object is constructed,
        // which for a statically-placed entity can happen BEFORE the screen's own AddLayer() calls for
        // things like a "draw above darkness/lighting" layer - landing us before them in draw order and
        // getting completely covered up, even though positioning/zoom are otherwise correct. AddLayer
        // always appends, so re-adding (public API, no access to the internal writeable list needed)
        // moves it back to the end - drawn last, on top of everything, whenever something else has been
        // added after it.
        private static void EnsureEntityAttachmentFrbLayerDrawsLast()
        {
            var layers = global::FlatRedBall.SpriteManager.Layers;
            var isLast = layers.Count > 0 && layers[layers.Count - 1] == entityAttachmentFrbLayer;
            if (entityAttachmentFrbLayer != null && !isLast)
            {
                global::FlatRedBall.SpriteManager.RemoveLayer(entityAttachmentFrbLayer);
                global::FlatRedBall.SpriteManager.AddLayer(entityAttachmentFrbLayer);
            }
        }

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
            GumParent.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
            GumParent.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;

            GumParent.XOrigin = HorizontalAlignment.Center;
            GumParent.YOrigin = VerticalAlignment.Center;


            this.FrbObject = frbObject;
            this.GumObject = gumObject;

            gumObject.Parent = GumParent;

            // Only re-home onto the shared zoom layer in edit mode - gameplay keeps gumObject on
            // whatever layer AddToManagers originally placed it on (LayerProvidedByContainer), so
            // draw order for shipped games is completely unaffected by this mechanism.
            if (global::FlatRedBall.Screens.ScreenManager.IsInEditMode)
            {
                // gumObject.Layer (mLayer) always names the layer AddToManagers registered it on -
                // AddToManagers hands the actual Renderables-list membership to the CONTAINED object
                // (e.g. an InvisibleRenderable for a ContainerRuntime-derived component like a health
                // bar), not to gumObject itself. A raw Layer.Remove(gumObject)/Add(gumObject) here would
                // therefore never remove the real registration - it would leave the original entry in
                // place and add gumObject as a second, independent one, so the same content draws
                // twice. GraphicalUiElement.MoveToLayer already contains the correct remove/add against
                // the real contained object (or, for a childless composite, against its own children);
                // its parented-element guard is compiled out for FRB builds (#if !FRB), so calling it on
                // a GumParent-parented element here is safe.
                gumObject.MoveToLayer(GetOrCreateEntityAttachmentZoomLayer());

                // Only reorder here, in the constructor - never from UpdateGumObject's per-frame path.
                // UpdateGumObject runs via ForceUpdateDependencies, which FRB can also call DURING the
                // draw pass itself (e.g. visibility/culling checks needing a fresh position). Mutating
                // SpriteManager's layer list (Remove then Add) while DrawLayers is actively iterating
                // that same list by index causes it to visit the reordered layer twice in one frame -
                // drawing this exact object twice, which is exactly what was observed (a duplicate that
                // perfectly overlaps at 100% zoom, since it's the same object drawn twice, not a second
                // registration).
                EnsureEntityAttachmentFrbLayerDrawsLast();
            }
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

        public void UpdateGumObject()
        {

            // This is going to get positioned according to the FRB object. I guess we'll force update dependencies, which is expensive...
            FrbObject.ForceUpdateDependencies();

            // todo - need to support multiple cameras and layers
            var camera = global::FlatRedBall.Camera.Main;


            int screenXRelativeToDestinationRectangle = 0;
            int screenYRelativeToDestinationRectangle = 0;

            var worldPosition = FrbObject.Position + this.RelativePosition;

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
                var managers = GumObject.Managers ?? SystemManagers.Default;
                var renderer = managers.Renderer;
                var gumCamera = renderer.Camera;

                RenderingLibrary.Graphics.Layer editModeZoomLayer = null;
                if (global::FlatRedBall.Screens.ScreenManager.IsInEditMode)
                {
                    editModeZoomLayer = GetOrCreateEntityAttachmentZoomLayer();

                    // Game code can call gumObject.MoveToLayer/MoveToFrbLayer at any time after this
                    // wrapper's constructor ran (e.g. re-parenting a health bar onto a HUD layer once
                    // its owning entity spawns) - that silently steals gumObject back off the zoom
                    // layer with no way for this wrapper to intervene at the call site. Reclaiming it
                    // here every frame makes edit-mode zoom tracking win regardless of what else moves
                    // the object, instead of depending on nothing else ever calling MoveToLayer on it
                    // after construction.
                    if (GumObject.Layer != editModeZoomLayer)
                    {
                        GumObject.MoveToLayer(editModeZoomLayer);
                    }
                }

                // SpriteRenderer.GetZoomAndMatrix now uses the same offset-free transform
                // (screenPos = (canvasPos - gumCamera.X) * zoom) for both a world-space layer with
                // explicit LayerCameraSettings (our editModeZoomLayer, in TopLeft mode - see the Gum-side
                // fix in SpriteRenderer.cs) and the null-LayerCameraSettings default (e.g. MainLayer, via
                // the mode-aware instance GetTransformationMatrix). So one inverted formula covers both -
                // just use whichever layer's effective zoom actually applies to gumObject right now.
                var effectiveZoom = editModeZoomLayer?.LayerCameraSettings?.Zoom ?? gumCamera.Zoom;
                if (effectiveZoom == 0) effectiveZoom = 1;
                GumParent.X = (screenXRelativeToDestinationRectangle - gumCamera.X) / effectiveZoom;
                GumParent.Y = (screenYRelativeToDestinationRectangle - gumCamera.Y) / effectiveZoom;
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
