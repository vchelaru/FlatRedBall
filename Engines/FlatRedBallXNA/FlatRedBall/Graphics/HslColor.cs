using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework;

namespace FlatRedBall.Graphics;

/// <summary>
/// HSL stands for Hue, Saturation, and Luminance.
/// HSL may also referred to as HSB, where the B
/// stands for Brightness. This color space makes
/// it easier to do calculations that operate on
/// these channels and is supported by most major
/// graphics applications.
///
/// An example operation that can be done on HSL
/// but not on RGB is finding the complement of
/// a color. This is a hue operation that just
/// requires adding 0.5 to the Hue channel (and
/// wrapping overflow) in HSL but is impossible
/// in RGB space.
/// 
/// Helpful color math can be found here:
/// https://www.easyrgb.com/en/math.php
/// </summary>
public struct HSLColor
{

    /// <summary>
    /// Hue, a value between 0 and 1. The center of major hues are
    /// red: 0, orange: 0.5, yellow 0.15, green 0.35, blue 0.65,
    /// purple, 0.75, and magenta 0.8.
    /// </summary>
    public float H;

    /// <summary>
    /// Saturation, a value between 0 and 1. A value of 0 will be black, white, or gray depending on Luminance.
    /// A value of 1 will be fully saturated, displaying the color determined by Hue and Luminance.
    /// </summary>
    public float S;

    /// <summary>
    /// Luminance (brightness), a value between 0 and 1. A value of 0 is black. A value of 0.5 depends on
    /// hue and saturation and represents a fully saturated color. A value of 1 is white.
    /// </summary>
    public float L;

    /// <summary>
    /// Gets this colors complementary color
    /// </summary>
    public HSLColor Complement
    {
        get
        {
            // complementary colors are across the color wheel
            // which is 180 degrees or 50% of the way around the
            // wheel. Add 50% to our hue (color) aspect of the
            // HSL color
            var h = H + 0.5f;
            if (h > 1)
            {
                h -= 1;
            }

            return new HSLColor(h, S, L);
        }
    }

    /// <summary>
    /// Constructor for a new HSL color
    /// </summary>
    /// <param name="h">Hue parameter, 0 to 1</param>
    /// <param name="s">Saturation parameter, 0 to 1</param>
    /// <param name="l">Luminosity/Brightness parameter, 0 to 1</param>
    public HSLColor(float h, float s, float l)
    {
        H = h;
        S = s;
        L = l;
    }

    /// <summary>
    /// Gets an HSL color from an RGB color
    /// </summary>
    /// <param name="R">Red byte value</param>
    /// <param name="G">Green byte value</param>
    /// <param name="B">Blue byte value</param>
    /// <returns>An HSL color that approximates the RGB values</returns>
    public static HSLColor FromRgb(byte R, byte G, byte B)
    {
        var hsl = new HSLColor();

        hsl.H = 0;
        hsl.S = 0;
        hsl.L = 0;

        float r = R / 255f;
        float g = G / 255f;
        float b = B / 255f;
        float min = System.Math.Min(System.Math.Min(r, g), b);
        float max = System.Math.Max(System.Math.Max(r, g), b);
        float delta = max - min;

        // luminance is the ave of max and min
        hsl.L = (max + min) / 2f;

        if (delta > 0)
        {
            if (hsl.L < 0.5f)
            {
                hsl.S = delta / (max + min);
            }
            else
            {
                hsl.S = delta / (2 - max - min);
            }

            float deltaR = (((max - r) / 6f) + (delta / 2f)) / delta;
            float deltaG = (((max - g) / 6f) + (delta / 2f)) / delta;
            float deltaB = (((max - b) / 6f) + (delta / 2f)) / delta;

            if (r == max)
            {
                hsl.H = deltaB - deltaG;
            }
            else if (g == max)
            {
                hsl.H = (1f / 3f) + deltaR - deltaB;
            }
            else if (b == max)
            {
                hsl.H = (2f / 3f) + deltaG - deltaR;
            }

            if (hsl.H < 0)
            {
                hsl.H += 1;
            }

            if (hsl.H > 1)
            {
                hsl.H -= 1;
            }
        }

        return hsl;
    }

    /// <summary>
    /// Gets an HSL color from an RGB color.
    /// 
    /// Wraps FromRgb for convenience.
    /// </summary>
    /// <param name="color">The color to convert</param>
    /// <returns>An HSL color that approximates the RGB values</returns>
    public static HSLColor FromRgb(this Color color)
    {
        return FromRgb(color.R, color.G, color.B);
    }

    /// <summary>
    /// Gets an RGB color from an HSL color
    /// </summary>
    /// <returns>The RGB approximation of this HSL color</returns>
    public Color ToRgb()
    {
        var hsl = this;
        var c = new Color();

        if (hsl.S == 0)
        {
            c.R = (byte)(hsl.L * 255f);
            c.G = (byte)(hsl.L * 255f);
            c.B = (byte)(hsl.L * 255f);
        }
        else
        {
            float v2 = (hsl.L + hsl.S) - (hsl.S * hsl.L);
            if (hsl.L < 0.5f)
            {
                v2 = hsl.L * (1 + hsl.S);
            }
            float v1 = 2f * hsl.L - v2;

            c.R = (byte)(255f * HueToRgb(v1, v2, hsl.H + (1f / 3f)));
            c.G = (byte)(255f * HueToRgb(v1, v2, hsl.H));
            c.B = (byte)(255f * HueToRgb(v1, v2, hsl.H - (1f / 3f)));
        }

        c.A = 255;

        return c;
    }

    /// <summary>
    /// Internal math function that is part of the conversion
    /// process. For details see:
    /// https://www.easyrgb.com/en/math.php
    /// </summary>
    private float HueToRgb(float v1, float v2, float vH)
    {
        vH += (vH < 0) ? 1 : 0;
        vH -= (vH > 1) ? 1 : 0;
        float returnValue = v1;

        if ((6 * vH) < 1)
        {
            returnValue = (v1 + (v2 - v1) * 6 * vH);
        }

        else if ((2 * vH) < 1)
        {
            returnValue = (v2);
        }

        else if ((3 * vH) < 2)
        {
            returnValue = (v1 + (v2 - v1) * ((2f / 3f) - vH) * 6f);
        }

        returnValue = System.Math.Min(returnValue, 1);
        returnValue = System.Math.Max(returnValue, 0);

        return returnValue;
    }

    public override string ToString()
    {
        return $"H: {H}, S: {S}, L: {L}";
    }
}
