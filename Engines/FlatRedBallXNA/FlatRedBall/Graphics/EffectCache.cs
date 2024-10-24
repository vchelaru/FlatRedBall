using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace FlatRedBall.Graphics
{
    #region XML Docs
    /// <summary>
    /// Caches effects and effect parameters
    /// </summary>
    #endregion
    public class EffectCache
    {
        #region Static Members

        #region Enums and defaults

        static int EffectParameterNamesCount = 40;
        public static string[] EffectParameterNames;
        public enum EffectParameterNamesEnum
        {
            World,
            View,
            Projection,
            ViewProj,
            // Lighting
            LightingEnable,
            AmbLight0Enable,
            AmbLight0DiffuseColor,
            DirLight0Enable,
            DirLight0Direction,
            DirLight0DiffuseColor,
            DirLight0SpecularColor,
            DirLight1Enable,
            DirLight1Direction,
            DirLight1DiffuseColor,
            DirLight1SpecularColor,
            DirLight2Enable,
            DirLight2Direction,
            DirLight2DiffuseColor,
            DirLight2SpecularColor,
            PointLight0Enable,
            PointLight0Position,
            PointLight0DiffuseColor,
            PointLight0SpecularColor,
            PointLight0Range,
            // Fog
            FogEnabled,
            FogColor,
            FogStart,
            FogEnd,
            // Shared parameters
            PixelSize,
            InvViewProj,
            NearClipPlane,
            FarClipPlane,
            CameraPosition,
            ViewportSize,
            // Shadow
            ShadowMapTexture,
            ShadowCameraMatrix,
            ShadowLightDirection,
            ShadowLightDist,
            ShadowCameraAt,
            // Animation
            MatrixPalette
        }

        #endregion

        #region Static Constructor

        #region XML Docs
        /// <summary>
        /// Initializes parameter name strings
        /// </summary>
        #endregion
        static EffectCache()
        {
            EffectParameterNames = new string[EffectParameterNamesCount];
            for (int i = 0; i < EffectParameterNamesCount; i++)
            {
                EffectParameterNames[i] = ((EffectParameterNamesEnum)i).ToString();
            }
        }

        #endregion

        #endregion

        #region Fields

        #region XML Docs
        /// <summary>
        /// Whether or not shared parameters should be cached
        /// </summary>
        #endregion
        bool mCacheShared;

        #region XML Docs
        /// <summary>
        /// The effect cached, if any
        /// </summary>
        #endregion
        Effect mEffect = null;

        #region XML Docs
        /// <summary>
        /// Caches all effect parameters for faster lookup, by effect
        /// </summary>
        #endregion
        Dictionary<Effect, Dictionary<string, EffectParameter>> mFullEffectParameterCache =
            new Dictionary<Effect, Dictionary<string, EffectParameter>>();

        #region XML Docs
        /// <summary>
        /// Caches all effect parameters for faster lookup, by parameter name
        /// </summary>
        #endregion
        Dictionary<string, List<EffectParameter>> mFullParameterCache =
            new Dictionary<string, List<EffectParameter>>();

        #region XML Docs
        /// <summary>
        /// Caches all effects
        /// </summary>
        #endregion
        List<Effect> mEffectCache = new List<Effect>();

        #region XML Docs
        /// <summary>
        /// Caches all enumerated effect parameters
        /// </summary>
        #endregion
        List<List<EffectParameter>> mEffectParameterCache = new List<List<EffectParameter>>();

        Dictionary<Effect, Dictionary<string, EffectTechnique>> mEffectTechniqueCache =
            new Dictionary<Effect, Dictionary<string, EffectTechnique>>();

        #endregion

        #region Properties

        #region XML Docs
        /// <summary>
        /// Whether or not shared parameters should be cached
        /// </summary>
        #endregion
        public bool CacheShared
        {
            get { return mCacheShared; }
        }

        #region XML Docs
        /// <summary>
        /// Gets a list of cached effects
        /// </summary>
        #endregion
        public List<Effect> Effects
        {
            get { return mEffectCache; }
        }

        #region XML Docs
        /// <summary>
        /// Gets a list of parameters of the specified name
        /// </summary>
        /// <param name="name">The name of the parameters to retrieve</param>
        /// <returns>A list of parameters (or null if name not found)</returns>
        #endregion
        public List<EffectParameter> this[string name]
        {
            get
            {
                if (mFullParameterCache.ContainsKey(name))
                    return mFullParameterCache[name];

                else
                    return null;
            }
        }

        #region XML Docs
        /// <summary>
        /// Retrieves all parameters of one of the standard types
        /// </summary>
        /// <param name="name">The name of the parameter</param>
        /// <returns>A list of parameters (or null if shared params aren't cached)</returns>
        #endregion
        public List<EffectParameter> this[EffectParameterNamesEnum name]
        {
            get
            {
                // If we don't cache shared parameters, there is no list
                if (!mCacheShared)
                    return null;

                else
                    return mEffectParameterCache[(int)name];
            }
        }

        #region XML Docs
        /// <summary>
        /// Gets a parameter in a specified effect
        /// </summary>
        /// <param name="effect">The effect</param>
        /// <param name="parameterName">The parameter name</param>
        /// <returns>The parameter, or null if either effect or parameter not found</returns>
        #endregion
        public EffectParameter this[Effect effect, string parameterName]
        {
            get
            {
                EffectParameter param = null;

                // Check if the effect and parameter are found
                if (mFullEffectParameterCache.ContainsKey(effect) &&
                    mFullEffectParameterCache[effect].ContainsKey(parameterName))
                {
                    param = 
                        mFullEffectParameterCache[effect][parameterName];
                }

                return param;
            }
        }

        #region XML Docs
        /// <summary>
        /// Gets the specified technique from the specified effect, or null if no technique by that name exists
        /// </summary>
        /// <param name="effect">The effect</param>
        /// <param name="techniqueName">The name of the technique</param>
        /// <returns>The technique requested, or null if not found</returns>
        #endregion
        public EffectTechnique GetTechnique(Effect effect, string techniqueName)
        {
            EffectTechnique technique = null;

            if (mEffectTechniqueCache.ContainsKey(effect) &&
                mEffectTechniqueCache[effect].ContainsKey(techniqueName))
            {
                technique = mEffectTechniqueCache[effect][techniqueName];
            }

            return technique;
        }

        #endregion

        #region Constructor

        #region XML Docs
        /// <summary>
        /// Caches an effect's parameters
        /// </summary>
        /// <param name="effect">The effect to cache</param>
        /// <param name="cacheShared">Whether or not shared parameters should be cached</param>
        #endregion
        public EffectCache(Effect effect, bool cacheShared)
        {
            mCacheShared = cacheShared;
            mEffect = effect;
            UpdateCache();
        }

        #endregion


        #region Methods

        #region XML Docs
        /// <summary>
        /// Adds an effect to the cache
        /// </summary>
        /// <param name="effect">The effect to cache</param>
        #endregion
        private void CacheEffect(Effect effect)
        {
            // Cache the effect
            if (!mEffectCache.Contains(effect))
                mEffectCache.Add(effect);

            // Cache the effect and its parameters
            if (!mFullEffectParameterCache.ContainsKey(effect))
                mFullEffectParameterCache.Add(effect, new Dictionary<string, EffectParameter>());

            // Create the full parameter dictionary
            Dictionary<string, EffectParameter> parameters = new Dictionary<string,EffectParameter>();
            foreach (EffectParameter param in effect.Parameters)
            {
                // Cache all parameters in the global cache
                parameters.Add(param.Name, param);

                // Cache parameter
                if (!mFullParameterCache.ContainsKey(param.Name))
                {
                    mFullParameterCache.Add(param.Name, new List<EffectParameter>(1));
                }
                mFullParameterCache[param.Name].Add(param);

                // Cache parameter by effect
                if (!mFullEffectParameterCache[effect].ContainsKey(param.Name))
                    mFullEffectParameterCache[effect].Add(param.Name, param);
                else
                    mFullEffectParameterCache[effect][param.Name] = param;

            }

            // Cache shared parameters if it is allowed
            if (mCacheShared)
            {
                for (int i = 0; i < EffectParameterNamesCount; i++)
                {
                    EffectParameter param = effect.Parameters[
                        ((EffectParameterNamesEnum)i).ToString()];

                    // Cache if found
                    if (param != null)
                    {
                        mEffectParameterCache[i].Add(param);
                    }
                }
            }

            // Cache techniques
            if (mEffectTechniqueCache.ContainsKey(effect))
            {
                mEffectTechniqueCache[effect].Clear();
            }
            else
            {
                mEffectTechniqueCache.Add(effect, new Dictionary<string, EffectTechnique>());
            }
            foreach (EffectTechnique technique in effect.Techniques)
            {
                mEffectTechniqueCache[effect].Add(technique.Name, technique);
            }
        }

        #region XML Docs
        /// <summary>
        /// Recreates the cache
        /// </summary>
        #endregion
        public void UpdateCache()
        {
            // Clear the caches
            mFullEffectParameterCache.Clear();
            mFullParameterCache.Clear();
            mEffectCache.Clear();
            mEffectParameterCache.Clear();
            // Recreate the parameter cache if allowed
            if (mCacheShared)
            {
                for (int i = 0; i < EffectParameterNamesCount; i++)
                {
                    mEffectParameterCache.Add(new List<EffectParameter>());
                }
            }

            // Update the cached effect
            if (mEffect != null)
            {
                CacheEffect(mEffect);
            }

        }

        #region XML Docs
        /// <summary>
        /// Validates the cached model/effect to make sure it hasn't changed
        /// </summary>
        /// <param name="updateNow">Whether or not the cache should be updated if found invalid</param>
        /// <returns>Whether or not the cache was valid</returns>
        #endregion
        public bool ValidateCache(bool updateNow)
        {
            bool isValid = true;

            // We just need to check to make sure all the required effects are cached
            if (mEffect != null)
            {
                if (!mEffectCache.Contains(mEffect))
                    isValid = false;
            }

            // Return validity
            return isValid;
        }

        #endregion
    }
}
