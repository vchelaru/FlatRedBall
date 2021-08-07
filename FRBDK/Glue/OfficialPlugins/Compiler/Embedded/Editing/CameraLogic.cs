{CompilerDirectives}

using {ProjectNamespace};

using FlatRedBall;
using FlatRedBall.Gui;
using System;

namespace GlueControl.Editing
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
                UpdateCameraToZoomLevel();
            }
            if (cursor.ZVelocity > 0)
            {
                currentZoomLevelIndex = Math.Max(currentZoomLevelIndex - 1, 0);
                UpdateCameraToZoomLevel();
            }
        }

        public static void UpdateZoomLevelToCamera()
        {
            var gameZoomLevel = FlatRedBall.Math.MathFunctions.RoundToInt(100 * Camera.Main.DestinationRectangle.Height / Camera.Main.OrthogonalHeight);

            if (zoomLevels.Contains(gameZoomLevel))
            {
                currentZoomLevelIndex = Array.IndexOf(zoomLevels, gameZoomLevel);
            }
            else
            {
                for (int i = 0; i < zoomLevels.Length; i++)
                {
                    if (zoomLevels[i] > gameZoomLevel)
                    {
                        currentZoomLevelIndex = i;
                        break;
                    }
                }
            }
        }

        private static void UpdateCameraToZoomLevel(bool zoomAroundCursorPosition = true)
        {
            var cursor = GuiManager.Cursor;
            var worldXBefore = cursor.WorldX;
            var worldYBefore = cursor.WorldY;

            var zoomLevel = zoomLevels[currentZoomLevelIndex];
            Camera.Main.OrthogonalHeight = (CameraSetup.Data.Scale / 100.0f) * CameraSetup.Data.ResolutionHeight / (zoomLevel / 100.0f);
            Camera.Main.FixAspectRatioYConstant();

            if (zoomAroundCursorPosition)
            {
                var worldXAfterZoom = cursor.WorldX;
                var worldYAfterZoom = cursor.WorldY;

                Camera.Main.X -= worldXAfterZoom - worldXBefore;
                Camera.Main.Y -= worldYAfterZoom - worldYBefore;
            }
        }

        internal static void DoHotkeyLogic()
        {
            const int movePerPush = 16;
            var keyboard = FlatRedBall.Input.InputManager.Keyboard;
            if (keyboard.IsCtrlDown)
            {
                if (keyboard.KeyTyped(Microsoft.Xna.Framework.Input.Keys.Up))
                {
                    Camera.Main.Y += movePerPush;
                }
                if (keyboard.KeyTyped(Microsoft.Xna.Framework.Input.Keys.Down))
                {
                    Camera.Main.Y -= movePerPush;
                }
                if (keyboard.KeyTyped(Microsoft.Xna.Framework.Input.Keys.Left))
                {
                    Camera.Main.X -= movePerPush;
                }
                if (keyboard.KeyTyped(Microsoft.Xna.Framework.Input.Keys.Right))
                {
                    Camera.Main.X += movePerPush;
                }
                if (keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.OemPlus))
                {
                    currentZoomLevelIndex = Math.Max(currentZoomLevelIndex - 1, 0);
                    UpdateCameraToZoomLevel(zoomAroundCursorPosition: false);
                }
                if (keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.OemMinus))
                {
                    currentZoomLevelIndex = Math.Min(currentZoomLevelIndex + 1, zoomLevels.Length - 1);
                    UpdateCameraToZoomLevel(zoomAroundCursorPosition: false);
                }
            }
        }
    }
}
