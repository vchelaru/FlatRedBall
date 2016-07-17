using System;
using System.Collections.Generic;
using System.Text;
#if FRB_MDX
using System.Drawing;
#elif FRB_XNA || SILVERLIGHT
using Microsoft.Xna.Framework.Graphics;

using Microsoft.Xna.Framework;
#endif

namespace FlatRedBall.Content.Math.Geometry
{
#if !UWP && !WINDOWS_8

    [Serializable]
#endif
    public class CircleSave
    {
        public float X;
        public float Y;
        public float Z;

        public float Radius;

        public string Name;
        public string Parent;

        public float Alpha = 1;
        public float Red = 1;
        public float Green = 1;
        public float Blue = 1;

        public static CircleSave FromCircle(FlatRedBall.Math.Geometry.Circle circle)
        {
            CircleSave circleSave = new CircleSave();
            circleSave.SetFrom(circle);

            return circleSave;
        }

        public void SetFrom(FlatRedBall.Math.Geometry.Circle circle)
        {
            X = circle.X;
            Y = circle.Y;
            Z = circle.Z;

            Radius = circle.Radius;

            Name = circle.Name;

            if (circle.Parent != null)
            {
                Parent = circle.Parent.Name;
            }

            Alpha = circle.Color.A / 255.0f;
            Red = circle.Color.R / 255.0f;
            Green = circle.Color.G / 255.0f;
            Blue = circle.Color.B / 255.0f;
        }

        public FlatRedBall.Math.Geometry.Circle ToCircle()
        {
            FlatRedBall.Math.Geometry.Circle circle = new FlatRedBall.Math.Geometry.Circle();

            circle.X = this.X;
            circle.Y = this.Y;
            circle.Z = this.Z;

            circle.Radius = this.Radius;

            circle.Name = this.Name;

            circle.Color =
#if FRB_MDX
                Color.FromArgb(
                (int)(Alpha * 255),
                (int)(Red * 255),
                (int)(Green * 255),
                (int)(Blue * 255));
#else
                new Color(
                    (byte)(Red * 255),
                    (byte)(Green * 255),
                    (byte)(Blue * 255),
                    (byte)(Alpha * 255));
#endif

            return circle;

        }

    }
}
