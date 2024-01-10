#if WINDOWS || MONOGAME_381
#define USE_CUSTOM_SHADER
#endif

using Microsoft.Xna.Framework;
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
        public EffectParameter ParameterColorModifier;

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

        EffectTechnique mTechniqueTexture_CM;
        EffectTechnique mTechniqueAdd_CM;
        EffectTechnique mTechniqueSubtract_CM;
        EffectTechnique mTechniqueModulate_CM;
        EffectTechnique mTechniqueModulate2X_CM;
        EffectTechnique mTechniqueModulate4X_CM;
        EffectTechnique mTechniqueInverseTexture_CM;
        EffectTechnique mTechniqueColor_CM;
        EffectTechnique mTechniqueColorTextureAlpha_CM;
        EffectTechnique mTechniqueInterpolateColor_CM;

        EffectTechnique mTechniqueTexture_LN;
        EffectTechnique mTechniqueAdd_LN;
        EffectTechnique mTechniqueSubtract_LN;
        EffectTechnique mTechniqueModulate_LN;
        EffectTechnique mTechniqueModulate2X_LN;
        EffectTechnique mTechniqueModulate4X_LN;
        EffectTechnique mTechniqueInverseTexture_LN;
        EffectTechnique mTechniqueColor_LN;
        EffectTechnique mTechniqueColorTextureAlpha_LN;
        EffectTechnique mTechniqueInterpolateColor_LN;

        EffectTechnique mTechniqueTexture_LN_CM;
        EffectTechnique mTechniqueAdd_LN_CM;
        EffectTechnique mTechniqueSubtract_LN_CM;
        EffectTechnique mTechniqueModulate_LN_CM;
        EffectTechnique mTechniqueModulate2X_LN_CM;
        EffectTechnique mTechniqueModulate4X_LN_CM;
        EffectTechnique mTechniqueInverseTexture_LN_CM;
        EffectTechnique mTechniqueColor_LN_CM;
        EffectTechnique mTechniqueColorTextureAlpha_LN_CM;
        EffectTechnique mTechniqueInterpolateColor_LN_CM;

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

        EffectTechnique mTechniqueTexture_Linear_CM;
        EffectTechnique mTechniqueAdd_Linear_CM;
        EffectTechnique mTechniqueSubtract_Linear_CM;
        EffectTechnique mTechniqueModulate_Linear_CM;
        EffectTechnique mTechniqueModulate2X_Linear_CM;
        EffectTechnique mTechniqueModulate4X_Linear_CM;
        EffectTechnique mTechniqueInverseTexture_Linear_CM;
        EffectTechnique mTechniqueColor_Linear_CM;
        EffectTechnique mTechniqueColorTextureAlpha_Linear_CM;
        EffectTechnique mTechniqueInterpolateColor_Linear_CM;

        EffectTechnique mTechniqueTexture_Linear_LN;
        EffectTechnique mTechniqueAdd_Linear_LN;
        EffectTechnique mTechniqueSubtract_Linear_LN;
        EffectTechnique mTechniqueModulate_Linear_LN;
        EffectTechnique mTechniqueModulate2X_Linear_LN;
        EffectTechnique mTechniqueModulate4X_Linear_LN;
        EffectTechnique mTechniqueInverseTexture_Linear_LN;
        EffectTechnique mTechniqueColor_Linear_LN;
        EffectTechnique mTechniqueColorTextureAlpha_Linear_LN;
        EffectTechnique mTechniqueInterpolateColor_Linear_LN;

        EffectTechnique mTechniqueTexture_Linear_LN_CM;
        EffectTechnique mTechniqueAdd_Linear_LN_CM;
        EffectTechnique mTechniqueSubtract_Linear_LN_CM;
        EffectTechnique mTechniqueModulate_Linear_LN_CM;
        EffectTechnique mTechniqueModulate2X_Linear_LN_CM;
        EffectTechnique mTechniqueModulate4X_Linear_LN_CM;
        EffectTechnique mTechniqueInverseTexture_Linear_LN_CM;
        EffectTechnique mTechniqueColor_Linear_LN_CM;
        EffectTechnique mTechniqueColorTextureAlpha_Linear_LN_CM;
        EffectTechnique mTechniqueInterpolateColor_Linear_LN_CM;

        public Effect Effect
        {
            get { return mEffect; }
            set
            {
                mEffect = value;

#if USE_CUSTOM_SHADER
                ParameterViewProj = mEffect.Parameters["ViewProj"];
                ParameterCurrentTexture = mEffect.Parameters["CurrentTexture"];
                try { ParameterColorModifier = mEffect.Parameters["ColorModifier"]; } catch { }

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

                    try { mTechniqueTexture_CM = mEffect.Techniques["Texture_Point_CM"]; } catch { }
                    try { mTechniqueAdd_CM = mEffect.Techniques["Add_Point_CM"]; } catch { }
                    try { mTechniqueSubtract_CM = mEffect.Techniques["Subtract_Point_CM"]; } catch { }
                    try { mTechniqueModulate_CM = mEffect.Techniques["Modulate_Point_CM"]; } catch { }
                    try { mTechniqueModulate2X_CM = mEffect.Techniques["Modulate2X_Point_CM"]; } catch { }
                    try { mTechniqueModulate4X_CM = mEffect.Techniques["Modulate4X_Point_CM"]; } catch { }
                    try { mTechniqueInverseTexture_CM = mEffect.Techniques["InverseTexture_Point_CM"]; } catch { }
                    try { mTechniqueColor_CM = mEffect.Techniques["Color_Point_CM"]; } catch { }
                    try { mTechniqueColorTextureAlpha_CM = mEffect.Techniques["ColorTextureAlpha_Point_CM"]; } catch { }
                    try { mTechniqueInterpolateColor_CM = mEffect.Techniques["InterpolateColor_Point_CM"]; } catch { }

                    try { mTechniqueTexture_LN = mEffect.Techniques["Texture_Point_LN"]; } catch { }
                    try { mTechniqueAdd_LN = mEffect.Techniques["Add_Point_LN"]; } catch { }
                    try { mTechniqueSubtract_LN = mEffect.Techniques["Subtract_Point_LN"]; } catch { }
                    try { mTechniqueModulate_LN = mEffect.Techniques["Modulate_Point_LN"]; } catch { }
                    try { mTechniqueModulate2X_LN = mEffect.Techniques["Modulate2X_Point_LN"]; } catch { }
                    try { mTechniqueModulate4X_LN = mEffect.Techniques["Modulate4X_Point_LN"]; } catch { }
                    try { mTechniqueInverseTexture_LN = mEffect.Techniques["InverseTexture_Point_LN"]; } catch { }
                    try { mTechniqueColor_LN = mEffect.Techniques["Color_Point_LN"]; } catch { }
                    try { mTechniqueColorTextureAlpha_LN = mEffect.Techniques["ColorTextureAlpha_Point_LN"]; } catch { }
                    try { mTechniqueInterpolateColor_LN = mEffect.Techniques["InterpolateColor_Point_LN"]; } catch { }

                    try { mTechniqueTexture_LN_CM = mEffect.Techniques["Texture_Point_LN_CM"]; } catch { }
                    try { mTechniqueAdd_LN_CM = mEffect.Techniques["Add_Point_LN_CM"]; } catch { }
                    try { mTechniqueSubtract_LN_CM = mEffect.Techniques["Subtract_Point_LN_CM"]; } catch { }
                    try { mTechniqueModulate_LN_CM = mEffect.Techniques["Modulate_Point_LN_CM"]; } catch { }
                    try { mTechniqueModulate2X_LN_CM = mEffect.Techniques["Modulate2X_Point_LN_CM"]; } catch { }
                    try { mTechniqueModulate4X_LN_CM = mEffect.Techniques["Modulate4X_Point_LN_CM"]; } catch { }
                    try { mTechniqueInverseTexture_LN_CM = mEffect.Techniques["InverseTexture_Point_LN_CM"]; } catch { }
                    try { mTechniqueColor_LN_CM = mEffect.Techniques["Color_Point_LN_CM"]; } catch { }
                    try { mTechniqueColorTextureAlpha_LN_CM = mEffect.Techniques["ColorTextureAlpha_Point_LN_CM"]; } catch { }
                    try { mTechniqueInterpolateColor_LN_CM = mEffect.Techniques["InterpolateColor_Point_LN_CM"]; } catch { }

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

                    try { mTechniqueTexture_Linear_CM = mEffect.Techniques["Texture_Linear_CM"]; } catch { }
                    try { mTechniqueAdd_Linear_CM = mEffect.Techniques["Add_Linear_CM"]; } catch { }
                    try { mTechniqueSubtract_Linear_CM = mEffect.Techniques["Subtract_Linear_CM"]; } catch { }
                    try { mTechniqueModulate_Linear_CM = mEffect.Techniques["Modulate_Linear_CM"]; } catch { }
                    try { mTechniqueModulate2X_Linear_CM = mEffect.Techniques["Modulate2X_Linear_CM"]; } catch { }
                    try { mTechniqueModulate4X_Linear_CM = mEffect.Techniques["Modulate4X_Linear_CM"]; } catch { }
                    try { mTechniqueInverseTexture_Linear_CM = mEffect.Techniques["InverseTexture_Linear_CM"]; } catch { }
                    try { mTechniqueColor_Linear_CM = mEffect.Techniques["Color_Linear_CM"]; } catch { }
                    try { mTechniqueColorTextureAlpha_Linear_CM = mEffect.Techniques["ColorTextureAlpha_Linear_CM"]; } catch { }
                    try { mTechniqueInterpolateColor_Linear_CM = mEffect.Techniques["InterpolateColor_Linear_CM"]; } catch { }

                    try { mTechniqueTexture_Linear_LN = mEffect.Techniques["Texture_Linear_LN"]; } catch { }
                    try { mTechniqueAdd_Linear_LN = mEffect.Techniques["Add_Linear_LN"]; } catch { }
                    try { mTechniqueSubtract_Linear_LN = mEffect.Techniques["Subtract_Linear_LN"]; } catch { }
                    try { mTechniqueModulate_Linear_LN = mEffect.Techniques["Modulate_Linear_LN"]; } catch { }
                    try { mTechniqueModulate2X_Linear_LN = mEffect.Techniques["Modulate2X_Linear_LN"]; } catch { }
                    try { mTechniqueModulate4X_Linear_LN = mEffect.Techniques["Modulate4X_Linear_LN"]; } catch { }
                    try { mTechniqueInverseTexture_Linear_LN = mEffect.Techniques["InverseTexture_Linear_LN"]; } catch { }
                    try { mTechniqueColor_Linear_LN = mEffect.Techniques["Color_Linear_LN"]; } catch { }
                    try { mTechniqueColorTextureAlpha_Linear_LN = mEffect.Techniques["ColorTextureAlpha_Linear_LN"]; } catch { }
                    try { mTechniqueInterpolateColor_Linear_LN = mEffect.Techniques["InterpolateColor_Linear_LN"]; } catch { }

                    try { mTechniqueTexture_Linear_LN_CM = mEffect.Techniques["Texture_Linear_LN_CM"]; } catch { }
                    try { mTechniqueAdd_Linear_LN_CM = mEffect.Techniques["Add_Linear_LN_CM"]; } catch { }
                    try { mTechniqueSubtract_Linear_LN_CM = mEffect.Techniques["Subtract_Linear_LN_CM"]; } catch { }
                    try { mTechniqueModulate_Linear_LN_CM = mEffect.Techniques["Modulate_Linear_LN_CM"]; } catch { }
                    try { mTechniqueModulate2X_Linear_LN_CM = mEffect.Techniques["Modulate2X_Linear_LN_CM"]; } catch { }
                    try { mTechniqueModulate4X_Linear_LN_CM = mEffect.Techniques["Modulate4X_Linear_LN_CM"]; } catch { }
                    try { mTechniqueInverseTexture_Linear_LN_CM = mEffect.Techniques["InverseTexture_Linear_LN_CM"]; } catch { }
                    try { mTechniqueColor_Linear_LN_CM = mEffect.Techniques["Color_Linear_LN_CM"]; } catch { }
                    try { mTechniqueColorTextureAlpha_Linear_LN_CM = mEffect.Techniques["ColorTextureAlpha_Linear_LN_CM"]; } catch { }
                    try { mTechniqueInterpolateColor_Linear_LN_CM = mEffect.Techniques["InterpolateColor_Linear_LN_CM"]; } catch { }
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

        public EffectTechnique GetVertexColorTechniqueFromColorOperation(ColorOperation value, bool? useDefaultOrPointFilter = null)
        {
            if (mEffect == null)
                throw new Exception("The effect hasn't been set.");

            EffectTechnique technique = null;

            bool useDefaultOrPointFilterInternal;

            if (mEffectHasNewformat)
            {
                // If the shader has the new format both point and linear are available
                if (!useDefaultOrPointFilter.HasValue)
                {
                    // Filter not specified, so we get the filter from options
                    useDefaultOrPointFilterInternal = FlatRedBallServices.GraphicsOptions.TextureFilter == TextureFilter.Point;
                }
                else
                {
                    // Filter specified
                    useDefaultOrPointFilterInternal = useDefaultOrPointFilter.Value;
                }
            }
            else
            {
                // If the shader doesn't have the new format only one version of
                // the techniques are available, probably using point filtering.
                useDefaultOrPointFilterInternal = true;
            }

            switch (value)
            {
                case ColorOperation.Texture:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, mTechniqueTexture, mTechniqueTexture_LN, mTechniqueTexture_Linear, mTechniqueTexture_Linear_LN); break;

                case ColorOperation.Add:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, mTechniqueAdd, mTechniqueAdd_LN, mTechniqueAdd_Linear, mTechniqueAdd_Linear_LN); break;

                case ColorOperation.Subtract:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, mTechniqueSubtract, mTechniqueSubtract_LN, mTechniqueSubtract_Linear, mTechniqueSubtract_Linear_LN); break;

                case ColorOperation.Modulate:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, mTechniqueModulate, mTechniqueModulate_LN, mTechniqueModulate_Linear, mTechniqueModulate_Linear_LN); break;

                case ColorOperation.Modulate2X:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, mTechniqueModulate2X, mTechniqueModulate2X_LN, mTechniqueModulate2X_Linear, mTechniqueModulate2X_Linear_LN); break;

                case ColorOperation.Modulate4X:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, mTechniqueModulate4X, mTechniqueModulate4X_LN, mTechniqueModulate4X_Linear, mTechniqueModulate4X_Linear_LN); break;

                case ColorOperation.InverseTexture:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, mTechniqueInverseTexture, mTechniqueInverseTexture_LN, mTechniqueInverseTexture_Linear, mTechniqueInverseTexture_Linear_LN); break;

                case ColorOperation.Color:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, mTechniqueColor, mTechniqueColor_LN, mTechniqueColor_Linear, mTechniqueColor_Linear_LN); break;

                case ColorOperation.ColorTextureAlpha:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, mTechniqueColorTextureAlpha, mTechniqueColorTextureAlpha_LN, mTechniqueColorTextureAlpha_Linear, mTechniqueColorTextureAlpha_Linear_LN); break;

                case ColorOperation.InterpolateColor:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, mTechniqueInterpolateColor, mTechniqueInterpolateColor_LN, mTechniqueInterpolateColor_Linear, mTechniqueInterpolateColor_Linear_LN); break;

                default: throw new InvalidOperationException();
            }

            return technique;
        }

        public EffectTechnique GetColorModifierTechniqueFromColorOperation(ColorOperation value, bool? useDefaultOrPointFilter = null)
        {
            if (mEffect == null)
                throw new Exception("The effect hasn't been set.");

            EffectTechnique technique = null;

            bool useDefaultOrPointFilterInternal;

            if (mEffectHasNewformat)
            {
                // If the shader has the new format both point and linear are available
                if (!useDefaultOrPointFilter.HasValue)
                {
                    // Filter not specified, so we get the filter from options
                    useDefaultOrPointFilterInternal = FlatRedBallServices.GraphicsOptions.TextureFilter == TextureFilter.Point;
                }
                else
                {
                    // Filter specified
                    useDefaultOrPointFilterInternal = useDefaultOrPointFilter.Value;
                }
            }
            else
            {
                // If the shader doesn't have the new format only one version of
                // the techniques are available, probably using point filtering.
                useDefaultOrPointFilterInternal = true;
            }

            switch (value)
            {
                case ColorOperation.Texture:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, mTechniqueTexture_CM, mTechniqueTexture_LN_CM, mTechniqueTexture_Linear_CM, mTechniqueTexture_Linear_LN_CM); break;

                case ColorOperation.Add:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, mTechniqueAdd_CM, mTechniqueAdd_LN_CM, mTechniqueAdd_Linear_CM, mTechniqueAdd_Linear_LN_CM); break;

                case ColorOperation.Subtract:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, mTechniqueSubtract_CM, mTechniqueSubtract_LN_CM, mTechniqueSubtract_Linear_CM, mTechniqueSubtract_Linear_LN_CM); break;

                case ColorOperation.Modulate:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, mTechniqueModulate_CM, mTechniqueModulate_LN_CM, mTechniqueModulate_Linear_CM, mTechniqueModulate_Linear_LN_CM); break;

                case ColorOperation.Modulate2X:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, mTechniqueModulate2X_CM, mTechniqueModulate2X_LN_CM, mTechniqueModulate2X_Linear_CM, mTechniqueModulate2X_Linear_LN_CM); break;

                case ColorOperation.Modulate4X:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, mTechniqueModulate4X_CM, mTechniqueModulate4X_LN_CM, mTechniqueModulate4X_Linear_CM, mTechniqueModulate4X_Linear_LN_CM); break;

                case ColorOperation.InverseTexture:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, mTechniqueInverseTexture_CM, mTechniqueInverseTexture_LN_CM, mTechniqueInverseTexture_Linear_CM, mTechniqueInverseTexture_Linear_LN_CM); break;

                case ColorOperation.Color:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, mTechniqueColor_CM, mTechniqueColor_LN_CM, mTechniqueColor_Linear_CM, mTechniqueColor_Linear_LN_CM); break;

                case ColorOperation.ColorTextureAlpha:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, mTechniqueColorTextureAlpha_CM, mTechniqueColorTextureAlpha_LN_CM, mTechniqueColorTextureAlpha_Linear_CM, mTechniqueColorTextureAlpha_Linear_LN_CM); break;

                case ColorOperation.InterpolateColor:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, mTechniqueInterpolateColor_CM, mTechniqueInterpolateColor_LN_CM, mTechniqueInterpolateColor_Linear_CM, mTechniqueInterpolateColor_Linear_LN_CM); break;

                default: throw new InvalidOperationException();
            }

            return technique;
        }

        public static Vector4 ProcessColorForColorOperation(ColorOperation colorOperation, Vector4 input)
        {
            if (colorOperation == ColorOperation.Color)
            {
                return new Vector4(input.X * input.W, input.Y * input.W, input.Z * input.W, input.W);
            }
            else if (colorOperation == ColorOperation.Texture)
            {
                return new Vector4(input.W, input.W, input.W, input.W);
            }
            else
            {
                return new Vector4(input.X, input.Y, input.Z, input.W);
            }
        }
    }
}
