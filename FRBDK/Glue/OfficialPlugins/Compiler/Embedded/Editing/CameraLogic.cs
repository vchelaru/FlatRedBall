{CompilerDirectives}

using FlatRedBall;
using FlatRedBall.Gui;
using System;

namespace {ProjectNamespace}.GlueControl.Editing
{
    class CameraLogic
    {
        #region Fields/Properties
        static int[] zoomLevels = new int[]
        {
            1600,
            1200,
            1000,
            800 ,
            700 ,
            600 ,
            500 ,
            400 ,
            350 ,
            300 ,
            250 ,
            200 ,
            175 ,
            150 ,
            125 ,
            100 ,
            87  ,
            75  ,
            63  ,
            50  ,
            33  ,
            25
        };
        static int currentZoomLevelIndex;

        public static float CurrentZoomRatio => zoomLevels[currentZoomLevelIndex] / 100.0f;

        #endregion

        static CameraLogic()
        {
            currentZoomLevelIndex = Array.IndexOf(zoomLevels, 100);
        }

        public static void DoCursorCameraControllingLogic()
        {
            var cursor = GuiManager.Cursor;
            var camera = Camera.Main;
            if (cursor.MiddleDown)
            {
                camera.X -= cursor.WorldXChangeAt(0);
                camera.Y -= cursor.WorldYChangeAt(0);
            }
            
            if (cursor.ZVelocity < 0)
            {
                currentZoomLevelIndex = Math.Min(currentZoomLevelIndex + 1, zoomLevels.Length - 1);
                UpdateToZoomLevel();
            }
            if(cursor.ZVelocity > 0)
            {
                currentZoomLevelIndex = Math.Max(currentZoomLevelIndex - 1, 0);
                UpdateToZoomLevel();
            }
        }

        private static void UpdateToZoomLevel()
        {
            var cursor = GuiManager.Cursor;
            var worldXBefore = cursor.WorldX;
            var worldYBefore = cursor.WorldY;
            Camera.Main.OrthogonalHeight = (CameraSetup.Data.Scale / 100.0f) * CameraSetup.Data.ResolutionHeight/ (zoomLevels[currentZoomLevelIndex] / 100.0f);
            Camera.Main.FixAspectRatioYConstant();

            var worldXAfterZoom = cursor.WorldX;
            var worldYAfterZoom = cursor.WorldY;

            Camera.Main.X -= worldXAfterZoom - worldXBefore;
            Camera.Main.Y -= worldYAfterZoom - worldYBefore;
        }
    }
}
