using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Gui
{
    /// <summary>
    /// Contains extension methods for the Cursor class for interacting with Gum.
    /// </summary>
    public static class CursorExtensions
    {
        /// <summary>
        /// Returns the screen X of the cursor, updated by zoom and viewport bounds.
        /// </summary>
        /// <param name="cursor">The argument cursor.</param>
        /// <returns>The X coordinate which can be used for UI.</returns>
        public static float GumX (this FlatRedBall.Gui.Cursor cursor)
        {
            var renderer = RenderingLibrary.SystemManagers.Default.Renderer;
            var zoom = renderer.Camera.Zoom;
            return (cursor.ScreenX / zoom) - renderer.GraphicsDevice.Viewport.Bounds.Left;
        }

        /// <summary>
        /// Returns the screen Y of the cursor, updated by zoom and viewport bounds.
        /// </summary>
        /// <param name="cursor">The argument cursor.</param>
        /// <returns>The Y coordiante which can be used for UI.</returns>
        public static float GumY(this FlatRedBall.Gui.Cursor cursor)
        {
            var renderer = RenderingLibrary.SystemManagers.Default.Renderer;
            var zoom = renderer.Camera.Zoom;
            return (cursor.ScreenY / zoom) - renderer.GraphicsDevice.Viewport.Bounds.Top ;
        }
    }
}
