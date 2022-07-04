{CompilerDirectives}

using FlatRedBall;
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

        INameable Owner { get; }

        void MakePersistent();
        void PlayBumpAnimation(float endingExtraPaddingBeforeZoom, bool isSynchronized);
        void Update();
        bool ShouldSuppress(string memberName);

        bool IsCursorOverThis();
        void Destroy();
    }

    #endregion

    #region Handles Class
    public class Handles
    {
        AxisAlignedRectangle[] rectangles = new AxisAlignedRectangle[8];
        const int DefaultHandleDimension = 10;

        // 14 wasn't noticeable for opposite handles when resizing from center
        //const int HighlightedHandleDimension = 14;
        const int HighlightedHandleDimension = 16;

        public bool Visible { get; set; }

        public ResizeMode ResizeMode { get; set; }

        public Handles()
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

        internal void UpdateDimension(HashSet<ResizeSide> sidesToHighlight)
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

        public ResizeSide GetSideOver()
        {
            var cursor = FlatRedBall.Gui.GuiManager.Cursor;

            for (int i = 0; i < this.rectangles.Length; i++)
            {
                if (rectangles[i].Visible && cursor.IsOn3D(rectangles[i]))
                {
                    return (ResizeSide)i;
                }
            }

            return ResizeSide.None;
        }

        internal void Destroy()
        {
            for (int i = 0; i < rectangles.Length; i++)
            {
                FlatRedBall.Screens.ScreenManager.PersistentAxisAlignedRectangles.Remove(rectangles[i]);
                rectangles[i].Visible = false;
            }
        }
    }

    #endregion

    public class SelectionMarker : ISelectionMarker, IReadOnlyScalable
    {
        #region Fields/Properties

        Polygon mainPolygon;

        Handles Handles;

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

        Vector3 Position
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

        bool shouldResizeXFromCenter;
        bool shouldResizeYFromCenter;

        public float PositionSnappingSize = 8;
        // Size snapping has to be 2x as big as position snapping, otherwise resizing through a handle can result in half-snap positions which is confusing
        public float SizeSnappingSize => PositionSnappingSize * 2;
        public bool IsSnappingEnabled = true;

        ResizeSide SideGrabbed = ResizeSide.None;


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

            Handles = new Handles();

        }

        public void MakePersistent()
        {
#if SupportsEditMode

#if ScreenManagerHasPersistentPolygons
            FlatRedBall.Screens.ScreenManager.PersistentPolygons.Add(mainPolygon);
#endif
            Handles.MakePersistent();
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

        public void Update()
        {
            LastUpdateMovement = Vector3.Zero;

            Visible = ownerAsPositionable != null;

            DoUpdatePushedLogic();

            UpdateColor();

            ApplyPrimaryDownDragEditing(EffectiveOwner, SideGrabbed);

            UpdateMainPolygonToItem(EffectiveOwner);

            if (ownerAsPositionedObject != null)
            {
                UpdateHandles(ownerAsPositionedObject, SideGrabbed);
            }


            mainPolygon.ForceUpdateDependencies();



            DoUpdateReleasedLogic();
        }

        private void DoUpdateReleasedLogic()
        {
            var cursor = FlatRedBall.Gui.GuiManager.Cursor;
            if (!cursor.PrimaryClick || ownerAsPositionedObject == null ||
                PropertyChanged == null)
            {
                return;
            }

            if (ownerAsPositionable.X != GrabbedPosition.X)
            {
                var value = ownerAsPositionedObject?.Parent == null
                    ? ownerAsPositionable.X
                    : ownerAsPositionedObject.RelativeX;
                PropertyChanged(Owner, nameof(ownerAsPositionable.X), value);
            }
            if (ownerAsPositionable.Y != GrabbedPosition.Y)
            {
                var value = ownerAsPositionedObject?.Parent == null
                    ? ownerAsPositionable.Y
                    : ownerAsPositionedObject.RelativeY;
                PropertyChanged(Owner, nameof(ownerAsPositionable.Y), value);
            }

            if (Owner is FlatRedBall.Math.Geometry.IScalable asScalable)
            {
                var didChangeWidth = GrabbedWidthAndHeight.X != asScalable.ScaleX * 2;
                var didChangeHeight = GrabbedWidthAndHeight.Y != asScalable.ScaleY * 2;
                if (Owner is Sprite asSprite && asSprite.TextureScale > 0 &&
                    GrabbedTextureScale != asSprite.TextureScale)
                {
                    PropertyChanged(Owner, nameof(asSprite.TextureScale), asSprite.TextureScale);
                }
                else
                {
                    if (didChangeWidth)
                    {
                        PropertyChanged(Owner, "Width", asScalable.ScaleX * 2);
                    }
                    if (didChangeHeight)
                    {
                        PropertyChanged(Owner, "Height", asScalable.ScaleY * 2);
                    }
                }
            }
            else if (Owner is FlatRedBall.Math.Geometry.Circle circle)
            {
                if (GrabbedRadius != circle.Radius)
                {
                    PropertyChanged(Owner, nameof(circle.Radius), circle.Radius);
                }
            }

            SideGrabbed = ResizeSide.None;
        }

        private void DoUpdatePushedLogic()
        {
            var cursor = FlatRedBall.Gui.GuiManager.Cursor;

            ///////////Early Out////////////////
            if (!cursor.PrimaryPush)
            {
                return;
            }
            /////////End Early Out///////////////


            shouldResizeXFromCenter = false;
            shouldResizeYFromCenter = false;

            ScreenPointPushed = new Microsoft.Xna.Framework.Point(cursor.ScreenX, cursor.ScreenY);
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

                    bool shouldAttemptResizeFromCenter = GetIfShouldResizeFromCenter(ownerAsPositionedObject);
                    // If we're resizing a rectangle on an object, we may not want to move on resize, so let's change the position
                    // values to 0 and double the dimension values
                    if (shouldAttemptResizeFromCenter)
                    {
                        if (ownerAsPositionedObject.RelativeX == 0)
                        {
                            shouldResizeXFromCenter = true;
                        }
                        if (ownerAsPositionedObject.RelativeY == 0)
                        {
                            shouldResizeYFromCenter = true;
                        }
                    }
                }

                SideGrabbed = GetSideOver();


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

            else
            {
                SideGrabbed = ResizeSide.None;
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
                    SelectionLogic.GetShapeFor(item, out Polygon polygon, out Circle circle);
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

        static HashSet<ResizeSide> sidesToHighlight = new HashSet<ResizeSide>();
        private void UpdateHandles(PositionedObject item, ResizeSide sideGrabbed)
        {
            Handles.Visible = Visible;

            Handles.UpdateVisibilityConsideringResizeMode();

            if (Visible)
            {
                // Update the "should resize" values if the cursor isn't down so that
                // highlights reflect whether opposite sides will be resized. 
                // Do not do this if the cursor is down because we want to take a snapshot
                // of these values when the cursor is pressed and continue to use those values
                // during the duration of the drag.
                if (!FlatRedBall.Gui.GuiManager.Cursor.PrimaryDown)
                {
                    bool shouldAttemptResizeFromCenter = GetIfShouldResizeFromCenter(ownerAsPositionedObject);
                    // If we're resizing a rectangle on an object, we may not want to move on resize, so let's change the position
                    // values to 0 and double the dimension values
                    if (shouldAttemptResizeFromCenter)
                    {
                        if (ownerAsPositionedObject.RelativeX == 0)
                        {
                            shouldResizeXFromCenter = true;
                        }
                        if (ownerAsPositionedObject.RelativeY == 0)
                        {
                            shouldResizeYFromCenter = true;
                        }
                    }

                }

                ResizeSide sideOver = sideGrabbed;
                if (sideOver == ResizeSide.None)
                {
                    sideOver = GetSideOver();
                }

                FillSidesToHighlight(item, sideOver);

                // UpdateDimension before UpdateHandlePositions because
                // UpdateHandlePositions depends on the size of the handles
                // to position them correctly. If the order is inverted then
                // handles will "pop" for 1 frame. Not a huge deal but looks unprofessional.
                Handles.UpdateDimension(sidesToHighlight);

                if (this.Owner is IReadOnlyScalable scalable)
                {
                    Handles.UpdateHandlePositions(scalable, this.Position);
                }

                if (CanMoveItem)
                {
                    if (ownerAsPositionedObject is Sprite asSprite && asSprite.TextureScale > 0)
                    {
                        Handles.ResizeMode = ResizeMode.Cardinal;
                    }
                    else if (ownerAsPositionedObject is FlatRedBall.Math.Geometry.Circle)
                    {
                        Handles.ResizeMode = ResizeMode.Cardinal;
                    }
                    else if (ownerAsPositionedObject is FlatRedBall.Math.Geometry.IScalable)
                    {
                        Handles.ResizeMode = ResizeMode.EightWay;
                    }
                    else
                    {
                        Handles.ResizeMode = ResizeMode.None;
                    }
                }

            }
        }

        private void FillSidesToHighlight(PositionedObject item, ResizeSide sideGrabbed)
        {
            sidesToHighlight.Clear();

            sidesToHighlight.Add(sideGrabbed);

            if (GetIfShouldResizeFromCenter(item))
            {
                if (sideGrabbed == ResizeSide.Left && shouldResizeXFromCenter) sidesToHighlight.Add(ResizeSide.Right);
                if (sideGrabbed == ResizeSide.Top && shouldResizeYFromCenter) sidesToHighlight.Add(ResizeSide.Bottom);
                if (sideGrabbed == ResizeSide.Right && shouldResizeXFromCenter) sidesToHighlight.Add(ResizeSide.Left);
                if (sideGrabbed == ResizeSide.Bottom && shouldResizeYFromCenter) sidesToHighlight.Add(ResizeSide.Top);

                // if we grab a diagonal, all can be resized:
                if (sideGrabbed == ResizeSide.TopLeft ||
                    sideGrabbed == ResizeSide.TopRight ||
                    sideGrabbed == ResizeSide.BottomRight ||
                    sideGrabbed == ResizeSide.BottomLeft)
                {
                    if (shouldResizeXFromCenter) sidesToHighlight.Add(ResizeSide.Right);
                    if (shouldResizeYFromCenter) sidesToHighlight.Add(ResizeSide.Bottom);
                    if (shouldResizeXFromCenter) sidesToHighlight.Add(ResizeSide.Left);
                    if (shouldResizeYFromCenter) sidesToHighlight.Add(ResizeSide.Top);
                }
            }

            if (item is Circle || GetIfSetsTextureScale(item))
            {
                if (sideGrabbed == ResizeSide.Left)
                {
                    sidesToHighlight.Add(ResizeSide.Top);
                    sidesToHighlight.Add(ResizeSide.Bottom);
                }
                else if (sideGrabbed == ResizeSide.Top)
                {
                    sidesToHighlight.Add(ResizeSide.Left);
                    sidesToHighlight.Add(ResizeSide.Right);
                }
                else if (sideGrabbed == ResizeSide.Right)
                {
                    sidesToHighlight.Add(ResizeSide.Top);
                    sidesToHighlight.Add(ResizeSide.Bottom);
                }
                else if (sideGrabbed == ResizeSide.Bottom)
                {
                    sidesToHighlight.Add(ResizeSide.Left);
                    sidesToHighlight.Add(ResizeSide.Right);
                }
            }
        }

        #endregion

        #region Drag to move/resize

        float lastWorldX = 0;
        float lastWorldY = 0;

        private void ApplyPrimaryDownDragEditing(IStaticPositionable item, ResizeSide sideGrabbed)
        {
            var cursor = FlatRedBall.Gui.GuiManager.Cursor;

            var itemZ = item?.Z ?? 0;

            if (cursor.PrimaryPush)
            {
                lastWorldX = cursor.WorldXAt(itemZ);
                lastWorldY = cursor.WorldYAt(itemZ);
            }

            var xChangeScreenSpace = cursor.WorldXAt(itemZ) - lastWorldX;
            var yChangeScreenSpace = cursor.WorldYAt(itemZ) - lastWorldY;

            var didCursorMove = xChangeScreenSpace != 0 || yChangeScreenSpace != 0;

            var hasMovedEnough = Math.Abs(ScreenPointPushed.X - cursor.ScreenX) > 4 ||
                Math.Abs(ScreenPointPushed.Y - cursor.ScreenY) > 4;

            if (CanMoveItem && cursor.PrimaryDown && didCursorMove && hasMovedEnough &&
                // Currently only PositionedObjects can be moved. If an object is
                // IStaticPositionalbe, techincally we could move it by changing its X
                // and Y values (and that has been tested), but the objet's Glue representation
                // may not have X and Y values, which would lead to confusing behavior. Ultimately
                // we need to have plugins that can map one value (such as X) to another value (such
                // as XOffset) so that changes in the game can make their way back into Glue. Until then
                // we'll only allow moving PositionedObjects.
                item is PositionedObject)
            {
                if (sideGrabbed == ResizeSide.None)
                {
                    var keyboard = FlatRedBall.Input.InputManager.Keyboard;

                    LastUpdateMovement = ChangePositionBy(item, xChangeScreenSpace, yChangeScreenSpace, keyboard.IsShiftDown);
                }
                else
                {
                    ChangeSizeBy(item as PositionedObject, sideGrabbed);
                }
            }

            lastWorldX = cursor.WorldXAt(itemZ);
            lastWorldY = cursor.WorldYAt(itemZ);
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

            var cursor = FlatRedBall.Gui.GuiManager.Cursor;
            var scalable = item as IScalable;

            if (shouldResizeXFromCenter)
            {
                // Should this be adjusted based on rotation?
                rotatedPositionMultiple.X = 0;
                unrotatedPositionMultiple.X = 0;

                widthMultiple *= 2;
            }
            if (shouldResizeYFromCenter)
            {
                // Should this be adjusted based on rotation?
                rotatedPositionMultiple.Y = 0;
                unrotatedPositionMultiple.Y = 0;
                heightMultiple *= 2;
            }

            var cursorChange = new Vector3(cursor.WorldXChangeAt(item.Z), cursor.WorldYChangeAt(item.Z), 0);
            cursorChange = cursorChange.RotatedBy(-item.RotationZ);

            float xChangeForPosition = rotatedPositionMultiple.X * cursorChange.X;
            float yChangeForPosition = rotatedPositionMultiple.Y * cursorChange.Y;

            bool setsTextureScale = GetIfSetsTextureScale(item);

            if (setsTextureScale)
            {
                var asSprite = scalable as Sprite;
                var currentScaleX = asSprite.ScaleX;
                var currentScaleY = asSprite.ScaleY;

                if (cursorChange.X != 0 && asSprite.ScaleX != 0 && widthMultiple != 0)
                {
                    var newRatio = (currentScaleX + 0.5f * cursorChange.X * widthMultiple) / currentScaleX;

                    asSprite.TextureScale *= newRatio;
                }
                else if (cursorChange.Y != 0 && asSprite.ScaleY != 0 && heightMultiple != 0)
                {
                    var newRatio = (currentScaleY + 0.5f * cursorChange.Y * heightMultiple) / currentScaleY;

                    asSprite.TextureScale *= newRatio;
                }
            }
            else if (item is Circle asCircle)
            {
                float? newRadius = null;
                if (cursorChange.X != 0 && widthMultiple != 0)
                {
                    newRadius = asCircle.Radius + cursorChange.X * widthMultiple / 2.0f;
                }
                else if (cursorChange.Y != 0 && heightMultiple != 0)
                {
                    newRadius = asCircle.Radius + cursorChange.Y * heightMultiple / 2.0f;
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
                if (cursorChange.X != 0 && widthMultiple != 0)
                {
                    //var newScaleX = scalable.ScaleX + cursorXChange * widthMultiple / 2.0f;
                    //newScaleX = Math.Max(0, newScaleX);
                    //scalable.ScaleX = newScaleX;
                    // Vic says - this needs more work. Didn't work like this and I don't want to dive in yet
                    unsnappedItemSize.X = unsnappedItemSize.X + cursorChange.X * widthMultiple;
                    unsnappedItemSize.X = Math.Max(0, unsnappedItemSize.X);
                    //unsnappedItemSize.X = MathFunctions.RoundFloat(unsnappedItemSize.X, sizeSnappingSize);
                    var newScaleX = SnapSize(unsnappedItemSize.X) / 2.0f;
                    scaleXChange = newScaleX - scalable.ScaleX;

                    if (scaleXChange != 0)
                    {
                        scalable.ScaleX = newScaleX;
                    }
                }


                if (cursorChange.Y != 0 && heightMultiple != 0)
                {
                    //var newScaleY = scalable.ScaleY + cursorYChange * heightMultiple / 2.0f;
                    //newScaleY = Math.Max(0, newScaleY);
                    //scalable.ScaleY = newScaleY;
                    unsnappedItemSize.Y = unsnappedItemSize.Y + cursorChange.Y * heightMultiple;
                    unsnappedItemSize.Y = Math.Max(0, unsnappedItemSize.Y);

                    var newScaleY = SnapSize(unsnappedItemSize.Y) / 2.0f;
                    scaleYChange = newScaleY - scalable.ScaleY;

                    if (scaleYChange != 0)
                    {
                        scalable.ScaleY = newScaleY;
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

        private static bool GetIfSetsTextureScale(PositionedObject item)
        {
            return item is Sprite asSprite && asSprite.TextureScale > 0 && asSprite.Texture != null;
        }

        private static bool GetIfShouldResizeFromCenter(PositionedObject item)
        {
            return item.Parent != null;
        }

        #endregion

        public bool IsCursorOverThis()
        {
            var cursor = FlatRedBall.Gui.GuiManager.Cursor;
            if (cursor.IsOn3D(mainPolygon))
            {
                return true;
            }

            if (GetSideOver() != ResizeSide.None)
            {
                return true;
            }

            return false;
        }

        public ResizeSide GetSideOver() => Handles.GetSideOver();

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

        public void Destroy()
        {
#if SupportsEditMode

            mainPolygon.Visible = false;
#if ScreenManagerHasPersistentPolygons

            FlatRedBall.Screens.ScreenManager.PersistentPolygons.Remove(mainPolygon);
#endif
            Handles.Destroy();
#endif
        }
    }

}
