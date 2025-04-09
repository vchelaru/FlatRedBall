using System;
using Microsoft.Xna.Framework;

namespace FlatRedBall.ExtensionMethods;

public static class ColorExtensionMethods
{

    /// <summary>
    /// Tints a color by the provided percent, which can be
    /// negative or positive.
    /// 
    /// A positive value will darken the color, a negative value will lighten it.
    /// </summary>
    /// <param name="color">The color to tint.</param>
    /// <param name="percent">The tint amount as a percent from -1 to 1</param>
    /// <returns>A tinted color</returns>
    public static Color Tint(this Color color, float percent)
    {
        var newColor = new Color();
        newColor.R = (byte)MathHelper.Clamp(color.R * (1f - percent), 0, 255);
        newColor.G = (byte)MathHelper.Clamp(color.G * (1f - percent), 0, 255);
        newColor.B = (byte)MathHelper.Clamp(color.B * (1f - percent), 0, 255);
        newColor.A = color.A;
        return newColor;
    }

    /// <summary>
    /// Returns the inverse color (white minus color) for the
    /// provided color
    /// </summary>
    /// <param name="color">The color to invert</param>
    /// <returns>An inverted color</returns>
    public static Color Inverse(this Color color)
    {
        return new Color(
            255 - color.R,
            255 - color.G,
            255 - color.B,
            color.A
        );
    }

    /// <summary>
    /// Returns a premultiplied alpha version of the source color.
    /// </summary>
    /// <param name="color">The source color.</param>
    /// <param name="alpha">The alpha value (0.0 to 1.0).</param>
    /// <returns>A Color with premultiplied alpha.</returns>
    public static Color GetPremul(this Color color, float alpha)
    {
        // Clamp alpha to valid range [0.0, 1.0]
        alpha = MathHelper.Clamp(alpha, 0f, 1f);

        // Premultiply RGB values by alpha
        byte r = (byte)(color.R * alpha);
        byte g = (byte)(color.G * alpha);
        byte b = (byte)(color.B * alpha);
        byte a = (byte)(alpha * 255);

        return new Color(r, g, b, a);
    }

    /// <summary>
    /// Converts the provided color into a six-digit hexidecimal string
    /// such as used in CSS and graphics programs.
    /// </summary>
    /// <param name="color">The color to convert</param>
    /// <returns>A six-digit hex string with no prefix</returns>
    public static string ToHexString(this Color color)
    {
        return $"{color.R.ToString("X2")}{color.G.ToString("X2")}{color.B.ToString("X2")}";
    }

    /// <summary>
    /// Converts a 3 or 6 character hexidecimal string with no prefix to an RGB Color.
    /// Does not support alpha as part of the hex string.
    ///
    /// Valid examples are "F90" (CSS shorthand) or "FF9900" (full hex string)
    /// 
    /// Will return black and debug assert if bad values are provided.
    /// </summary>
    /// <param name="str">A valid, 6-digit hex string</param>
    /// <returns>The color from the provided string, will return pure black on error.</returns>
    public static Color HexStringToColor(this string str)
    {
        Color outColor = Color.Black;

        if (String.IsNullOrWhiteSpace(str) == false)
        {
            if (str.Length == 3)
            {
                var r = str.Substring(0, 1);
                var g = str.Substring(1, 1);
                var b = str.Substring(2, 1);
                str = r + r + g + g + b + b;
            }

            if (str.Length == 6)
            {
                try
                {
                    var R = Convert.ToInt16(str.Substring(0, 2), 16);
                    var G = Convert.ToInt16(str.Substring(2, 2), 16);
                    var B = Convert.ToInt16(str.Substring(4, 2), 16);
                    outColor = new Color(R, G, B);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.Assert(false, $"Bad values were provided to color conversion: {e.Message}");
                }

            }
        }

        return outColor;
    }
}
