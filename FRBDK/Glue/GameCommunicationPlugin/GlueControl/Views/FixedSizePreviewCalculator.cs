using System;

namespace GameCommunicationPlugin.GlueControl.Views
{
    /// <summary>
    /// Computes the embedded game window size for the Game tab's fixed-size preview toggle
    /// (issue #2035): the target resolution scaled down to fit the available panel, but never
    /// scaled up past 100% - the caller centers the result, letting excess panel space show as
    /// letterbox bars instead of stretching the game window to fill the whole tab.
    /// </summary>
    public static class FixedSizePreviewCalculator
    {
        public static (int Width, int Height) GetEmbeddedWindowSize(
            double panelWidth, double panelHeight, int targetWidth, int targetHeight)
        {
            if (targetWidth <= 0 || targetHeight <= 0 || panelWidth <= 0 || panelHeight <= 0)
            {
                return ((int)Math.Max(0, panelWidth), (int)Math.Max(0, panelHeight));
            }

            var scale = Math.Min(1.0, Math.Min(panelWidth / targetWidth, panelHeight / targetHeight));

            return (
                (int)Math.Round(targetWidth * scale),
                (int)Math.Round(targetHeight * scale));
        }

        /// <summary>
        /// "100%" for fixed-size preview is the project's default desktop display size, not its
        /// raw internal resolution - Camera Settings' Scale (e.g. 400% for a pixel-art project)
        /// is how a real launched game window would already be upscaled.
        /// </summary>
        public static (int Width, int Height) GetEffectiveTargetResolution(
            int resolutionWidth, int resolutionHeight, int scalePercent)
        {
            if (scalePercent <= 0)
            {
                return (resolutionWidth, resolutionHeight);
            }

            var scale = scalePercent / 100.0;

            return (
                (int)Math.Round(resolutionWidth * scale),
                (int)Math.Round(resolutionHeight * scale));
        }
    }
}
