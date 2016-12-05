using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace FlatRedBall.Content.Math.Geometry
{
    public class SphereSave
    {
        #region Fields

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

        #endregion

        #region Methods

        public static SphereSave FromSphere(FlatRedBall.Math.Geometry.Sphere sphere)
        {
            SphereSave sphereSave = new SphereSave();
            sphereSave.X = sphere.X;
            sphereSave.Y = sphere.Y;
            sphereSave.Z = sphere.Z;

            sphereSave.Radius = sphere.Radius;

            sphereSave.Name = sphere.Name;

            if (sphere.Parent != null)
            {
                sphereSave.Parent = sphere.Parent.Name;
            }

            sphereSave.Alpha = sphere.Color.A / 255.0f;
            sphereSave.Red = sphere.Color.R / 255.0f;
            sphereSave.Green = sphere.Color.G / 255.0f;
            sphereSave.Blue = sphere.Color.B / 255.0f;

            return sphereSave;
        }

        public FlatRedBall.Math.Geometry.Sphere ToSphere()
        {
            FlatRedBall.Math.Geometry.Sphere sphere = new FlatRedBall.Math.Geometry.Sphere();

            sphere.X = this.X;
            sphere.Y = this.Y;
            sphere.Z = this.Z;

            sphere.Radius = this.Radius;

            sphere.Name = this.Name;

            sphere.Color =
                new Color(
                    (byte)(Red * 255),
                    (byte)(Green * 255),
                    (byte)(Blue * 255),
                    (byte)(Alpha * 255));

            return sphere;

        }

        #endregion
    }
}
