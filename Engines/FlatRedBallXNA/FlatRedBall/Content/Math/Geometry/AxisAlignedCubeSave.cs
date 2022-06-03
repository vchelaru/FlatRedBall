using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using FlatRedBall.Math.Geometry;

namespace FlatRedBall.Content.Math.Geometry
{
    public class AxisAlignedCubeSave
    {
        public float X;
        public float Y;
        public float Z;

        public float ScaleX;
        public float ScaleY;
        public float ScaleZ;

        public string Name;
        public string Parent;

        public float Alpha = 1;
        public float Red = 1;
        public float Green = 1;
        public float Blue = 1;

        public static AxisAlignedCubeSave FromAxisAlignedCube(FlatRedBall.Math.Geometry.AxisAlignedCube cube)
        {
            AxisAlignedCubeSave aacs = new AxisAlignedCubeSave();
            aacs.X = cube.X;
            aacs.Y = cube.Y;
            aacs.Z = cube.Z;

            aacs.ScaleX = cube.ScaleX;
            aacs.ScaleY = cube.ScaleY;
            aacs.ScaleZ = cube.ScaleZ;

            aacs.Name = cube.Name;

            if (cube.Parent != null)
            {
                aacs.Parent = cube.Parent.Name;
            }

            aacs.Alpha = cube.Color.A / 255.0f;
            aacs.Red = cube.Color.R / 255.0f;
            aacs.Green = cube.Color.G / 255.0f;
            aacs.Blue = cube.Color.B / 255.0f;

            return aacs;
        }

        public FlatRedBall.Math.Geometry.AxisAlignedCube ToAxisAlignedCube()
        {
            FlatRedBall.Math.Geometry.AxisAlignedCube cube = new FlatRedBall.Math.Geometry.AxisAlignedCube();

            SetValuesOn(cube);

            return cube;

        }

        public void SetValuesOn(AxisAlignedCube cube)
        {
            cube.X = this.X;
            cube.Y = this.Y;
            cube.Z = this.Z;

            cube.ScaleX = this.ScaleX;
            cube.ScaleY = this.ScaleY;
            cube.ScaleZ = this.ScaleZ;

            cube.Name = this.Name;

            cube.Color =
                 new Color(
                    (byte)(Red * 255),
                    (byte)(Green * 255),
                    (byte)(Blue * 255),
                    (byte)(Alpha * 255));
        }
    }
}
