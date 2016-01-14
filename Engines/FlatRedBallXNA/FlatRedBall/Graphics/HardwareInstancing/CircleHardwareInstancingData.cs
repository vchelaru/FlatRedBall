using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall.Math.Geometry;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FlatRedBall.Graphics.HardwareInstancing
{
    class CircleHardwareInstancingData : HardwareInstancingData
    {
        // Initially this will hold up to 3000 circles.
        // If more are needed the array will be expanded
        Vector3[] mPositions = new Vector3[3000];


        internal CircleHardwareInstancingData()
        {
            Effect =
                FlatRedBallServices.Load<Effect>(@"Assets\Shaders\HardwareInstancedLine.fx");

            Vector2[] vertices =
                new Vector2[21];

            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = new Vector2(
                    (float)System.Math.Cos(2 * System.Math.PI * ((float)i / (float)(vertices.Length))),
                    (float)System.Math.Sin(2 * System.Math.PI * ((float)i / (float)(vertices.Length))));
            }

            HardwareInstancedGeometry = new VertexBuffer(
                FlatRedBallServices.GraphicsDevice,
                21 * (2 * 4), // 21 points @ 2 floats per vertex @ 4 bytes each
                BufferUsage.None);

            HardwareInstancedGeometry.SetData<Vector2>(vertices);

            HardwareInstancedIndexBuffer = new IndexBuffer(
                FlatRedBallServices.GraphicsDevice,
                21 * 2, // two floats per point
                BufferUsage.None,
                IndexElementSize.SixteenBits);

            HardwareInstancedIndexBuffer.SetData<short>(
                new short[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 
                            11, 12, 13, 14, 15, 16, 17, 18, 19, 20 });

            VertexElement[] elements = new VertexElement[
                2]; // Position the individual vertex + offset position per instance
            elements[0] = new VertexElement(0, 0, 
                VertexElementFormat.Vector2, 
                VertexElementMethod.Default, 
                VertexElementUsage.Position, 1);

            elements[1] = new VertexElement(1, 0,
                VertexElementFormat.Vector3, 
                VertexElementMethod.Default, 
                VertexElementUsage.Position, 1);

            VertexDeclaration = new VertexDeclaration(
                FlatRedBallServices.GraphicsDevice, elements);
        }

        public void Update()
        {
            if (ShapeManager.mCircles.Count > mPositions.Length)
            {
                int newLength = mPositions.Length * 2;

                mPositions = new Vector3[newLength];
            }



        }
    }
}
