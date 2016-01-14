using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Graphics.Model;
using FlatRedBall.Math;


using System.Collections.ObjectModel;

using FlatRedBall.Content.Model.Helpers;


using GraphicsDevice = Microsoft.Xna.Framework.Graphics.GraphicsDevice;
using ModelMeshXna = Microsoft.Xna.Framework.Graphics.ModelMesh;
using BasicEffect = Microsoft.Xna.Framework.Graphics.BasicEffect;
using Matrix = Microsoft.Xna.Framework.Matrix;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using PrimitiveType = Microsoft.Xna.Framework.Graphics.PrimitiveType;
#if !SILVERLIGHT

using FlatRedBall.Graphics.Lighting;

using VertexPositionNormalTexture = Microsoft.Xna.Framework.Graphics.VertexPositionNormalTexture;
using VertexPositionTexture = Microsoft.Xna.Framework.Graphics.VertexPositionTexture;

using EffectPass = Microsoft.Xna.Framework.Graphics.EffectPass;
using EffectTechnique = Microsoft.Xna.Framework.Graphics.EffectTechnique;
using ModelMeshPartXna = Microsoft.Xna.Framework.Graphics.ModelMeshPart;
using VertexBuffer = Microsoft.Xna.Framework.Graphics.VertexBuffer;
using IndexBuffer = Microsoft.Xna.Framework.Graphics.IndexBuffer;
using BufferUsage = Microsoft.Xna.Framework.Graphics.BufferUsage;
#endif

#if SILVERLIGHT

#elif !WINDOWS_PHONE
using Effect = Microsoft.Xna.Framework.Graphics.Effect;
#else
using Effect = FlatRedBall.Graphics.GenericEffect;
#endif

#if XNA4
using DepthStencilState = Microsoft.Xna.Framework.Graphics.DepthStencilState;
using RasterizerState = Microsoft.Xna.Framework.Graphics.RasterizerState;
using BlendState = Microsoft.Xna.Framework.Graphics.BlendState;
#else
using RenderState = Microsoft.Xna.Framework.Graphics.RenderState;
using CompareFunction = Microsoft.Xna.Framework.Graphics.CompareFunction;
using SamplerState = Microsoft.Xna.Framework.Graphics.SamplerState;
using TextureAddressMode = Microsoft.Xna.Framework.Graphics.TextureAddressMode;
using FillMode = Microsoft.Xna.Framework.Graphics.FillMode;
using CullMode = Microsoft.Xna.Framework.Graphics.CullMode;
using SaveStateMode = Microsoft.Xna.Framework.Graphics.SaveStateMode;

#if !SILVERLIGHT
using EffectParameter = Microsoft.Xna.Framework.Graphics.EffectParameter;
#endif

#endif


namespace FlatRedBall.Graphics.Renderers
{
    #region ModelLayer struct

    internal struct ModelLayer
    {
        #region Fields

        internal int CurrentItem;
        internal Camera Camera;
        internal Layer Layer;
        internal PositionedObjectList<PositionedModel> Models;
        internal bool DrawnThisFrame;

        #endregion

        internal ModelLayer(Camera camera, Layer layer, PositionedObjectList<PositionedModel> models)
        {
            CurrentItem = 0;
            Camera = camera;
            Layer = layer;
            Models = models;
            DrawnThisFrame = false;
        }
    }

    #endregion

    #region LayerDinition Struct

    internal struct LayerDefinition
    {
        public Camera Camera;
        public Layer Layer;

        internal LayerDefinition(Camera camera, Layer layer)
        {
            Camera = camera;
            Layer = layer;
        }
    }

    #endregion

    public class ModelRenderer

#if !SILVERLIGHT
 : IRenderer
#endif
    {
#if !SILVERLIGHT
        #region Fields

        private SortMode mSortMode = SortMode.DistanceAlongForwardVector;

        private Dictionary<LayerDefinition, ModelLayer> mLayers = new Dictionary<LayerDefinition, ModelLayer>();

#if PROFILE
        static internal int ModelsDrawnThisFrame = 0;
#endif


#if XNA4 && WINDOWS_PHONE && USING_EMULATOR && DEBUG
        const bool AllowQuickRender = false;
        int mMaxNumberOfVertices = 60000;
        //VertexBuffer mReusableVertexBuffer;


        //VertexPositionNormalTexture[] mReusableVertexList;
        //ushort[] mReusableIndexList;
        //ushort[] mPerInstanceIndexBuffer = new ushort[6000];
        //IndexBuffer mReusableIndexBuffer;

#else
        const bool AllowQuickRender = false;
#endif







#if XNA4
        DepthStencilState mDepthStencilState = DepthStencilState.Default;

        Dictionary<FlatRedBall.Content.Model.Helpers.ModelMeshPart,
            List<ModelMeshPartRender>> mSortedModelMeshes =
            new Dictionary<Content.Model.Helpers.ModelMeshPart, List<ModelMeshPartRender>>();



#endif

        #endregion

        #region Properties

        public bool DrawSorted
        {
            get { return false; }
        }

        public SortMode SortMode
        {
            get { return mSortMode; }
            set { mSortMode = value; }
        }


        internal static ReadOnlyCollection<LightBase> AvailableLights { get; set; }
        #endregion

        #region Constructor / Initialization

        public ModelRenderer()
        {
            #if XNA4 && WINDOWS_PHONE && USING_EMULATOR && DEBUG
            //mReusableVertexBuffer = new VertexBuffer(FlatRedBallServices.GraphicsDevice,
            //    typeof(VertexPositionNormalTexture), mMaxNumberOfVertices, BufferUsage.WriteOnly);

            //mReusableIndexBuffer = new IndexBuffer(FlatRedBallServices.GraphicsDevice,
            //    typeof(ushort), mMaxNumberOfVertices, BufferUsage.WriteOnly);

            //mReusableVertexList = new VertexPositionNormalTexture[mMaxNumberOfVertices];

            //mReusableIndexList = new ushort[mMaxNumberOfVertices];
            #endif

        }

        #endregion

#endif

        #region Methods

#if !SILVERLIGHT

        public void Prepare(Camera camera)
        {

        }

        public void SetDeviceSettings(Camera camera, RenderMode renderMode)
        {
            #region Set device settings for model drawing

#if XNA4

            Renderer.GraphicsDevice.RasterizerState = RasterizerState.CullClockwise;

			if (camera.ClearsDepthBuffer)
			{
				Renderer.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
			}

            //Renderer.GraphicsDevice.SamplerStates[0] = Microsoft.Xna.Framework.Graphics.SamplerState.LinearWrap;
            Renderer.TextureAddressMode = Microsoft.Xna.Framework.Graphics.TextureAddressMode.Wrap;

            //throw new NotImplementedException();
            // TODO:  Gotta set up the device for rendering models.  We'll do nothing for now
#else
            RenderState renderState = FlatRedBallServices.GraphicsDevice.RenderState;
            //renderState.CullMode = CullMode.CullCounterClockwiseFace;
            renderState.DepthBufferFunction = CompareFunction.Less;

            renderState.DepthBufferEnable = true;
            renderState.DepthBufferWriteEnable = true;
            SamplerState samplerState = FlatRedBallServices.GraphicsDevice.SamplerStates[0];



            samplerState.AddressU = TextureAddressMode.Wrap;
            samplerState.AddressV = TextureAddressMode.Wrap;
#endif


            #endregion
        }

        #region Model Drawing Helpers


        static Matrix[] transforms = new Matrix[100];

        private void DrawModel(Camera camera, PositionedModel model, RenderMode renderMode)
        {
#if PROFILE
            ModelsDrawnThisFrame++;
#endif

            //TimeManager.SumTimeSection("Draw Model Start");

            bool flipped = model.FlipX ^ model.FlipY ^ model.FlipZ;
#if XNA4

            SetCullStateForModel(model, flipped);

#else

            if (model.mDrawWireframe)
                FlatRedBallServices.GraphicsDevice.RenderState.FillMode = FillMode.WireFrame;

            switch (model.FaceCullMode)
            {
                case ModelFaceCullMode.CullClockwiseFace:
                    if (!flipped)
                        Renderer.GraphicsDevice.RenderState.CullMode = CullMode.CullClockwiseFace;
                    else
                        Renderer.GraphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;

                    break;
                case ModelFaceCullMode.CullCounterClockwiseFace:
                    if (!flipped)
                        Renderer.GraphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
                    else
                        Renderer.GraphicsDevice.RenderState.CullMode = CullMode.CullClockwiseFace;

                    break;
                case ModelFaceCullMode.None:
                    Renderer.GraphicsDevice.RenderState.CullMode = CullMode.None;

                    break;
            }



            #region Reset device settings if they've changed - They may change in a shader

            FlatRedBallServices.GraphicsDevice.RenderState.DepthBufferEnable = true;
            FlatRedBallServices.GraphicsDevice.RenderState.DepthBufferWriteEnable = true;

            #endregion
#endif
            //TimeManager.SumTimeSection("Set depth states");
#if XNA4

            // I don't think we need to worry about vertex declarations 
#else
            #region Set Vertex Declaration

            if (model.XnaModel != null)
            {
                if (model.XnaModel.Meshes.Count != 0 &&
                    model.XnaModel.Meshes[0].MeshParts.Count != 0)
                {
                    // The assumption is that each mesh part is using the same vertex declaration for the
                    // whole model.  Instead of setting the vertex declaration in the DrawMeshPart method we'll
                    // set it up here to save on the number of method calls on the 360.



                    FlatRedBallServices.GraphicsDevice.VertexDeclaration =
                        model.XnaModel.Meshes[0].MeshParts[0].VertexDeclaration;

                }
            }
            #endregion
#endif
            //TimeManager.SumTimeSection("Set vertex declaration");

            #region Set Point Light



            // Find the closest light
            //int lightIndex = 0;
            //float distance;

            Vector3 meshCenter = (model == null) ? Vector3.Zero : model.Position;

            #endregion

            #region Draw Model
#if WINDOWS_PHONE
            if (model.XnaModel != null)
            {

                model.XnaModel.CopyAbsoluteBoneTransformsTo(transforms);

                Matrix transformationMatrixFlippedAsNeeded = model.TransformationMatrix;

                foreach (ModelMeshXna mesh in model.XnaModel.Meshes)
                {
                    for( int iCurEffect = 0; iCurEffect < mesh.Effects.Count; ++iCurEffect )
                    {
                        GenericEffect effect = new GenericEffect( mesh.Effects[ iCurEffect ] );
                        ApplyColorOperation(model, effect);


                        effect.EnableDefaultLighting();

                        // Set this to false and all is fixed magically!
                        effect.VertexColorEnabled = false;

                        SpriteManager.Camera.SetDeviceViewAndProjection(effect, false);

                        // World can be used to set the mesh's transform
                        effect.World = transforms[mesh.ParentBone.Index] * transformationMatrixFlippedAsNeeded;
                    }


                    mesh.Draw();


                }
            }
            else if (model.CustomModel != null)
            {
                RenderCustomModel(model);
            }
#else
            #region If using Custom Effect

            if (model.CustomEffect != null)
            {

                // Set technique here if using custom effect
                model.CustomEffect.SetParameterValues();
                EffectTechnique technique = model.EffectCache.GetTechnique(
                    model.CustomEffect.Effect, Renderer.EffectTechniqueNames[(int)renderMode]);
                if (technique == null && renderMode == RenderMode.Default)
                {
                    technique = model.CustomEffect.Effect.Techniques[0];
                }

                if (technique != null)
                {
                    // Draw meshes only if custom effect has the required technique
                    model.CustomEffect.Effect.CurrentTechnique = technique;
                    DrawModelMeshes(camera, model, renderMode);
                }

            }



            #endregion
            else if (model.CustomModel != null)
            {
                RenderCustomModel(model);

            }
            else
            {
                // Just draw the meshes
                DrawModelMeshes(camera, model, renderMode);
            }
#endif
            #endregion

            //TimeManager.SumTimeSection("DrawModelMeshes");

            if (model.mDrawWireframe)
            {
#if XNA4
                throw new NotImplementedException();
#else
                FlatRedBallServices.GraphicsDevice.RenderState.FillMode = FillMode.Solid;
#endif
            }
        }

#if XNA4
        private static void SetCullStateForModel(PositionedModel model, bool flipped)
        {
            switch (model.FaceCullMode)
            {
                case ModelFaceCullMode.CullClockwiseFace:
                    if (!flipped)
                        Renderer.GraphicsDevice.RasterizerState = RasterizerState.CullClockwise;
                    else
                        Renderer.GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;

                    break;
                case ModelFaceCullMode.CullCounterClockwiseFace:
                    if (!flipped)
                        Renderer.GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
                    else
                        Renderer.GraphicsDevice.RasterizerState = RasterizerState.CullClockwise;

                    break;
                case ModelFaceCullMode.None:
                    Renderer.GraphicsDevice.RasterizerState = RasterizerState.CullNone;

                    break;
            }
        }
#endif


        private static void RenderCustomModel(PositionedModel model)
        {

            //There are three things that need to happen here
            // The first one is common to all CustomModels
            // The second is code that is unique to unskinned CustomModels
            // The third is code that is unique to skinned CustomModels



            bool isSkinned = model.CustomModel.IsSkinnedMesh;
            bool isAnimated = model.AnimationController != null && model.AnimationController.Matrices != null;

            if (!isSkinned) //unskinned
            {
                if (isAnimated)
                {
                    int matrixCount = model.AnimationController.Matrices.Length;

                    for (int i = 0; i < matrixCount; i++)
                    {
                        transforms[i] = model.AnimationController.Matrices[i].Transform;
                    }
                }
            }
            else //Skinned
            {
                if (isAnimated)
                {
                    model.CustomModel.SetBones(model.AnimationController.Matrices);
                }
                else
                {
                    model.CustomModel.ResetVertexBuffers();
                }
            }

            FlatRedBall.Content.Model.Helpers.CustomModel customModel = model.CustomModel;
            int meshCount = customModel.Meshes.Count;
            FlatRedBall.Content.Model.Helpers.ModelMesh mesh;
#if !WINDOWS_PHONE
            BasicEffect effect;
#else 
            GenericEffect effect;
#endif
            if (customModel.SharesEffects)
            {

#if WINDOWS_PHONE
                effect = customModel.Meshes[0].MeshParts[0].Effect;
#else
                effect = (BasicEffect)customModel.Meshes[0].MeshParts[0].Effect;
#endif
                PrepareEffectForRendering(model, effect);
            }
            
            Matrix transformationMatrixFlippedAsNeeded = model.TransformationMatrix;
            for (int meshIndex = 0; meshIndex < meshCount; meshIndex++)
            {
                mesh = customModel.Meshes[meshIndex];

                if (!isSkinned && isAnimated && mesh.BoneIndex == -1)
                {
                    int boneCount = model.AnimationController.Matrices.Length;
                    for (int boneIndex = 0; boneIndex < boneCount; boneIndex++)
                    {
                        FlatRedBall.Graphics.Animation3D.Animation3DJoint boneInfo = model.AnimationController.Matrices[boneIndex];
                        if (mesh.Name == boneInfo.Name)
                        {
                            mesh.BoneIndex = boneIndex;
                        }
                    }
                }


#if !WINDOWS_PHONE && XNA4
                //foreach(ModelMeshPart meshPart in mesh.MeshParts)
                for(int partIndex = 0; partIndex < mesh.MeshParts.Count; partIndex++)
                {
                    effect = mesh.MeshParts[partIndex].Effect as BasicEffect;

#else
                for (int effectIndex = 0; effectIndex < mesh.Effects.Count; effectIndex++)
                {
#if !WINDOWS_PHONE || !XNA4
                    effect = (BasicEffect)mesh.Effects[effectIndex];
#else
                    effect = mesh.Effects[effectIndex];
#endif
#endif
                    if (!customModel.SharesEffects)
                    {
                        PrepareEffectForRendering(model, effect);
                    }

                    if (!isSkinned && isAnimated && mesh.BoneIndex != -1)
                    {
                        effect.World = transforms[mesh.BoneIndex];
                        effect.World *= transformationMatrixFlippedAsNeeded;
                    }
                    else
                        effect.World = transformationMatrixFlippedAsNeeded;



                }
                mesh.Draw(model.RenderOverrides);
            }


#if false


            //Copy bone matrices.
            bool hasBones = model.CustomModel.Bones != null;
            Matrix[] transforms = null;


            if (model.AnimationController != null && model.AnimationController.Matrices != null)
            {
                model.CustomModel.SetBones(model.AnimationController.Matrices);
            }
            else
            {
                model.CustomModel.ResetVertexBuffers();
            }

#if XNA4                
                if (hasBones)
                {
                    transforms = new Matrix[model.CustomModel.Bones.Count];
                    //model.CustomModel.CopyAbsoluteBoneTransformsTo(transforms);
                    for (int i = 0; i < model.CustomModel.Bones.Count; i++)
                    {
                        transforms[i] = model.AnimationController.Matrices[i].Transform;
                    }
                }

                FlatRedBallServices.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
#endif
            FlatRedBall.Content.Model.Helpers.CustomModel customModel = model.CustomModel;
            int meshCount = customModel.Meshes.Count;
            FlatRedBall.Content.Model.Helpers.ModelMesh mesh;
            BasicEffect effect;
            for(int meshIndex = 0; meshIndex < meshCount; meshIndex++)
            {
                mesh = customModel.Meshes[meshIndex];
                for(int effectIndex = 0; effectIndex < mesh.Effects.Count; effectIndex++)
                {
                    effect = (BasicEffect)mesh.Effects[effectIndex];
                    if (AvailableLights != null && AvailableLights.Count > 0)
                    {
                        effect.EnableDefaultLighting();
                        //Start by turning off all the lights.
                        effect.DirectionalLight0.Enabled = effect.DirectionalLight1.Enabled = effect.DirectionalLight2.Enabled = false;

                        //This tracks which directional light in the shader we are working with.
                        int effectLightIndex = 0;
                        //Loop through all lights
                        for (int i = 0; i < AvailableLights.Count; i++)
                        {

                            LightBase baseLight = AvailableLights[i];

                            if (baseLight.Enabled)
                            {

                                if (baseLight is AmbientLight)
                                {
                                    SetAmbientLight(effect, baseLight);
                                }
                                else
                                {
#if XNA4

                                    Microsoft.Xna.Framework.Graphics.DirectionalLight directionalLight;
                                    if (effectLightIndex == 0)
                                        directionalLight = effect.DirectionalLight0;
                                    else if (effectLightIndex == 1)
                                        directionalLight = effect.DirectionalLight1;
                                    else
                                        directionalLight = effect.DirectionalLight2;

                                    SetDirectionalLight(directionalLight, baseLight, model.Position);
                                    effectLightIndex++;
#endif
                                }
                            }
                        }

                    }
                    else
                    {
                        effect.EnableDefaultLighting();
                        effect.DirectionalLight0.Enabled = effect.DirectionalLight1.Enabled = effect.DirectionalLight2.Enabled = false;
                        effect.AmbientLightColor = Vector3.Zero;
                    }
                    // Set this to false and all is fixed magically!
                    effect.VertexColorEnabled = false;

                    ApplyColorOperation(model, effect);

                    effect.World = model.TransformationMatrix;
                    if (hasBones && mesh.ParentBone != null)
                    {
                        effect.World *= transforms[mesh.ParentBone.Index];
                    }

                    SpriteManager.Camera.SetDeviceViewAndProjection(effect, false);
                }
                mesh.Draw(model.RenderOverrides);
            }
#endif
        }

#if WINDOWS_PHONE
        private static void PrepareEffectForRendering(PositionedModel model, GenericEffect effect)
#else
        private static void PrepareEffectForRendering(PositionedModel model, BasicEffect effect)
#endif
        {
            ApplyLighting(model, effect);

            // Set this to false and all is fixed magically!
            effect.VertexColorEnabled = false;

            ApplyColorOperation(model, effect);
            SpriteManager.Camera.SetDeviceViewAndProjection(effect, false);
        }

        private void DrawModelMeshes(Camera camera, PositionedModel model, RenderMode renderMode)
        {
            switch (model.LodType)
            {
                case PositionedModel.LodTypes.Mesh:
                    #region Mesh-Based LOD

                    // Get the mesh index
                    int l = model.LodMeshOrder.Count - 1;
                    float dist = Vector3.Distance(camera.Position, model.Position);
                    while (model.LodMeshOrder[l].LodDistance > dist && l > 0)
                    {
                        l--;
                    }

                    // Set parameters and draw the mesh
                    ModelMeshXna lodMesh = model.XnaModel.Meshes[model.LodMeshOrder[l].MeshId];

                    // Draw the mesh
                    DrawMesh(camera, model, model.LodMeshOrder[l].MeshId, renderMode);

                    #endregion
                    break;
                case PositionedModel.LodTypes.None:
                default:
                    #region No LOD (Draw whole model)

                    int meshCount = model.XnaModel.Meshes.Count;

                    for (int i = 0; i < meshCount; i++)
                    {
                        DrawMesh(camera, model, i, renderMode);
                    }

                    #endregion
                    break;
            }
        }

        private void DrawMesh(Camera camera, PositionedModel model, int meshIndex, RenderMode renderMode)
        {

#if XNA4
            throw new NotImplementedException();
#else
            //TimeManager.SumTimeSection("Start of DrawMesh");

            ModelMeshXna mesh = model.XnaModel.Meshes[meshIndex];



            if (model.CustomEffect != null)
            {
            #region Draw using custom effect

                model.CustomEffect.Effect.Begin(SaveStateMode.None);

                FlatRedBallServices.GraphicsDevice.Indices = mesh.IndexBuffer;

                for (int i = 0; i < model.CustomEffect.Effect.CurrentTechnique.Passes.Count; i++)
                {
                    EffectPass pass = model.CustomEffect.Effect.CurrentTechnique.Passes[i];

                    pass.Begin();

                    for (int partIndex = 0; partIndex < mesh.MeshParts.Count; partIndex++)
                    {
                        ModelMeshPartXna part = mesh.MeshParts[partIndex];

                        // Set parameters
                        //SetPartEffectParameters(model, model.CustomEffect.Effect, meshIndex, part);

            #region Set the world matrix for the World Effect Parameter

                        // Get parameters
                        EffectParameter worldParameter = model.mEffectCache[model.CustomEffect.Effect, EffectCache.EffectParameterNames[(int)EffectCache.EffectParameterNamesEnum.World]];
                        if (worldParameter != null) worldParameter.SetValue(model.mBoneWorldTransformations[meshIndex]);


            #endregion

                        model.CustomEffect.Effect.CommitChanges();

                        // Just draw the part - the effect has been taken care of already
                        DrawMeshPart(mesh, part);
                    }

                    pass.End();
                }
                model.CustomEffect.Effect.End();

            #endregion
            }
            else
            {
            #region Draw parts with their own effects

                Effect effect;

                FlatRedBallServices.GraphicsDevice.Indices = mesh.IndexBuffer;

                for (int i = 0; i < mesh.MeshParts.Count; i++)
                {
                    ModelMeshPartXna part = mesh.MeshParts[i];

                    effect = part.Effect;

                    //TimeManager.SumTimeSection("Start of meshpart loop");

                    // Check if the effect has the required technique
                    EffectTechnique technique = null;// = model.EffectCache.GetTechnique(
                    //effect, Renderer.EffectTechniqueNames[(int)renderMode]);

                    //if (Renderer.LightingEnabled)
                    //{
                    //    switch (model.LightingMethod)
                    //    {
                    //        case PositionedModel.LightingType.None:
                    //            technique = model.EffectCache.GetTechnique(effect, "Default");
                    //            break;

                    //        case PositionedModel.LightingType.Ambient:
                    //            technique = model.EffectCache.GetTechnique(effect, "AmbientOnly");
                    //            break;

                    //        case PositionedModel.LightingType.Diffuse:
                    //            technique = model.EffectCache.GetTechnique(effect, "DiffuseOnly");
                    //            break;

                    //        case PositionedModel.LightingType.Specular:
                    //            technique = model.EffectCache.GetTechnique(effect, "SpecularOnly");
                    //            break;

                    //        case (PositionedModel.LightingType.Ambient | PositionedModel.LightingType.Diffuse):
                    //            technique = model.EffectCache.GetTechnique(effect, "AmbientAndDiffuse");
                    //            break;

                    //        case (PositionedModel.LightingType.Ambient | PositionedModel.LightingType.Specular):
                    //            technique = model.EffectCache.GetTechnique(effect, "AmbientAndSpecular");
                    //            break;

                    //        case (PositionedModel.LightingType.Diffuse | PositionedModel.LightingType.Specular):
                    //            technique = model.EffectCache.GetTechnique(effect, "DiffuseAndSpecular");
                    //            break;

                    //        case PositionedModel.LightingType.All:
                    //            technique = model.EffectCache.GetTechnique(effect, "AmbientDiffuseSpecular");
                    //            break;
                    //    }
                    //}
                    //else
                    {
                        technique = model.EffectCache.GetTechnique(effect, "Default");
                    }


                    if (technique == null && renderMode == RenderMode.Default)
                    {
                        technique = model.EffectCache.GetTechnique(effect, "Default");// effect.Techniques[0];
                    }

                    //TimeManager.SumTimeSection("Get technique");

                    if (technique != null)
                    {

                        // Set the technique and draw the part
                        effect.CurrentTechnique = technique;

                        if (!model.mHasClonedEffects)
                        {
            #region Set the world matrix for the World Effect Parameter

                            // Get parameters
                            EffectParameter worldParameter = model.mEffectCache[effect, EffectCache.EffectParameterNames[(int)EffectCache.EffectParameterNamesEnum.World]];
                            if (worldParameter != null) worldParameter.SetValue(model.mBoneWorldTransformations[meshIndex]);


            #endregion

                        }

                        /// This code, is the old Drawing code. This is all done in the mesh.Draw() method and is not needed. 
                        /// However vic would like to keep this code here, incase something isn't right after release.
                        /// then we can go back and return it to it's original state.

            #region OldCode

                        //// Pulled this out of the foreach loop since it looks like it only
                        //// has to be done once
                        //FlatRedBallServices.GraphicsDevice.Vertices[0].SetSource(
                        //    mesh.VertexBuffer, part.StreamOffset, part.VertexStride);

                        //TimeManager.SumTimeSection("Set effect Parameters");

                        // Draw the part
                        //effect.Begin(SaveStateMode.None);

                        //for (int passIndex = 0; passIndex < effect.CurrentTechnique.Passes.Count; passIndex++)
                        //{
                        //    EffectPass pass = effect.CurrentTechnique.Passes[passIndex];
                        //    pass.Begin();

                        //    // Contents of DrawMeshPart are below and just outside
                        //    // of the foreach for performance reasons.
                        //    // DrawMeshPart(mesh, part);

                        //    //if (part.NumVertices > 0)
                        //    //{

                        //    //    FlatRedBallServices.GraphicsDevice.DrawIndexedPrimitives(
                        //    //        PrimitiveType.TriangleList,
                        //    //        part.BaseVertex,
                        //    //        0,
                        //    //        part.NumVertices,
                        //    //        part.StartIndex,
                        //    //        part.PrimitiveCount);

                        //    //}                            

                        //    pass.End();

                        //}

                        //effect.End();

                        mesh.Draw(SaveStateMode.None);

            #endregion


                        //TimeManager.SumTimeSection("DrawMeshPart");
                    }
                }

            #endregion
            }


#endif

        }

        private void DrawMeshPart(ModelMeshXna mesh, ModelMeshPartXna part)
        {
#if XNA4
            throw new NotImplementedException();
#else
            // Draw part
            FlatRedBallServices.GraphicsDevice.Vertices[0].SetSource(
                mesh.VertexBuffer, part.StreamOffset, part.VertexStride);

            FlatRedBallServices.GraphicsDevice.DrawIndexedPrimitives(
                PrimitiveType.TriangleList,
                part.BaseVertex,
                0,
                part.NumVertices,
                part.StartIndex,
                part.PrimitiveCount);
#endif
        }

        #endregion


        public void Draw(Camera camera, Layer layer, RenderMode renderMode)
        {
            PositionedObjectList<PositionedModel> listToDraw = null;
            List< RenderOverrideBatch > batchesToDraw = null;

            if (layer != null)
            {
                listToDraw = layer.mModels;
            }
            else
            {
                listToDraw = ModelManager.mDrawnModels;
                if (ModelManager.mRenderOverrideBatches != null && ModelManager.mRenderOverrideBatches.Count > 0)
                {
                    batchesToDraw = ModelManager.mRenderOverrideBatches;
                }
            }

            if (listToDraw.Count == 0 && (batchesToDraw == null || batchesToDraw.Count == 0))
            {
                return;
            }

            //TimeManager.TimeSection("1");
#if XNA4
            foreach (KeyValuePair<FlatRedBall.Content.Model.Helpers.ModelMeshPart, List<ModelMeshPartRender>> kvp in mSortedModelMeshes)
            {
                kvp.Value.Clear();
            }
#endif


            // Draw the positioned models
            for (int i = 0; i < listToDraw.Count; i++)
            {
                PositionedModel model = listToDraw[i];

                DrawModelOrAddToSortedDictionary(camera, renderMode, model);
            }



            // Draw the render override batches if there are any
            if (batchesToDraw != null)
            {
                for (int iCurBatch = 0; iCurBatch < batchesToDraw.Count; iCurBatch++)
                {
                    batchesToDraw[iCurBatch].Draw(camera);
                }
            }
            //TimeManager.TimeSection("2");

            #region Render everything in the dictionary

#if XNA4
            //if (AllowQuickRender)
            //{
            //    lastEffect = null;
            //    foreach (KeyValuePair<FlatRedBall.Content.Model.Helpers.ModelMeshPart, List<ModelMeshPartRender>> kvp in mSortedModelMeshes)
            //    {
            //        RenderMmprList(kvp.Value);
            //    }
            //}
#endif
            //TimeManager.TimeSection("3");
            #endregion
        }
#if XNA4
        //GenericEffect lastEffect = null;
#endif

//        private void RenderMmprList(List<ModelMeshPartRender> list)
//        {

//#if XNA4 && WINDOWS_PHONE && USING_EMULATOR && DEBUG
//            if (list.Count != 0)
//            {
//                GenericEffect effectToUse = list[0].ModelMeshPart.Effect;
//                FlatRedBall.Content.Model.Helpers.ModelMeshPart modelMeshPart =
//                    list[0].ModelMeshPart;

//                GraphicsDevice graphicsDevice = Renderer.GraphicsDevice;

//                // temporary to test things out:
//                Renderer.GraphicsDevice.RasterizerState = RasterizerState.CullNone;


//                // TODO:  Apply lighting
//                if (!LightManager.EnableLighting)
//                {
//                    effectToUse.LightingEnabled = false;

//                }


//                // This causes problems
//                //                effectToUse.VertexColorEnabled = true;

//                /* 
//                                // TODO:  ApplyColorOperation
//                                effectToUse.EmissiveColor = Vector3.Zero;
//                                effectToUse.DiffuseColor = Vector3.One;
//                                */


//                int indexIncrementPerModelMeshPart = modelMeshPart.PrimitiveCount * 3;




//                for (int i = 0; i < indexIncrementPerModelMeshPart; i++)
//                {
//                    mPerInstanceIndexBuffer[i] = modelMeshPart.mIndexList[i + modelMeshPart.StartIndex];
//                }

//                // Assumes 1 pass
//                if( effectToUse.GetCurrentTechnique(true).Passes.Count > 1 )
//                {
//                    throw new InvalidOperationException("The new efficient rendering system only supports effects with 1 render pass");
//                }


//                BlendState previousState = FlatRedBallServices.GraphicsDevice.BlendState;


//                EffectPass effectPass;

//                if (lastEffect != effectToUse)
//                {
//                    SpriteManager.Camera.SetDeviceViewAndProjection(effectToUse, false);
//                    effectPass = effectToUse.GetCurrentTechnique( true ).Passes[0];
//                    effectPass.Apply();
//                    lastEffect = effectToUse;
//                }

//                int reusableIndex = 0;
//                VertexPositionNormalTexture tempVertex = new VertexPositionNormalTexture();
//                Vector3 tempVector = new Vector3();
//                Matrix matrix;


//                int mmprIndex = 0;

//                VertexPositionNormalTexture[] verticesToCopy = modelMeshPart.CpuVertices;


//                foreach (ModelMeshPartRender mmpr in list)
//                {
//                    if (mmpr.PreCalculatedVerts != null)
//                    {
//                        mmpr.PreCalculatedVerts.CopyTo(
//                            mReusableVertexList, reusableIndex);
//                        reusableIndex += mmpr.PreCalculatedVerts.Length;


//                        //for (int i = 0; i < verticesToCopy.Length; i++)
//                        //{

//                        //    mReusableVertexList[reusableIndex] = mmpr.PreCalculatedVerts[i];
//                        //    reusableIndex++;
//                        //}
//                    }
//                    else
//                    {
//                        matrix = mmpr.World;

//                        for (int i = 0; i < verticesToCopy.Length; i++)
//                        {

//                            tempVertex = verticesToCopy[i];
//                            tempVector.X = tempVertex.Position.X;
//                            tempVector.Y = tempVertex.Position.Y;
//                            tempVector.Z = tempVertex.Position.Z;

//                            MathFunctions.TransformVector(ref tempVector, ref matrix);

//                            tempVertex.Position.X = tempVector.X;
//                            tempVertex.Position.Y = tempVector.Y;
//                            tempVertex.Position.Z = tempVector.Z;

//                            mReusableVertexList[reusableIndex] = tempVertex;
//                            reusableIndex++;
//                        }
//                    }

//                    ushort extraAmountDestination = (ushort)(mmprIndex * indexIncrementPerModelMeshPart);
//                    ushort extraAmountSource = (ushort)(mmprIndex * verticesToCopy.Length);

//                    for (int i = indexIncrementPerModelMeshPart - 1; i > -1; i--)
//                    //for (int i = 0; i < indexIncrementPerModelMeshPart; i++)
//                    {

//                        mReusableIndexList[i + extraAmountDestination] = (ushort)(mPerInstanceIndexBuffer[i] + extraAmountSource);

//                    }


//                    //graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, modelMeshPart.numVertices, modelMeshPart.startIndex, modelMeshPart.primitiveCount);

//                    mmprIndex++;


//                    /*
//                    if (mmpr.RenderOverrides != null)
//                    {
//                        for (int i = 0; i < mmpr.RenderOverrides.Count; i++)
//                        {
//                            mmpr.RenderOverrides[i].Apply(ref modelMeshPart.vertexBuffer);
//                            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, modelMeshPart.numVertices, modelMeshPart.startIndex, modelMeshPart.primitiveCount);
//                        }
//                    }
//                    */


//                }

//                int numberOfVerts = reusableIndex;
//                int numberOfIndices = mmprIndex * indexIncrementPerModelMeshPart;
//                int numberOfPrimitives = numberOfIndices / 3;

//                mReusableVertexBuffer.SetData<VertexPositionNormalTexture>(mReusableVertexList, 0, numberOfVerts);
//                mReusableIndexBuffer.SetData<ushort>(mReusableIndexList, 0, numberOfIndices);

//                FlatRedBallServices.GraphicsDevice.SetVertexBuffer(mReusableVertexBuffer);
//                FlatRedBallServices.GraphicsDevice.Indices = mReusableIndexBuffer;


//                graphicsDevice.DrawIndexedPrimitives(
//                    PrimitiveType.TriangleList, 0, 0, numberOfVerts, 0, numberOfPrimitives);





//                FlatRedBallServices.GraphicsDevice.SetVertexBuffer(null);
//                FlatRedBallServices.GraphicsDevice.Indices = null;

//                FlatRedBallServices.GraphicsDevice.BlendState = previousState;
//            }
//#else
//            throw new NotImplementedException();
//#endif
//        }

        private void DrawModelOrAddToSortedDictionary(Camera camera, RenderMode renderMode, PositionedModel model)
        {

            if (model.Visible && (camera.CameraModelCullMode == CameraModelCullMode.None || camera.IsModelInView(model)))
            {

                if (model.AnimationController == null && model.CustomModel != null && AllowQuickRender)
                {
#if XNA4
                    Matrix transformationMatrixFlippedAsNeeded = model.TransformationMatrix;

                    foreach (FlatRedBall.Content.Model.Helpers.ModelMesh mesh in model.CustomModel.Meshes)
                    {
                        foreach (FlatRedBall.Content.Model.Helpers.ModelMeshPart meshPart in mesh.MeshParts)
                        {
                            // It's okay if we call this over and over,
                            // it has a bool which it uses internally to
                            // make sure there isn't unnecessary copying of
                            // verts.
                            meshPart.ResetVertexBuffer();

                            ModelMeshPartRender mmpr = new ModelMeshPartRender();
                            mmpr.ModelMeshPart = meshPart;

                            if (model.RenderOverrides != null && model.RenderOverrides.ContainsKey(meshPart))
                            {
                                mmpr.RenderOverrides = model.RenderOverrides[meshPart];
                            }
                            mmpr.World = transformationMatrixFlippedAsNeeded;

                            if (!model.IsAutomaticallyUpdated)
                            {
                                mmpr.PreCalculatedVerts = model.mPrecalculatedVertices[meshPart];
                            }

                            if (mSortedModelMeshes.ContainsKey(meshPart))
                            {
                                mSortedModelMeshes[meshPart].Add(mmpr);
                            }
                            else
                            {

                                List<ModelMeshPartRender> newList = new List<ModelMeshPartRender>();
                                newList.Add(mmpr);

                                mSortedModelMeshes.Add(meshPart, newList);
                            }
                        }
                    }
#endif
                }
                else
                {
                    DrawModel(camera, model, renderMode);
                }
            }
        }

#endif


        internal void ClearRenderingDictionary()
        {
#if XNA4
            mSortedModelMeshes.Clear();
#endif
        }


#if !SILVERLIGHT



        public float GetNextObjectDepth(Camera camera, Layer layer)
        {
            return 0f;
        }

        public bool HasObjectsLeftToDraw(Camera camera, Layer layer)
        {
            LayerDefinition def = new LayerDefinition(camera, layer);

            if (mLayers.ContainsKey(def))
                return mLayers[def].DrawnThisFrame;
            else
                return true;
        }

        public void RemoveLayer(Camera camera, Layer layer)
        {
            LayerDefinition def = new LayerDefinition(camera, layer);

            if (mLayers.ContainsKey(def)) mLayers.Remove(def);
        }

        #region Lighting Helpers

#if XNA4
        protected static void SetDirectionalLight(Microsoft.Xna.Framework.Graphics.DirectionalLight directionalLight, LightBase light, Vector3 objectPosition)
        {
            directionalLight.DiffuseColor = light.DiffuseColor;
            directionalLight.SpecularColor = light.SpecularColor;
            directionalLight.Direction = light.GetDirectionTo(objectPosition);
            directionalLight.Enabled = true;
        }
#endif

        protected static void SetAmbientLight(Microsoft.Xna.Framework.Graphics.BasicEffect effect, LightBase light)
        {
            effect.AmbientLightColor = light.DiffuseColor;
        }

#if XNA4
        protected static void SetAmbientLight(GenericEffect effect, LightBase light)
        {
            effect.AmbientLightColor = light.DiffuseColor;
        }
#endif
        #endregion


#if !WINDOWS_PHONE
        private static void ApplyColorOperation(PositionedModel model, BasicEffect effect)
#else
        private static void ApplyColorOperation(PositionedModel model, GenericEffect effect)
#endif
        {
            switch (model.ColorOperation)
            {
                case ColorOperation.None:
                    effect.EmissiveColor = Vector3.Zero;
                    effect.DiffuseColor = Vector3.One;
                    effect.Alpha = model.Alpha;
                    effect.TextureEnabled = true;
                    break;
                case ColorOperation.Add:
                    effect.EmissiveColor = new Vector3(model.Red, model.Green, model.Blue);
                    effect.DiffuseColor = Vector3.Zero;

                    effect.Alpha = model.Alpha;
                    effect.TextureEnabled = true;
                    break;
                case ColorOperation.Modulate:
                    effect.EmissiveColor = Vector3.Zero;
                    effect.DiffuseColor = new Vector3(model.Red, model.Green, model.Blue);
                    effect.Alpha = model.Alpha;
                    effect.TextureEnabled = true;
                    break;

                case ColorOperation.Color:
                    effect.EmissiveColor = Vector3.Zero;
                    effect.DiffuseColor = new Vector3(model.Red, model.Green, model.Blue);
                    effect.Alpha = model.Alpha;
                    effect.TextureEnabled = false;
                    break;
                default:
                    throw new NotImplementedException("Models do not support the color operation " + model.ColorOperation);
                    //break;

            }
        }

        private static void ApplyLighting(PositionedModel model, BasicEffect effect)
        {
            if (!LightManager.EnableLighting)
            {
                effect.LightingEnabled = false;

            }
            else if (AvailableLights != null && AvailableLights.Count > 0)
            {
                //effect.EnableDefaultLighting();
                //Start by turning off all the lights.
                effect.LightingEnabled = true;
                effect.DirectionalLight0.Enabled = effect.DirectionalLight1.Enabled = effect.DirectionalLight2.Enabled = false;

                //This tracks which directional light in the shader we are working with.
                int effectLightIndex = 0;
                //Loop through all lights
                for (int i = 0; i < AvailableLights.Count; i++)
                {

                    LightBase baseLight = AvailableLights[i];

                    if (baseLight.Enabled)
                    {

                        if (baseLight is AmbientLight)
                        {
                            SetAmbientLight(effect, baseLight);
                        }
                        else
                        {
#if XNA4
                            Microsoft.Xna.Framework.Graphics.DirectionalLight directionalLight;
                            if (effectLightIndex == 0)
                                directionalLight = effect.DirectionalLight0;
                            else if (effectLightIndex == 1)
                                directionalLight = effect.DirectionalLight1;
                            else
                                directionalLight = effect.DirectionalLight2;

                            SetDirectionalLight(directionalLight, baseLight, model.Position);
                            effectLightIndex++;
#endif
                        }
                    }
                }

            }
            else
            {
                effect.EnableDefaultLighting();
                effect.DirectionalLight0.Enabled = effect.DirectionalLight1.Enabled = effect.DirectionalLight2.Enabled = false;
                effect.AmbientLightColor = Vector3.Zero;
            }
        }


#if XNA4
        private static void ApplyLighting(PositionedModel model, GenericEffect effect)
        {
            if (!LightManager.EnableLighting)
            {
                effect.LightingEnabled = false;

            }
            else if (AvailableLights != null && AvailableLights.Count > 0)
            {
                //effect.EnableDefaultLighting();
                //Start by turning off all the lights.
                effect.LightingEnabled = true;
                effect.DirectionalLight0.Enabled = effect.DirectionalLight1.Enabled = effect.DirectionalLight2.Enabled = false;

                //This tracks which directional light in the shader we are working with.
                int effectLightIndex = 0;
                //Loop through all lights
                for (int i = 0; i < AvailableLights.Count; i++)
                {

                    LightBase baseLight = AvailableLights[i];

                    if (baseLight.Enabled)
                    {

                        if (baseLight is AmbientLight)
                        {
                            SetAmbientLight(effect, baseLight);
                        }
                        else
                        {
                            Microsoft.Xna.Framework.Graphics.DirectionalLight directionalLight;
                            if (effectLightIndex == 0)
                                directionalLight = effect.DirectionalLight0;
                            else if (effectLightIndex == 1)
                                directionalLight = effect.DirectionalLight1;
                            else
                                directionalLight = effect.DirectionalLight2;

                            SetDirectionalLight(directionalLight, baseLight, model.Position);
                            effectLightIndex++;
                        }
                    }
                }

            }
            else
            {
                effect.EnableDefaultLighting();
                effect.DirectionalLight0.Enabled = effect.DirectionalLight1.Enabled = effect.DirectionalLight2.Enabled = false;
                effect.AmbientLightColor = Vector3.Zero;
            }
        }
#endif

#endif

        #endregion

    }
}