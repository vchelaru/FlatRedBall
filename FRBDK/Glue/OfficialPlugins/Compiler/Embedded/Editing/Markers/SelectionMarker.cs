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
        void Update(ResizeSide sideGrabbed);
        bool ShouldSuppress(string memberName);

        bool IsCursorOverThis();
        void HandleCursorRelease();
        void HandleCursorPushed();
        void Destroy();
    }

    #endregion

    #region Handles Class
    public class Handles
    {
        AxisAlignedRectangle[] rectangles = new AxisAlignedRectangle[8];
        const int DefaultHandleDimension = 10;
        const int HighlightedHandleDimension = 14;


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

        internal void UpdateVisibilityConsideringResizeMode(bool isVisible)
        {
            if (ResizeMode == ResizeMode.EightWay)
            {
                foreach (var handle in rectangles)
                {
                    handle.Visible = isVisible;
                }
            }
            else if (ResizeMode == ResizeMode.Cardinal)
            {
                for (int i = 0; i < rectangles.Length; i++)
                {
                    var handle = rectangles[i];
                    // every other one, starting with index 1
                    handle.Visible = isVisible && (i % 2) == 1;
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

            handle = rectangles[1];
            handle.X = 0;
            handle.Y = owner.ScaleY + handle.Height / 2;

            handle = rectangles[2];
            handle.X = owner.ScaleX + handle.Width / 2;
            handle.Y = owner.ScaleY + handle.Height / 2;

            handle = rectangles[3];
            handle.X = owner.ScaleX + handle.Width / 2;
            handle.Y = 0;

            handle = rectangles[4];
            handle.X = +owner.ScaleX + handle.Width / 2;
            handle.Y = -owner.ScaleY - handle.Height / 2;

            handle = rectangles[5];
            handle.X = 0;
            handle.Y = -owner.ScaleY - handle.Height / 2;

            handle = rectangles[6];
            handle.X = -owner.ScaleX - handle.Width / 2;
            handle.Y = -owner.ScaleY - handle.Height / 2;

            handle = rectangles[7];
            handle.X = -owner.ScaleX - handle.Width / 2;
            handle.Y = 0;

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

        public float PositionSnappingSize = 8;
        public float SizeSnappingSize = 8;
        public bool IsSnappingEnabled = true;

        public Vector3 LastUpdateMovement { get; private set; }

        Vector3 GrabbedPosition;
        Vector2 GrabbedWidthAndHeight;
        float GrabbedRadius;
        float GrabbedTextureScale;

        PositionedObject ownerAsPositionedObject;
        IStaticPositionable ownerAsPositionable;
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
            ShapeManager.AddToLayer(mainPolygon, SpriteManager.TopLayer, makeAutomaticallyUpdated: false);

            Handles = new Handles();

        }

        public void MakePersistent()
        {
#if SupportsEditMode

            FlatRedBall.Screens.ScreenManager.PersistentPolygons.Add(mainPolygon);

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

        public void Update(ResizeSide sideGrabbed)
        {
            LastUpdateMovement = Vector3.Zero;

            Visible = ownerAsPositionable != null;

            UpdateScreenPointPushed();

            IStaticPositionable effectiveOwner = null;
            if (Owner is NameableWrapper nameableWrapper)
            {
                effectiveOwner = nameableWrapper.ContainedObject as IStaticPositionable;
            }
            else
            {
                effectiveOwner = Owner as IStaticPositionable;
            }

            UpdateMainPolygonToItem(effectiveOwner);

            UpdateColor();

            ApplyPrimaryDownDragEditing(effectiveOwner, sideGrabbed);

            if (ownerAsPositionedObject != null)
            {
                UpdateHandles(ownerAsPositionedObject, sideGrabbed);
            }

            mainPolygon.UpdateDependencies(TimeManager.CurrentTime);

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

        private void UpdateScreenPointPushed()
        {
            var cursor = FlatRedBall.Gui.GuiManager.Cursor;

            if (cursor.PrimaryPush)
            {
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
                    }
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
                SelectionLogic.GetDimensionsFor(item,
                    out float minX, out float maxX,
                    out float minY, out float maxY);

                var newPosition = new Vector3();
                newPosition.X = (maxX + minX) / 2.0f;
                newPosition.Y = (maxY + minY) / 2.0f;
                newPosition.Z = item.Z;

                Position = newPosition;

                ScaleX = ExtraPaddingInPixels / CameraLogic.CurrentZoomRatio + (maxX - minX) / 2.0f;
                ScaleY = ExtraPaddingInPixels / CameraLogic.CurrentZoomRatio + (maxY - minY) / 2.0f;

                if (item is IRotatable asRotatable)
                {
                    mainPolygon.RotationMatrix = asRotatable.RotationMatrix;
                }
                else
                {
                    mainPolygon.RotationMatrix = Matrix.Identity;
                }
            }
        }

        static HashSet<ResizeSide> sidesToHighlight = new HashSet<ResizeSide>();
        private void UpdateHandles(PositionedObject item, ResizeSide sideGrabbed)
        {
            Handles.UpdateVisibilityConsideringResizeMode(Visible);

            if (Visible)
            {
                ResizeSide sideOver = sideGrabbed;
                if (sideOver == ResizeSide.None)
                {
                    sideOver = GetSideOver();
                }
                Handles.UpdateHandlePositions(this, this.Position);

                FillSidesToHighlight(item, sideOver);

                Handles.UpdateDimension(sidesToHighlight);

            }
        }

        private static void FillSidesToHighlight(PositionedObject item, ResizeSide sideGrabbed)
        {
            sidesToHighlight.Clear();

            sidesToHighlight.Add(sideGrabbed);

            if (GetIfShouldResizeFromCenter(item))
            {
                if (sideGrabbed == ResizeSide.Left && item.RelativeX == 0) sidesToHighlight.Add(ResizeSide.Right);
                if (sideGrabbed == ResizeSide.Top && item.RelativeY == 0) sidesToHighlight.Add(ResizeSide.Bottom);
                if (sideGrabbed == ResizeSide.Right && item.RelativeX == 0) sidesToHighlight.Add(ResizeSide.Left);
                if (sideGrabbed == ResizeSide.Bottom && item.RelativeY == 0) sidesToHighlight.Add(ResizeSide.Top);

                // if we grab a diagonal, all can be resized:
                if (sideGrabbed == ResizeSide.TopLeft ||
                    sideGrabbed == ResizeSide.TopRight ||
                    sideGrabbed == ResizeSide.BottomRight ||
                    sideGrabbed == ResizeSide.BottomLeft)
                {
                    if (item.RelativeX == 0) sidesToHighlight.Add(ResizeSide.Right);
                    if (item.RelativeY == 0) sidesToHighlight.Add(ResizeSide.Bottom);
                    if (item.RelativeX == 0) sidesToHighlight.Add(ResizeSide.Left);
                    if (item.RelativeY == 0) sidesToHighlight.Add(ResizeSide.Top);
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

            var xChange = cursor.WorldXAt(itemZ) - lastWorldX;
            var yChange = cursor.WorldYAt(itemZ) - lastWorldY;

            var didCursorMove = xChange != 0 || yChange != 0;

            if (CanMoveItem && cursor.PrimaryDown && didCursorMove &&
                // Currently only PositionedObjects can be moved. If an object is
                // IStaticPositionalbe, techincally we could move it by changing its X
                // and Y values (and that has been tested), but the objet's Glue representation
                // may not have X and Y values, which would lead to confusing behavior. Ultimately
                // we need to have plugins that can map one value (such as X) to another value (such
                // as XOffset) so that changes in the game can make their way back into Glue. Until then
                // we'll only allow moving PositionedObjects.
                item is PositionedObject)
            {
                var hasMovedEnough = Math.Abs(ScreenPointPushed.X - cursor.ScreenX) > 4 ||
                    Math.Abs(ScreenPointPushed.Y - cursor.ScreenY) > 4;

                if (item != null && hasMovedEnough)
                {
                    if (sideGrabbed == ResizeSide.None)
                    {
                        var keyboard = FlatRedBall.Input.InputManager.Keyboard;

                        LastUpdateMovement = ChangePositionBy(item, xChange, yChange, keyboard.IsShiftDown);
                    }
                    else
                    {
                        ChangeSizeBy(item as PositionedObject, sideGrabbed, xChange, yChange);
                    }
                }
            }

            lastWorldX = cursor.WorldXAt(itemZ);
            lastWorldY = cursor.WorldYAt(itemZ);
        }


        private Vector3 ChangePositionBy(IStaticPositionable item, float xChange, float yChange, bool isShiftDown)
        {
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
            float Snap(float value) =>
                IsSnappingEnabled
                ? MathFunctions.RoundFloat(value, PositionSnappingSize)
                : value;
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

        private void ChangeSizeBy(PositionedObject item, ResizeSide sideOver, float xChange, float yChange)
        {
            float xPositionMultiple = 0;
            float yPositionMultiple = 0;
            float widthMultiple = 0;
            float heightMultiple = 0;

            switch (sideOver)
            {
                case ResizeSide.TopLeft:
                    xPositionMultiple = 1 / 2.0f;
                    widthMultiple = -1;

                    yPositionMultiple = 1 / 2.0f;
                    heightMultiple = 1;
                    break;
                case ResizeSide.Top:
                    xPositionMultiple = 0;
                    widthMultiple = 0;

                    yPositionMultiple = 1 / 2.0f;
                    heightMultiple = 1;
                    break;
                case ResizeSide.TopRight:
                    xPositionMultiple = 1 / 2.0f;
                    widthMultiple = 1;


                    yPositionMultiple = 1 / 2.0f;
                    heightMultiple = 1;

                    break;
                case ResizeSide.Right:
                    xPositionMultiple = 1 / 2.0f;
                    widthMultiple = 1;

                    yPositionMultiple = 0;
                    heightMultiple = 0;
                    break;

                case ResizeSide.BottomRight:
                    xPositionMultiple = 1 / 2.0f;
                    widthMultiple = 1;

                    yPositionMultiple = 1 / 2.0f;
                    heightMultiple = -1;

                    break;

                case ResizeSide.Bottom:
                    xPositionMultiple = 0;
                    widthMultiple = 0;

                    yPositionMultiple = 1 / 2.0f;
                    heightMultiple = -1;

                    break;
                case ResizeSide.BottomLeft:
                    xPositionMultiple = 1 / 2.0f;
                    widthMultiple = -1;

                    yPositionMultiple = 1 / 2.0f;
                    heightMultiple = -1;
                    break;
                case ResizeSide.Left:
                    xPositionMultiple = 1 / 2.0f;
                    widthMultiple = -1;

                    yPositionMultiple = 0;
                    heightMultiple = 0;

                    break;
            }

            bool shouldResizeFromCenter = GetIfShouldResizeFromCenter(item);
            // If we're resizing a rectangle on an object, we may not want to move on resize, so let's change the position
            // values to 0 and double the dimension values
            if (shouldResizeFromCenter)
            {
                if (item.RelativeX == 0)
                {
                    xPositionMultiple = 0;
                    widthMultiple *= 2;
                }
                if (item.RelativeY == 0)
                {
                    yPositionMultiple = 0;
                    heightMultiple *= 2;
                }
            }

            var cursor = FlatRedBall.Gui.GuiManager.Cursor;



            var scalable = item as IScalable;

            var cursorXChange = cursor.WorldXChangeAt(item.Z);
            var cursorYChange = cursor.WorldYChangeAt(item.Z);

            float xChangeForPosition = xPositionMultiple * cursor.WorldXChangeAt(item.Z);
            float yChangeForPosition = yPositionMultiple * cursor.WorldYChangeAt(item.Z);

            bool setsTextureScale = GetIfSetsTextureScale(item);

            if (setsTextureScale)
            {
                var asSprite = scalable as Sprite;
                var currentScaleX = asSprite.ScaleX;
                var currentScaleY = asSprite.ScaleY;

                if (cursorXChange != 0 && asSprite.ScaleX != 0 && widthMultiple != 0)
                {
                    var newRatio = (currentScaleX + 0.5f * cursorXChange * widthMultiple) / currentScaleX;

                    asSprite.TextureScale *= newRatio;
                }
                else if (cursorYChange != 0 && asSprite.ScaleY != 0 && heightMultiple != 0)
                {
                    var newRatio = (currentScaleY + 0.5f * cursorYChange * heightMultiple) / currentScaleY;

                    asSprite.TextureScale *= newRatio;
                }
            }
            else if (item is Circle asCircle)
            {
                float? newRadius = null;
                if (cursorXChange != 0 && widthMultiple != 0)
                {
                    newRadius = asCircle.Radius + cursorXChange * widthMultiple / 2.0f;
                }
                else if (cursorYChange != 0 && heightMultiple != 0)
                {
                    newRadius = asCircle.Radius + cursorYChange * heightMultiple / 2.0f;
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
                float Snap(float value) =>
                    IsSnappingEnabled
                    ? MathFunctions.RoundFloat(value, SizeSnappingSize)
                    : value;
                if (xChange != 0)
                {
                    //var newScaleX = scalable.ScaleX + cursorXChange * widthMultiple / 2.0f;
                    //newScaleX = Math.Max(0, newScaleX);
                    //scalable.ScaleX = newScaleX;
                    // Vic says - this needs more work. Didn't work like this and I don't want to dive in yet
                    unsnappedItemSize.X = unsnappedItemSize.X + cursorXChange * widthMultiple;
                    unsnappedItemSize.X = Math.Max(0, unsnappedItemSize.X);
                    //unsnappedItemSize.X = MathFunctions.RoundFloat(unsnappedItemSize.X, sizeSnappingSize);
                    var newScaleX = Snap(unsnappedItemSize.X / 2.0f);
                    var scaleXChange = newScaleX - scalable.ScaleX;

                    xChangeForPosition = 0;
                    if (scaleXChange != 0)
                    {
                        scalable.ScaleX = Snap(unsnappedItemSize.X / 2.0f);
                        xChangeForPosition = scaleXChange * 2 * widthMultiple * xPositionMultiple;
                    }
                }

                if (yChange != 0)
                {
                    //var newScaleY = scalable.ScaleY + cursorYChange * heightMultiple / 2.0f;
                    //newScaleY = Math.Max(0, newScaleY);
                    //scalable.ScaleY = newScaleY;
                    unsnappedItemSize.Y = unsnappedItemSize.Y + cursorYChange * heightMultiple;
                    unsnappedItemSize.Y = Math.Max(0, unsnappedItemSize.Y);

                    var newScaleY = Snap(unsnappedItemSize.Y / 2.0f);
                    var scaleYChange = newScaleY - scalable.ScaleY;

                    yChangeForPosition = 0;
                    if (scaleYChange != 0)
                    {
                        scalable.ScaleY = Snap(unsnappedItemSize.Y / 2.0f);
                        yChangeForPosition = scaleYChange * 2 * heightMultiple * yPositionMultiple;
                    }
                }
            }
            ChangePositionBy(item, xChangeForPosition, yChangeForPosition, FlatRedBall.Input.InputManager.Keyboard.IsShiftDown);
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

        public void HandleCursorRelease()
        {


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
        }

        public void HandleCursorPushed()
        {
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

        public void Destroy()
        {
#if SupportsEditMode

            mainPolygon.Visible = false;
            FlatRedBall.Screens.ScreenManager.PersistentPolygons.Remove(mainPolygon);

            Handles.Destroy();
#endif
        }
    }


}
