using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Gui
{
    public static class CursorExtensions
    {
        public static float GumX (this FlatRedBall.Gui.Cursor cursor)
        {
            var zoom = RenderingLibrary.SystemManagers.Default.Renderer.Camera.Zoom;
            return cursor.ScreenX / zoom;
        }

        public static float GumY(this FlatRedBall.Gui.Cursor cursor)
        {
            var zoom = RenderingLibrary.SystemManagers.Default.Renderer.Camera.Zoom;
            return cursor.ScreenY / zoom;
        }
    }
}
