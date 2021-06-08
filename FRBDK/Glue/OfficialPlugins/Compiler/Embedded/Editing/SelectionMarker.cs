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

        public SelectionMarker()
        {
            rectangle = new AxisAlignedRectangle();
        }
        internal void Update()
        {
            var value = (float)(1 + System.Math.Sin(TimeManager.CurrentTime * 5)) / 2;

            rectangle.Red = value * BrightColor.R/255.0f;
            rectangle.Green = value * BrightColor.G / 255.0f;
            rectangle.Blue = value * BrightColor.B / 255.0f;
        }
    }
}
