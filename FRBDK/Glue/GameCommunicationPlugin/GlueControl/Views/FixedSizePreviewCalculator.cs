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
    }
}
