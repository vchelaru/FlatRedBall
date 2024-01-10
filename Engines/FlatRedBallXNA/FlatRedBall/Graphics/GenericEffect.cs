using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

#if SILVERLIGHT
using System.Windows.Media.Effects;
#endif

namespace FlatRedBall.Graphics
{
    public class GenericEffect
        : IEffectFog, IEffectLights, IEffectMatrices, IDisposable
    {
        #region Enum
        public enum DefaultShaderType
        {
            Determine,
            Basic,
            DualTexture,
            EnvironmentMapped,
            Skinned,
            AlphaTest
        }
        #endregion

        /// <summary>
        /// Private members
        /// </summary>
        #region Fields
        BasicEffect mBasicEffect;
        AlphaTestEffect mAlphaTestEffect;

#if !MONOGAME
        DualTextureEffect mDualTextureEffect;
        SkinnedEffect mSkinnedEffect;
        EnvironmentMapEffect mEnvironmentMapEffect;
#endif
        Dictionary<string, float> mPrecedenceTable = new Dictionary<string,float>();
        Matrix[] mBoneTransforms;
        TextureAddressMode mTextureAddressMode;

        #region Default Precedence Values
        const float DefaultBasicPrecedence = 0.0f;
        const float DefaultDualTexturePrecedence = .25f;
        const float DefaultEnvironmentMappingPrecedence = .5f;
        const float DefaultSkinningPrecedence = 1.0f;
        #endregion
        #endregion

        /// <summary>
        /// Properties inherited from the interfaces
        /// </summary>
        #region InterfaceProperties
        public Matrix World
        {
            get;
            set;
        }
        public Matrix View
        {
            get;
            set;
        }
        public Matrix Projection
        {
            get;
            set;
        }
        public DirectionalLight DirectionalLight0
        {
            get;
            set;
        }
        public DirectionalLight DirectionalLight1
        {
            get;
            set;
        }
        public DirectionalLight DirectionalLight2
        {
            get;
            set;
        }
        public Vector3 AmbientLightColor
        {
            get;
            set;
        }

        private bool _lightingEnabled;

        public bool LightingEnabled
        {
            get { return _lightingEnabled; }
            set
            {
#if MONOGAME
                if (value)
                    throw new NotImplementedException();
#else
                _lightingEnabled = value;
#endif
            }
        }

        public Vector3 FogColor
        {
            get;
            set;
        }
        public float FogStart
        {
            get;
            set;
        }
        public float FogEnd
        {
            get;
            set;
        }
        public bool FogEnabled
        {
            get;
            set;
        }
        public float Alpha
        {
            get;
            set;
        }
        public Vector3 EmissiveColor
        {
            get;
            set;
        }
        public Vector3 DiffuseColor
        {
            get;
            set;
        }
        public Vector3 SpecularColor
        {
            get;
            set;
        }
        public float SpecularPower
        {
            get;
            set;
        }
        public Texture2D Texture
        {
            get;
            set;
        }
        public Texture2D Texture2
        {
            get;
            set;
        }
#if !MONOGAME
        public TextureCube CubeMap
        {
            get;
            set;
        }
#endif
        public int WeightsPerVertex
        {
            get;
            set;
        }
        public Matrix[] BoneTransforms
        {
            get{ return mBoneTransforms; }
            set{ mBoneTransforms = value; }
        }
        public float EnvironmentMapAmount
        {
            get;
            set;
        }
        public float FresnelFactor
        {
            get;
            set;
        }
        public bool VertexColorEnabled
        {
            get;
            set;
        }
        public bool TextureEnabled
        {
            get;
            set;
        }
        public bool PreferPerPixelLighting
        {
            get;
            set;
        }
        #endregion

        /// <summary>
        /// Class properties
        /// </summary>
        #region Properties
        public Effect Effect
        {
            set
            {
                if (value is BasicEffect)
                {
                    mBasicEffect = value as BasicEffect;
                }

                
                else if (value is AlphaTestEffect)
                {
                    mAlphaTestEffect = value as AlphaTestEffect;
                }
#if !MONOGAME

                else
                {
                    mDualTextureEffect = value as DualTextureEffect;
                    if( mDualTextureEffect == null )
                    {
                        mEnvironmentMapEffect = value as EnvironmentMapEffect;
                        if( mEnvironmentMapEffect == null )
                        {
                            mSkinnedEffect = value as SkinnedEffect;                            
                        }
                    }
                }
#endif
            }
        }

#if !SILVERLIGHT
        public TextureAddressMode TextureAddressMode
        {
            get { return mTextureAddressMode; }
            set { mTextureAddressMode = value; Renderer.TextureAddressMode = mTextureAddressMode;}
        }

        public void SetTextureAddressModeNoCall(TextureAddressMode textureAddressMode)
        {
            mTextureAddressMode = textureAddressMode;
        }

#endif
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        protected GenericEffect()
        {
            World = View = Projection = Matrix.Identity;
            LightingEnabled = false;
            FogEnabled = false;
            Alpha = 1.0f;
            DiffuseColor = Vector3.One;
            SpecularColor = Vector3.One;
            EmissiveColor = Vector3.One;
            SpecularPower = 16.0f;
            FresnelFactor = 1.0f;
            WeightsPerVertex = 3;

            this.Texture = this.Texture2 = null;
#if !MONOGAME
            this.CubeMap = null;
#endif
            this.BoneTransforms = null;
            mTextureAddressMode = Microsoft.Xna.Framework.Graphics.TextureAddressMode.Clamp;

            SetPrecendenceValues(DefaultBasicPrecedence, DefaultDualTexturePrecedence, DefaultEnvironmentMappingPrecedence, DefaultSkinningPrecedence);
        }
        public GenericEffect( DefaultShaderType type )
            :this()
        {
            if( type != DefaultShaderType.Determine )
            {
                if( type == DefaultShaderType.Basic )
                {
                    mBasicEffect = new BasicEffect( FlatRedBallServices.GraphicsDevice );
                }

                else if (type == DefaultShaderType.AlphaTest)
                {
                    mAlphaTestEffect = new AlphaTestEffect(FlatRedBallServices.GraphicsDevice);
                }

                else if (type == DefaultShaderType.DualTexture)
                {
#if MONOGAME
                    throw new NotImplementedException();
#else
                    mDualTextureEffect = new DualTextureEffect(FlatRedBallServices.GraphicsDevice);
#endif
                }

                else if (type == DefaultShaderType.EnvironmentMapped)
                {
#if MONOGAME
                    throw new NotImplementedException();
#else
                    mEnvironmentMapEffect = new EnvironmentMapEffect(FlatRedBallServices.GraphicsDevice);
#endif
                }

                else if (type == DefaultShaderType.Skinned)
                {
#if MONOGAME
                    throw new NotImplementedException();
#else
                    mSkinnedEffect = new SkinnedEffect(FlatRedBallServices.GraphicsDevice);
#endif
                }
            }
        }
        public GenericEffect( Effect effectToAssimilate )
            :this()
        {
            /*
            DefaultShaderType type = DetermineEffect( false, effectToAssimilate );
            if( type == DefaultShaderType.Basic )
            {
                mBasicEffect = (BasicEffect)effectToAssimilate;
            }
            else if( type == DefaultShaderType.DualTexture )
            {
                mDualTextureEffect = (DualTextureEffect)effectToAssimilate;
            }
            else if( type == DefaultShaderType.EnvironmentMapped )
            {
                mEnvironmentMapEffect = (EnvironmentMapEffect)effectToAssimilate;
            }
            else if( type == DefaultShaderType.Skinned )
            {
                mSkinnedEffect = (SkinnedEffect)effectToAssimilate;
            }
            */
            if ( effectToAssimilate is BasicEffect )
            {
                mBasicEffect = effectToAssimilate as BasicEffect;
            }
            else if (effectToAssimilate is AlphaTestEffect)
            {
                mAlphaTestEffect = effectToAssimilate as AlphaTestEffect;
            }
#if !MONOGAME

            else if (effectToAssimilate is DualTextureEffect)
            {
                mDualTextureEffect = effectToAssimilate as DualTextureEffect;
            }
            else if (effectToAssimilate is EnvironmentMapEffect)
            {
                mEnvironmentMapEffect = effectToAssimilate as EnvironmentMapEffect;
            }
            else if (effectToAssimilate is SkinnedEffect)
            {
                mSkinnedEffect = effectToAssimilate as SkinnedEffect;
            }
#endif
        }

        public void SetPrecendenceValues( float basicPrecedence,
                                     float dualTexturePrecedence,
                                     float environmentPredecence,
                                     float skinningPrecedence )
        {
            if(mPrecedenceTable.Count == 0 )
            {
                mPrecedenceTable.Add("BasicEffect", basicPrecedence);
                mPrecedenceTable.Add("DualTextureEffect", dualTexturePrecedence);
                mPrecedenceTable.Add("EnvironmentMapEffect", environmentPredecence);
                mPrecedenceTable.Add("SkinnedEffect", skinningPrecedence);
            }
            else
            {
                mPrecedenceTable["BasicEffect"] = basicPrecedence;
                mPrecedenceTable["DualTextureEffect"] = dualTexturePrecedence;
                mPrecedenceTable["EnvironmentMapEffect"] = environmentPredecence;
                mPrecedenceTable["SkinnedEffect"] = skinningPrecedence;
            }
        }

        /// <summary>
        /// Builds the precedence table, determines 
        /// the effect
        /// </summary>
        public Effect BuildEffect()
        {         
            if(mPrecedenceTable.Count == 0 )
            {
                mPrecedenceTable.Add("BasicEffect", 10.0f);
                mPrecedenceTable.Add("DualTextureEffect", 0.25f);
                mPrecedenceTable.Add("EnvironmentMapEffect", 0.5f);
                mPrecedenceTable.Add("SkinnedEffect", 1.0f);
            }
            DetermineEffect(true);
            if( this.LightingEnabled )
            {
                EnableDefaultLighting();
            }

            return GetActiveEffect();
        }

        /// <summary>
        /// Sets the current technique to the one named.
        /// </summary>
        /// <param name="TechniqueName"></param>
        public void SetCurrentTechnique( string TechniqueName )
       {
           GetEffect(false).CurrentTechnique = GetEffect(false).Techniques[ TechniqueName ];
       }

        /// <summary>
        /// Gets the internal effect
        /// </summary>
        /// <param name="ShouldSetParameters"> Should the parameters be set</param>
        /// <returns>The effect for this GenericEffect</returns>
        public Effect GetEffect( bool ShouldSetParameters )
        {
            Effect effect = GetActiveEffect();
            if (ShouldSetParameters)
                SetEffectParameters(effect);
            return effect;
        }

        /// <summary>
        /// Returns a technique based on the index provided. 
        /// Will set the parameters for this effect;
        /// </summary>
        /// <param name="TechniqueName">The index into the Technique List</param>
        /// <param name="ShouldSetParameters">Should we set the parameters</param>
        /// <returns>The EffectTechnique requested</returns>
        public EffectTechnique GetEffectTechnique( string TechniqueName, bool ShouldSetParameters )
        {
            return GetEffect(ShouldSetParameters).Techniques[TechniqueName];
        }

        /// <summary>
        /// Get the current technique
        /// </summary>
        /// <param name="ShouldSetParameters"> Should we set the parameters this call</param>
        /// <returns> The current effect</returns>
        public EffectTechnique GetCurrentTechnique( bool ShouldSetParameters )
        {
            return GetEffect(ShouldSetParameters).CurrentTechnique;
        }

        /// <summary>
        /// Determine's which of the four shaders this instance will use
        /// </summary>
        private DefaultShaderType DetermineEffect(bool createNewEffect) { return DetermineEffect(createNewEffect, null); }
        private DefaultShaderType DetermineEffect( bool createNewEffect, Effect effectToTest )
        {
            #region Determine precendence between effects
            float skinningPrecedence = mPrecedenceTable["SkinnedEffect"];
            float dualTexturePrecedence = mPrecedenceTable["DualTextureEffect"];
            float environmentMappingPrecedence = mPrecedenceTable["EnvironmentMapEffect"];
            float basicEffectPrecedence = mPrecedenceTable["BasicEffect"];
            #endregion

            #region Determining the possible types of Effect

            #region Check for key information for determining possible effect types
            Texture2D tex = Texture2;
            Matrix[] transforms = BoneTransforms;
#if !MONOGAME
            TextureCube cube = CubeMap;
#endif
            if (tex == null)
            {
                dualTexturePrecedence = float.MinValue;
            }

#if !MONOGAME
            if (cube == null)
            {
                environmentMappingPrecedence = float.MinValue;
            }
#endif

            if (transforms == null)
            {
                skinningPrecedence = float.MinValue;
            }
            #endregion
                

            // Three possible outcomes. If its a lower precedence than any
            // of it's competitors, it will lose out.
            // If it's greater than the others, it will win,
            // If it's equal to one another, well in technical terms, we guess.
            // ...
            // WELCOME TO IF STATEMENT HELL
#if !MONOGAME
            #region Skinning Shader determination... Don't look. Ever.
            //... I said don't look. Sigh
            if( skinningPrecedence >= environmentMappingPrecedence )
            {
                if( skinningPrecedence >= dualTexturePrecedence )
                {
                    if( skinningPrecedence > basicEffectPrecedence )
                    {
                        if( createNewEffect )
                        {
                            // Create a skinning shader
                            mSkinnedEffect = new SkinnedEffect( FlatRedBallServices.GraphicsDevice );
                            SetSkinningEffectParameters();
                        }

                        return DefaultShaderType.Skinned;
                    }
                }
            }
            #endregion

            #region Environment Mapping Determination
            if( environmentMappingPrecedence >= dualTexturePrecedence )
            {
                if( environmentMappingPrecedence >= basicEffectPrecedence )
                {
                    if( createNewEffect )
                    {
                        // Create Environment map
                        mEnvironmentMapEffect = new EnvironmentMapEffect(FlatRedBallServices.GraphicsDevice);
                        SetEnvironmentMapEffectParameters();
                    }
                    return DefaultShaderType.EnvironmentMapped;
                }
            }
            #endregion

            #region Dual Texture determination
            if( dualTexturePrecedence >= basicEffectPrecedence )
            {
                // Create Dual Texture
                if( createNewEffect )
                {
                    mDualTextureEffect = new DualTextureEffect( FlatRedBallServices.GraphicsDevice );
                    SetDualTextureEffectParameters();
                }
                return DefaultShaderType.DualTexture;
            }
            #endregion
#endif

            #region Basic Shader determination
             // Create a basic Shader
            if( createNewEffect )
            {
                mBasicEffect = new BasicEffect(FlatRedBallServices.GraphicsDevice);
                SetBasicEffectParameters();
            }

            return DefaultShaderType.Basic;

            #endregion
        #endregion
        }


        private void SetAlphaTestEffectParameters()
        {
            mAlphaTestEffect.World = this.World;
            mAlphaTestEffect.View = this.View;
            mAlphaTestEffect.Projection = this.Projection;

            mAlphaTestEffect.DiffuseColor = this.DiffuseColor;
            mAlphaTestEffect.FogColor = this.FogColor;
            mAlphaTestEffect.FogEnabled = this.FogEnabled;
            mAlphaTestEffect.FogStart = this.FogStart;
            mAlphaTestEffect.FogEnabled = this.FogEnabled;

            mAlphaTestEffect.VertexColorEnabled = this.VertexColorEnabled;
            mAlphaTestEffect.Alpha = this.Alpha;
            mAlphaTestEffect.Texture = this.Texture;
        }

        /// <summary>
        /// Set the parameters for Basic Shader
        /// </summary>
        private void SetBasicEffectParameters()
        {
            mBasicEffect.World              = this.World;
            mBasicEffect.View               = this.View;
            mBasicEffect.Projection         = this.Projection;
            mBasicEffect.LightingEnabled    = this.LightingEnabled;
            if (this.LightingEnabled)
            {
#if !MONOGAME
                mBasicEffect.DirectionalLight0.DiffuseColor     = this.DirectionalLight0.DiffuseColor;
                mBasicEffect.DirectionalLight0.Direction        = this.DirectionalLight0.Direction;
                mBasicEffect.DirectionalLight0.Enabled          = this.DirectionalLight0.Enabled;
                mBasicEffect.DirectionalLight0.SpecularColor    = this.DirectionalLight0.SpecularColor;

                mBasicEffect.DirectionalLight1.DiffuseColor     = this.DirectionalLight1.DiffuseColor;
                mBasicEffect.DirectionalLight1.Direction        = this.DirectionalLight1.Direction;
                mBasicEffect.DirectionalLight1.Enabled          = this.DirectionalLight1.Enabled;
                mBasicEffect.DirectionalLight1.SpecularColor    = this.DirectionalLight1.SpecularColor;

                mBasicEffect.DirectionalLight2.DiffuseColor     = this.DirectionalLight2.DiffuseColor;
                mBasicEffect.DirectionalLight2.Direction        = this.DirectionalLight2.Direction;
                mBasicEffect.DirectionalLight2.Enabled          = this.DirectionalLight2.Enabled;
                mBasicEffect.DirectionalLight2.SpecularColor    = this.DirectionalLight2.SpecularColor;
#endif
            }
            mBasicEffect.DiffuseColor       = this.DiffuseColor;
            mBasicEffect.AmbientLightColor  = this.AmbientLightColor;
            mBasicEffect.FogColor           = this.FogColor;
            mBasicEffect.FogEnabled         = this.FogEnabled;
            mBasicEffect.FogStart           = this.FogStart;
            mBasicEffect.FogEnd             = this.FogEnd;

            mBasicEffect.VertexColorEnabled = this.VertexColorEnabled;
            mBasicEffect.Alpha              = this.Alpha;
            mBasicEffect.Texture            = this.Texture;
            mBasicEffect.TextureEnabled     = this.TextureEnabled;
#if !MONOGAME
            mBasicEffect.PreferPerPixelLighting = this.PreferPerPixelLighting;
            mBasicEffect.SpecularColor = this.SpecularColor;
            mBasicEffect.SpecularPower = this.SpecularPower;
#endif

        }

        /// <summary>
        /// Sets the parameters for the dual texture effect
        /// </summary>
        private void SetDualTextureEffectParameters()
        {
#if !MONOGAME
            mDualTextureEffect.World              = this.World;
            mDualTextureEffect.View               = this.View;
            mDualTextureEffect.Projection         = this.Projection;
            mDualTextureEffect.FogColor           = this.FogColor;
            mDualTextureEffect.FogEnabled         = this.FogEnabled;
            mDualTextureEffect.FogStart           = this.FogStart;
            mDualTextureEffect.VertexColorEnabled = this.VertexColorEnabled;
            mDualTextureEffect.Alpha              = this.Alpha;
            mDualTextureEffect.DiffuseColor       = this.DiffuseColor;
            mDualTextureEffect.Texture            = this.Texture;
            mDualTextureEffect.Texture2           = this.Texture2;
#endif
        }

        /// <summary>
        /// Sets the parameters for the environment map effect
        /// </summary>
        private void SetEnvironmentMapEffectParameters()
        {
#if !MONOGAME
            mEnvironmentMapEffect.World              = this.World;
            mEnvironmentMapEffect.View               = this.View;
            mEnvironmentMapEffect.Projection         = this.Projection;
            if (this.LightingEnabled)
            {
                mEnvironmentMapEffect.DirectionalLight0.DiffuseColor     = this.DirectionalLight0.DiffuseColor;
                mEnvironmentMapEffect.DirectionalLight0.Direction        = this.DirectionalLight0.Direction;
                mEnvironmentMapEffect.DirectionalLight0.Enabled          = this.DirectionalLight0.Enabled;
                mEnvironmentMapEffect.DirectionalLight0.SpecularColor    = this.DirectionalLight0.SpecularColor;

                mEnvironmentMapEffect.DirectionalLight1.DiffuseColor     = this.DirectionalLight1.DiffuseColor;
                mEnvironmentMapEffect.DirectionalLight1.Direction        = this.DirectionalLight1.Direction;
                mEnvironmentMapEffect.DirectionalLight1.Enabled          = this.DirectionalLight1.Enabled;
                mEnvironmentMapEffect.DirectionalLight1.SpecularColor    = this.DirectionalLight1.SpecularColor;

                mEnvironmentMapEffect.DirectionalLight2.DiffuseColor     = this.DirectionalLight2.DiffuseColor;
                mEnvironmentMapEffect.DirectionalLight2.Direction        = this.DirectionalLight2.Direction;
                mEnvironmentMapEffect.DirectionalLight2.Enabled          = this.DirectionalLight2.Enabled;
                mEnvironmentMapEffect.DirectionalLight2.SpecularColor    = this.DirectionalLight2.SpecularColor;
            }
            mEnvironmentMapEffect.DiffuseColor       = this.DiffuseColor;
            mEnvironmentMapEffect.AmbientLightColor  = this.AmbientLightColor;
            mEnvironmentMapEffect.FogColor           = this.FogColor;
            mEnvironmentMapEffect.FogEnabled         = this.FogEnabled;
            mEnvironmentMapEffect.FogStart           = this.FogStart;
            mEnvironmentMapEffect.FogEnd             = this.FogEnd;
            mEnvironmentMapEffect.Alpha              = this.Alpha;
            mEnvironmentMapEffect.Texture            = this.Texture;
            mEnvironmentMapEffect.EmissiveColor      = this.EmissiveColor;
            mEnvironmentMapEffect.EnvironmentMapAmount = this.EnvironmentMapAmount;
            mEnvironmentMapEffect.EnvironmentMap     = this.CubeMap;
            mEnvironmentMapEffect.EnvironmentMapSpecular = this.SpecularColor;
            mEnvironmentMapEffect.FresnelFactor      = this.FresnelFactor;
#endif
        }

        /// <summary>
        /// Sets the parameters for the skinned effect
        /// </summary>
        private void SetSkinningEffectParameters()
        {
#if !MONOGAME
            mSkinnedEffect.World              = this.World;
            mSkinnedEffect.View               = this.View;
            mSkinnedEffect.Projection         = this.Projection;
            if (this.LightingEnabled)
            {
                mSkinnedEffect.DirectionalLight0.DiffuseColor     = this.DirectionalLight0.DiffuseColor;
                mSkinnedEffect.DirectionalLight0.Direction        = this.DirectionalLight0.Direction;
                mSkinnedEffect.DirectionalLight0.Enabled          = this.DirectionalLight0.Enabled;
                mSkinnedEffect.DirectionalLight0.SpecularColor    = this.DirectionalLight0.SpecularColor;

                mSkinnedEffect.DirectionalLight1.DiffuseColor     = this.DirectionalLight1.DiffuseColor;
                mSkinnedEffect.DirectionalLight1.Direction        = this.DirectionalLight1.Direction;
                mSkinnedEffect.DirectionalLight1.Enabled          = this.DirectionalLight1.Enabled;
                mSkinnedEffect.DirectionalLight1.SpecularColor    = this.DirectionalLight1.SpecularColor;

                mSkinnedEffect.DirectionalLight2.DiffuseColor     = this.DirectionalLight2.DiffuseColor;
                mSkinnedEffect.DirectionalLight2.Direction        = this.DirectionalLight2.Direction;
                mSkinnedEffect.DirectionalLight2.Enabled          = this.DirectionalLight2.Enabled;
                mSkinnedEffect.DirectionalLight2.SpecularColor    = this.DirectionalLight2.SpecularColor;
            }
            mSkinnedEffect.DiffuseColor       = this.DiffuseColor;
            mSkinnedEffect.AmbientLightColor  = this.AmbientLightColor;
            mSkinnedEffect.FogColor           = this.FogColor;
            mSkinnedEffect.FogEnabled         = this.FogEnabled;
            mSkinnedEffect.FogStart           = this.FogStart;
            mSkinnedEffect.FogEnd             = this.FogEnd;
            mSkinnedEffect.SpecularColor      = this.SpecularColor;
            mSkinnedEffect.SpecularPower      = this.SpecularPower;
            mSkinnedEffect.Alpha              = this.Alpha;
            mSkinnedEffect.Texture            = this.Texture;
            mSkinnedEffect.PreferPerPixelLighting = this.PreferPerPixelLighting;
            mSkinnedEffect.EmissiveColor      = this.EmissiveColor;
            mSkinnedEffect.WeightsPerVertex   = this.WeightsPerVertex;
            mSkinnedEffect.SetBoneTransforms( mBoneTransforms );
#endif
        }

        /// <summary>
        /// Pass common information to the graphics device
        /// </summary>
        private void SetCommonParameters()
        {
#if !SILVERLIGHT
            if(mTextureAddressMode != FlatRedBall.Graphics.Renderer.TextureAddressMode)
                FlatRedBall.Graphics.Renderer.TextureAddressMode = mTextureAddressMode;

            //We need to ensure that the second texture gets turned off,
            // if we need it, it will be set again when Effect.Apply is called.
            FlatRedBallServices.GraphicsDevice.Textures[1] = null;

            FlatRedBall.Graphics.Renderer.Texture = this.Texture;
#endif
        }

        /// <summary>
        /// Destroy this object
        /// </summary>
        public void Dispose()
        {
            if( mBasicEffect != null )
            {
                mBasicEffect.Dispose();
            }

            if (mAlphaTestEffect != null)
            {
                mAlphaTestEffect.Dispose();
            }

#if !MONOGAME
            if( mDualTextureEffect != null )
            {
                mDualTextureEffect.Dispose();
            }
#endif

#if !MONOGAME
            if( mEnvironmentMapEffect != null )
            {
                mEnvironmentMapEffect.Dispose();
            }
#endif

#if !MONOGAME
            if( mSkinnedEffect!= null )
            {
                mSkinnedEffect.Dispose();
            }
#endif
        }

        /// <summary>
        /// Needs to be implemented by the IEffectLight child
        /// </summary>
        public void EnableDefaultLighting()
        {
            // Key light.
            this.DirectionalLight0.Direction = new Vector3(-0.5265408f, -0.5735765f, -0.6275069f);
            this.DirectionalLight0.DiffuseColor = new Vector3(1, 0.9607844f, 0.8078432f);
            this.DirectionalLight0.SpecularColor = new Vector3(1, 0.9607844f, 0.8078432f);
            this.DirectionalLight0.Enabled = true;

            // Fill light.
            this.DirectionalLight1.Direction = new Vector3(0.7198464f, 0.3420201f, 0.6040227f);
            this.DirectionalLight1.DiffuseColor = new Vector3(0.9647059f, 0.7607844f, 0.4078432f);
            this.DirectionalLight1.SpecularColor = Vector3.Zero;
            this.DirectionalLight1.Enabled = true;

            // Back light.
            this.DirectionalLight2.Direction = new Vector3(0.4545195f, -0.7660444f, 0.4545195f);
            this.DirectionalLight2.DiffuseColor = new Vector3(0.3231373f, 0.3607844f, 0.3937255f);
            this.DirectionalLight2.SpecularColor = new Vector3(0.3231373f, 0.3607844f, 0.3937255f);
            this.DirectionalLight2.Enabled = true;

            // Ambient light.
            this.AmbientLightColor = new Vector3(0.05333332f, 0.09882354f, 0.1819608f);
        }

        private Effect GetActiveEffect()
        {
            Effect effect = null;

            if (mBasicEffect != null)
                effect = mBasicEffect;
            else if (mAlphaTestEffect != null)
                effect = mAlphaTestEffect;
#if !MONOGAME
            else if (mDualTextureEffect != null)
                effect = mDualTextureEffect;
            else if (mSkinnedEffect != null)
                effect = mSkinnedEffect;
            else if (mEnvironmentMapEffect != null)
                effect = mEnvironmentMapEffect;
#endif
            else
            {
                //No valid Effect set yet, build it...
                effect = BuildEffect();
            }

            return effect;
        }

        private void SetEffectParameters(Effect effect)
        {
            if (effect == mBasicEffect)
                SetBasicEffectParameters();
            else if (effect == mAlphaTestEffect)
                SetAlphaTestEffectParameters();
#if !MONOGAME

            else if (effect == mDualTextureEffect)
                SetDualTextureEffectParameters();
            else if (effect == mSkinnedEffect)
                SetSkinningEffectParameters();
            else if (effect == mEnvironmentMapEffect)
                SetEnvironmentMapEffectParameters();
#endif
            else
                throw new InvalidOperationException("Cannot set parameters, no valid effect found.");

            SetCommonParameters();
        }
    }
}
