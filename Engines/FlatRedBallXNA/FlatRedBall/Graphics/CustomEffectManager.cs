using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FlatRedBall.Graphics
{
    /// <summary>
    /// Manages custom effects from the custom shader file. Main purposes:
    /// <list type="number">
    /// <item><description>Caches effect parameters and techniques to avoid lookups during rendering.</description></item>
    /// <item><description>Handles compatibility between old and new effect specifications with automatic fallback.</description></item>
    /// <item><description>Provides methods to retrieve techniques based on:
    /// <list type="bullet">
    /// <item><description>Texture filtering (Point/Linear)</description></item>
    /// <item><description>Color source (VertexColor/ColorModifier)</description></item>
    /// <item><description>Color operation (Add, Subtract, Modulate, etc.)</description></item>
    /// <item><description>Gamma correction (Linearize methods)</description></item>
    /// </list>
    /// </description></item>
    /// </list>
    /// This class is designed for use by renderers and custom graphics code.
    /// </summary>
    public class CustomEffectManager
    {
        Effect _effect = null!;

        // Cached effect members to avoid list lookups while rendering
        public EffectParameter ParameterCurrentTexture = null!;
        public EffectParameter ParameterViewProj = null!;
        public EffectParameter? ParameterColorModifier;

        bool _effectHasNewformat;

        EffectTechnique? _techniqueTexture;
        EffectTechnique? _techniqueAdd;
        EffectTechnique? _techniqueSubtract;
        EffectTechnique? _techniqueModulate;
        EffectTechnique? _techniqueModulate2X;
        EffectTechnique? _techniqueModulate4X;
        EffectTechnique? _techniqueInverseTexture;
        EffectTechnique? _techniqueColor;
        EffectTechnique? _techniqueColorTextureAlpha;
        EffectTechnique? _techniqueInterpolateColor;

        EffectTechnique? _techniqueTexture_CM;
        EffectTechnique? _techniqueAdd_CM;
        EffectTechnique? _techniqueSubtract_CM;
        EffectTechnique? _techniqueModulate_CM;
        EffectTechnique? _techniqueModulate2X_CM;
        EffectTechnique? _techniqueModulate4X_CM;
        EffectTechnique? _techniqueInverseTexture_CM;
        EffectTechnique? _techniqueColor_CM;
        EffectTechnique? _techniqueColorTextureAlpha_CM;
        EffectTechnique? _techniqueInterpolateColor_CM;

        EffectTechnique? _techniqueTexture_LN;
        EffectTechnique? _techniqueAdd_LN;
        EffectTechnique? _techniqueSubtract_LN;
        EffectTechnique? _techniqueModulate_LN;
        EffectTechnique? _techniqueModulate2X_LN;
        EffectTechnique? _techniqueModulate4X_LN;
        EffectTechnique? _techniqueInverseTexture_LN;
        EffectTechnique? _techniqueColor_LN;
        EffectTechnique? _techniqueColorTextureAlpha_LN;
        EffectTechnique? _techniqueInterpolateColor_LN;

        EffectTechnique? _techniqueTexture_LN_CM;
        EffectTechnique? _techniqueAdd_LN_CM;
        EffectTechnique? _techniqueSubtract_LN_CM;
        EffectTechnique? _techniqueModulate_LN_CM;
        EffectTechnique? _techniqueModulate2X_LN_CM;
        EffectTechnique? _techniqueModulate4X_LN_CM;
        EffectTechnique? _techniqueInverseTexture_LN_CM;
        EffectTechnique? _techniqueColor_LN_CM;
        EffectTechnique? _techniqueColorTextureAlpha_LN_CM;
        EffectTechnique? _techniqueInterpolateColor_LN_CM;

        EffectTechnique? _techniqueTexture_Linear;
        EffectTechnique? _techniqueAdd_Linear;
        EffectTechnique? _techniqueSubtract_Linear;
        EffectTechnique? _techniqueModulate_Linear;
        EffectTechnique? _techniqueModulate2X_Linear;
        EffectTechnique? _techniqueModulate4X_Linear;
        EffectTechnique? _techniqueInverseTexture_Linear;
        EffectTechnique? _techniqueColor_Linear;
        EffectTechnique? _techniqueColorTextureAlpha_Linear;
        EffectTechnique? _techniqueInterpolateColor_Linear;

        EffectTechnique? _techniqueTexture_Linear_CM;
        EffectTechnique? _techniqueAdd_Linear_CM;
        EffectTechnique? _techniqueSubtract_Linear_CM;
        EffectTechnique? _techniqueModulate_Linear_CM;
        EffectTechnique? _techniqueModulate2X_Linear_CM;
        EffectTechnique? _techniqueModulate4X_Linear_CM;
        EffectTechnique? _techniqueInverseTexture_Linear_CM;
        EffectTechnique? _techniqueColor_Linear_CM;
        EffectTechnique? _techniqueColorTextureAlpha_Linear_CM;
        EffectTechnique? _techniqueInterpolateColor_Linear_CM;

        EffectTechnique? _techniqueTexture_Linear_LN;
        EffectTechnique? _techniqueAdd_Linear_LN;
        EffectTechnique? _techniqueSubtract_Linear_LN;
        EffectTechnique? _techniqueModulate_Linear_LN;
        EffectTechnique? _techniqueModulate2X_Linear_LN;
        EffectTechnique? _techniqueModulate4X_Linear_LN;
        EffectTechnique? _techniqueInverseTexture_Linear_LN;
        EffectTechnique? _techniqueColor_Linear_LN;
        EffectTechnique? _techniqueColorTextureAlpha_Linear_LN;
        EffectTechnique? _techniqueInterpolateColor_Linear_LN;

        EffectTechnique? _techniqueTexture_Linear_LN_CM;
        EffectTechnique? _techniqueAdd_Linear_LN_CM;
        EffectTechnique? _techniqueSubtract_Linear_LN_CM;
        EffectTechnique? _techniqueModulate_Linear_LN_CM;
        EffectTechnique? _techniqueModulate2X_Linear_LN_CM;
        EffectTechnique? _techniqueModulate4X_Linear_LN_CM;
        EffectTechnique? _techniqueInverseTexture_Linear_LN_CM;
        EffectTechnique? _techniqueColor_Linear_LN_CM;
        EffectTechnique? _techniqueColorTextureAlpha_Linear_LN_CM;
        EffectTechnique? _techniqueInterpolateColor_Linear_LN_CM;

        public Effect Effect
        {
            get { return _effect; }
            set
            {
                _effect = value;

                var parameterViewProj = GetParameterSafe("ViewProj");
                if (parameterViewProj == null) // ViewProj is required. Throw exception if null.
                {
                    throw new InvalidOperationException("Shader.xnb must contain a parameter called ViewProj.");
                }

                ParameterViewProj = parameterViewProj;

                var parameterCurrentTexture = GetParameterSafe("CurrentTexture");
                if (parameterCurrentTexture == null) // CurrentTexture is required. Throw exception if null.
                {
                    throw new InvalidOperationException("Shader.xnb must contain a parameter called CurrentTexture.");
                }

                ParameterCurrentTexture = parameterCurrentTexture;

                ParameterColorModifier = GetParameterSafe("ColorModifier");

                // Let's check if the shader has the new format (which includes
                // separate versions of techniques for Point and Linear filtering).
                // We try to cache the first technique in order to do so.
                _techniqueTexture = GetTechniqueSafe("Texture_Point");

                if (_techniqueTexture != null)
                {
                    _effectHasNewformat = true;

                    //_techniqueTexture = GetTechniqueSafe("Texture_Point"); // This has been already cached
                    _techniqueAdd = GetTechniqueSafe("Add_Point");
                    _techniqueSubtract = GetTechniqueSafe("Subtract_Point");
                    _techniqueModulate = GetTechniqueSafe("Modulate_Point");
                    _techniqueModulate2X = GetTechniqueSafe("Modulate2X_Point");
                    _techniqueModulate4X = GetTechniqueSafe("Modulate4X_Point");
                    _techniqueInverseTexture = GetTechniqueSafe("InverseTexture_Point");
                    _techniqueColor = GetTechniqueSafe("Color_Point");
                    _techniqueColorTextureAlpha = GetTechniqueSafe("ColorTextureAlpha_Point");
                    _techniqueInterpolateColor = GetTechniqueSafe("InterpolateColor_Point");

                    _techniqueTexture_CM = GetTechniqueSafe("Texture_Point_CM");
                    _techniqueAdd_CM = GetTechniqueSafe("Add_Point_CM");
                    _techniqueSubtract_CM = GetTechniqueSafe("Subtract_Point_CM");
                    _techniqueModulate_CM = GetTechniqueSafe("Modulate_Point_CM");
                    _techniqueModulate2X_CM = GetTechniqueSafe("Modulate2X_Point_CM");
                    _techniqueModulate4X_CM = GetTechniqueSafe("Modulate4X_Point_CM");
                    _techniqueInverseTexture_CM = GetTechniqueSafe("InverseTexture_Point_CM");
                    _techniqueColor_CM = GetTechniqueSafe("Color_Point_CM");
                    _techniqueColorTextureAlpha_CM = GetTechniqueSafe("ColorTextureAlpha_Point_CM");
                    _techniqueInterpolateColor_CM = GetTechniqueSafe("InterpolateColor_Point_CM");

                    _techniqueTexture_LN = GetTechniqueSafe("Texture_Point_LN");
                    _techniqueAdd_LN = GetTechniqueSafe("Add_Point_LN");
                    _techniqueSubtract_LN = GetTechniqueSafe("Subtract_Point_LN");
                    _techniqueModulate_LN = GetTechniqueSafe("Modulate_Point_LN");
                    _techniqueModulate2X_LN = GetTechniqueSafe("Modulate2X_Point_LN");
                    _techniqueModulate4X_LN = GetTechniqueSafe("Modulate4X_Point_LN");
                    _techniqueInverseTexture_LN = GetTechniqueSafe("InverseTexture_Point_LN");
                    _techniqueColor_LN = GetTechniqueSafe("Color_Point_LN");
                    _techniqueColorTextureAlpha_LN = GetTechniqueSafe("ColorTextureAlpha_Point_LN");
                    _techniqueInterpolateColor_LN = GetTechniqueSafe("InterpolateColor_Point_LN");

                    _techniqueTexture_LN_CM = GetTechniqueSafe("Texture_Point_LN_CM");
                    _techniqueAdd_LN_CM = GetTechniqueSafe("Add_Point_LN_CM");
                    _techniqueSubtract_LN_CM = GetTechniqueSafe("Subtract_Point_LN_CM");
                    _techniqueModulate_LN_CM = GetTechniqueSafe("Modulate_Point_LN_CM");
                    _techniqueModulate2X_LN_CM = GetTechniqueSafe("Modulate2X_Point_LN_CM");
                    _techniqueModulate4X_LN_CM = GetTechniqueSafe("Modulate4X_Point_LN_CM");
                    _techniqueInverseTexture_LN_CM = GetTechniqueSafe("InverseTexture_Point_LN_CM");
                    _techniqueColor_LN_CM = GetTechniqueSafe("Color_Point_LN_CM");
                    _techniqueColorTextureAlpha_LN_CM = GetTechniqueSafe("ColorTextureAlpha_Point_LN_CM");
                    _techniqueInterpolateColor_LN_CM = GetTechniqueSafe("InterpolateColor_Point_LN_CM");

                    _techniqueTexture_Linear = GetTechniqueSafe("Texture_Linear");
                    _techniqueAdd_Linear = GetTechniqueSafe("Add_Linear");
                    _techniqueSubtract_Linear = GetTechniqueSafe("Subtract_Linear");
                    _techniqueModulate_Linear = GetTechniqueSafe("Modulate_Linear");
                    _techniqueModulate2X_Linear = GetTechniqueSafe("Modulate2X_Linear");
                    _techniqueModulate4X_Linear = GetTechniqueSafe("Modulate4X_Linear");
                    _techniqueInverseTexture_Linear = GetTechniqueSafe("InverseTexture_Linear");
                    _techniqueColor_Linear = GetTechniqueSafe("Color_Linear");
                    _techniqueColorTextureAlpha_Linear = GetTechniqueSafe("ColorTextureAlpha_Linear");
                    _techniqueInterpolateColor_Linear = GetTechniqueSafe("InterpolateColor_Linear");

                    _techniqueTexture_Linear_CM = GetTechniqueSafe("Texture_Linear_CM");
                    _techniqueAdd_Linear_CM = GetTechniqueSafe("Add_Linear_CM");
                    _techniqueSubtract_Linear_CM = GetTechniqueSafe("Subtract_Linear_CM");
                    _techniqueModulate_Linear_CM = GetTechniqueSafe("Modulate_Linear_CM");
                    _techniqueModulate2X_Linear_CM = GetTechniqueSafe("Modulate2X_Linear_CM");
                    _techniqueModulate4X_Linear_CM = GetTechniqueSafe("Modulate4X_Linear_CM");
                    _techniqueInverseTexture_Linear_CM = GetTechniqueSafe("InverseTexture_Linear_CM");
                    _techniqueColor_Linear_CM = GetTechniqueSafe("Color_Linear_CM");
                    _techniqueColorTextureAlpha_Linear_CM = GetTechniqueSafe("ColorTextureAlpha_Linear_CM");
                    _techniqueInterpolateColor_Linear_CM = GetTechniqueSafe("InterpolateColor_Linear_CM");

                    _techniqueTexture_Linear_LN = GetTechniqueSafe("Texture_Linear_LN");
                    _techniqueAdd_Linear_LN = GetTechniqueSafe("Add_Linear_LN");
                    _techniqueSubtract_Linear_LN = GetTechniqueSafe("Subtract_Linear_LN");
                    _techniqueModulate_Linear_LN = GetTechniqueSafe("Modulate_Linear_LN");
                    _techniqueModulate2X_Linear_LN = GetTechniqueSafe("Modulate2X_Linear_LN");
                    _techniqueModulate4X_Linear_LN = GetTechniqueSafe("Modulate4X_Linear_LN");
                    _techniqueInverseTexture_Linear_LN = GetTechniqueSafe("InverseTexture_Linear_LN");
                    _techniqueColor_Linear_LN = GetTechniqueSafe("Color_Linear_LN");
                    _techniqueColorTextureAlpha_Linear_LN = GetTechniqueSafe("ColorTextureAlpha_Linear_LN");
                    _techniqueInterpolateColor_Linear_LN = GetTechniqueSafe("InterpolateColor_Linear_LN");

                    _techniqueTexture_Linear_LN_CM = GetTechniqueSafe("Texture_Linear_LN_CM");
                    _techniqueAdd_Linear_LN_CM = GetTechniqueSafe("Add_Linear_LN_CM");
                    _techniqueSubtract_Linear_LN_CM = GetTechniqueSafe("Subtract_Linear_LN_CM");
                    _techniqueModulate_Linear_LN_CM = GetTechniqueSafe("Modulate_Linear_LN_CM");
                    _techniqueModulate2X_Linear_LN_CM = GetTechniqueSafe("Modulate2X_Linear_LN_CM");
                    _techniqueModulate4X_Linear_LN_CM = GetTechniqueSafe("Modulate4X_Linear_LN_CM");
                    _techniqueInverseTexture_Linear_LN_CM = GetTechniqueSafe("InverseTexture_Linear_LN_CM");
                    _techniqueColor_Linear_LN_CM = GetTechniqueSafe("Color_Linear_LN_CM");
                    _techniqueColorTextureAlpha_Linear_LN_CM = GetTechniqueSafe("ColorTextureAlpha_Linear_LN_CM");
                    _techniqueInterpolateColor_Linear_LN_CM = GetTechniqueSafe("InterpolateColor_Linear_LN_CM");
                }
                else
                {
                    _effectHasNewformat = false;

                    _techniqueTexture = GetTechniqueSafe("Texture");
                    _techniqueAdd = GetTechniqueSafe("Add");
                    _techniqueSubtract = GetTechniqueSafe("Subtract");
                    _techniqueModulate = GetTechniqueSafe("Modulate");
                    _techniqueModulate2X = GetTechniqueSafe("Modulate2X");
                    _techniqueModulate4X = GetTechniqueSafe("Modulate4X");
                    _techniqueInverseTexture = GetTechniqueSafe("InverseTexture");
                    _techniqueColor = GetTechniqueSafe("Color");
                    _techniqueColorTextureAlpha = GetTechniqueSafe("ColorTextureAlpha");
                    _techniqueInterpolateColor = GetTechniqueSafe("InterpolateColor");
                }
            }
        }

        EffectParameter? GetParameterSafe(string parameterName)
        {
            if (_effect == null)
                return null;

            for (int i = 0; i < _effect.Parameters.Count; i++)
            {
                var parameter = _effect.Parameters[i];
                if (parameter.Name == parameterName)
                    return parameter;
            }

            return null;
        }

        EffectTechnique? GetTechniqueSafe(string techniqueName)
        {
            if (_effect == null)
                return null;

            for (int i = 0; i < _effect.Techniques.Count; i++)
            {
                var technique = _effect.Techniques[i];
                if (technique.Name == techniqueName)
                    return technique;
            }

            return null;
        }

        static EffectTechnique GetTechniqueVariant(bool useDefaultOrPointFilter, EffectTechnique point, EffectTechnique pointLinearized, EffectTechnique linear, EffectTechnique linearLinearized)
        {
            return useDefaultOrPointFilter ?
                (Renderer.LinearizeTextures ? pointLinearized : point) :
                (Renderer.LinearizeTextures ? linearLinearized : linear);
        }

        public EffectTechnique GetVertexColorTechniqueFromColorOperation(ColorOperation value, bool? useDefaultOrPointFilter = null)
        {
            if (_effect == null)
                throw new InvalidOperationException("The effect hasn't been set.");

            EffectTechnique technique = null!;

            bool useDefaultOrPointFilterInternal;

            if (_effectHasNewformat)
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
                    useDefaultOrPointFilterInternal, _techniqueTexture, _techniqueTexture_LN, _techniqueTexture_Linear, _techniqueTexture_Linear_LN); break;

                case ColorOperation.Add:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, _techniqueAdd, _techniqueAdd_LN, _techniqueAdd_Linear, _techniqueAdd_Linear_LN); break;

                case ColorOperation.Subtract:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, _techniqueSubtract, _techniqueSubtract_LN, _techniqueSubtract_Linear, _techniqueSubtract_Linear_LN); break;

                case ColorOperation.Modulate:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, _techniqueModulate, _techniqueModulate_LN, _techniqueModulate_Linear, _techniqueModulate_Linear_LN); break;

                case ColorOperation.Modulate2X:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, _techniqueModulate2X, _techniqueModulate2X_LN, _techniqueModulate2X_Linear, _techniqueModulate2X_Linear_LN); break;

                case ColorOperation.Modulate4X:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, _techniqueModulate4X, _techniqueModulate4X_LN, _techniqueModulate4X_Linear, _techniqueModulate4X_Linear_LN); break;

                case ColorOperation.InverseTexture:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, _techniqueInverseTexture, _techniqueInverseTexture_LN, _techniqueInverseTexture_Linear, _techniqueInverseTexture_Linear_LN); break;

                case ColorOperation.Color:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, _techniqueColor, _techniqueColor_LN, _techniqueColor_Linear, _techniqueColor_Linear_LN); break;

                case ColorOperation.ColorTextureAlpha:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, _techniqueColorTextureAlpha, _techniqueColorTextureAlpha_LN, _techniqueColorTextureAlpha_Linear, _techniqueColorTextureAlpha_Linear_LN); break;

                case ColorOperation.InterpolateColor:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, _techniqueInterpolateColor, _techniqueInterpolateColor_LN, _techniqueInterpolateColor_Linear, _techniqueInterpolateColor_Linear_LN); break;

                default: throw new InvalidOperationException();
            }

            return technique;
        }

        public EffectTechnique GetColorModifierTechniqueFromColorOperation(ColorOperation value, bool? useDefaultOrPointFilter = null)
        {
            if (_effect == null)
                throw new InvalidOperationException("The effect hasn't been set.");

            EffectTechnique technique = null!;

            bool useDefaultOrPointFilterInternal;

            if (_effectHasNewformat)
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
                    useDefaultOrPointFilterInternal, _techniqueTexture_CM, _techniqueTexture_LN_CM, _techniqueTexture_Linear_CM, _techniqueTexture_Linear_LN_CM); break;

                case ColorOperation.Add:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, _techniqueAdd_CM, _techniqueAdd_LN_CM, _techniqueAdd_Linear_CM, _techniqueAdd_Linear_LN_CM); break;

                case ColorOperation.Subtract:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, _techniqueSubtract_CM, _techniqueSubtract_LN_CM, _techniqueSubtract_Linear_CM, _techniqueSubtract_Linear_LN_CM); break;

                case ColorOperation.Modulate:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, _techniqueModulate_CM, _techniqueModulate_LN_CM, _techniqueModulate_Linear_CM, _techniqueModulate_Linear_LN_CM); break;

                case ColorOperation.Modulate2X:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, _techniqueModulate2X_CM, _techniqueModulate2X_LN_CM, _techniqueModulate2X_Linear_CM, _techniqueModulate2X_Linear_LN_CM); break;

                case ColorOperation.Modulate4X:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, _techniqueModulate4X_CM, _techniqueModulate4X_LN_CM, _techniqueModulate4X_Linear_CM, _techniqueModulate4X_Linear_LN_CM); break;

                case ColorOperation.InverseTexture:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, _techniqueInverseTexture_CM, _techniqueInverseTexture_LN_CM, _techniqueInverseTexture_Linear_CM, _techniqueInverseTexture_Linear_LN_CM); break;

                case ColorOperation.Color:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, _techniqueColor_CM, _techniqueColor_LN_CM, _techniqueColor_Linear_CM, _techniqueColor_Linear_LN_CM); break;

                case ColorOperation.ColorTextureAlpha:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, _techniqueColorTextureAlpha_CM, _techniqueColorTextureAlpha_LN_CM, _techniqueColorTextureAlpha_Linear_CM, _techniqueColorTextureAlpha_Linear_LN_CM); break;

                case ColorOperation.InterpolateColor:
                    technique = GetTechniqueVariant(
                    useDefaultOrPointFilterInternal, _techniqueInterpolateColor_CM, _techniqueInterpolateColor_LN_CM, _techniqueInterpolateColor_Linear_CM, _techniqueInterpolateColor_Linear_LN_CM); break;

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
