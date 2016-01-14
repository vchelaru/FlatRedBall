using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace FlatRedBall.Graphics.IDrawableBatches
{
    public class GeometryDrawableBatch : PositionedObject, IDrawableBatch
    {
        #region Fields

        #region XML Docs
        /// <summary>
        /// The effect used to draw
        /// </summary>
        #endregion
        private BasicEffect mEffect;

        #region XML Docs
        /// <summary>
        /// The vertex declaration
        /// </summary>
        #endregion
        private VertexDeclaration mVertexDeclaration;


        private VertexPositionColor[] mPositionColorVertices;
        private VertexPositionTexture[] mPositionTextureVertices;

        #region XML Docs
        /// <summary>
        /// The indices to draw the shape
        /// </summary>
        #endregion
        private short[] mIndices;

        #endregion

        #region Properties

        public VertexPositionColor[] PositionColorVertices
        {
            get { return mPositionColorVertices; }
            set
            {
                mPositionColorVertices = value;
                if (value != null)
                {
                    mPositionTextureVertices = null;
                }
            }
        }

        public VertexPositionTexture[] PositionTextureVertices
        {
            get { return mPositionTextureVertices; }
            set
            {
                mPositionTextureVertices = value;
                if (value != null)
                {
                    mPositionColorVertices = null;
                }
            }
        }

        public VertexElement[] VertexElements
        {
            get { return mVertexDeclaration.GetVertexElements(); }
            set
            {
                mVertexDeclaration = new VertexDeclaration(
                    FlatRedBallServices.GraphicsDevice,
                    value);
            }
        }

        public bool Visible
        {
            get;
            set;
        }

        public short[] Indices
        {
            get { return mIndices; }
            set { mIndices = value; }
        }

        public Texture2D Texture
        {
            get;
            set;
        }

        #region XML Docs
        /// <summary>
        /// Here we tell the engine if we want this batch
        /// updated every frame.  Since we have no updating to
        /// do though, we will set this to false
        /// </summary>
        #endregion
        public bool UpdateEveryFrame
        {
            get { return true; }
        }

        #endregion

        #region Constructor / Initialization

        #region XML Docs
        /// <summary>
        /// Create and initialize all assets
        /// </summary>
        #endregion
        public GeometryDrawableBatch()
            : base()
        {
#if XNA4
            // Create the effect
            mEffect = new BasicEffect(FlatRedBallServices.GraphicsDevice);
#else
            // Create the effect
            mEffect = new BasicEffect(
                FlatRedBallServices.GraphicsDevice,
                null);
#endif

#if XNA4
            VertexElements = VertexPositionColor.VertexDeclaration.GetVertexElements();
#else
            VertexElements = VertexPositionColor.VertexElements;
#endif

            // Create the vertices
            mPositionColorVertices = new VertexPositionColor[] {
                new VertexPositionColor(new Vector3(-1f,-1.732f / 3f,0f), Color.Red),
                new VertexPositionColor(new Vector3(1f,-1.732f / 3f,0f), Color.Green),
                new VertexPositionColor(new Vector3(0f,1.732f * 2f / 3f,0f), Color.Blue)
            };

            // Create the indices
            mIndices = new short[] { 0, 1, 2 };
        }

        #endregion

        #region Methods

        #region XML Docs
        /// <summary>
        /// Custom drawing technique - sets graphics states and
        /// draws the custom shape
        /// </summary>
        /// <param name="camera">The currently drawing camera</param>
        #endregion
        public void Draw(Camera camera)
        {
            if (Visible)
            {
                // Set graphics states
                FlatRedBallServices.GraphicsDevice.RenderState.FillMode = FillMode.Solid;
                FlatRedBallServices.GraphicsDevice.RenderState.CullMode = CullMode.None;

                // Set the vertex declaration
                FlatRedBallServices.GraphicsDevice.VertexDeclaration = mVertexDeclaration;

                // Have the current camera set our current view/projection variables
                camera.SetDeviceViewAndProjection(mEffect, false);

                // Here we get the positioned object's transformation (position / rotation)
                mEffect.World = base.TransformationMatrix;

                if (mPositionColorVertices != null)
                {
                    mEffect.VertexColorEnabled = true;
                    mEffect.TextureEnabled = false;
                }
                else
                {
                    mEffect.VertexColorEnabled = false;
                    mEffect.TextureEnabled = true;
                    mEffect.Texture = Texture;
                }


                // Start the effect
                mEffect.Begin();
                foreach (EffectPass pass in mEffect.CurrentTechnique.Passes)
                {
                    // Start each pass

                    pass.Begin();
                    if (mPositionColorVertices != null)
                    {
                        // Draw the shape
                        FlatRedBallServices.GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionColor>(
                            PrimitiveType.TriangleList,
                            mPositionColorVertices, 0, mPositionColorVertices.Length,
                            mIndices, 0, mIndices.Length / 3);
                    }
                    else
                    {
                        // Draw the shape
                        FlatRedBallServices.GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionTexture>(
                            PrimitiveType.TriangleList,
                            mPositionTextureVertices, 0, mPositionTextureVertices.Length,
                            mIndices, 0, mIndices.Length / 3);
                    }

                    // End each pass
                    pass.End();
                }
                // End the effect
                mEffect.End();
            }
        }

        #region XML Docs
        /// <summary>
        /// Here we update our batch - but this batch doesn't
        /// need to be updated
        /// </summary>
        #endregion
        public void Update()
        {
            TimedActivity(TimeManager.SecondDifference, TimeManager.SecondDifferenceSquaredDividedByTwo, TimeManager.LastSecondDifference);

            UpdateDependencies(TimeManager.CurrentTime);
        }

        #region XML Docs
        /// <summary>
        /// Here we destroy all assets that need destroying.
        /// In this case, all our assets will be destroyed
        /// automatically upon quitting
        /// </summary>
        #endregion
        public void Destroy()
        {
        }

        #endregion
    }
}
