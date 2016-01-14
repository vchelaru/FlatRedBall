using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FlatRedBall.Graphics.HardwareInstancing
{
    /// <summary>
    /// Unsupported
    /// </summary>
    public class HardwareInstancingData
    {
        #region Fields

        #region Static Fields

        public const int NumberOfElementsInSpriteIndexBuffer = 6;

        #endregion

        #region XML Docs
        /// <summary>
        /// Reference to the shader that will be used
        /// to render the objects.
        /// </summary>
        #endregion
        public Effect Effect;

        #region XML Docs
        /// <summary>
        /// Defines what one instance looks like.
        /// For a Sprite it would be 4 points, for a circle
        /// it'd be each point on the circle.
        /// </summary>
        #endregion
        public VertexBuffer HardwareInstancedGeometry;

#if XBOX360
        #region XML Docs
        /// <summary>
        /// The indexes to be used with the the 
        /// geometry VertexBuffer.
        /// </summary>
        #endregion
        public VertexBuffer HardwareInstancedIndexBuffer;
#else
        #region XML Docs
        /// <summary>
        /// The indexes to be used with the the 
        /// geometry VertexBuffer.
        /// </summary>
        #endregion
        public IndexBuffer HardwareInstancedIndexBuffer;
#endif

        #region XML Docs
        /// <summary>
        /// Holds the unique data for each instance (like position)
        /// </summary>
        #endregion

        public List<VertexBuffer> HardwareInstancedInstanceData = new List<VertexBuffer>();


        public VertexDeclaration VertexDeclaration;

        #endregion

        #region Methods

        public HardwareInstancingData()
        {
            //// Get a new MDUTIL.DLL (e.g. compiled for XNA 2.0) or this will break the build
//#if !XBOX360
//            XNAInfo.Experimental.MDXUtil.EnableATIInstancingHack(FlatRedBallServices.GraphicsDevice);
//#endif
        }


        /// <summary>
        /// Sets up the GraphicsDevice so that it can render the 
        /// hardware instanced objects.  This method should be called before
        /// DrawIndexedPrimitives
        /// </summary>
        /// <param name="instanceCount">The number of instances to draw.</param>
        /// <param name="sizeOfInstanceData">The size in bytes of the instance data.  
        /// For example, if the data unique per instance is just position, then 
        /// the value would be the size of a Vector3</param>
#if XBOX360
        public void BeginDrawing(int sizeOfGeometryVertex)
#else
        public void BeginDrawing()
#endif
        {

            throw new NotImplementedException("We dont' support this anymore in XNA 4, so we've removed it from maintenance for XNA3");


//            Renderer.CurrentEffect = Effect;
//            FlatRedBallServices.GraphicsDevice.VertexDeclaration = VertexDeclaration;

//#if !XBOX360

//            SpriteManager.Camera.SetDeviceViewAndProjection(Effect, false);
//            FlatRedBallServices.GraphicsDevice.Vertices[0].SetSource(HardwareInstancedGeometry, 0,
//                VertexDeclaration.GetVertexStrideSize(0));

//            FlatRedBallServices.GraphicsDevice.Vertices[1].SetFrequencyOfInstanceData(1);


//            FlatRedBallServices.GraphicsDevice.Indices = HardwareInstancedIndexBuffer;
//#else

//            FlatRedBallServices.GraphicsDevice.Vertices[0].SetSource(HardwareInstancedGeometry, 0,
//                sizeOfGeometryVertex);
//            FlatRedBallServices.GraphicsDevice.Vertices[1].SetSource(HardwareInstancedIndexBuffer, 0, sizeof(float));
//#endif
        
        
        
        }

#if XBOX360
        public void CallDrawPrimitives(int vertexBufferIndex, int sizeOfInstanceData, int numberOfPrimitivesPerInstance,
            int instanceCount)
        {
            FlatRedBallServices.GraphicsDevice.Vertices[2].SetSource(HardwareInstancedInstanceData[vertexBufferIndex], 0,
                sizeOfInstanceData);
#else
        public void CallDrawPrimitives(int vertexBufferIndex, int sizeOfInstanceData, int numberOfPrimitivesPerInstance, 
            int instanceCount, int numberOfVerticesPerInstance)
        {
            FlatRedBallServices.GraphicsDevice.Vertices[0].SetFrequencyOfIndexData(instanceCount);


            FlatRedBallServices.GraphicsDevice.Vertices[1].SetSource(HardwareInstancedInstanceData[vertexBufferIndex], 0,
                sizeOfInstanceData);
#endif

            Effect.Begin();

            foreach (EffectPass pass in Effect.CurrentTechnique.Passes)
            {
                pass.Begin();

#if XBOX360
                // numberOfTotalPrimitives is the number of instances * number of primitives per instance
                FlatRedBallServices.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0,
                    numberOfPrimitivesPerInstance * instanceCount);

#else
//                FlatRedBallServices.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 
  //                  0, 0, 4, 0, numberOfPrimitivesPerInstance);

                FlatRedBallServices.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                    0, 0, numberOfVerticesPerInstance, 0, numberOfPrimitivesPerInstance);



#endif
                pass.End();
            }
            Effect.End();

#if !XBOX360
            FlatRedBallServices.GraphicsDevice.Vertices[0].SetFrequency(1);
#endif
        }


        #region XML Docs
        /// <summary>
        /// Disposes all contained IDisposables.
        /// </summary>
        #endregion
        public void Dispose()
        {
            HardwareInstancedGeometry.Dispose();
            HardwareInstancedIndexBuffer.Dispose();
            VertexDeclaration.Dispose();
            for (int i = 0; i < HardwareInstancedInstanceData.Count; i++)
            {
                HardwareInstancedInstanceData[i].Dispose();
            }
        }
        #endregion
    }
}
