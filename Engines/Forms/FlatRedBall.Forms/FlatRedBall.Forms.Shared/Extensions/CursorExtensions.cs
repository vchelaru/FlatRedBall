using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Gui
{
    public static class CursorExtensions
    {
        public static float GumX (this FlatRedBall.Gui.Cursor cursor)
        {
            var renderer = RenderingLibrary.SystemManagers.Default.Renderer;
            var zoom = renderer.Camera.Zoom;
            return (cursor.ScreenX / zoom) - renderer.GraphicsDevice.Viewport.Bounds.Left;
        }

        public static float GumY(this FlatRedBall.Gui.Cursor cursor)
        {
            var renderer = RenderingLibrary.SystemManagers.Default.Renderer;
            var zoom = renderer.Camera.Zoom;
            return (cursor.ScreenY / zoom) - renderer.GraphicsDevice.Viewport.Bounds.Top ;
        }
    }
}
