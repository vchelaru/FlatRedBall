using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework.Graphics;

using FlatRedBall;

using FlatRedBall.Graphics;
using Microsoft.Xna.Framework;

namespace FlatRedBall.Math.Collision
{
    public class HeightMapDrawableBatch : IDrawableBatch
    {
        #region Fields

        private float mX = 0f;
        private float mY = 0f;
        private float mZ = 0f;
        
        
        VertexBuffer mVertexBuffer;
        IndexBuffer mIndexBuffer;

        BasicEffect mEffect;

        //List<DynamicVertexBuffer> mVertexBuffers = new List<DynamicVertexBuffer>();
        //List<RenderBreak> mRenderBreaks = new List<RenderBreak>();

        int mNumVertices;
        int mNumLines;
        HeightMap mHeightMap;
        int mWidth;
        int mHeight;

        #endregion

        #region Properties

        public float X
        {
            get { return mX; }
            set { mX = value; }
        }

        public float Y
        {
            get { return mY; }
            set { mY = value; }
        }


        public float Z
        {
            get { return mZ; }
            set { mZ = value; }
        }



        public bool UpdateEveryFrame
        {
            get { return false; }
            set { }
        }

        #endregion

        #region Methods

        public HeightMapDrawableBatch()
        {
            UpdateEveryFrame = false;
            mEffect = new BasicEffect(FlatRedBallServices.GraphicsDevice, null);
        }

        public void UpdateTo(HeightMap heightMap)
        {
            mHeightMap = heightMap;
            mWidth = heightMap.Width;
            mHeight = heightMap.Height;

            // Release old assets
            Destroy();

            mNumVertices = mWidth * mHeight;
            mNumLines = (mHeight * (mWidth - 1));

            // Create new assets
            mVertexBuffer = new VertexBuffer(
                FlatRedBallServices.GraphicsDevice,
                mNumVertices * VertexPositionColor.SizeInBytes,
                BufferUsage.None);
            mIndexBuffer = new IndexBuffer(
                FlatRedBallServices.GraphicsDevice,
                2 * mNumLines * sizeof(int),
                BufferUsage.None,
                IndexElementSize.ThirtyTwoBits);

            // Create data
            VertexPositionColor[] vertices = new VertexPositionColor[mNumVertices];
            int[] indices = new int[2 * mNumLines];
            int indicesIndex = 0;

            // Add data
            float xstep = 2f / (float)mWidth;
            float ystep = 2f / (float)mHeight;

            for (int y = 0; y < mHeight; y++)
            {
                for (int x = 0; x < mWidth; x++)
                {
                    vertices[y * mWidth + x] =
                        new VertexPositionColor(new Vector3(
                            xstep * (float)x - 1f,
                            ystep * (float)y - 1f,
                            heightMap.RawData[x, y]),
                        Color.White);

                    // Set up horizontal lines
                    if (x < mWidth - 1)
                    {
                        // add a horizontal line to the next x vertex
                        indices[indicesIndex] = y * mWidth + x;
                        indices[indicesIndex + 1] = y * mWidth + (x + 1);
                        indicesIndex += 2;
                    }
                }
            }

            // Set buffer data
            mVertexBuffer.SetData<VertexPositionColor>(vertices);
            mIndexBuffer.SetData<int>(indices);
        }

        public void Draw(Camera camera)
        {
            Renderer.Graphics.GraphicsDevice.VertexDeclaration = Renderer.PositionColorVertexDeclaration;

            // Set states
            FillMode oldFillMode = FlatRedBallServices.GraphicsDevice.RenderState.FillMode;
            FlatRedBallServices.GraphicsDevice.RenderState.FillMode = FillMode.WireFrame;

            // Set buffers
            FlatRedBallServices.GraphicsDevice.Vertices[0].SetSource(mVertexBuffer, 0, VertexPositionColor.SizeInBytes);
            FlatRedBallServices.GraphicsDevice.Indices = mIndexBuffer;

            // Set effect info
            SpriteManager.Camera.SetDeviceViewAndProjection(mEffect, false);
            mEffect.World =
                Matrix.CreateScale(mHeightMap.ScaleX, mHeightMap.ScaleY, mHeightMap.ScaleZ) *
                Matrix.CreateTranslation(mHeightMap.X, mHeightMap.Y, mHeightMap.Z);
            

            // Draw
            mEffect.Begin();

            foreach (EffectPass pass in mEffect.CurrentTechnique.Passes)
            {
                pass.Begin();

                FlatRedBallServices.GraphicsDevice.DrawIndexedPrimitives(
                    PrimitiveType.LineList, 0, 0, mNumVertices, 0, mNumLines);

                pass.End();
            }

            mEffect.End();

            // Restore fillmode (likely to be expected as solid)
            FlatRedBallServices.GraphicsDevice.RenderState.FillMode = FillMode.Solid;
            //Renderer.DrawVBList(SpriteManager.Camera, mVertexBuffers, mRenderBreaks, mCount, PrimitiveType.PointList, VertexPositionColor.SizeInBytes, mCount);
        }

        public void Destroy()
        {
            if (mVertexBuffer != null)
            {
                mVertexBuffer.Dispose();
                mVertexBuffer = null;
            }
            if (mIndexBuffer != null)
            {
                mIndexBuffer.Dispose();
                mIndexBuffer = null;
            }

            //if (mVertexBuffers.Count != 0)
            //{
            //    mVertexBuffers[0].Dispose();
            //    mVertexBuffers.Clear();
            //}

            //mRenderBreaks.Clear();
            //mVertexBuffers.Clear();
        }

        #region IDrawableBatch Members

        public void Update()
        {
        }

        #endregion

        #endregion
    }
}
