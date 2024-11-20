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
        [Obsolete("Use XRespectingGumZoomAndBounds")]
        public static float GumX (this FlatRedBall.Gui.Cursor cursor) => XRespectingGumZoomAndBounds(cursor);

        /// <summary>
        /// Returns the screen X of the cursor, updated by zoom and viewport bounds.
        /// </summary>
        /// <param name="cursor">The argument cursor.</param>
        /// <returns>The X coordinate which can be used for UI.</returns>
        public static float XRespectingGumZoomAndBounds(this FlatRedBall.Gui.Cursor cursor)
        {
            var renderer = RenderingLibrary.SystemManagers.Default.Renderer;
            var zoom = renderer.Camera.Zoom;
            return (cursor.ScreenX / zoom) - renderer.GraphicsDevice.Viewport.Bounds.Left;
        }


        [Obsolete("Use YRespectingGumZoomAndBounds")]
        public static float GumY(this FlatRedBall.Gui.Cursor cursor) => YRespectingGumZoomAndBounds(cursor);


        /// <summary>
        /// Returns the screen Y of the cursor, updated by zoom and viewport bounds.
        /// </summary>
        /// <param name="cursor">The argument cursor.</param>
        /// <returns>The Y coordiante which can be used for UI.</returns>
        public static float YRespectingGumZoomAndBounds(this FlatRedBall.Gui.Cursor cursor)
        {
            var renderer = RenderingLibrary.SystemManagers.Default.Renderer;
            var zoom = renderer.Camera.Zoom;
            return (cursor.ScreenY / zoom) - renderer.GraphicsDevice.Viewport.Bounds.Top ;
        }

    }
}
