#if WINDOWS || MONOGAME_381
#define USE_CUSTOM_SHADER
#endif

using Microsoft.Xna.Framework.Graphics;
using System;

namespace FlatRedBall.Graphics
{
    public class CustomEffectManager
    {
        Effect mEffect;

        // Cached effect members to avoid list lookups while rendering
        public EffectParameter ParameterCurrentTexture;
        public EffectParameter ParameterViewProj;

        bool mEffectHasNewformat;

        EffectTechnique mTechniqueTexture;
        EffectTechnique mTechniqueAdd;
        EffectTechnique mTechniqueSubtract;
        EffectTechnique mTechniqueModulate;
        EffectTechnique mTechniqueModulate2X;
        EffectTechnique mTechniqueModulate4X;
        EffectTechnique mTechniqueInverseTexture;
        EffectTechnique mTechniqueColor;
        EffectTechnique mTechniqueColorTextureAlpha;
        EffectTechnique mTechniqueInterpolateColor;

        EffectTechnique mTechniqueTexture_Linearize;
        EffectTechnique mTechniqueAdd_Linearize;
        EffectTechnique mTechniqueSubtract_Linearize;
        EffectTechnique mTechniqueModulate_Linearize;
        EffectTechnique mTechniqueModulate2X_Linearize;
        EffectTechnique mTechniqueModulate4X_Linearize;
        EffectTechnique mTechniqueInverseTexture_Linearize;
        EffectTechnique mTechniqueColor_Linearize;
        EffectTechnique mTechniqueColorTextureAlpha_Linearize;
        EffectTechnique mTechniqueInterpolateColor_Linearize;

        EffectTechnique mTechniqueTexture_Linear;
        EffectTechnique mTechniqueAdd_Linear;
        EffectTechnique mTechniqueSubtract_Linear;
        EffectTechnique mTechniqueModulate_Linear;
        EffectTechnique mTechniqueModulate2X_Linear;
        EffectTechnique mTechniqueModulate4X_Linear;
        EffectTechnique mTechniqueInverseTexture_Linear;
        EffectTechnique mTechniqueColor_Linear;
        EffectTechnique mTechniqueColorTextureAlpha_Linear;
        EffectTechnique mTechniqueInterpolateColor_Linear;

        EffectTechnique mTechniqueTexture_Linear_Linearize;
        EffectTechnique mTechniqueAdd_Linear_Linearize;
        EffectTechnique mTechniqueSubtract_Linear_Linearize;
        EffectTechnique mTechniqueModulate_Linear_Linearize;
        EffectTechnique mTechniqueModulate2X_Linear_Linearize;
        EffectTechnique mTechniqueModulate4X_Linear_Linearize;
        EffectTechnique mTechniqueInverseTexture_Linear_Linearize;
        EffectTechnique mTechniqueColor_Linear_Linearize;
        EffectTechnique mTechniqueColorTextureAlpha_Linear_Linearize;
        EffectTechnique mTechniqueInterpolateColor_Linear_Linearize;

        public Effect Effect
        {
            get { return mEffect; }
            set
            {
                mEffect = value;

#if USE_CUSTOM_SHADER
                ParameterViewProj = mEffect.Parameters["ViewProj"];
                ParameterCurrentTexture = mEffect.Parameters["CurrentTexture"];

                // Let's check if the shader has the new format (which includes
                // separate versions of techniques for Point and Linear filtering).
                // We try to cache the first technique in order to do so.
                try { mTechniqueTexture = mEffect.Techniques["Texture_Point"]; } catch { }

                if (mTechniqueTexture != null)
                {
                    mEffectHasNewformat = true;

                    //try { mTechniqueTexture = mEffect.Techniques["Texture_Point"]; } catch { } // This has been already cached
                    try { mTechniqueAdd = mEffect.Techniques["Add_Point"]; } catch { }
                    try { mTechniqueSubtract = mEffect.Techniques["Subtract_Point"]; } catch { }
                    try { mTechniqueModulate = mEffect.Techniques["Modulate_Point"]; } catch { }
                    try { mTechniqueModulate2X = mEffect.Techniques["Modulate2X_Point"]; } catch { }
                    try { mTechniqueModulate4X = mEffect.Techniques["Modulate4X_Point"]; } catch { }
                    try { mTechniqueInverseTexture = mEffect.Techniques["InverseTexture_Point"]; } catch { }
                    try { mTechniqueColor = mEffect.Techniques["Color_Point"]; } catch { }
                    try { mTechniqueColorTextureAlpha = mEffect.Techniques["ColorTextureAlpha_Point"]; } catch { }
                    try { mTechniqueInterpolateColor = mEffect.Techniques["InterpolateColor_Point"]; } catch { }

                    try { mTechniqueTexture_Linearize = mEffect.Techniques["Texture_Point_Linearize"]; } catch { }
                    try { mTechniqueAdd_Linearize = mEffect.Techniques["Add_Point_Linearize"]; } catch { }
                    try { mTechniqueSubtract_Linearize = mEffect.Techniques["Subtract_Point_Linearize"]; } catch { }
                    try { mTechniqueModulate_Linearize = mEffect.Techniques["Modulate_Point_Linearize"]; } catch { }
                    try { mTechniqueModulate2X_Linearize = mEffect.Techniques["Modulate2X_Point_Linearize"]; } catch { }
                    try { mTechniqueModulate4X_Linearize = mEffect.Techniques["Modulate4X_Point_Linearize"]; } catch { }
                    try { mTechniqueInverseTexture_Linearize = mEffect.Techniques["InverseTexture_Point_Linearize"]; } catch { }
                    try { mTechniqueColor_Linearize = mEffect.Techniques["Color_Point_Linearize"]; } catch { }
                    try { mTechniqueColorTextureAlpha_Linearize = mEffect.Techniques["ColorTextureAlpha_Point_Linearize"]; } catch { }
                    try { mTechniqueInterpolateColor_Linearize = mEffect.Techniques["InterpolateColor_Point_Linearize"]; } catch { }

                    try { mTechniqueTexture_Linear = mEffect.Techniques["Texture_Linear"]; } catch { }
                    try { mTechniqueAdd_Linear = mEffect.Techniques["Add_Linear"]; } catch { }
                    try { mTechniqueSubtract_Linear = mEffect.Techniques["Subtract_Linear"]; } catch { }
                    try { mTechniqueModulate_Linear = mEffect.Techniques["Modulate_Linear"]; } catch { }
                    try { mTechniqueModulate2X_Linear = mEffect.Techniques["Modulate2X_Linear"]; } catch { }
                    try { mTechniqueModulate4X_Linear = mEffect.Techniques["Modulate4X_Linear"]; } catch { }
                    try { mTechniqueInverseTexture_Linear = mEffect.Techniques["InverseTexture_Linear"]; } catch { }
                    try { mTechniqueColor_Linear = mEffect.Techniques["Color_Linear"]; } catch { }
                    try { mTechniqueColorTextureAlpha_Linear = mEffect.Techniques["ColorTextureAlpha_Linear"]; } catch { }
                    try { mTechniqueInterpolateColor_Linear = mEffect.Techniques["InterpolateColor_Linear"]; } catch { }

                    try { mTechniqueTexture_Linear_Linearize = mEffect.Techniques["Texture_Linear_Linearize"]; } catch { }
                    try { mTechniqueAdd_Linear_Linearize = mEffect.Techniques["Add_Linear_Linearize"]; } catch { }
                    try { mTechniqueSubtract_Linear_Linearize = mEffect.Techniques["Subtract_Linear_Linearize"]; } catch { }
                    try { mTechniqueModulate_Linear_Linearize = mEffect.Techniques["Modulate_Linear_Linearize"]; } catch { }
                    try { mTechniqueModulate2X_Linear_Linearize = mEffect.Techniques["Modulate2X_Linear_Linearize"]; } catch { }
                    try { mTechniqueModulate4X_Linear_Linearize = mEffect.Techniques["Modulate4X_Linear_Linearize"]; } catch { }
                    try { mTechniqueInverseTexture_Linear_Linearize = mEffect.Techniques["InverseTexture_Linear_Linearize"]; } catch { }
                    try { mTechniqueColor_Linear_Linearize = mEffect.Techniques["Color_Linear_Linearize"]; } catch { }
                    try { mTechniqueColorTextureAlpha_Linear_Linearize = mEffect.Techniques["ColorTextureAlpha_Linear_Linearize"]; } catch { }
                    try { mTechniqueInterpolateColor_Linear_Linearize = mEffect.Techniques["InterpolateColor_Linear_Linearize"]; } catch { }
                }
                else
                {
                    mEffectHasNewformat = false;

                    try { mTechniqueTexture = mEffect.Techniques["Texture"]; } catch { }
                    try { mTechniqueAdd = mEffect.Techniques["Add"]; } catch { }
                    try { mTechniqueSubtract = mEffect.Techniques["Subtract"]; } catch { }
                    try { mTechniqueModulate = mEffect.Techniques["Modulate"]; } catch { }
                    try { mTechniqueModulate2X = mEffect.Techniques["Modulate2X"]; } catch { }
                    try { mTechniqueModulate4X = mEffect.Techniques["Modulate4X"]; } catch { }
                    try { mTechniqueInverseTexture = mEffect.Techniques["InverseTexture"]; } catch { }
                    try { mTechniqueColor = mEffect.Techniques["Color"]; } catch { }
                    try { mTechniqueColorTextureAlpha = mEffect.Techniques["ColorTextureAlpha"]; } catch { }
                    try { mTechniqueInterpolateColor = mEffect.Techniques["InterpolateColor"]; } catch { }
                }
#endif
            }
        }

        static EffectTechnique GetTechniqueVariant(bool useDefaultOrPointFilter, EffectTechnique point, EffectTechnique pointLinearized, EffectTechnique linear, EffectTechnique linearLinearized)
        {
            return useDefaultOrPointFilter ?
                (Renderer.LinearizeTextures ? pointLinearized : point) :
                (Renderer.LinearizeTextures ? linearLinearized : linear);
        }

        public EffectTechnique GetTechniqueVariantFromColorOperation(ColorOperation value)
        {
            if (mEffect == null)
                throw new Exception("The effect hasn't been set.");

            EffectTechnique technique = null;

            bool useDefaultOrPointFilter = true;

            if (mEffectHasNewformat)
            {
                useDefaultOrPointFilter = FlatRedBallServices.GraphicsOptions.TextureFilter == TextureFilter.Point;
            }

            switch (value)
            {
                case ColorOperation.Texture:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilter, mTechniqueTexture, mTechniqueTexture_Linearize, mTechniqueTexture_Linear, mTechniqueTexture_Linear_Linearize); break;

                case ColorOperation.Add:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilter, mTechniqueAdd, mTechniqueAdd_Linearize, mTechniqueAdd_Linear, mTechniqueAdd_Linear_Linearize); break;

                case ColorOperation.Subtract:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilter, mTechniqueSubtract, mTechniqueSubtract_Linearize, mTechniqueSubtract_Linear, mTechniqueSubtract_Linear_Linearize); break;

                case ColorOperation.Modulate:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilter, mTechniqueModulate, mTechniqueModulate_Linearize, mTechniqueModulate_Linear, mTechniqueModulate_Linear_Linearize); break;

                case ColorOperation.Modulate2X:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilter, mTechniqueModulate2X, mTechniqueModulate2X_Linearize, mTechniqueModulate2X_Linear, mTechniqueModulate2X_Linear_Linearize); break;

                case ColorOperation.Modulate4X:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilter, mTechniqueModulate4X, mTechniqueModulate4X_Linearize, mTechniqueModulate4X_Linear, mTechniqueModulate4X_Linear_Linearize); break;

                case ColorOperation.InverseTexture:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilter, mTechniqueInverseTexture, mTechniqueInverseTexture_Linearize, mTechniqueInverseTexture_Linear, mTechniqueInverseTexture_Linear_Linearize); break;

                case ColorOperation.Color:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilter, mTechniqueColor, mTechniqueColor_Linearize, mTechniqueColor_Linear, mTechniqueColor_Linear_Linearize); break;

                case ColorOperation.ColorTextureAlpha:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilter, mTechniqueColorTextureAlpha, mTechniqueColorTextureAlpha_Linearize, mTechniqueColorTextureAlpha_Linear, mTechniqueColorTextureAlpha_Linear_Linearize); break;

                case ColorOperation.InterpolateColor:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilter, mTechniqueInterpolateColor, mTechniqueInterpolateColor_Linearize, mTechniqueInterpolateColor_Linear, mTechniqueInterpolateColor_Linear_Linearize); break;

                default: throw new InvalidOperationException();
            }

            return technique;
        }
    }
}
