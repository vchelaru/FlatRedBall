{CompilerDirectives}

using FlatRedBall;
using FlatRedBall.Instructions;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
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
        Cardinal
    }

    #endregion

    public class SelectionMarker : IScalable
    {
        #region Fields/Properties

        AxisAlignedRectangle rectangle;

        AxisAlignedRectangle[] handles = new AxisAlignedRectangle[8];

        public float ExtraPaddingInPixels { get; set; } = 2;

        public float ScaleX
        {
            get => rectangle.ScaleX;
            set => rectangle.ScaleX = value;
        }
        public float ScaleY
        {
            get => rectangle.ScaleY;
            set => rectangle.ScaleY = value;
        }
        public float ScaleXVelocity
        {
            get => rectangle.ScaleXVelocity;
            set => rectangle.ScaleXVelocity = value;
        }

        public float ScaleYVelocity
        {
            get => rectangle.ScaleYVelocity;
            set => rectangle.ScaleYVelocity = value;
        }

        public Vector3 Position
        {
            get => rectangle.Position;
            set => rectangle.Position = value;
        }

        float IReadOnlyScalable.ScaleX => rectangle.ScaleX;

        float IReadOnlyScalable.ScaleY => rectangle.ScaleY;

        public bool Visible
        {
            get => rectangle.Visible;
            set
            {
                if(value != rectangle.Visible)
                {
                    rectangle.Visible = value;
                }
            }
        }

        public double FadingSeed { get; set; } = 0;
        public Color BrightColor
        {
            get; set;
        } = Color.White;
        public bool IsFadingInAndOut { get; set; } = true;

        string name;
        public string Name
        {
            get => name;
            set
            {
                name = value;
                rectangle.Name = $"{name}Rectangle";
            }
        }

        public bool CanMoveItem { get; set; }

        public ResizeMode ResizeMode
        {
            get; set;
        }

        const int DefaultHandleDimension = 10;
        const int HighlightedHandleDimension = 14;

        Microsoft.Xna.Framework.Point ScreenPointPushed;
        Vector3 unsnappedItemPosition;
        Vector2 unsnappedItemSize;

        const float positionSnappingSize = 8;
        const float sizeSnappingSize = 8;

        public Vector3 LastUpdateMovement { get; private set; }


        public Vector3 GrabbedPosition;
        public Vector2 GrabbedWidthAndHeight;
        public float GrabbedRadius;
        public float GrabbedTextureScale;


        #endregion

        #region Constructor/Init

        public SelectionMarker()
        {
            rectangle = new AxisAlignedRectangle();

            for(int i = 0; i < handles.Length; i++)
            {
                handles[i] = new AxisAlignedRectangle();

                handles[i].Width = DefaultHandleDimension;
                handles[i].Height = DefaultHandleDimension;

            }
        }

        public void MakePersistent()
        {
#if SupportsEditMode

            FlatRedBall.Screens.ScreenManager.PersistentAxisAlignedRectangles.Add(rectangle);

            for(int i = 0; i < handles.Length; i++)
            {
                FlatRedBall.Screens.ScreenManager.PersistentAxisAlignedRectangles.Add(handles[i]);
            }
#endif
        }

        #endregion

        public void PlayBumpAnimation(float endingExtraPaddingBeforeZoom, bool isSynchronized)
        {
            var endingExtraPadding = endingExtraPaddingBeforeZoom;
            TweenerManager.Self.StopAllTweenersOwnedBy(rectangle);

            IsFadingInAndOut = false;
            ExtraPaddingInPixels = 0;
            const float growTime = 0.25f;
            float extraPaddingFromBump = 10;

            var tweener = rectangle.Tween((newValue) => this.ExtraPaddingInPixels = newValue, this.ExtraPaddingInPixels, extraPaddingFromBump, growTime,
                FlatRedBall.Glue.StateInterpolation.InterpolationType.Quadratic,
                FlatRedBall.Glue.StateInterpolation.Easing.Out);

            tweener.Ended += () =>
            {
                var shrinkTime = growTime;
                var tweener2 = rectangle.Tween((newValue) => this.ExtraPaddingInPixels = newValue, this.ExtraPaddingInPixels, endingExtraPadding, shrinkTime,
                    FlatRedBall.Glue.StateInterpolation.InterpolationType.Quadratic,
                    FlatRedBall.Glue.StateInterpolation.Easing.InOut);

                tweener2.Ended += () =>
                {
                    IsFadingInAndOut = true;
                    if(!isSynchronized)
                    {
                        FadingSeed = TimeManager.CurrentTime;
                    }
                    
                };
            };
        }

        internal void Update(PositionedObject item, ResizeSide sideGrabbed)
        {
            LastUpdateMovement = Vector3.Zero;

            Visible = item != null;

            UpdateScreenPointPushed(item);

            UpdateMainRectangleSizeToItem(item);

            UpdateColor();

            ApplyPrimaryDownDragEditing(item, sideGrabbed);

            UpdateHandles(item, sideGrabbed);
        }

        private void UpdateScreenPointPushed(PositionedObject item)
        {
            var cursor = FlatRedBall.Gui.GuiManager.Cursor;

            if(cursor.PrimaryPush)
            {
                ScreenPointPushed = new Microsoft.Xna.Framework.Point(cursor.ScreenX, cursor.ScreenY);
                if (item != null)
                {
                    if(item.Parent == null)
                    {
                        unsnappedItemPosition = item.Position;
                    }
                    else
                    {
                        unsnappedItemPosition = item.RelativePosition;
                    }

                    if(item is IScalable scalable)
                    {
                        unsnappedItemSize = new Vector2(scalable.ScaleX * 2, scalable.ScaleY * 2);
                    }
                }
            }
        }

        private void UpdateColor()
        {
            float value = 1;
            if(IsFadingInAndOut)
            {
                value = (float)(1 + System.Math.Sin((TimeManager.CurrentTime - FadingSeed) * 5)) / 2;
            }

            rectangle.Red = value * BrightColor.R / 255.0f;
            rectangle.Green = value * BrightColor.G / 255.0f;
            rectangle.Blue = value * BrightColor.B / 255.0f;
        }

        private void UpdateMainRectangleSizeToItem(PositionedObject item)
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

                ScaleX = ExtraPaddingInPixels/CameraLogic.CurrentZoomRatio + (maxX - minX) / 2.0f;
                ScaleY = ExtraPaddingInPixels / CameraLogic.CurrentZoomRatio + (maxY - minY) / 2.0f;
            }
        }

        static HashSet<ResizeSide> sidesToHighlight = new HashSet<ResizeSide>();
        private void UpdateHandles(PositionedObject item, ResizeSide sideGrabbed)
        {
            #region Update Handle Visibility
            if (ResizeMode == ResizeMode.EightWay)
            {
                foreach(var handle in handles)
                {
                    handle.Visible = Visible;
                }
            }
            else if(ResizeMode == ResizeMode.Cardinal)
            {
                for(int i = 0; i < handles.Length; i++)
                {
                    var handle = handles[i];
                    // every other one, starting with index 1
                    handle.Visible = Visible && (i%2) == 1;
                }
            }
            else
            {
                foreach (var handle in handles)
                {
                    handle.Visible = false;
                }
            }
            #endregion

            if(Visible)
            {
                ResizeSide sideOver = sideGrabbed;
                if (sideOver == ResizeSide.None)
                {
                    sideOver = GetSideOver();
                }
                UpdateHandleRelativePositions();

                FillSidesToHighlight(item, sideOver);
                for(int i = 0; i  < handles.Count(); i++)
                {
                    var handle = handles[i];
                    handle.Position += this.Position;

                    var side = (ResizeSide)i;
                    float size = sidesToHighlight.Contains(side) ? HighlightedHandleDimension : DefaultHandleDimension;
                    size /= CameraLogic.CurrentZoomRatio;

                    handle.Width = size;
                    handle.Height = size;
                }
            }
        }

        private static void FillSidesToHighlight(PositionedObject item, ResizeSide sideGrabbed)
        {
            sidesToHighlight.Clear();

            sidesToHighlight.Add(sideGrabbed);

            if(GetIfShouldResizeFromCenter(item))
            {
                if (sideGrabbed == ResizeSide.Left && item.RelativeX == 0) sidesToHighlight.Add(ResizeSide.Right);
                if (sideGrabbed == ResizeSide.Top && item.RelativeY == 0) sidesToHighlight.Add(ResizeSide.Bottom);
                if (sideGrabbed == ResizeSide.Right && item.RelativeX == 0) sidesToHighlight.Add(ResizeSide.Left);
                if (sideGrabbed == ResizeSide.Bottom && item.RelativeY == 0) sidesToHighlight.Add(ResizeSide.Top);

                // if we grab a diagonal, all can be resized:
                if(sideGrabbed == ResizeSide.TopLeft ||
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

            if(item is Circle || GetIfSetsTextureScale(item))
            {
                if(sideGrabbed == ResizeSide.Left)
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

 
        private void UpdateHandleRelativePositions()
        {
            var handle = handles[0];
            handle.X = -rectangle.Width / 2 - handle.Width / 2;
            handle.Y = rectangle.Height / 2 + handle.Height / 2;

            handle = handles[1];
            handle.X = 0;
            handle.Y = rectangle.Height / 2 + handle.Height / 2;

            handle = handles[2];
            handle.X = rectangle.Width / 2 + handle.Width / 2;
            handle.Y = rectangle.Height / 2 + handle.Height / 2;

            handle = handles[3];
            handle.X = rectangle.Width / 2 + handle.Width / 2;
            handle.Y = 0;

            handle = handles[4];
            handle.X = +rectangle.Width / 2 + handle.Width / 2;
            handle.Y = -rectangle.Height / 2 - handle.Height / 2;

            handle = handles[5];
            handle.X = 0;
            handle.Y = -rectangle.Height / 2 - handle.Height / 2;

            handle = handles[6];
            handle.X = -rectangle.Width / 2 - handle.Width / 2;
            handle.Y = -rectangle.Height / 2 - handle.Height / 2;

            handle = handles[7];
            handle.X = -rectangle.Width / 2 - handle.Width / 2;
            handle.Y = 0;

        }

        #region Drag to move/resize

        private void ApplyPrimaryDownDragEditing(PositionedObject item, ResizeSide sideGrabbed)
        {
            var cursor = FlatRedBall.Gui.GuiManager.Cursor;

            if (CanMoveItem && cursor.PrimaryDown &&
                (cursor.ScreenXChange != 0 || cursor.ScreenYChange != 0))
            {
                var hasMovedEnough = Math.Abs(ScreenPointPushed.X - cursor.ScreenX) > 4 || 
                    Math.Abs(ScreenPointPushed.Y - cursor.ScreenY) > 4;

                if (item != null && hasMovedEnough)
                {
                    if (sideGrabbed == ResizeSide.None)
                    {

                        float cursorXChange = cursor.WorldXChangeAt(item.Z);
                        float cursorYChange = cursor.WorldYChangeAt(item.Z);


                        LastUpdateMovement = ChangePositionBy(item, cursorXChange, cursorYChange);
                    }
                    else
                    {
                        ChangeSizeBy(item, sideGrabbed, cursor.WorldXChangeAt(item.Z), cursor.WorldYChangeAt(0));
                    }
                }
            }
        }


        private Vector3 ChangePositionBy(PositionedObject item, float xChange, float yChange)
        {
            unsnappedItemPosition.X += xChange;
            unsnappedItemPosition.Y += yChange;

            Vector3 changeAfterSnapping = Vector3.Zero;

            if (item.Parent == null)
            {
                var before = item.Position;
                item.X = MathFunctions.RoundFloat(unsnappedItemPosition.X, positionSnappingSize);
                item.Y = MathFunctions.RoundFloat(unsnappedItemPosition.Y, positionSnappingSize);
                changeAfterSnapping = item.Position - before;
            }
            else
            {
                var before = item.RelativePosition;
                item.RelativeX = unsnappedItemPosition.X;
                item.RelativeY = unsnappedItemPosition.Y;
                changeAfterSnapping = item.RelativePosition - before;
            }
            return changeAfterSnapping;
        }

        private void ChangeSizeBy(PositionedObject item, ResizeSide sideOver, float v1, float v2)
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
                if (cursorXChange != 0 && widthMultiple != 0)
                {
                    var newRadius = asCircle.Radius + cursorXChange * widthMultiple / 2.0f;
                    newRadius = Math.Max(0, newRadius);
                    asCircle.Radius = newRadius;
                }
                else if (cursorYChange != 0 && heightMultiple != 0)
                {
                    var newRadius = asCircle.Radius + cursorYChange * heightMultiple / 2.0f;
                    newRadius = Math.Max(0, newRadius);
                    asCircle.Radius = newRadius;
                }
            }
            else
            {
                //var newScaleX = scalable.ScaleX + cursorXChange * widthMultiple / 2.0f;
                //newScaleX = Math.Max(0, newScaleX);
                //scalable.ScaleX = newScaleX;

                // Vic says - this needs more work. Didn't work lik ethis and I don't want to dive in yet
                unsnappedItemSize.X = unsnappedItemSize.X + cursorXChange * widthMultiple;
                unsnappedItemSize.X = Math.Max(0, unsnappedItemSize.X);
                //unsnappedItemSize.X = MathFunctions.RoundFloat(unsnappedItemSize.X, sizeSnappingSize);
                var newScaleX = MathFunctions.RoundFloat(unsnappedItemSize.X / 2.0f, sizeSnappingSize);
                var scaleXChange = newScaleX - scalable.ScaleX;

                xChangeForPosition = 0;
                if (scaleXChange != 0)
                {
                    scalable.ScaleX = MathFunctions.RoundFloat(unsnappedItemSize.X / 2.0f, sizeSnappingSize);
                    xChangeForPosition = scaleXChange * 2 * widthMultiple * xPositionMultiple;
                }

                //var newScaleY = scalable.ScaleY + cursorYChange * heightMultiple / 2.0f;
                //newScaleY = Math.Max(0, newScaleY);
                //scalable.ScaleY = newScaleY;
                unsnappedItemSize.Y = unsnappedItemSize.Y + cursorYChange * heightMultiple;
                unsnappedItemSize.Y = Math.Max(0, unsnappedItemSize.Y);

                var newScaleY = MathFunctions.RoundFloat(unsnappedItemSize.Y / 2.0f, sizeSnappingSize);
                var scaleYChange = newScaleY - scalable.ScaleY;

                yChangeForPosition = 0;
                if (scaleYChange != 0)
                {
                    scalable.ScaleY = MathFunctions.RoundFloat(unsnappedItemSize.Y / 2.0f, sizeSnappingSize);
                    yChangeForPosition = scaleYChange * 2 * heightMultiple * yPositionMultiple;
                }
            }
            ChangePositionBy(item, xChangeForPosition, yChangeForPosition);
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
            if(cursor.IsOn3D(rectangle))
            {
                return true;
            }

            if(GetSideOver() != ResizeSide.None)
            {
                return true;
            }

            return false;
        }

        public ResizeSide GetSideOver()
        {
            var cursor = FlatRedBall.Gui.GuiManager.Cursor;
                
            for (int i = 0; i < this.handles.Length; i++)
            {
                if (handles[i].Visible && cursor.IsOn3D(handles[i]))
                {
                    return (ResizeSide)i;
                }
            }

            return ResizeSide.None;
        }

        public void Destroy()
        {
#if SupportsEditMode

            rectangle.Visible = false;
            FlatRedBall.Screens.ScreenManager.PersistentAxisAlignedRectangles.Remove(rectangle);

            for (int i = 0; i < handles.Length; i++)
            {
                FlatRedBall.Screens.ScreenManager.PersistentAxisAlignedRectangles.Remove(handles[i]);
                handles[i].Visible = false;
            }
#endif
        }
    }
}
