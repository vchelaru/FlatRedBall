{CompilerDirectives}

using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Utilities;
using Microsoft.Xna.Framework;
using StateInterpolationPlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueControl.Editing
{
    #region Enums

    public enum ResizeSide
    {
        None = -1,
        TopLeft,
        Top,
        TopRight,
        Right,
        BottomRight,
        Bottom,
        BottomLeft,
        Left
    }

    public enum ResizeMode
    {
        None,
        EightWay,
        Cardinal,
        RightAndDown
    }

    #endregion

    #region Interfaces


    public interface ISelectionMarker
    {
        event Action<INameable, string, object> PropertyChanged;

        float ExtraPaddingInPixels { get; set; }
        bool Visible { get; set; }
        double FadingSeed { get; set; }
        Color BrightColor { get; set; }
        string Name { get; set; }
        bool CanMoveItem { get; set; }
        Vector3 LastUpdateMovement { get; }
        bool UsesRightMouseButton { get; }

        INameable Owner { get; }

        bool HandleDelete();
        void MakePersistent();
        void PlayBumpAnimation(float endingExtraPaddingBeforeZoom, bool isSynchronized);
        void Update(bool didGameBecomeActive);
        bool ShouldSuppress(string memberName);

        bool IsMouseOverThis();
        void Destroy();
    }

    #endregion

    #region Handles Class
    public class ResizeHandles
    {
        #region Fields/Properties

        AxisAlignedRectangle[] rectangles = new AxisAlignedRectangle[8];
        public const int DefaultHandleDimension = 10;

        // 14 wasn't noticeable for opposite handles when resizing from center
        //const int HighlightedHandleDimension = 14;
        public const int HighlightedHandleDimension = 16;


        public bool ShouldResizeXFromCenter { get; private set; }
        public bool ShouldResizeYFromCenter { get; private set; }

        public ResizeSide SideGrabbed
        {
            get;
            private set;
        } = ResizeSide.None;

        HashSet<ResizeSide> sidesToHighlight = new HashSet<ResizeSide>();

        public bool Visible { get; set; }

        public ResizeMode ResizeMode { get; set; }

        #endregion

        #region Constructor/Destroy
        public ResizeHandles()
        {
            for (int i = 0; i < rectangles.Length; i++)
            {
                rectangles[i] = new AxisAlignedRectangle();

                rectangles[i].Width = DefaultHandleDimension;
                rectangles[i].Height = DefaultHandleDimension;

                rectangles[i].Visible = false;
                ShapeManager.AddToLayer(rectangles[i], SpriteManager.TopLayer, makeAutomaticallyUpdated: false);
            }
        }

        internal void MakePersistent()
        {
            for (int i = 0; i < rectangles.Length; i++)
            {
                FlatRedBall.Screens.ScreenManager.PersistentAxisAlignedRectangles.Add(rectangles[i]);
            }
        }

        internal void Destroy()
        {
            for (int i = 0; i < rectangles.Length; i++)
            {
                FlatRedBall.Screens.ScreenManager.PersistentAxisAlignedRectangles.Remove(rectangles[i]);
                rectangles[i].Visible = false;
            }
        }

        #endregion

        #region Update

        public void EveryFrameUpdate(PositionedObject item, SelectionMarker selectionMarker, bool didGameBecomeActive)
        {
            Visible = selectionMarker.Visible;

            var mouse = FlatRedBall.Input.InputManager.Mouse;


            UpdateVisibilityConsideringResizeMode();

            if (Visible)
            {
                // Update the "should resize" values if the mouse isn't down so that
                // highlights reflect whether opposite sides will be resized. 
                // Do not do this if the mouse is down because we want to take a snapshot
                // of these values when the mouse is pressed and continue to use those values
                // during the duration of the drag.
                if (!mouse.ButtonDown(FlatRedBall.Input.Mouse.MouseButtons.LeftButton))
                {
                    bool shouldAttemptResizeFromCenter = GetIfShouldResizeFromCenter(item);
                    // If we're resizing a rectangle on an object, we may not want to move on resize, so let's change the position
                    // values to 0 and double the dimension values
                    if (shouldAttemptResizeFromCenter)
                    {
                        if (item.RelativeX == 0)
                        {
                            ShouldResizeXFromCenter = true;
                        }
                        if (item.RelativeY == 0)
                        {
                            ShouldResizeYFromCenter = true;
                        }
                    }

                }


                FillSidesToHighlight(item);

                // UpdateDimension before UpdateHandlePositions because
                // UpdateHandlePositions depends on the size of the handles
                // to position them correctly. If the order is inverted then
                // handles will "pop" for 1 frame. Not a huge deal but looks unprofessional.
                UpdateDimension();

                if (item is IReadOnlyScalable scalable)
                {
                    UpdateHandlePositions(scalable, selectionMarker.Position);
                }
                else if (item is Circle asCircle)
                {
                    UpdateHandlePositions(asCircle, selectionMarker.Position);
                }

                if (selectionMarker.CanMoveItem)
                {
                    if (item is Sprite asSprite && asSprite.TextureScale > 0)
                    {
                        ResizeMode = ResizeMode.Cardinal;
                    }
                    else if (item is FlatRedBall.Math.Geometry.Circle)
                    {
                        ResizeMode = ResizeMode.Cardinal;
                    }
                    else if (item is FlatRedBall.Math.Geometry.IScalable)
                    {
                        ResizeMode = ResizeMode.EightWay;
                    }
                    else
                    {
                        ResizeMode = ResizeMode.None;
                    }
                }

                var shouldHandlePush =
                    (mouse.ButtonPushed(FlatRedBall.Input.Mouse.MouseButtons.LeftButton) && FlatRedBallServices.Game.IsActive) ||
                    (mouse.ButtonDown(FlatRedBall.Input.Mouse.MouseButtons.LeftButton) && didGameBecomeActive);
                if (shouldHandlePush)
                {
                    HandleMousePushed(item);
                }

                if (mouse.ButtonReleased(FlatRedBall.Input.Mouse.MouseButtons.LeftButton))
                {
                    HandleMouseRelease();
                }

            }
        }

        internal void UpdateVisibilityConsideringResizeMode()
        {
            if (Visible && ResizeMode == ResizeMode.EightWay)
            {
                foreach (var handle in rectangles)
                {
                    handle.Visible = true;
                }
            }
            else if (Visible && ResizeMode == ResizeMode.Cardinal)
            {
                for (int i = 0; i < rectangles.Length; i++)
                {
                    var handle = rectangles[i];
                    // every other one, starting with index 1
                    handle.Visible = (i % 2) == 1;
                }
            }
            else
            {
                foreach (var handle in rectangles)
                {
                    handle.Visible = false;
                }
            }
        }

        internal void UpdateDimension()
        {

            for (int i = 0; i < rectangles.Count(); i++)
            {
                var handle = rectangles[i];

                var side = (ResizeSide)i;
                float size = sidesToHighlight.Contains(side) ? HighlightedHandleDimension : DefaultHandleDimension;
                size /= CameraLogic.CurrentZoomRatio;

                handle.Width = size;
                handle.Height = size;
            }
        }

        public void UpdateHandlePositions(IReadOnlyScalable owner, Vector3 objectCenter)
        {
            var handle = rectangles[0];
            handle.X = -owner.ScaleX - handle.Width / 2;
            handle.Y = owner.ScaleY + handle.Height / 2;
            handle.Z = 0;

            handle = rectangles[1];
            handle.X = 0;
            handle.Y = owner.ScaleY + handle.Height / 2;
            handle.Z = 0;

            handle = rectangles[2];
            handle.X = owner.ScaleX + handle.Width / 2;
            handle.Y = owner.ScaleY + handle.Height / 2;
            handle.Z = 0;

            handle = rectangles[3];
            handle.X = owner.ScaleX + handle.Width / 2;
            handle.Y = 0;
            handle.Z = 0;

            handle = rectangles[4];
            handle.X = +owner.ScaleX + handle.Width / 2;
            handle.Y = -owner.ScaleY - handle.Height / 2;
            handle.Z = 0;

            handle = rectangles[5];
            handle.X = 0;
            handle.Y = -owner.ScaleY - handle.Height / 2;
            handle.Z = 0;

            handle = rectangles[6];
            handle.X = -owner.ScaleX - handle.Width / 2;
            handle.Y = -owner.ScaleY - handle.Height / 2;
            handle.Z = 0;

            handle = rectangles[7];
            handle.X = -owner.ScaleX - handle.Width / 2;
            handle.Y = 0;
            handle.Z = 0;

            var rotationMatrix = Matrix.Identity;
            if (owner is IRotatable asRotatable)
            {
                rotationMatrix = asRotatable.RotationMatrix;
            }
            foreach (var rect in rectangles)
            {
                rect.Position = objectCenter + Vector3.Transform(rect.Position, rotationMatrix);
            }

        }

        void UpdateHandlePositions(Circle circle, Vector3 objectCenter)
        {
            var handle = rectangles[0];
            handle.X = -circle.Radius - handle.Width / 2;
            handle.Y = circle.Radius + handle.Height / 2;
            handle.Z = 0;

            handle = rectangles[1];
            handle.X = 0;
            handle.Y = circle.Radius + handle.Height / 2;
            handle.Z = 0;

            handle = rectangles[2];
            handle.X = circle.Radius + handle.Width / 2;
            handle.Y = circle.Radius + handle.Height / 2;
            handle.Z = 0;

            handle = rectangles[3];
            handle.X = circle.Radius + handle.Width / 2;
            handle.Y = 0;
            handle.Z = 0;

            handle = rectangles[4];
            handle.X = circle.Radius + handle.Width / 2;
            handle.Y = -circle.Radius - handle.Height / 2;
            handle.Z = 0;

            handle = rectangles[5];
            handle.X = 0;
            handle.Y = -circle.Radius - handle.Height / 2;
            handle.Z = 0;

            handle = rectangles[6];
            handle.X = -circle.Radius - handle.Width / 2;
            handle.Y = -circle.Radius - handle.Height / 2;
            handle.Z = 0;

            handle = rectangles[7];
            handle.X = -circle.Radius - handle.Width / 2;
            handle.Y = 0;
            handle.Z = 0;

            foreach (var rect in rectangles)
            {
                rect.Position += objectCenter;
            }
        }

        private void HandleMousePushed(PositionedObject ownerAsPositionedObject)
        {
            ShouldResizeXFromCenter = false;
            ShouldResizeYFromCenter = false;

            if (ownerAsPositionedObject != null)
            {
                if (ownerAsPositionedObject is IScalable scalable)
                {
                    bool shouldAttemptResizeFromCenter = GetIfShouldResizeFromCenter(ownerAsPositionedObject);
                    // If we're resizing a rectangle on an object, we may not want to move on resize, so let's change the position
                    // values to 0 and double the dimension values
                    if (shouldAttemptResizeFromCenter)
                    {
                        if (ownerAsPositionedObject.RelativeX == 0)
                        {
                            ShouldResizeXFromCenter = true;
                        }
                        if (ownerAsPositionedObject.RelativeY == 0)
                        {
                            ShouldResizeYFromCenter = true;
                        }
                    }
                }
                SideGrabbed = GetSideOver();
            }
            else
            {
                SideGrabbed = ResizeSide.None;
            }
        }

        private void FillSidesToHighlight(PositionedObject item)
        {

            var sidesForHighlighting = SideGrabbed;
            if (sidesForHighlighting == ResizeSide.None)
            {
                sidesForHighlighting = GetSideOver();
            }

            sidesToHighlight.Clear();

            sidesToHighlight.Add(sidesForHighlighting);

            if (GetIfShouldResizeFromCenter(item))
            {
                if (sidesForHighlighting == ResizeSide.Left && ShouldResizeXFromCenter) sidesToHighlight.Add(ResizeSide.Right);
                if (sidesForHighlighting == ResizeSide.Top && ShouldResizeYFromCenter) sidesToHighlight.Add(ResizeSide.Bottom);
                if (sidesForHighlighting == ResizeSide.Right && ShouldResizeXFromCenter) sidesToHighlight.Add(ResizeSide.Left);
                if (sidesForHighlighting == ResizeSide.Bottom && ShouldResizeYFromCenter) sidesToHighlight.Add(ResizeSide.Top);

                // if we grab a diagonal, all can be resized:
                if (sidesForHighlighting == ResizeSide.TopLeft ||
                    sidesForHighlighting == ResizeSide.TopRight ||
                    sidesForHighlighting == ResizeSide.BottomRight ||
                    sidesForHighlighting == ResizeSide.BottomLeft)
                {
                    if (ShouldResizeXFromCenter) sidesToHighlight.Add(ResizeSide.Right);
                    if (ShouldResizeYFromCenter) sidesToHighlight.Add(ResizeSide.Bottom);
                    if (ShouldResizeXFromCenter) sidesToHighlight.Add(ResizeSide.Left);
                    if (ShouldResizeYFromCenter) sidesToHighlight.Add(ResizeSide.Top);
                }
            }

            if (item is Circle || GetIfSetsTextureScale(item))
            {
                if (sidesForHighlighting == ResizeSide.Left)
                {
                    sidesToHighlight.Add(ResizeSide.Top);
                    sidesToHighlight.Add(ResizeSide.Bottom);
                }
                else if (sidesForHighlighting == ResizeSide.Top)
                {
                    sidesToHighlight.Add(ResizeSide.Left);
                    sidesToHighlight.Add(ResizeSide.Right);
                }
                else if (sidesForHighlighting == ResizeSide.Right)
                {
                    sidesToHighlight.Add(ResizeSide.Top);
                    sidesToHighlight.Add(ResizeSide.Bottom);
                }
                else if (sidesForHighlighting == ResizeSide.Bottom)
                {
                    sidesToHighlight.Add(ResizeSide.Left);
                    sidesToHighlight.Add(ResizeSide.Right);
                }
            }
        }

        private void HandleMouseRelease()
        {
            SideGrabbed = ResizeSide.None;
        }

        #endregion

        public ResizeSide GetSideOver()
        {
            var mouse = InputManager.Mouse;

            for (int i = 0; i < this.rectangles.Length; i++)
            {
                if (rectangles[i].Visible && IsOn3D(rectangles[i]))
                {
                    return (ResizeSide)i;
                }
            }

            return ResizeSide.None;
        }

        public bool IsOn3D<T>(T objectToTest) where T : IPositionable, IRotatable, IReadOnlyScalable
        {
            return IsOn3D(objectToTest, false, null, out Microsoft.Xna.Framework.Vector3 vector3);
        }

        public bool IsOn3D<T>(T spriteToTest, bool relativeToCamera, FlatRedBall.Graphics.Layer layer, out Vector3 intersectionPoint) where T : IPositionable, IRotatable, IReadOnlyScalable
        {
            intersectionPoint = Vector3.Zero;
            if (spriteToTest == null)
                return false;
#if SupportsEditMode

            return MathFunctions.IsOn3D<T>(
                spriteToTest, relativeToCamera, InputManager.Mouse.GetMouseRay(FlatRedBall.Camera.Main),
                FlatRedBall.Camera.Main, out intersectionPoint);
#else
            return false;
#endif
        }


        public bool GetIfSetsTextureScale(PositionedObject item)
        {
            return item is Sprite asSprite && asSprite.TextureScale > 0 && asSprite.Texture != null;
        }

        public bool GetIfShouldResizeFromCenter(PositionedObject item)
        {
            return item.Parent != null;
        }

    }

    #endregion

    public class SelectionMarker : ISelectionMarker, IReadOnlyScalable
    {
        #region Fields/Properties

        bool IsGrabbed = false;

        public bool UsesRightMouseButton => false;

        Polygon mainPolygon;

        ResizeHandles ResizeHandles;
        PolygonPointHandles PolygonPointHandles;

        public float ExtraPaddingInPixels { get; set; } = 2;

        float scaleX;
        public float ScaleX
        {
            get => scaleX;
            set
            {
                scaleX = value;
                UpdatePolygonPoints();
            }
        }

        float scaleY;
        public float ScaleY
        {
            get => scaleY;
            set
            {
                scaleY = value;
                UpdatePolygonPoints();
            }
        }

        private void UpdatePolygonPoints()
        {
            mainPolygon.SetPoint(0, -scaleX, scaleY);
            mainPolygon.SetPoint(1, scaleX, scaleY);
            mainPolygon.SetPoint(2, scaleX, -scaleY);
            mainPolygon.SetPoint(3, -scaleX, -scaleY);
            mainPolygon.SetPoint(4, -scaleX, scaleY);
        }

        public Vector3 Position
        {
            get => mainPolygon.Position;
            set => mainPolygon.Position = value;
        }

        public bool Visible
        {
            get => mainPolygon.Visible;
            set
            {
                if (value != mainPolygon.Visible)
                {
                    mainPolygon.Visible = value;
                }
            }
        }

        public double FadingSeed { get; set; } = 0;
        public Color BrightColor
        {
            get; set;
        } = Color.White;
        bool IsFadingInAndOut { get; set; } = true;

        string name;
        public string Name
        {
            get => name;
            set
            {
                name = value;
                mainPolygon.Name = $"{name}Rectangle";
            }
        }

        public bool CanMoveItem { get; set; }

        Microsoft.Xna.Framework.Point ScreenPointPushed;
        Vector3 unsnappedItemPosition;
        Vector2 unsnappedItemSize;


        public float PositionSnappingSize = 8;
        float polygonPointSnapSize;
        public float PolygonPointSnapSize
        {
            get => polygonPointSnapSize;

            set
            {
                polygonPointSnapSize = value;
                RefreshPolygonPointSnapSize();
            }
        }

        private void RefreshPolygonPointSnapSize()
        {
            PolygonPointHandles.PointSnapSize =
                IsSnappingEnabled
                    ? polygonPointSnapSize
                    : (float?)null;
        }

        // Size snapping has to be 2x as big as position snapping, otherwise resizing through a handle can result in half-snap positions which is confusing
        public float SizeSnappingSize => PositionSnappingSize * 2;

        bool isSnappingEnabled = true;
        public bool IsSnappingEnabled
        {
            get => isSnappingEnabled;
            set
            {
                isSnappingEnabled = value;
                RefreshPolygonPointSnapSize();
            }
        }



        public Vector3 LastUpdateMovement { get; private set; }

        Vector3 GrabbedPosition;
        Vector2 GrabbedWidthAndHeight;
        float GrabbedRadius;
        float GrabbedTextureScale;

        PositionedObject ownerAsPositionedObject;
        IStaticPositionable ownerAsPositionable;
        IStaticPositionable EffectiveOwner => Owner is NameableWrapper nameableWrapper
            ? nameableWrapper.ContainedObject as IStaticPositionable
            : Owner as IStaticPositionable;

        INameable ownerAsNameable;
        public INameable Owner
        {
            get => ownerAsNameable;
            set
            {
                ownerAsPositionedObject = value as PositionedObject;
                ownerAsPositionable = value as IStaticPositionable;
                ownerAsNameable = value;
            }
        }

        #endregion

        // owner, variable name, variable value
        public event Action<INameable, string, object> PropertyChanged;

        #region Constructor/Init

        public SelectionMarker(INameable owner)
        {
            this.Owner = owner;
            mainPolygon = Polygon.CreateRectangle(scaleX, scaleY);
            mainPolygon.Name = "Main Polygon for SelectionMarker";

            mainPolygon.Visible = false;
            // Due to a bug in FRB, the polygon will be added to the automatically
            // updated list. We don't want this! This is fixed in the version that supports
            // PersistenPolygons so let's put that if:
#if ScreenManagerHasPersistentPolygons
            ShapeManager.AddToLayer(mainPolygon, SpriteManager.TopLayer, makeAutomaticallyUpdated: false);
#endif

            ResizeHandles = new ResizeHandles();
            PolygonPointHandles = new PolygonPointHandles();
        }

        public void MakePersistent()
        {
#if SupportsEditMode

#if ScreenManagerHasPersistentPolygons
            FlatRedBall.Screens.ScreenManager.PersistentPolygons.Add(mainPolygon);
#endif
            ResizeHandles.MakePersistent();
#endif
        }

        #endregion

        #region Updates

        public void PlayBumpAnimation(float endingExtraPaddingBeforeZoom, bool isSynchronized)
        {
            var endingExtraPadding = endingExtraPaddingBeforeZoom;
            TweenerManager.Self.StopAllTweenersOwnedBy(mainPolygon);

            IsFadingInAndOut = false;
            ExtraPaddingInPixels = 0;
            const float growTime = 0.25f;
            float extraPaddingFromBump = 10;

            var tweener = mainPolygon.Tween((newValue) => this.ExtraPaddingInPixels = newValue, this.ExtraPaddingInPixels, extraPaddingFromBump, growTime,
                FlatRedBall.Glue.StateInterpolation.InterpolationType.Quadratic,
                FlatRedBall.Glue.StateInterpolation.Easing.Out);

            tweener.Ended += () =>
            {
                var shrinkTime = growTime;
                var tweener2 = mainPolygon.Tween((newValue) => this.ExtraPaddingInPixels = newValue, this.ExtraPaddingInPixels, endingExtraPadding, shrinkTime,
                    FlatRedBall.Glue.StateInterpolation.InterpolationType.Quadratic,
                    FlatRedBall.Glue.StateInterpolation.Easing.InOut);

                tweener2.Ended += () =>
                {
                    IsFadingInAndOut = true;
                    if (!isSynchronized)
                    {
                        FadingSeed = TimeManager.CurrentTime;
                    }

                };
            };
        }

        public void Update(bool didGameBecomeActive)
        {
            LastUpdateMovement = Vector3.Zero;

            Visible = ownerAsPositionable != null;

            DoUpdatePushedLogic();

            UpdateColor();

            ApplyPrimaryDownDragEditing(EffectiveOwner);

            UpdateMainPolygonToItem(EffectiveOwner);

            if (ownerAsPositionedObject != null)
            {
                ResizeHandles.EveryFrameUpdate(ownerAsPositionedObject, this, didGameBecomeActive);
            }
            PolygonPointHandles.EveryFrameUpdate(ownerAsPositionedObject, this);


            mainPolygon.ForceUpdateDependencies();



            DoUpdateReleasedLogic();
        }

        private void DoUpdateReleasedLogic()
        {
            if (!InputManager.Mouse.ButtonReleased(Mouse.MouseButtons.LeftButton) || ownerAsPositionedObject == null)
            {
                return;
            }

            if (IsGrabbed)
            {
                if (ownerAsPositionable.X != GrabbedPosition.X)
                {
                    var value = ownerAsPositionedObject?.Parent == null
                        ? ownerAsPositionable.X
                        : ownerAsPositionedObject.RelativeX;
                    PropertyChanged?.Invoke(Owner, nameof(ownerAsPositionable.X), value);
                }
                if (ownerAsPositionable.Y != GrabbedPosition.Y)
                {
                    var value = ownerAsPositionedObject?.Parent == null
                        ? ownerAsPositionable.Y
                        : ownerAsPositionedObject.RelativeY;
                    PropertyChanged?.Invoke(Owner, nameof(ownerAsPositionable.Y), value);
                }

                if (Owner is FlatRedBall.Math.Geometry.IScalable asScalable)
                {
                    var didChangeWidth = GrabbedWidthAndHeight.X != asScalable.ScaleX * 2;
                    var didChangeHeight = GrabbedWidthAndHeight.Y != asScalable.ScaleY * 2;
                    if (Owner is Sprite asSprite && asSprite.TextureScale > 0 &&
                        GrabbedTextureScale != asSprite.TextureScale)
                    {
                        PropertyChanged?.Invoke(Owner, nameof(asSprite.TextureScale), asSprite.TextureScale);
                    }
                    else
                    {
                        if (didChangeWidth)
                        {
                            PropertyChanged?.Invoke(Owner, "Width", asScalable.ScaleX * 2);
                        }
                        if (didChangeHeight)
                        {
                            PropertyChanged?.Invoke(Owner, "Height", asScalable.ScaleY * 2);
                        }
                    }
                }
                else if (Owner is FlatRedBall.Math.Geometry.Circle circle)
                {
                    if (GrabbedRadius != circle.Radius)
                    {
                        PropertyChanged?.Invoke(Owner, nameof(circle.Radius), circle.Radius);
                    }
                }
            }

            IsGrabbed = false;

        }

        private void DoUpdatePushedLogic()
        {
            var mouse = InputManager.Mouse;

            ///////////Early Out////////////////
            if (!InputManager.Mouse.ButtonPushed(Mouse.MouseButtons.LeftButton))
            {
                return;
            }
            /////////End Early Out///////////////


            IsGrabbed = ownerAsPositionable != null;

            ScreenPointPushed = new Microsoft.Xna.Framework.Point(mouse.X, mouse.Y);
            if (ownerAsPositionable != null)
            {
                if (ownerAsPositionedObject?.Parent == null)
                {
                    unsnappedItemPosition = new Vector3(ownerAsPositionable.X, ownerAsPositionable.Y, ownerAsPositionable.Z);
                }
                else
                {
                    unsnappedItemPosition = ownerAsPositionedObject.RelativePosition;
                }

                if (ownerAsPositionable is IScalable scalable)
                {
                    unsnappedItemSize = new Vector2(scalable.ScaleX * 2, scalable.ScaleY * 2);
                }

                GrabbedPosition = new Vector3(ownerAsPositionable.X, ownerAsPositionable.Y, ownerAsPositionable.Z);

                if (Owner is FlatRedBall.Math.Geometry.IScalable itemGrabbedAsScalable)
                {
                    GrabbedWidthAndHeight = new Vector2(itemGrabbedAsScalable.ScaleX * 2, itemGrabbedAsScalable.ScaleY * 2);
                    if (Owner is Sprite asSprite)
                    {
                        GrabbedTextureScale = asSprite.TextureScale;
                    }
                }
                else if (Owner is FlatRedBall.Math.Geometry.Circle circle)
                {
                    GrabbedRadius = circle.Radius;
                }
            }

        }

        private void UpdateColor()
        {
            float value = 1;
            if (IsFadingInAndOut)
            {
                value = (float)(1 + System.Math.Sin((TimeManager.CurrentTime - FadingSeed) * 5)) / 2;
            }

            mainPolygon.Color = new Color(
                value * BrightColor.R / 255.0f,
                value * BrightColor.G / 255.0f,
                value * BrightColor.B / 255.0f);
        }

        private void UpdateMainPolygonToItem(IStaticPositionable item)
        {
            if (item != null)
            {
                float minX, minY, maxX, maxY;
                var handledByPolygon = false;
                if (item is PositionedObject asPositionedObject && asPositionedObject.RotationZ != 0)
                {
                    // it's rotated, so we want to get the acutal shape and try to match that:
                    SelectionLogic.GetShapeFor(item, out SelectionLogic.PolygonFast polygon, out Circle circle);
                    if (polygon != null && polygon.Points.Count == 5)
                    {
                        Position = polygon.Position;
                        for (int i = 0; i < 5; i++)
                        {
                            mainPolygon.SetPoint(i, polygon.Points[i]);
                        }
                        mainPolygon.RotationMatrix = polygon.RotationMatrix;
                        handledByPolygon = true;
                    }
                }

                if (!handledByPolygon)
                {
                    SelectionLogic.GetDimensionsFor(item,
                        out minX, out maxX,
                        out minY, out maxY);


                    var newPosition = new Vector3();
                    newPosition.X = (maxX + minX) / 2.0f;
                    newPosition.Y = (maxY + minY) / 2.0f;
                    newPosition.Z = item.Z;

                    Position = newPosition;

                    ScaleX = ExtraPaddingInPixels / CameraLogic.CurrentZoomRatio + (maxX - minX) / 2.0f;
                    ScaleY = ExtraPaddingInPixels / CameraLogic.CurrentZoomRatio + (maxY - minY) / 2.0f;

                    mainPolygon.RotationMatrix = Matrix.Identity;
                }
            }
        }

        #endregion

        #region Drag to move/resize


        float? lastWorldX = null;
        float? lastWorldY = null;

        private void ApplyPrimaryDownDragEditing(IStaticPositionable item)
        {
            var mouse = InputManager.Mouse;

            var itemZ = item?.Z ?? 0;

            if (mouse.ButtonPushed(Mouse.MouseButtons.LeftButton) ||
                (mouse.ButtonDown(Mouse.MouseButtons.LeftButton) && lastWorldX == null))
            {
                lastWorldX = mouse.WorldXAt(itemZ);
                lastWorldY = mouse.WorldYAt(itemZ);
            }

            var xChangeScreenSpace = mouse.WorldXAt(itemZ) - lastWorldX;
            var yChangeScreenSpace = mouse.WorldYAt(itemZ) - lastWorldY;

            var didMouseMove = xChangeScreenSpace != 0 || yChangeScreenSpace != 0;

            var handledByPolygonHandles = PolygonPointHandles.PointIndexHighlighted != null;

            var hasMovedEnough = Math.Abs(ScreenPointPushed.X - mouse.X) > 4 ||
                Math.Abs(ScreenPointPushed.Y - mouse.Y) > 4;

            if (CanMoveItem && mouse.ButtonDown(Mouse.MouseButtons.LeftButton) && didMouseMove && hasMovedEnough &&
                !handledByPolygonHandles &&
                FlatRedBallServices.Game.IsActive &&
                // Currently only PositionedObjects can be moved. If an object is
                // IStaticPositionalbe, techincally we could move it by changing its X
                // and Y values (and that has been tested), but the objet's Glue representation
                // may not have X and Y values, which would lead to confusing behavior. Ultimately
                // we need to have plugins that can map one value (such as X) to another value (such
                // as XOffset) so that changes in the game can make their way back into Glue. Until then
                // we'll only allow moving PositionedObjects.
                item is PositionedObject)
            {
                var sideGrabbed = ResizeHandles.SideGrabbed;
                if (sideGrabbed != ResizeSide.None)
                {
                    ChangeSizeBy(item as PositionedObject, sideGrabbed);
                }
                else
                {
                    var keyboard = FlatRedBall.Input.InputManager.Keyboard;

                    LastUpdateMovement = ChangePositionBy(item, xChangeScreenSpace.Value, yChangeScreenSpace.Value, keyboard.IsShiftDown);
                }
            }

            lastWorldX = mouse.WorldXAt(itemZ);
            lastWorldY = mouse.WorldYAt(itemZ);
        }


        private Vector3 ChangePositionBy(IStaticPositionable item, float xChange, float yChange, bool isShiftDown,
            // Snapping is controlled potentially by 2 things:
            // 1. If the user is dragging an object to move it, then snapping is controlled by the global scale in the editor
            // 2. If the user is resizing, then we want to disable snapping because a resize may result in the positioin being moved a "half snap". Explanation:
            //    Consider an object at X = 32, width = 16. If the user shrinks it with the handles, the size may snap to width 16, which should set the X to 8. 8
            //    is not a valid X position if snapping is considered. Therefore, when resizing handles, X snapping should be force disabled
            // 
            bool forceDisableSnapping = false)
        {
            float Snap(float value) =>
                IsSnappingEnabled && !forceDisableSnapping
                ? MathFunctions.RoundFloat(value, PositionSnappingSize)
                : value;



            unsnappedItemPosition.X += xChange;
            unsnappedItemPosition.Y += yChange;

            var positionConsideringShift = unsnappedItemPosition;

            if (isShiftDown)
            {
                var xDifference = Math.Abs(unsnappedItemPosition.X - GrabbedPosition.X);
                var yDifference = Math.Abs(unsnappedItemPosition.Y - GrabbedPosition.Y);

                if (xDifference > yDifference)
                {
                    positionConsideringShift.Y = GrabbedPosition.Y;
                }
                else
                {
                    positionConsideringShift.X = GrabbedPosition.X;
                }
            }

            Vector3 changeAfterSnapping = Vector3.Zero;

            var itemAsPositionedObject = item as PositionedObject;
            if (itemAsPositionedObject?.Parent == null)
            {
                var before = itemAsPositionedObject?.Position ?? new Vector3(item.X, item.Y, item.Z);

                var newX = Snap(positionConsideringShift.X);
                var newY = Snap(positionConsideringShift.Y);
                item.X = newX;
                item.Y = newY;
                changeAfterSnapping = (itemAsPositionedObject?.Position ?? new Vector3(newX, newY, item.Z)) - before;
            }
            else
            {
                var before = itemAsPositionedObject.RelativePosition;
                itemAsPositionedObject.RelativeX = Snap(positionConsideringShift.X);
                itemAsPositionedObject.RelativeY = Snap(positionConsideringShift.Y);
                changeAfterSnapping = itemAsPositionedObject.RelativePosition - before;
            }
            return changeAfterSnapping;
        }

        float SnapSize(float value) =>
            IsSnappingEnabled
            ? MathFunctions.RoundFloat(value, SizeSnappingSize)
            : value;

        private void ChangeSizeBy(PositionedObject item, ResizeSide sideOver)
        {
            Vector3 rotatedPositionMultiple = new Vector3();
            Vector3 unrotatedPositionMultiple = new Vector3();

            float widthMultiple = 0;
            float heightMultiple = 0;

            switch (sideOver)
            {
                case ResizeSide.TopLeft:
                    rotatedPositionMultiple = (item.RotationMatrix.Right + item.RotationMatrix.Up) / 2;
                    unrotatedPositionMultiple = (Vector3.Right + Vector3.Up) / 2;
                    widthMultiple = -1;
                    heightMultiple = 1;
                    break;
                case ResizeSide.Top:
                    rotatedPositionMultiple = item.RotationMatrix.Up / 2.0f;
                    unrotatedPositionMultiple = Vector3.Up / 2.0f;

                    widthMultiple = 0;
                    heightMultiple = 1;
                    break;
                case ResizeSide.TopRight:
                    rotatedPositionMultiple = (item.RotationMatrix.Right + item.RotationMatrix.Up) / 2;
                    unrotatedPositionMultiple = (Vector3.Right + Vector3.Up) / 2;

                    widthMultiple = 1;
                    heightMultiple = 1;
                    break;
                case ResizeSide.Right:
                    rotatedPositionMultiple = item.RotationMatrix.Right / 2.0f;
                    unrotatedPositionMultiple = Vector3.Right / 2.0f;

                    widthMultiple = 1;
                    heightMultiple = 0;
                    break;

                case ResizeSide.BottomRight:
                    rotatedPositionMultiple = (item.RotationMatrix.Right + item.RotationMatrix.Up) / 2;
                    unrotatedPositionMultiple = (Vector3.Right + Vector3.Up) / 2;

                    widthMultiple = 1;
                    heightMultiple = -1;
                    break;

                case ResizeSide.Bottom:
                    rotatedPositionMultiple = item.RotationMatrix.Up / 2.0f;
                    unrotatedPositionMultiple = Vector3.Up / 2.0f;

                    widthMultiple = 0;
                    heightMultiple = -1;
                    break;
                case ResizeSide.BottomLeft:
                    rotatedPositionMultiple = (item.RotationMatrix.Right + item.RotationMatrix.Up) / 2;
                    unrotatedPositionMultiple = (Vector3.Right + Vector3.Up) / 2;

                    widthMultiple = -1;
                    heightMultiple = -1;
                    break;
                case ResizeSide.Left:
                    rotatedPositionMultiple = item.RotationMatrix.Right / 2.0f;
                    unrotatedPositionMultiple = Vector3.Right / 2.0f;

                    widthMultiple = -1;
                    heightMultiple = 0;
                    break;
            }


            if (ResizeHandles.ShouldResizeXFromCenter)
            {
                // Should this be adjusted based on rotation?
                rotatedPositionMultiple.X = 0;
                unrotatedPositionMultiple.X = 0;

                widthMultiple *= 2;
            }
            if (ResizeHandles.ShouldResizeYFromCenter)
            {
                // Should this be adjusted based on rotation?
                rotatedPositionMultiple.Y = 0;
                unrotatedPositionMultiple.Y = 0;
                heightMultiple *= 2;
            }

            var mouse = InputManager.Mouse;
            var scalable = item as IScalable;
            var mouseChange = new Vector3(mouse.WorldXChangeAt(item.Z), mouse.WorldYChangeAt(item.Z), 0);
            mouseChange = mouseChange.RotatedBy(-item.RotationZ);

            float xChangeForPosition = rotatedPositionMultiple.X * mouseChange.X;
            float yChangeForPosition = rotatedPositionMultiple.Y * mouseChange.Y;

            bool setsTextureScale = ResizeHandles.GetIfSetsTextureScale(item);

            if (setsTextureScale)
            {
                var asSprite = scalable as Sprite;
                var currentScaleX = asSprite.ScaleX;
                var currentScaleY = asSprite.ScaleY;

                if (mouseChange.X != 0 && asSprite.ScaleX != 0 && widthMultiple != 0)
                {
                    var newRatio = (currentScaleX + 0.5f * mouseChange.X * widthMultiple) / currentScaleX;

                    asSprite.TextureScale *= newRatio;
                }
                else if (mouseChange.Y != 0 && asSprite.ScaleY != 0 && heightMultiple != 0)
                {
                    var newRatio = (currentScaleY + 0.5f * mouseChange.Y * heightMultiple) / currentScaleY;

                    asSprite.TextureScale *= newRatio;
                }
            }
            else if (item is Circle asCircle)
            {
                float? newRadius = null;
                if (mouseChange.X != 0 && widthMultiple != 0)
                {
                    newRadius = asCircle.Radius + mouseChange.X * widthMultiple / 2.0f;
                }
                else if (mouseChange.Y != 0 && heightMultiple != 0)
                {
                    newRadius = asCircle.Radius + mouseChange.Y * heightMultiple / 2.0f;
                }
                if (newRadius != null)
                {
                    newRadius = Math.Max(0, newRadius.Value);

                    // Vic says - I want to enable snapping here at some point, but currently if it's 
                    // enabled, the snapping on size conflicts with the snapping on position, resulting
                    // in the circle shifting positions around when the handles are grabbed. Instead, the
                    // sizing should be implemented at the selection marker level, and only then should the positions
                    // be translated down to the object. Until then, snapping is turned off for circle resizing.
                    //newRadius = MathFunctions.RoundFloat(newRadius.Value, sizeSnappingSize);
                    asCircle.Radius = newRadius.Value;
                }
            }
            else
            {

                float scaleXChange = 0;
                float scaleYChange = 0;
                if (mouseChange.X != 0 && widthMultiple != 0)
                {
                    //var newScaleX = scalable.ScaleX + cursorXChange * widthMultiple / 2.0f;
                    //newScaleX = Math.Max(0, newScaleX);
                    //scalable.ScaleX = newScaleX;
                    // Vic says - this needs more work. Didn't work like this and I don't want to dive in yet
                    unsnappedItemSize.X = unsnappedItemSize.X + mouseChange.X * widthMultiple;
                    unsnappedItemSize.X = Math.Max(0, unsnappedItemSize.X);
                    //unsnappedItemSize.X = MathFunctions.RoundFloat(unsnappedItemSize.X, sizeSnappingSize);
                    var newScaleX = SnapSize(unsnappedItemSize.X) / 2.0f;
                    scaleXChange = newScaleX - scalable.ScaleX;

                    if (scaleXChange != 0)
                    {
                        var scaleBefore = scalable.ScaleX;
                        scalable.ScaleX = newScaleX;
                        // Normally the object that is being resized will accept this scale. However, if it's a custom game object, it may
                        // have its own internal snapping. Therefore, we should figure out the change by looking at the ScaleX again:
                        scaleXChange = scalable.ScaleX - scaleBefore;
                    }
                }


                if (mouseChange.Y != 0 && heightMultiple != 0)
                {
                    //var newScaleY = scalable.ScaleY + cursorYChange * heightMultiple / 2.0f;
                    //newScaleY = Math.Max(0, newScaleY);
                    //scalable.ScaleY = newScaleY;
                    unsnappedItemSize.Y = unsnappedItemSize.Y + mouseChange.Y * heightMultiple;
                    unsnappedItemSize.Y = Math.Max(0, unsnappedItemSize.Y);

                    var newScaleY = SnapSize(unsnappedItemSize.Y) / 2.0f;
                    scaleYChange = newScaleY - scalable.ScaleY;

                    if (scaleYChange != 0)
                    {
                        var scaleBefore = scalable.ScaleY;
                        scalable.ScaleY = newScaleY;
                        // see the scaleXAssignment above for info on why we use the object:
                        scaleYChange = scalable.ScaleY - scaleBefore;
                    }
                }

                var scaleChange = new Vector3(scaleXChange, scaleYChange, 0);
                var widthHeightMultiple = new Vector3(widthMultiple, heightMultiple, 0);

                var xComponent = (scaleChange.X * 2 * widthHeightMultiple.X * unrotatedPositionMultiple.X) * item.RotationMatrix.Right;
                var yComponent = (scaleChange.Y * 2 * widthHeightMultiple.Y * unrotatedPositionMultiple.Y) * item.RotationMatrix.Up;

                xChangeForPosition = (xComponent + yComponent).X;
                yChangeForPosition = (xComponent + yComponent).Y;
            }
            ChangePositionBy(item, xChangeForPosition, yChangeForPosition, FlatRedBall.Input.InputManager.Keyboard.IsShiftDown, forceDisableSnapping: true);
            item.ForceUpdateDependencies();
        }


        #endregion

        public bool IsMouseOverThis()
        {
            var mouse = InputManager.Mouse;
            if (IsOn3D(mainPolygon))
            {
                return true;
            }

            if (ResizeHandles.GetSideOver() != ResizeSide.None)
            {
                return true;
            }
            if (PolygonPointHandles.PointIndexHighlighted != null)
            {
                return true;
            }
            return false;
        }

        public bool IsOn3D(Polygon polygon)
        {
            Ray ray = InputManager.Mouse.GetMouseRay(FlatRedBall.Camera.Main);
            Matrix inverseRotation = polygon.RotationMatrix;

            Matrix.Invert(ref inverseRotation, out inverseRotation);

            Plane plane = new Plane(polygon.Position, polygon.Position + polygon.RotationMatrix.Up,
                polygon.Position + polygon.RotationMatrix.Right);

            float? result = ray.Intersects(plane);

            if (!result.HasValue)
            {
                return false;
            }

            Vector3 intersection = ray.Position + ray.Direction * result.Value;


            return polygon.IsPointInside(ref intersection);
        }

        public bool ShouldSuppress(string variableName) =>
            variableName == "X" ||
            variableName == "Y" ||
            variableName == "Z" ||
            variableName == "RelativeX" ||
            variableName == "RelativeY" ||
            variableName == "RelativeZ" ||

            variableName == "Width" ||
            variableName == "Height" ||

            variableName == "Radius"
            ;

        public bool HandleDelete()
        {
            if (ownerAsPositionable is Polygon asPolygon)
            {
                return PolygonPointHandles.HandleDelete(asPolygon);
            }
            else
            {
                return false;
            }
        }

        public void Destroy()
        {
#if SupportsEditMode

            mainPolygon.Visible = false;
#if ScreenManagerHasPersistentPolygons

            FlatRedBall.Screens.ScreenManager.PersistentPolygons.Remove(mainPolygon);
#endif
            ResizeHandles.Destroy();
            PolygonPointHandles.Destroy();

#endif
        }
    }

}
