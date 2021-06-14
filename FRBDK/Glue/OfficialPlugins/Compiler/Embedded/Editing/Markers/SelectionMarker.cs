using FlatRedBall;
using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace {ProjectNamespace}.GlueControl.Editing
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

        public float ExtraPadding { get; set; } = 2;

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
                    UpdateHandlesVisibilityAndPosition();
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

        const int handleDimension = 10;

        #endregion

        #region Constructor/Init

        public SelectionMarker()
        {
            rectangle = new AxisAlignedRectangle();

            for(int i = 0; i < handles.Length; i++)
            {
                handles[i] = new AxisAlignedRectangle();
                handles[i].Width = handleDimension;
                handles[i].Height = handleDimension;

            }
        }

        public void MakePersistent()
        {
            FlatRedBall.Screens.ScreenManager.PersistentAxisAlignedRectangles.Add(rectangle);

            for(int i = 0; i < handles.Length; i++)
            {
                FlatRedBall.Screens.ScreenManager.PersistentAxisAlignedRectangles.Add(handles[i]);
            }
        }

        #endregion

        internal void Update(PositionedObject item, ResizeSide sideGrabbed)
        {
            Visible = item != null;

            UpdateMainRectangleSizeToItem(item);

            UpdateColor();

            var cursor = FlatRedBall.Gui.GuiManager.Cursor;

            if (CanMoveItem && cursor.PrimaryDown &&
                (cursor.ScreenXChange != 0 || cursor.ScreenYChange != 0))
            {
                if (item != null)
                {
                    if (sideGrabbed == ResizeSide.None)
                    {

                        float xChange = cursor.WorldXChangeAt(item.Z);
                        float yChange = cursor.WorldYChangeAt(item.Z);

                        ChangePositionBy(item, xChange, yChange);
                    }
                    else
                    {
                        ChangeSizeBy(item, sideGrabbed, cursor.WorldXChangeAt(item.Z), cursor.WorldYChangeAt(0));
                    }
                }
            }

            UpdateHandlesVisibilityAndPosition();
        }

        private void UpdateColor()
        {
            if(IsFadingInAndOut)
            {
                var value = (float)(1 + System.Math.Sin((TimeManager.CurrentTime - FadingSeed) * 5)) / 2;

                rectangle.Red = value * BrightColor.R / 255.0f;
                rectangle.Green = value * BrightColor.G / 255.0f;
                rectangle.Blue = value * BrightColor.B / 255.0f;
            }
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

                ScaleX = ExtraPadding + (maxX - minX) / 2.0f;
                ScaleY = ExtraPadding + (maxY - minY) / 2.0f;
            }
        }

        void UpdateHandlesVisibilityAndPosition()
        {
            foreach(var handle in handles)
            {
                handle.Visible = Visible && ResizeMode == ResizeMode.EightWay;
            }

            if(Visible)
            {
                UpdateHandleRelativePositions();

                foreach(var handle in handles)
                {
                    handle.Position += this.Position;
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

        private static void ChangePositionBy(PositionedObject item, float xChange, float yChange)
        {
            if (item.Parent == null)
            {
                item.X += xChange;
                item.Y += yChange;
            }
            else
            {
                item.RelativeX += xChange;
                item.RelativeY += yChange;
            }
        }

        private void ChangeSizeBy(PositionedObject item, ResizeSide sideOver, float v1, float v2)
        {
            float xPositionMultiple = 0;
            float yPositionMultiple = 0;
            float widthMultiple = 0;
            float heightMultiple = 0;

            switch(sideOver)
            {
                case ResizeSide.TopLeft:
                    xPositionMultiple = 1/2.0f;
                    widthMultiple = -1;
                    
                    yPositionMultiple = 1/2.0f;
                    heightMultiple = 1;
                    break;
                case ResizeSide.Top:
                    xPositionMultiple = 0;
                    widthMultiple = 0;

                    yPositionMultiple = 1 / 2.0f;
                    heightMultiple = 1;
                    break;
                case ResizeSide.TopRight:
                    xPositionMultiple = 1/2.0f;
                    widthMultiple = 1;


                    yPositionMultiple = 1 / 2.0f;
                    heightMultiple = 1;

                    break;
                case ResizeSide.Right:
                    xPositionMultiple = 1/2.0f;
                    widthMultiple = 1;

                    yPositionMultiple = 0;
                    heightMultiple = 0;
                    break;

                case ResizeSide.BottomRight:
                    xPositionMultiple = 1/2.0f;
                    widthMultiple = 1;

                    yPositionMultiple = 1/2.0f;
                    heightMultiple = -1;

                    break;

                case ResizeSide.Bottom:
                    xPositionMultiple = 0;
                    widthMultiple = 0;

                    yPositionMultiple = 1/2.0f;
                    heightMultiple = -1;

                    break;
                case ResizeSide.BottomLeft:
                    xPositionMultiple = 1/2.0f;
                    widthMultiple = -1;

                    yPositionMultiple = 1/2.0f;
                    heightMultiple = -1;
                    break;
                case ResizeSide.Left:
                    xPositionMultiple = 1/2.0f;
                    widthMultiple = -1;

                    yPositionMultiple = 0;
                    heightMultiple = 0;

                    break;
            }

            var cursor = FlatRedBall.Gui.GuiManager.Cursor;


            float xChange = xPositionMultiple * cursor.WorldXChangeAt(item.Z);
            float yChange = yPositionMultiple * cursor.WorldYChangeAt(item.Z);

            ChangePositionBy(item, xChange, yChange);

            var scalable = item as IScalable;

            var newScaleX = scalable.ScaleX + cursor.WorldXChangeAt(item.Z) * widthMultiple / 2.0f;
            newScaleX = Math.Max(0, newScaleX);
            scalable.ScaleX = newScaleX;

            var newScaleY = scalable.ScaleY + cursor.WorldYChangeAt(item.Z) * heightMultiple / 2.0f;
            newScaleY = Math.Max(0, newScaleY);
            scalable.ScaleY = newScaleY;
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
                if (cursor.IsOn3D(handles[i]))
                {
                    return (ResizeSide)i;
                }
            }

            return ResizeSide.None;
        }
    }
}
