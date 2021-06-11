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
    public class SelectionMarker : IScalable
    {
        #region Fields/Properties

        AxisAlignedRectangle rectangle;

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
            set => rectangle.Visible = value;
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

        #endregion

        #region Constructor/Init

        public SelectionMarker()
        {
            rectangle = new AxisAlignedRectangle();
        }

        public void MakePersistent()
        {
            FlatRedBall.Screens.ScreenManager.PersistentAxisAlignedRectangles.Add(rectangle);
        }

        #endregion

        internal void Update(PositionedObject item)
        {
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
        }
    }
}
