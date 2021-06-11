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

    #endregion

    public class SelectionMarker : IScalable
    {
        #region Fields/Properties

        AxisAlignedRectangle rectangle;

        AxisAlignedRectangle[] handles = new AxisAlignedRectangle[8];


        public Color BrightColor
        {
            get; set;
        } = Color.White;

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
                    UpdateHandles();
                }
            }
        }

        public double FadingSeed { get; set; } = 0;

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

        ResizeMode resizeMode;
        public ResizeMode ResizeMode
        {
            get => resizeMode;
            set
            {
                if(value != resizeMode)
                {
                    resizeMode = value;
                    UpdateHandles();
                }
            }
        }

        const int handleDimension = 6;

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

        internal void Update(PositionedObject item, float extraPadding)
        {
            Visible = item != null;
            
            foreach(var handle in handles)
            {
                handle.Visible = Visible;
            }

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

                ScaleX = extraPadding + (maxX - minX) / 2.0f;
                ScaleY = extraPadding + (maxY - minY) / 2.0f;
            }

            var value = (float)(1 + System.Math.Sin((TimeManager.CurrentTime - FadingSeed) * 5)) / 2;

            rectangle.Red = value * BrightColor.R/255.0f;
            rectangle.Green = value * BrightColor.G / 255.0f;
            rectangle.Blue = value * BrightColor.B / 255.0f;

            var cursor = FlatRedBall.Gui.GuiManager.Cursor;

            if(CanMoveItem && cursor.PrimaryDown && 
                (cursor.ScreenXChange != 0 || cursor.ScreenYChange != 0))
            {
                if (item != null)
                {
                    if (item.Parent == null)
                    {
                        item.X += cursor.WorldXChangeAt(item.Z);
                        item.Y += cursor.WorldYChangeAt(item.Z);
                    }
                    else
                    {
                        item.RelativeX += cursor.WorldXChangeAt(item.Z);
                        item.RelativeY += cursor.WorldYChangeAt(item.Z);
                    }
                }
            }

            UpdateHandles();
        }

        void UpdateHandles()
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

            foreach(var handle2 in handles)
            {
                handle2.Position += this.Position;
            }

        }

        ResizeSide GetSideOver(float x, float y)
        {
            for (int i = 0; i < this.handles.Length; i++)
            {
                var cursor = FlatRedBall.Gui.GuiManager.Cursor;
                
                if (cursor.IsOn3D(handles[i]))
                {
                    return (ResizeSide)i;
                }
            }

            return ResizeSide.None;
        }
    }
}
