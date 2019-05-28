using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.AnimationEditorForms.Controls;
using FlatRedBall.AnimationEditorForms.Wireframe;
using FlatRedBall.SpecializedXnaControls;
using FlatRedBall.SpecializedXnaControls.Input;
using InputLibrary;
using RenderingLibrary;
using XnaAndWinforms;

namespace FlatRedBall.AnimationEditorForms.Preview
{
    class CameraController
    {
        CameraPanningLogic cameraPanningLogic;

        public Ruler LeftRuler { get; set; }
        public Ruler TopRuler { get; set; }

        public int ZoomValue
        {
            get
            {
                return RenderingLibrary.Math.MathFunctions.RoundToInt(Managers.Renderer.Camera.Zoom * 100);
            }
            set
            {
                if (Managers != null)
                {
                    Managers.Renderer.Camera.Zoom = value / 100.0f;

                    this.TopRuler.ZoomValue = value / 100.0f;
                    this.LeftRuler.ZoomValue = value / 100.0f;
                }
            }
        }

        public Cursor Cursor
        {
            get;
            private set;
        }

        public SystemManagers Managers
        {
            get;
            private set;
        }

        public RenderingLibrary.Camera Camera
        {
            get;
            private set;
        }

        public CameraController(RenderingLibrary.Camera camera, SystemManagers managers, Cursor cursor, Keyboard keyboard, GraphicsDeviceControl control, Ruler topRuler, Ruler leftRuler)
        {
            this.TopRuler = topRuler;
            this.LeftRuler = leftRuler;
            Cursor = cursor;
            Camera = camera;
            Managers = managers;

            cameraPanningLogic = new CameraPanningLogic(control, managers, cursor, keyboard);

        }

        public void HandleMouseWheel(Cursor cursor, int change, PreviewControls previewControls)
        {
            
            float worldX = cursor.GetWorldX(Managers);
            float worldY = cursor.GetWorldY(Managers);

            float oldCameraX = Camera.X;
            float oldCameraY = Camera.Y;


            float oldZoom = ZoomValue / 100.0f;

            if (change > 0)
            {
                previewControls.ZoomIn();
            }
            else
            {
                previewControls.ZoomOut();
            }

            ImageRegionSelectionControl.AdjustCameraPositionAfterZoom(worldX, worldY,
                oldCameraX, oldCameraY, oldZoom, ZoomValue, Camera);


        }
    }
}
