#if FRB_MDX || XNA3
#define SUPPORTS_FRB_DRAWN_GUI
#endif

using System;
using System.Collections.Generic;
using System.Text;

#if FRB_MDX
using Keys = Microsoft.DirectX.DirectInput.Key;
using Vector2 = Microsoft.DirectX.Vector2;
using Vector3 = Microsoft.DirectX.Vector3;
using Matrix = Microsoft.DirectX.Matrix;

using FlatRedBall.Math;
#elif FRB_XNA
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using Keys = Microsoft.Xna.Framework.Input.Keys;
#endif

using FlatRedBall;


using FlatRedBall.Gui;
using FlatRedBall.Input;
using FlatRedBall.Math;


namespace EditorObjects
{


    public static class CameraMethods
    {
        #region Fields

        public static Axis mUpAxis;

        public static Vector3 CenterTarget;

        #endregion

        #region Properties

        public static Axis UpAxis
        {
            get { return mUpAxis; }
            set
            {
                mUpAxis = value;


            }
        }

        #endregion

        #region Static Constructor

        static CameraMethods()
        {
            UpAxis = Axis.Y;
        }

        #endregion

        public static void FocusOn(Sprite sprite)
        {
            float distanceAway = 10 * 
                Math.Max(sprite.ScaleX, sprite.ScaleY);

            SetFocusValues(distanceAway, sprite.Position);
        }

        private static void SetFocusValues(float distanceAway, Vector3 targetPosition)
        {
            float minimumDistanceFromClipPlane = SpriteManager.Camera.NearClipPlane * 1.05f;
            float maximumDistanceFromClipPlane = SpriteManager.Camera.FarClipPlane * .95f;

            distanceAway = Math.Max(distanceAway, minimumDistanceFromClipPlane);
            distanceAway = Math.Min(distanceAway, maximumDistanceFromClipPlane);

#if FRB_MDX
            SpriteManager.Camera.Position = targetPosition - distanceAway * SpriteManager.Camera.RotationMatrix.Forward();
#else
            SpriteManager.Camera.Position = targetPosition - distanceAway * SpriteManager.Camera.RotationMatrix.Forward;
#endif
            CameraMethods.CenterTarget = targetPosition;

        }

        public static void MouseCameraControl(Camera camera)
        {
            Cursor cursor = GuiManager.Cursor;




            if (cursor.WindowOver == null && GuiManager.DominantWindowActive == false && !cursor.MiddlePush

#if SUPPORTS_FRB_DRAWN_GUI
                && cursor.WindowMiddleButtonPushed == null
#endif
#if! FRB_MDX
                && FlatRedBallServices.Game.IsActive
#endif    
                )
            {
                float pixelSize = 1/camera.PixelsPerUnitAt(0);


                // middle-click drag moves the camera
                if (InputManager.Mouse.ButtonDown(FlatRedBall.Input.Mouse.MouseButtons.MiddleButton))
                {
                    camera.X += -InputManager.Mouse.XChange * pixelSize;
                    camera.Y += InputManager.Mouse.YChange * pixelSize;
                }

                // double-click the middle-mouse button to center on the mouse's position
                if (InputManager.Mouse.ButtonDoubleClicked(FlatRedBall.Input.Mouse.MouseButtons.MiddleButton))
                {
                    cursor.GetCursorPosition(camera, 0);
                }

                // mouse-wheel scrolling zooms in and out
                if (InputManager.Mouse.ScrollWheelChange != 0)
                {
                    if (camera.Orthogonal == false)
                        camera.Z *= 1 + System.Math.Sign(InputManager.Mouse.ScrollWheelChange) * -.1f;
                    else
                    {
                        camera.OrthogonalHeight *= 1 + System.Math.Sign(InputManager.Mouse.ScrollWheelChange) * -.1f;
                        camera.OrthogonalWidth *= 1 + System.Math.Sign(InputManager.Mouse.ScrollWheelChange) * -.1f;
                    }
                }
            }
        }

        public static void MouseCameraControl3D(Camera camera)
        {
            Cursor cursor = GuiManager.Cursor;


            if (cursor.WindowOver == null && GuiManager.DominantWindowActive == false &&
                !cursor.MiddlePush 
#if SUPPORTS_FRB_DRAWN_GUI
                && cursor.WindowMiddleButtonPushed == null
#endif

#if FRB_XNA
                && FlatRedBallServices.Game.IsActive
#endif
                
                
                )
            {
                #region ScrollWheel zooms in/out

                if (InputManager.Mouse.ScrollWheelChange != 0)
                {
                    int zoomValue = System.Math.Sign(InputManager.Mouse.ScrollWheelChange);

                    ZoomBy(camera, zoomValue);
                }

                #endregion

                #region Alt+Right Mouse Button Down - Zoom

                if ((InputManager.Keyboard.KeyDown(Keys.LeftAlt) || InputManager.Keyboard.KeyDown(Keys.RightAlt)) &&
                    InputManager.Mouse.ButtonDown(FlatRedBall.Input.Mouse.MouseButtons.RightButton))
                {
                    if (InputManager.Mouse.YVelocity != 0)
                    {
                        ZoomBy(camera, (-InputManager.Mouse.YVelocity/512.0f ));
                    }

                }

                #endregion

                #region Alt+Middle Mouse Button - Rotate

                if ((InputManager.Keyboard.KeyDown(Keys.LeftAlt) || InputManager.Keyboard.KeyDown(Keys.RightAlt)) &&
                    InputManager.Mouse.ButtonDown(FlatRedBall.Input.Mouse.MouseButtons.MiddleButton))
                {
                    Vector3 upVector;
                    switch(mUpAxis)
                    {
                        case Axis.X:
                            upVector = new Vector3(1,0,0);
                            break;
                        case Axis.Y:
                            upVector = new Vector3(0,1,0);
                            break;
                        case Axis.Z:
                            upVector = new Vector3(0,0,1);
                            break;
                        default:
                            upVector = new Vector3(0, 1, 0);
                            break;
                    }

                    InputManager.Mouse.ControlPositionedObjectOrbit(camera, CenterTarget, false, upVector);
                }

                #endregion

                #region MiddleMouseButtonPan

                else if (InputManager.Mouse.ButtonDown(FlatRedBall.Input.Mouse.MouseButtons.MiddleButton))
                {
                    float distanceAway = (camera.Position - CenterTarget).Length();

                    const float multiplier = .0015f;

#if FRB_MDX
                    Vector3 cameraRight = camera.RotationMatrix.Right();
                    Vector3 cameraUp = camera.RotationMatrix.Up();
#else
                    Vector3 cameraRight = camera.RotationMatrix.Right;
                    Vector3 cameraUp = camera.RotationMatrix.Up;
#endif

                    Vector3 offset = -InputManager.Mouse.XChange * distanceAway * multiplier * cameraRight +
                        InputManager.Mouse.YChange * distanceAway * multiplier * cameraUp;

                    camera.Position += offset;

                    CenterTarget += offset;
                }



                #endregion

            }
        }

        private static void ZoomBy(Camera camera, float zoomValue)
        {
            if (camera.Orthogonal == false)
            {
                Vector3 distanceFromTarget = camera.Position - CenterTarget;

                distanceFromTarget *= 1 + zoomValue * -.1f;

                camera.Position = CenterTarget + distanceFromTarget;
            }
            else
            {
                camera.OrthogonalHeight *= 1 + zoomValue * -.1f;
                camera.OrthogonalWidth *= 1 + zoomValue * -.1f;
            }
        }

        public static void KeyboardCameraControl(Camera camera)
        {
            if (InputManager.ReceivingInput == null)
            {
                // movement should be time based, so use velocity
                #region if ortho
                if (camera.Orthogonal)
                {
                    if (InputManager.Keyboard.KeyDown(Keys.Up)) camera.YVelocity = camera.OrthogonalHeight;
                    else if (InputManager.Keyboard.KeyDown(Keys.Down)) camera.YVelocity = -camera.OrthogonalHeight;
                    else camera.YVelocity = 0;

                    if (InputManager.Keyboard.KeyDown(Keys.Left)) camera.XVelocity = -camera.OrthogonalWidth;
                    else if (InputManager.Keyboard.KeyDown(Keys.Right)) camera.XVelocity = camera.OrthogonalWidth;
                    else camera.XVelocity = 0;

                    // TODO:  Make this time based
#if FRB_MDX
                    if (InputManager.Keyboard.KeyDown(Keys.Equals))
#elif FRB_XNA
                    if(InputManager.Keyboard.KeyDown(Keys.OemPlus))
#endif
                    {
                        camera.OrthogonalWidth *= .98f;
                        camera.OrthogonalHeight *= .98f;
                    }

#if FRB_MDX
                    else if (InputManager.Keyboard.KeyDown(Keys.Minus))
#elif FRB_XNA
                    else if(InputManager.Keyboard.KeyDown(Keys.OemMinus))
#endif
                    {
                        camera.OrthogonalWidth *= 1.02f;
                        camera.OrthogonalHeight *= 1.02f;
                    }
                }
                #endregion

                #region 3D view
                else
                {
                    // Y axis movement by pushing UP/DOWN
                    if (InputManager.Keyboard.KeyDown(Keys.Up)) 
                        camera.YVelocity = -FlatRedBall.Math.MathFunctions.ForwardVector3.Z * camera.Z;
                    else if (InputManager.Keyboard.KeyDown(Keys.Down)) 
                        camera.YVelocity = FlatRedBall.Math.MathFunctions.ForwardVector3.Z * camera.Z;
                    else 
                        camera.YVelocity = 0;

                    // X axis movement by pushing LEFT/RIGHT
                    if (InputManager.Keyboard.KeyDown(Keys.Left)) camera.XVelocity = FlatRedBall.Math.MathFunctions.ForwardVector3.Z * camera.Z;
                    else if (InputManager.Keyboard.KeyDown(Keys.Right)) camera.XVelocity = -FlatRedBall.Math.MathFunctions.ForwardVector3.Z * camera.Z;
                    else camera.XVelocity = 0;

                    // Z axis movement by pushing +/-
#if FRB_MDX
                    if (InputManager.Keyboard.KeyDown(Keys.Equals))
#elif FRB_XNA
                    if(InputManager.Keyboard.KeyDown(Keys.OemPlus))
#endif                    
                    {
                        camera.ZVelocity = -camera.Z;
                    }
#if FRB_MDX
                    else if (InputManager.Keyboard.KeyDown(Keys.Minus))
#elif FRB_XNA
                    else if(InputManager.Keyboard.KeyDown(Keys.OemMinus))
#endif
                    {
                        camera.ZVelocity = camera.Z;


#if FRB_MDX
                        // it's been the case before that users have gotten to a camera Z = 0;
                        // When this occurs the camera does not respond to keyboard input commands.
                        // To remedy this problem simply move the camera back slightly if the camera is too far forward.
                        // Later when implementing true 3D naviation this will have to be fixed.
                        if (camera.Z > -.006f)
                            camera.Z = -.006f;
#endif
                    }
                    else camera.ZVelocity = 0;
                }

                #endregion

                #region set maximum bounds for the camera
                if (camera.X > 200000)
                    camera.X = 200000;
                if (camera.X < -200000)
                    camera.X = -200000;

                if (camera.Y > 200000)
                    camera.Y = 200000;
                if (camera.Y < -200000)
                    camera.Y = -200000;

                if (camera.Z < -200000)
                    camera.Z = -200000;
                #endregion


                #region reset the position of the camera if any of its positions have a float.NaN value
                if (float.IsNaN(camera.X))
                    camera.X = 0;
                if (float.IsNaN(camera.Y))
                    camera.Y = 0;
                if (float.IsNaN(camera.Z))
                    camera.Z = FlatRedBall.Math.MathFunctions.ForwardVector3.Z * 50;
                #endregion
            }

        }

        public static void CameraControlFps(Camera camera)
        {
            GuiManager.Cursor.StaticPosition = true;
            Vector3 up = new Vector3(0, 1, 0);

            camera.Velocity = new Vector3();

            Keys forwardKey = Keys.W;
            Keys backKey = Keys.S;
            Keys leftKey = Keys.A;
            Keys rightKey = Keys.D;

            FlatRedBall.Input.Keyboard keyboard = InputManager.Keyboard;

            float movementSpeed = 7;

            if (keyboard.KeyDown(forwardKey))
            {
                camera.Velocity += 
                    new Vector3(camera.RotationMatrix.M31, camera.RotationMatrix.M32, camera.RotationMatrix.M33) * 
                    movementSpeed;
            }
            else if (keyboard.KeyDown(backKey))
            {
                camera.Velocity +=
                    new Vector3(camera.RotationMatrix.M31, camera.RotationMatrix.M32, camera.RotationMatrix.M33) *
                    -movementSpeed;
            }

            if (keyboard.KeyDown(leftKey))
            {
                camera.Velocity +=
                    new Vector3(camera.RotationMatrix.M11, camera.RotationMatrix.M12, camera.RotationMatrix.M13) *
                    -movementSpeed;
            }
            if (keyboard.KeyDown(rightKey))
            {
                camera.Velocity += 
                    new Vector3(camera.RotationMatrix.M11, camera.RotationMatrix.M12, camera.RotationMatrix.M13) *
                    movementSpeed;

            }

#if FRB_XNA
            // These vaules may be way too fast/slow because I modified it to use pixels rather
            // than the somewhat arbitrary world coordinates
            camera.RotationMatrix *= 
                Matrix.CreateFromAxisAngle(
                    camera.RotationMatrix.Right, 
                    -.2f * GuiManager.Cursor.ScreenYChange * TimeManager.SecondDifference);
            
            camera.RotationMatrix *= 
                Matrix.CreateFromAxisAngle(
                    up,
                    -.2f * GuiManager.Cursor.ScreenXChange * TimeManager.SecondDifference);
#elif FRB_MDX
             camera.RotationMatrix *= 
                Matrix.RotationAxis(
                    new Vector3(camera.RotationMatrix.M11, camera.RotationMatrix.M12, camera.RotationMatrix.M13), 
                    -.2f * GuiManager.Cursor.YVelocity * TimeManager.SecondDifference);
            
            camera.RotationMatrix *=
                Matrix.RotationAxis(
                    up,
                    -.2f * GuiManager.Cursor.XVelocity * TimeManager.SecondDifference);          
#endif
            

        }
    }
}
