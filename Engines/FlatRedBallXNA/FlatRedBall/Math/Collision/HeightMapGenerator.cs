using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using FlatRedBall.Graphics.Model;
using FlatRedBall.Graphics.PostProcessing;
using Microsoft.Xna.Framework.Graphics;
using FlatRedBall.Graphics;

namespace FlatRedBall.Math.Collision
{
    internal class HeightMapRenderer
    {
        #region Fields

        // The camera to use for rendering
        Camera mCamera;

        // min and max positions in the model
        Vector3 mMin;
        Vector3 mMax;


        // the model
        PositionedModel mModel;

        HeightMap mHeightMap;

        #endregion

        #region Properties

        // The center of the model
        public Vector3 ModelCenter
        {
            get { return (mMin + mMax) / 2f; }
        }

        #endregion

        #region Constructors

        public HeightMapRenderer(string contentManagerName, int width, int height, Model model, HeightMap heightMap)
            : this(contentManagerName, width, height, model, heightMap, Matrix.Identity)
        {
        }

        public HeightMapRenderer(string contentManagerName,
            int width, int height, Model model, HeightMap heightMap, Matrix transformationMatrix)
        {
            mHeightMap = heightMap;

            // Create camera
            mCamera = new Camera(contentManagerName, width, height);

#if !XNA4
            // Set up rendering mode
            mCamera.RenderOrder.Clear();
            mCamera.RenderOrder.Add(FlatRedBall.Graphics.RenderMode.Position);

            mCamera.DrawsCameraLayer = true;
            mCamera.DrawsWorld = false;
            mCamera.DrawToScreen = false;
#endif

            // Get bounds
            GetMinMax(model, transformationMatrix);

            // Set up camera orthogonality
            mCamera.Orthogonal = true;
            mCamera.OrthogonalWidth = mMax.X - mMin.X;
            mCamera.OrthogonalHeight = mMax.Y - mMin.Y;
            mCamera.FarClipPlane = 3f + mMax.Z - mMin.Z;
            mCamera.NearClipPlane = 1f;

            // Set up camera position
            mCamera.Position = new Vector3(
                (mMax.X + mMin.X) / 2f,
                (mMax.Y + mMin.Y) / 2f,
                (mMax.Z + 2f));

            // Add post-processing
            mCamera.PostProcessing.EffectCombineOrder.Clear();
            mCamera.PostProcessing.EffectCombineOrder.Add(new HeightMapPostProcessor(heightMap));

            // Add the camera to the engine
            Renderer.Cameras.Add(mCamera);

            // Add model to camera layer
            mModel = ModelManager.AddModel(model);
            ModelManager.AddToLayer(mModel, mCamera.Layer);
        }

        public HeightMapRenderer(string contentManagerName, int width, int height, PositionedModel model, HeightMap heightMap) :
            this(contentManagerName, width, height, model.XnaModel, heightMap,
            Matrix.CreateScale(model.ScaleX, model.ScaleY, model.ScaleZ) * model.RotationMatrix)
        {
            mModel.ScaleX = model.ScaleX;
            mModel.ScaleY = model.ScaleY;
            mModel.ScaleZ = model.ScaleZ;
            mModel.RotationX = model.RotationX;
            mModel.RotationY = model.RotationY;
            mModel.RotationZ = model.RotationZ;
        }

        #endregion

        #region Methods

        private void GetMinMax(Model model, Matrix transformationMatrix)
        {
            // Local variables
            int positionOffset;
            int vertexStride;
            VertexElement[] vertexElements;

            Vector3 vertexPosition;
            Matrix meshTransform;

            int startOffset;

            byte[] vertices;

            // Get the bone transforms
            Matrix[] boneTransforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(boneTransforms);

            #region calculate min/max

            bool first = true;

            foreach (ModelMesh mesh in model.Meshes)
            {
                // Get mesh transformation
                meshTransform = boneTransforms[mesh.ParentBone.Index] * transformationMatrix;

                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    #region Get Vertex Information

                    // stride
                    vertexStride = part.VertexStride;

                    // Position offset
                    vertexElements = part.VertexDeclaration.GetVertexElements();
                    positionOffset = 0;
                    for (int e = 0; e < vertexElements.Length; e++)
                    {
                        if (vertexElements[e].VertexElementUsage == VertexElementUsage.Position)
                        {
                            positionOffset = vertexElements[e].Offset;
                        }
                    }

                    #endregion

                    // Get Vertices
                    vertices = new byte[part.NumVertices * vertexStride];
                    mesh.VertexBuffer.GetData<byte>(
                        part.StreamOffset,
                        vertices,
                        0,
                        part.NumVertices * vertexStride,
                        1);

                    // Find min/max
                    for (int i = 0; i < part.NumVertices; i++)
                    {
                        startOffset = i * vertexStride + positionOffset;

                        vertexPosition = Vector3.Transform(
                            new Vector3(
                            BitConverter.ToSingle(vertices, startOffset),
                            BitConverter.ToSingle(vertices, startOffset + sizeof(float)),
                            BitConverter.ToSingle(vertices, startOffset + sizeof(float) * 2)),
                            meshTransform);

                        if (first)
                        {
                            mMin = mMax = vertexPosition;
                            first = false;
                        }
                        else
                        {
                            if (vertexPosition.X < mMin.X) mMin.X = vertexPosition.X;
                            if (vertexPosition.Y < mMin.Y) mMin.Y = vertexPosition.Y;
                            if (vertexPosition.Z < mMin.Z) mMin.Z = vertexPosition.Z;
                            if (vertexPosition.X > mMax.X) mMax.X = vertexPosition.X;
                            if (vertexPosition.Y > mMax.Y) mMax.Y = vertexPosition.Y;
                            if (vertexPosition.Z > mMax.Z) mMax.Z = vertexPosition.Z;
                        }
                    }
                }
            }

            #endregion
        }

        internal void Cleanup()
        {
            // Remove model
            ModelManager.RemoveModel(mModel);

            // Remove camera from cameras collection
            Renderer.Cameras.Remove(mCamera);

            mHeightMap.ScaleX = (mMax.X - mMin.X) / 2f;
            mHeightMap.ScaleY = (mMax.Y - mMin.Y) / 2f;
        }

        #endregion
    }

    #region XML Docs
    /// <summary>
    /// Height map post-processor, used to dump heightmap texture to data structure
    /// </summary>
    #endregion
    internal class HeightMapPostProcessor : PostProcessingEffectBase
    {
        #region Fields

        #region XML Docs
        /// <summary>
        /// The height map to copy data to
        /// </summary>
        #endregion
        HeightMap mHeightMap;

        #endregion

        #region Constructor

        public HeightMapPostProcessor(HeightMap heightMap)
        {
            mHeightMap = heightMap;
            InitializeEffect();
            mEnabled = true;
        }

        #endregion

        #region Methods

        // Dump information to heightmap
        public override void Draw(Camera camera, ref Texture2D screenTexture, ref Rectangle baseRectangle, Color clearColor)
        {
            // Copy to heightmap
            if (!mHeightMap.IsInitialized) mHeightMap.SetDataHV4(screenTexture);

            // Disable, so this only runs once
            mEnabled = false;

            // Pass on texture
            mTexture = screenTexture;
        }

        #endregion
    }
}
