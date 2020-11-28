using System;
using System.Collections.Generic;
using System.Text;


using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

using FlatRedBall;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;

using FlatRedBall.Graphics;

namespace FlatRedBall.Input
{
    public delegate void ModifyMouseState(ref MouseState mouseState);

    public class Mouse : IEquatable<Mouse>, IInputDevice
    {
        #region Enums
        public enum MouseButtons
        {
            LeftButton = 0,
            RightButton = 1,
            MiddleButton = 2,
            XButton1 = 3,
            XButton2 = 4
            // Once extra buttons are added, modify the NumberOfButtons const below
        }
        #endregion

        #region Fields
        #region Static and Const Fields

        public const int NumberOfButtons = (int)MouseButtons.XButton2 + 1;

        public const float MaximumSecondsBetweenClickForDoubleClick = .25f;
        #endregion

        MouseState mMouseState;
        MouseState mLastFrameMouseState = new MouseState();

        Dictionary<MouseButtons, DelegateBasedPressableInput> mButtons = new Dictionary<MouseButtons, DelegateBasedPressableInput>();



        int mThisFrameRepositionX;
        int mThisFrameRepositionY;

        int mLastFrameRepositionX;
        int mLastFrameRepositionY;
        float mXVelocity;
        float mYVelocity;

        //bool mClearUntilNextClick = false;

        double[] mLastClickTime;
        double[] mLastPushTime;
        bool[] mDoubleClick;
        bool[] mDoublePush;

        bool mWindowsCursorVisible = true;


        #region XML docs
        /// <summary>
        /// The camera-relative X coordinate position of the Mouse at 100 units away.
        /// </summary>
        #endregion
        float mXAt100Units;
        float mYAt100Units;
        // Why do we have this instead of just relying on the last state?
        int mLastWheel;

        PositionedObject mGrabbedPositionedObject;
        float mGrabbedPositionedObjectRelativeX;
        float mGrabbedPositionedObjectRelativeY;


        bool mActive = true;

        bool mWasJustCleared = false;

        #endregion

        #region Properties

        public bool AnyButtonPushed()
        {
            bool valueToReturn = false;

            for (int i = 0; i < NumberOfButtons; i++)
            {
                valueToReturn |= ButtonPushed((MouseButtons)i);
            }

            return valueToReturn;
        }


#if !XBOX360

        public MouseState MouseState
        {
            get
            {
                return mMouseState;
            }
        }

        public bool Active
        {
            get { return mActive; }
            set
            {
                mActive = value;
            }
        }

        #region XML Docs
        /// <summary>
        /// Grabs a PositionedObject.  The PositionedObject will automatically update
        /// its position according to mouse movement while the reference remains.
        /// </summary>
        #endregion
        public PositionedObject GrabbedPositionedObject
        {
            get { return mGrabbedPositionedObject; }
            set
            {
                if (value != null)
                {
                    mGrabbedPositionedObjectRelativeX = value.X - WorldXAt(value.Z);
                    mGrabbedPositionedObjectRelativeY = value.Y - WorldYAt(value.Z);

                }
                mGrabbedPositionedObject = value;
            }
        }

        DelegateBased1DInput scrollWheel;

        public I1DInput ScrollWheel
        {
            get
            {
                if(scrollWheel == null)
                {
                    scrollWheel = new DelegateBased1DInput(

                        () => mMouseState.ScrollWheelValue / 120.0f,
                        () => ScrollWheelChange
                        );
                }
                return scrollWheel;
            }
        }

        public float ScrollWheelChange
        {
            get 
            {
#if MONODROID
                return InputManager.TouchScreen.PinchRatioChange;
#else
                return (mMouseState.ScrollWheelValue - mLastWheel)/120.0f; 
#endif
            
            }
        }
#endif


#if !MONOGAME
        public bool IsOwnerFocused
        {
            get
            {
                //bool value = false;

                System.Windows.Forms.Control control = FlatRedBallServices.Owner;

                bool returnValue = false;
                while (true)
                {
                    if (control == null)
                    {
                        returnValue = false;
                        break;
                    }
                    else if (control.Focused)
                    {
                        returnValue = true;
                        break;
                    }

                    control = control.Parent;
                }
                
                return returnValue;
            }

        }
#endif

        #region XML Docs
        /// <summary>
        /// Returns the client rectangle-relative X pixel coordinate of the cursor.
        /// </summary>
        #endregion
        public int X
        {
            get 
            {
                #if FRB_MDX

                return mOwner.PointToClient( System.Windows.Forms.Cursor.Position ).X;
                #elif XBOX360
                return 0;
                #else
                return mMouseState.X; 
                #endif
            }
        }

        #region XML Docs
        /// <summary>
        /// Returns the client rectangle-Y pixel coordinate of the cursor.
        /// </summary>
        #endregion
        public int Y
        {
            get 
            { 
                #if FRB_MDX
                return mOwner.PointToClient( System.Windows.Forms.Cursor.Position ).Y;
                #elif XBOX360
                return 0;
                #else
                return mMouseState.Y; 
                #endif
            }
        }

        #region XML Docs
        /// <summary>
        /// The number of pixels that the mouse has moved on the
        /// X axis during the last frame.
        /// </summary>
        #endregion
        public int XChange
        {
            get 
            { 
#if FRB_MDX
                return mMouseState.X;
#else
                return mMouseState.X - mLastFrameMouseState.X + mLastFrameRepositionX; 
#endif
            }
        }

        #region XML Docs
        /// <summary>
        /// The number of pixels that the mouse has moved on the
        /// Y axis during the last frame.
        /// </summary>
        #endregion
        public int YChange
        {
            get 
            { 
#if FRB_MDX
                return mMouseState.Y;
#else
                return mMouseState.Y - mLastFrameMouseState.Y + mLastFrameRepositionY; 
#endif
            }
        }

        #region XML Docs
        /// <summary>
        /// The rate of change of the X property in 
        /// pixels per second.
        /// </summary>
        #endregion
        public float XVelocity
        {
            get { return mXVelocity; }
        }

        #region XML Docs
        /// <summary>
        /// The rate of change of the Y property in 
        /// pixels per second.
        /// </summary>
        #endregion
        public float YVelocity
        {
            get { return mYVelocity; }
        }

        I2DInput IInputDevice.Default2DInput => Zero2DInput.Instance;
        IPressableInput IInputDevice.DefaultUpPressable => FalsePressableInput.Instance;
        IPressableInput IInputDevice.DefaultDownPressable => FalsePressableInput.Instance;
        IPressableInput IInputDevice.DefaultLeftPressable => FalsePressableInput.Instance;
        IPressableInput IInputDevice.DefaultRightPressable => FalsePressableInput.Instance;
        I1DInput IInputDevice.DefaultHorizontalInput => Zero1DInput.Instance;
        I1DInput IInputDevice.DefaultVerticalInput => Zero1DInput.Instance;
        IPressableInput IInputDevice.DefaultPrimaryActionInput => FalsePressableInput.Instance;
        IPressableInput IInputDevice.DefaultSecondaryActionInput => FalsePressableInput.Instance;
        IPressableInput IInputDevice.DefaultConfirmInput => FalsePressableInput.Instance;
        IPressableInput IInputDevice.DefaultJoinInput => FalsePressableInput.Instance;
        IPressableInput IInputDevice.DefaultPauseInput => FalsePressableInput.Instance;
        IPressableInput IInputDevice.DefaultBackInput => FalsePressableInput.Instance;

        #endregion

        #region Events

        public static event ModifyMouseState ModifyMouseState;

        #endregion

        #region Methods

        #region Constructor/Initialize
#if FRB_MDX
        internal Mouse(System.Windows.Forms.Control owner, bool useWindowsCursor)
        {
            // This is needed to hide and show the Windows cursor
            mOwner = owner;



            mLastClickTime = new double[NumberOfButtons];
            mLastPushTime = new double[NumberOfButtons];
            mDoubleClick = new bool[NumberOfButtons];
            mDoublePush = new bool[NumberOfButtons];

            mMouseButtonClicked = new bool[NumberOfButtons];
            mMouseButtonPushed = new bool[NumberOfButtons];

            mMouseOffset = new MouseOffset[NumberOfButtons];

            mMouseOffset[(int)MouseButtons.LeftButton] = MouseOffset.Button0;
            mMouseOffset[(int)MouseButtons.RightButton] = MouseOffset.Button1;
            mMouseOffset[(int)MouseButtons.MiddleButton] = MouseOffset.Button2;
            mMouseOffset[(int)MouseButtons.XButton1] = MouseOffset.Button3;
            mMouseOffset[(int)MouseButtons.XButton2] = MouseOffset.Button4;

            mMouseDevice = new Device(SystemGuid.Mouse);
            mMouseDevice.SetDataFormat(DeviceDataFormat.Mouse);

            if (useWindowsCursor)
            {
                mWindowsCursorVisible = true;
                mMouseDevice.SetCooperativeLevel(owner, CooperativeLevelFlags.Foreground |
                    CooperativeLevelFlags.NonExclusive);
            }
            else
            {
                mWindowsCursorVisible = false;
                mMouseDevice.SetCooperativeLevel(owner, CooperativeLevelFlags.Foreground |
                    CooperativeLevelFlags.Exclusive);
            }

            try
            { mMouseDevice.Acquire(); }
            catch (InputLostException)
            {
                //				return;
            }
            catch (OtherApplicationHasPriorityException)
            {
                //				return;
            }
            catch (System.ArgumentException)
            {
            }

            // i don't know why this doesn't work, but input still works without it.
            mMouseDevice.Properties.BufferSize = 5;


        }
#else
        internal Mouse(IntPtr windowHandle)
        {
#if SILVERLIGHT
            //SilverArcade.SilverSprite.Input.Mouse.WindowHandle = windowHandle;
            mLastFrameMouseState = new MouseState();
            mMouseState = new MouseState();

            Microsoft.Xna.Framework.Input.Mouse.CreatesNewState = false;
#elif !MONOGAME
            Microsoft.Xna.Framework.Input.Mouse.WindowHandle = windowHandle;
#endif
            mLastClickTime = new double[NumberOfButtons];
            mLastPushTime = new double[NumberOfButtons];
            mDoubleClick = new bool[NumberOfButtons];
            mDoublePush = new bool[NumberOfButtons];
        }
#endif

        internal void Initialize()
        {

        }

        #endregion


        #region Public Methods

        public IPressableInput GetButton(MouseButtons button)
        {
            if(mButtons.ContainsKey(button) == false)
            {
                mButtons[button] = new DelegateBasedPressableInput(
                    () => this.ButtonDown(button),
                    () => this.ButtonPushed(button),
                    () => this.ButtonReleased(button)
                    );
            }

            return mButtons[button];
        }

        #region Button state methods (pushed, down, released, double clicked)
      
        public bool ButtonPushed(MouseButtons button)
        {
#if !XBOX360

			//Removed checking for focus to keep consistent with the other mouse events.  
			//Checking should now be done manually

//            bool isOwnerFocused = true;

//#if !WINDOWS_PHONE && !SILVERLIGHT && !MONODROID
//            isOwnerFocused = IsOwnerFocused;
//#endif



            if (mActive == false || InputManager.mIgnorePushesThisFrame) // || !isOwnerFocused)
                return false;
#if FRB_MDX
            if (mMouseBufferedData != null)
            {
                foreach (Microsoft.DirectX.DirectInput.BufferedData d in mMouseBufferedData)
                {
                    if (d.Offset == (int)mMouseOffset[(int)button])
                    {
                        if ((d.Data & 0x80) != 0)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
#else
            switch (button)
            {
                case MouseButtons.LeftButton:
                    return !InputManager.CurrentFrameInputSuspended && mMouseState.LeftButton == ButtonState.Pressed && mLastFrameMouseState.LeftButton == ButtonState.Released;
                case MouseButtons.RightButton:
                    return !InputManager.CurrentFrameInputSuspended && mMouseState.RightButton == ButtonState.Pressed && mLastFrameMouseState.RightButton == ButtonState.Released;
#if !SILVERLIGHT
                case MouseButtons.MiddleButton:
                    return !InputManager.CurrentFrameInputSuspended && mMouseState.MiddleButton == ButtonState.Pressed && mLastFrameMouseState.MiddleButton == ButtonState.Released;
                case MouseButtons.XButton1:
                    return !InputManager.CurrentFrameInputSuspended && mMouseState.XButton1 == ButtonState.Pressed && mLastFrameMouseState.XButton1 == ButtonState.Released;
                case MouseButtons.XButton2:
                    return !InputManager.CurrentFrameInputSuspended && mMouseState.XButton2 == ButtonState.Pressed && mLastFrameMouseState.XButton2 == ButtonState.Released;
#endif                
                default:
                    return false;
            }
#endif

#else
            return false;
#endif
        }

        public bool ButtonReleased(MouseButtons button)
        {
#if !XBOX360
            // Silverlight has a null mMouseState at the beginning, so check for that.
            bool isMouseStateNull = false;

#if SILVERLIGHT
            isMouseStateNull = mMouseState == null;
#endif

            if (mActive == false || isMouseStateNull)
                return false;

#if FRB_MDX
            return mMouseButtonClicked[(int)button];
#else
            switch (button)
            {
                case MouseButtons.LeftButton:
                    return !InputManager.CurrentFrameInputSuspended &&
                        mMouseState.LeftButton == ButtonState.Released && mLastFrameMouseState.LeftButton == ButtonState.Pressed;
                case MouseButtons.RightButton:
                    return !InputManager.CurrentFrameInputSuspended &&
                        mMouseState.RightButton == ButtonState.Released && mLastFrameMouseState.RightButton == ButtonState.Pressed;
#if !SILVERLIGHT
                case MouseButtons.MiddleButton:
                    return !InputManager.CurrentFrameInputSuspended && mMouseState.MiddleButton == ButtonState.Released && mLastFrameMouseState.MiddleButton == ButtonState.Pressed;
                case MouseButtons.XButton1:
                    return !InputManager.CurrentFrameInputSuspended && mMouseState.XButton1 == ButtonState.Released && mLastFrameMouseState.XButton1 == ButtonState.Pressed;
                case MouseButtons.XButton2:
                    return !InputManager.CurrentFrameInputSuspended && mMouseState.XButton2 == ButtonState.Released && mLastFrameMouseState.XButton2 == ButtonState.Pressed;
#endif
                default:
                    return false;
            }
#endif
#else
            return false;
#endif
        }


        public bool ButtonDown(MouseButtons button)
        {
#if XBOX360
            return false;
#else
            if (mActive == false)
                return false;

#if FRB_MDX
            byte[] tempMouseButton = mMouseState.GetMouseButtons();
            if (tempMouseButton != null && tempMouseButton.Length != 0)
                return mMouseState.GetMouseButtons()[(int)button] != 0;
            else
                return false;
#else
            switch (button)
            {
                case MouseButtons.LeftButton:
                    return !InputManager.CurrentFrameInputSuspended && mMouseState.LeftButton == ButtonState.Pressed;
                case MouseButtons.RightButton:
                    return !InputManager.CurrentFrameInputSuspended && mMouseState.RightButton == ButtonState.Pressed;
#if !SILVERLIGHT                
                case MouseButtons.MiddleButton:
                    return !InputManager.CurrentFrameInputSuspended && mMouseState.MiddleButton == ButtonState.Pressed;
                case MouseButtons.XButton1:
                    return !InputManager.CurrentFrameInputSuspended && mMouseState.XButton1 == ButtonState.Pressed;
                case MouseButtons.XButton2:
                    return !InputManager.CurrentFrameInputSuspended && mMouseState.XButton2 == ButtonState.Pressed;
#endif                
                default:
                    return false;
            }
#endif
#endif

        }

#if !XBOX360



        public bool ButtonDoubleClicked(MouseButtons button)
        {
            return mActive && !InputManager.CurrentFrameInputSuspended && mDoubleClick[(int)button];
        }

        public bool ButtonDoublePushed(MouseButtons button)
        {
            return mActive && !InputManager.CurrentFrameInputSuspended && mDoublePush[(int)button];
        }
#endif
        #endregion

        public void Clear()
        {
            mWasJustCleared = true;

            mMouseState = new MouseState();
            mLastFrameMouseState = new MouseState();

            mLastWheel = 0;
        }

        Vector3 mDefaultUpVector = new Vector3(0, 1, 0);


        public void ControlPositionedObjectOrbit(PositionedObject positionedObject,
            Vector3 orbitCenter, bool requireMiddleMouse)
        {
            ControlPositionedObjectOrbit(positionedObject, orbitCenter, requireMiddleMouse,
                mDefaultUpVector);
        }


        public void ControlPositionedObjectOrbit(PositionedObject positionedObject,
            Vector3 orbitCenter, bool requireMiddleMouse, Vector3 upVector)
        {
            const float coefficient = .00016f;

            if (!requireMiddleMouse || this.ButtonDown(MouseButtons.MiddleButton))
            {

                if (XVelocity != 0)
                {
                    float angleToRotateBy = -coefficient * XVelocity;

                    Matrix rotationMatrix = Matrix.CreateFromAxisAngle(upVector, angleToRotateBy);

                    positionedObject.Position -= orbitCenter;

                    MathFunctions.TransformVector(ref positionedObject.Position, ref rotationMatrix);

                    positionedObject.Position += orbitCenter;

                    //float x = positionedObject.X;
                    //float z = positionedObject.Z;

                    //FlatRedBall.Math.MathFunctions.RotatePointAroundPoint(
                    //    orbitCenter.X, orbitCenter.Z, ref x, ref z, angleToRotateBy);

                    //positionedObject.X = x;
                    //positionedObject.Z = z;

                }

                if (YVelocity != 0)
                {
                    Vector3 relativePosition = positionedObject.Position - orbitCenter;

                    Matrix transformation = Matrix.CreateFromAxisAngle(
                        positionedObject.RotationMatrix.Right, coefficient * -YVelocity);

                    FlatRedBall.Math.MathFunctions.TransformVector(
                        ref relativePosition, ref transformation);

                    positionedObject.Position = relativePosition + orbitCenter;
                }

            }




#if FRB_MDX
            Vector3 forward = Vector3.Normalize(orbitCenter - positionedObject.Position);

            Matrix matrix = positionedObject.RotationMatrix;
            matrix.M31 = forward.X;
            matrix.M32 = forward.Y;
            matrix.M33 = forward.Z;

            Vector3 right = Vector3.Normalize(Vector3.Cross(upVector, forward));

            matrix.M11 = right.X;
            matrix.M12 = right.Y;
            matrix.M13 = right.Z;

            Vector3 up = Vector3.Normalize(Vector3.Cross(forward, right));

            matrix.M21 = up.X;
            matrix.M22 = up.Y;
            matrix.M23 = up.Z;

            positionedObject.UpdateRotationValuesAccordingToMatrix(matrix);

            // to fix accumulation and weird math issues:
            positionedObject.RotationZ = positionedObject.RotationZ;

#else
            Vector3 relativePositionForView = orbitCenter - positionedObject.Position;

            // Vic says:  Why do we invert?  Well, because the CreateLookAt matrix method creates
            // a matrix by which you multiply everything in the world to simulate the camera looking
            // at an object.  But we don't want to rotate the world to look like the camera is looking 
            // at a point - instead we want to rotate the camera so that it actually is looking at the point.
            // FlatRedBall will take care of the actual inverting when it goes to draw the world.
            positionedObject.RotationMatrix = Matrix.Invert(Matrix.CreateLookAt(new Vector3(), relativePositionForView, upVector));

            
#endif

#if !SILVERLIGHT
            if (this.ScrollWheelChange != 0)
            {
                float scrollCoefficient = (positionedObject.Position - orbitCenter).Length() * .125f;

#if FRB_MDX
                positionedObject.Position += scrollCoefficient *
                    this.ScrollWheelChange * positionedObject.RotationMatrix.Forward();
#else
                positionedObject.Position += scrollCoefficient *
                    this.ScrollWheelChange * positionedObject.RotationMatrix.Forward;
#endif

            }
#endif

        }

        public Ray GetMouseRay(Camera camera)
        {
#if FRB_MDX
            // Not sure if this works for non-default Cameras
            return MathFunctions.GetRay(this.mXAt100Units, this.mYAt100Units, camera);
#else
            return MathFunctions.GetRay(X, Y, 1, camera);

            //if (InputManager.Keyboard.KeyPushed(Keys.D))
            //{
            //    int m = 3;
            //}

            //int screenX = X;
            //int screenY = Y;

            //Matrix matrix = Matrix.Invert(camera.TransformationMatrix);

            //Matrix transformationMatrix = Matrix.CreateTranslation(camera.Position);


            //Vector3 absoluteRayEnd = Renderer.GraphicsDevice.Viewport.Unproject(new Vector3(screenX, screenY, 1),
            //    camera.GetProjectionMatrix(), camera.GetLookAtMatrix(false), Matrix.Identity);

            //Vector3 directionRay = absoluteRayEnd;
            ////Vector3 directionRay = absoluteRayEnd - camera.Position;
            //directionRay.Normalize();

            //return new Ray(camera.Position, directionRay);
#endif

        }

        public void HideNativeWindowsCursor()
        {
#if FRB_MDX
            if (mWindowsCursorVisible)
            {
                System.Windows.Forms.Cursor.Hide();
                mWindowsCursorVisible = false;
                try
                {
                    mMouseDevice.Unacquire();
                    mMouseDevice.SetCooperativeLevel(mOwner, CooperativeLevelFlags.Foreground |
                        CooperativeLevelFlags.Exclusive);
                    mMouseDevice.Acquire();

                }
                catch (Exception)
                {
                }
            }
#endif
        }
        #region XML Docs
        /// <summary>
        /// Returns whether the Mouse is over the argument Circle.
        /// </summary>
        /// <param name="circle">The Circle to check.</param>
        /// <returns>Whether the mouse is over the argument Circle.</returns>
        #endregion
        public bool IsOn(Circle circle)
        {
            return circle.IsPointInside(WorldXAt(0), WorldYAt(0));
        }

        public bool IsOn(Circle circle, Camera camera)
        {
            return circle.IsPointInside(WorldXAt(0, camera), WorldYAt(0, camera));
        }


        public bool IsOn(Polygon polygon)
        {
            return polygon.IsPointInside(WorldXAt(polygon.Z), WorldYAt(polygon.Z));
        }

        public bool IsOn(Polygon polygon, Camera camera)
        {
            return polygon.IsPointInside(WorldXAt(polygon.Z, camera), WorldYAt(polygon.Z, camera));
        }


        public bool IsOn(AxisAlignedRectangle rectangle)
        {
            return rectangle.IsPointInside(WorldXAt(0), WorldYAt(0));
        }

        public bool IsOn(AxisAlignedRectangle rectangle, Camera camera)
        {
            return rectangle.IsPointInside(WorldXAt(0, camera), WorldYAt(0, camera));
        }

        public bool IsOn(Camera camera)
        {
            return X > camera.LeftDestination && X < camera.RightDestination &&
                Y > camera.TopDestination && Y < camera.BottomDestination;
        }
#if !XBOX360
        public bool IsOn3D(FlatRedBall.Graphics.Text text, bool relativeToCamera)
        {
			Vector3 offset = new Vector3();
            switch (text.HorizontalAlignment)
            {
                case FlatRedBall.Graphics.HorizontalAlignment.Left:
						offset.X = text.ScaleX;
						break;
                case FlatRedBall.Graphics.HorizontalAlignment.Right:
						offset.X = -text.ScaleX;
						break;
            }

			switch (text.VerticalAlignment)
			{
				case FlatRedBall.Graphics.VerticalAlignment.Top:
					offset.Y = -text.ScaleY;
					break;
				case FlatRedBall.Graphics.VerticalAlignment.Bottom:
					offset.Y = text.ScaleY;
					break;
			}

			text.Position += offset;
            bool value = IsOn3D<FlatRedBall.Graphics.Text>(text, relativeToCamera);
			text.Position -= offset;

			return value;
        }
#endif

#if !XBOX360

        public bool IsOn3D<T>(T objectToTest, bool relativeToCamera) where T : IPositionable, IRotatable, IReadOnlyScalable
        {
            Vector3 temporaryVector = new Vector3();
            return IsOn3D<T>(objectToTest, false, SpriteManager.Camera, out temporaryVector);
        }


        public bool IsOn3D<T>(T objectToTest, bool relativeToCamera, ref Vector3 intersectionPoint)
            where T : IPositionable, IRotatable, IReadOnlyScalable
        {
            return IsOn3D(objectToTest, relativeToCamera, SpriteManager.Camera, out intersectionPoint);
        }

        public bool IsOn3D<T>(T objectToTest, bool relativeToCamera, Camera camera)
            where T : IPositionable, IRotatable, IReadOnlyScalable
        {
            Vector3 temporaryVector = new Vector3();
            return IsOn3D(objectToTest, relativeToCamera, camera, out temporaryVector);
        }

        /// <summary>
        /// Determines whether the Mouse is over the objectToTest argument.
        /// </summary>
        /// <remarks>
        /// If a Text object is passed this method will only work appropriately if 
        /// the Text object has centered text.  See the IsOn3D overload which takes a Text argument.
        /// </remarks>
        /// <typeparam name="T">The type of the first argument.</typeparam>
        /// <param name="objectToTest">The object to test if the mouse is on.</param>
        /// <param name="relativeToCamera">Whether the object's Position is relative to the Camera.</param>
        /// <param name="camera"></param>
        /// <param name="intersectionPoint">The point where the intersection between the ray casted from the 
        /// mouse into the distance and the argument objectToTest occurred.</param>
        /// <returns>Whether the mouse is over the argument objectToTest</returns>
        public bool IsOn3D<T>(T objectToTest, bool relativeToCamera, Camera camera, out Vector3 intersectionPoint) 
            where T : IPositionable, IRotatable, IReadOnlyScalable
        {
            if (camera == SpriteManager.Camera)
            {
                return MathFunctions.IsOn3D<T>(
                    objectToTest,
                    relativeToCamera,
                    this.GetMouseRay(SpriteManager.Camera),
                    camera,
                    out intersectionPoint);
            }
            else
            {
                float xAt100Units = 0;
                float yAt100Units = 0;

                FlatRedBall.Math.MathFunctions.WindowToAbsolute(
                    X - camera.DestinationRectangle.Left,
                    Y - camera.DestinationRectangle.Top,
                    ref xAt100Units, ref yAt100Units,
                    FlatRedBall.Math.MathFunctions.ForwardVector3.Z * 100, camera,
                     Camera.CoordinateRelativity.RelativeToCamera);

                return MathFunctions.IsOn3D<T>(
                    objectToTest,
                    relativeToCamera,
                    this.GetMouseRay(SpriteManager.Camera),
                    camera,
                    out intersectionPoint);

            }
        }
#endif

        public bool IsInGameWindow()
        {
#if SILVERLIGHT
            return Microsoft.Xna.Framework.Input.Mouse.IsOnGameWindow;

#else

            // Not sure why we do greater than 0 instead of greater than or equal to
            // 0.  On W8 the cursor initially starts at 0,0 and that is in the window, 
            // so we want to consider 0 inside.
            return X >= 0 && X < FlatRedBallServices.ClientWidth &&
                Y >= 0 && Y < FlatRedBallServices.ClientHeight;
#endif
        }

#if !SILVERLIGHT && !XBOX360 && !WINDOWS_PHONE
        public void SetScreenPosition(int newX, int newY)
        {
#if XNA
            // The velocity should not change when positions are set.
            mThisFrameRepositionX += newX - System.Windows.Forms.Cursor.Position.X;
            mThisFrameRepositionY += newY - System.Windows.Forms.Cursor.Position.Y;

            System.Windows.Forms.Cursor.Position = new System.Drawing.Point( 
                   newX,
                   newY);
#else
            // The velocity should not change when positions are set.
            MouseState currentState = Microsoft.Xna.Framework.Input.Mouse.GetState();
            mThisFrameRepositionX += newX - currentState.X;
            mThisFrameRepositionY += newY - currentState.Y;

            Microsoft.Xna.Framework.Input.Mouse.SetPosition(newX, newY);
#endif
        }
#endif

        public void ShowNativeWindowsCursor()
        {
            if (mWindowsCursorVisible == false)
            {
#if FRB_MDX
                System.Windows.Forms.Cursor.Show();
                mWindowsCursorVisible = true;
                try
                {
                    mMouseDevice.Unacquire();
                    mMouseDevice.SetCooperativeLevel(mOwner, CooperativeLevelFlags.Foreground |
                        CooperativeLevelFlags.NonExclusive);
                    mMouseDevice.Acquire();
                }
                catch (Exception)
                {
                }
#endif
            }
        }


        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

#if !XBOX360
            stringBuilder.Append("Internal MouseState:").Append(mMouseState.ToString());
#endif
            stringBuilder.Append("\nWorldX at 100 units: ").Append(mXAt100Units);
            stringBuilder.Append("\nWorldY at 100 units: ").Append(mYAt100Units);
#if !SILVERLIGHT && !XBOX360
            stringBuilder.Append("\nScrollWheel: ").Append(ScrollWheelChange);
#endif
            return stringBuilder.ToString();
        }


        #region World Values At
        public float WorldXAt(float zValue)
        {
            return WorldXAt(zValue, SpriteManager.Camera);
        }

        public float WorldXAt(float zValue, Camera camera)
        {
            if (camera.Orthogonal == false)
            {
                if (camera == SpriteManager.Camera)
                {
                    return camera.X +
                        FlatRedBall.Math.MathFunctions.ForwardVector3.Z * (zValue - camera.Z) * mXAt100Units / 100.0f;
                }
                else
                {
                    float xAt100Units = 0;
                    float yAt100Units = 0;
#if !SILVERLIGHT
                    FlatRedBall.Math.MathFunctions.WindowToAbsolute(
                        X - camera.DestinationRectangle.Left,
                        Y - camera.DestinationRectangle.Top,
                        ref xAt100Units, ref yAt100Units,
                        FlatRedBall.Math.MathFunctions.ForwardVector3.Z * 100, camera,
                         Camera.CoordinateRelativity.RelativeToCamera);
#endif
                    return camera.X +
                        FlatRedBall.Math.MathFunctions.ForwardVector3.Z * (zValue - camera.Z) * xAt100Units / 100.0f;
                }
            }
            else
            {
                return camera.X - camera.OrthogonalWidth / 2.0f + // left border
                    ((X - camera.DestinationRectangle.Left) *camera.OrthogonalWidth/camera.DestinationRectangle.Width );
            }

        }


        public float WorldYAt(float zValue)
        {
            return WorldYAt(zValue, SpriteManager.Camera);
        }

        public float WorldYAt(float zValue, Camera camera)
        {
            if (camera.Orthogonal == false)
            {
                if (camera == SpriteManager.Camera)
                {
                    return camera.Y +
                        FlatRedBall.Math.MathFunctions.ForwardVector3.Z * (zValue - camera.Z) * mYAt100Units / 100;
                }
                else
                {
                    float xAt100Units = 0;
                    float yAt100Units = 0;
#if !SILVERLIGHT
                    FlatRedBall.Math.MathFunctions.WindowToAbsolute(
                        X - camera.DestinationRectangle.Left,
                        Y - camera.DestinationRectangle.Top,
                        ref xAt100Units, ref yAt100Units,
                        FlatRedBall.Math.MathFunctions.ForwardVector3.Z * 100, camera,
                         Camera.CoordinateRelativity.RelativeToCamera);
#endif
                    return camera.Y +
                        FlatRedBall.Math.MathFunctions.ForwardVector3.Z * (zValue - camera.Z) * yAt100Units / 100;
                }
            }
            else
            {
                return camera.Y + camera.OrthogonalHeight / 2.0f -
                    ((Y - camera.TopDestination) * camera.OrthogonalHeight / camera.DestinationRectangle.Height);
            }

        }

#if !XBOX360
        public float WorldXChangeAt(float zPosition)
        {
            int change = mMouseState.X - mLastFrameMouseState.X + mLastFrameRepositionX;

            float resultX;
            float dummy;

            MathFunctions.ScreenToAbsoluteDistance(change, 0, out resultX, out dummy, zPosition, SpriteManager.Camera);

            return resultX;
        }

        public float WorldYChangeAt(float zPosition)
        {
            int change = mMouseState.Y - mLastFrameMouseState.Y + mLastFrameRepositionY;

            float resultY;
            float dummy;

            MathFunctions.ScreenToAbsoluteDistance(0, change, out dummy, out resultY, zPosition, SpriteManager.Camera);

            return resultY;
        }
#endif

        #endregion

        //#endif

        #endregion

#if !XBOX360

        #region Internal Methods

#if FRB_MDX
        // These methods are called when the user hides and shows the cursor.
        // FlatRedBallServices calls these methods.
        internal void ReacquireExclusive()
        {
            try
            {
                mMouseDevice.Unacquire();
                mMouseDevice.SetCooperativeLevel(mOwner, CooperativeLevelFlags.Foreground |
                    CooperativeLevelFlags.Exclusive);
                mMouseDevice.Acquire();
            }
            catch (Exception)
            {
                // no big deal
            }
        }

        internal void ReacquireNonExclusive()
        {
            try
            {
                mMouseDevice.Unacquire();
                mMouseDevice.SetCooperativeLevel(mOwner, CooperativeLevelFlags.Foreground |
                    CooperativeLevelFlags.NonExclusive);
                mMouseDevice.Acquire();
            }
            catch (Exception)
            {
                // no big deal
            }
        }

#endif

        internal void Update(float secondDifference, double currentTime)
        {

#if SILVERLIGHT
            // Vic says - The temporary is needed so that clicking and pushing works properly
            
            mLastFrameMouseState.X = mTemporaryMouseState.X;
            mLastFrameMouseState.Y = mTemporaryMouseState.Y;
            mLastFrameMouseState.LeftButton = mTemporaryMouseState.LeftButton;
            mLastFrameMouseState.RightButton = mTemporaryMouseState.RightButton;


            mTemporaryMouseState.X = mMouseState.X;
            mTemporaryMouseState.Y = mMouseState.Y;
            mTemporaryMouseState.LeftButton = mMouseState.LeftButton;
            mTemporaryMouseState.RightButton = mMouseState.RightButton;


#else
            mLastFrameMouseState = mMouseState;
#endif

            mLastFrameRepositionX = mThisFrameRepositionX;
            mLastFrameRepositionY = mThisFrameRepositionY;

            mThisFrameRepositionX = 0;
            mThisFrameRepositionY = 0;
#if FRB_MDX
            mMouseBufferedData = null;

            if (mMouseDevice.Properties.BufferSize != 0)
            {

                do
                {// Try to get the current state
                    try
                    {
                        if (mMouseBufferedData != null)
                            mMouseBufferedData.Clear();

                        mMouseState = mMouseDevice.CurrentMouseState;
                        mMouseBufferedData = mMouseDevice.GetBufferedData();

                        break; // everything's ok, so we get out
                    }
                    catch (DirectXException)
                    { 	// let the application handle Windows messages
                        try
                        {
                            System.Windows.Forms.Application.DoEvents();
                        }
                        catch (DirectXException)
                        {
                            continue;
                        }
                        // Try to get reacquire the mouse and don't care about exceptions
                        try { mMouseDevice.Acquire(); }
                        catch (InputLostException) { continue; }
                        catch (OtherApplicationHasPriorityException) { break; }
                    }
                }
                while (true); // Do this until it's successful
            }

            for (int i = 0; i < mMouseButtonClicked.Length; i++)
            {
                mMouseButtonClicked[i] = false;
                mMouseButtonPushed[i] = false;
                mDoubleClick[i] = false;
                mDoublePush[i] = false;
            }

            if (mMouseBufferedData != null)
            {
                foreach (Microsoft.DirectX.DirectInput.BufferedData d in mMouseBufferedData)
                {
                    for (int i = 0; i < mMouseOffset.Length; i++)
                    {
                        if (d.Offset == (int)mMouseOffset[i])
                        {
                            if ((d.Data & 0x80) == 0)
                            {
                                mMouseButtonClicked[i] = true;

                                if (TimeManager.CurrentTime - mLastClickTime[i] < .25f)
                                    mDoubleClick[i] = true;

                                mLastClickTime[i] = TimeManager.CurrentTime;
                                break;
                            }
                            if ((d.Data & 0x80) == 0x80)
                            {
                                mMouseButtonPushed[i] = true;
                                if (TimeManager.CurrentTime - mLastPushTime[i] < .25f)
                                    mDoublePush[i] = true;

                                mLastPushTime[i] = TimeManager.CurrentTime;
                                break;
                            }
                        }
                    }
                }
            }

            mXVelocity = (mMouseState.X) / secondDifference;
            mYVelocity = (mMouseState.Y) / secondDifference;
#else
            XnaAndSilverlightSpecificUpdateLogic(secondDifference, currentTime);
#endif
            //if (mClearUntilNextClick)
            //{
            //    if (ButtonReleased(MouseButtons.LeftButton))
            //    {
            //        mClearUntilNextClick = false;
            //    }
            //    Clear();

                
            //}
            //else
            {

                FlatRedBall.Math.MathFunctions.WindowToAbsolute(
                    X - SpriteManager.Camera.DestinationRectangle.Left,
                    Y - SpriteManager.Camera.DestinationRectangle.Top,
                    ref mXAt100Units, ref mYAt100Units,
                    FlatRedBall.Math.MathFunctions.ForwardVector3.Z * 100, SpriteManager.Camera,
                    Camera.CoordinateRelativity.RelativeToCamera);


                if (mGrabbedPositionedObject != null)
                {
                    mGrabbedPositionedObject.X = WorldXAt(mGrabbedPositionedObject.Z) + mGrabbedPositionedObjectRelativeX;
                    mGrabbedPositionedObject.Y = WorldYAt(mGrabbedPositionedObject.Z) + mGrabbedPositionedObjectRelativeY;
                }
            }
        }

        private void XnaAndSilverlightSpecificUpdateLogic(float secondDifference, double currentTime)
        {
#if FRB_XNA
            mLastWheel = mMouseState.ScrollWheelValue;
#endif

#if SILVERLIGHT
            mMouseState = Microsoft.Xna.Framework.Input.Mouse.GetState();

            if (mMouseState == null)
            {
                // This is null the first frame.
                mMouseState = new MouseState();
            }
#elif FRB_XNA
            mMouseState = Microsoft.Xna.Framework.Input.Mouse.GetState();

            if (ModifyMouseState != null)
            {
                ModifyMouseState(ref mMouseState);
            }


#if WINDOWS_8
            // MonoGame behaves slightly 
            // differently compared to XNA.
            // If the user runs the game in full
            // screen in XNA (on the PC) then the 
            // back buffer size is set to the resolution
            // of the game *as well as the display mode*.
            // However, Windows 8 does not allow you to change
            // the display mode, so the display mode always remains
            // the native size of the device regardless of what the user
            // does in regards to resolution and fullscreen.  The MonoGame
            // team decided to not change this - they want to use the actual
            // display mode, which means the mouse will always return values based
            // on the current resolution.  This causes problems in FRB so I think I'm 
            // going to scale it.
            if (
                (FlatRedBallServices.GraphicsOptions.ResolutionWidth != FlatRedBallServices.Game.Window.ClientBounds.Width ||
                FlatRedBallServices.GraphicsOptions.ResolutionHeight != FlatRedBallServices.Game.Window.ClientBounds.Height)
                )
            {
                float scaleX = FlatRedBallServices.GraphicsOptions.ResolutionWidth / (float)FlatRedBallServices.Game.Window.ClientBounds.Width;
                float scaleY = FlatRedBallServices.GraphicsOptions.ResolutionHeight / (float)FlatRedBallServices.Game.Window.ClientBounds.Height;

                mMouseState = new MouseState(
                    FlatRedBall.Math.MathFunctions.RoundToInt(mMouseState.X * scaleX),
                    FlatRedBall.Math.MathFunctions.RoundToInt(mMouseState.Y * scaleY),
                    mMouseState.ScrollWheelValue,
                    mMouseState.LeftButton,
                    mMouseState.MiddleButton,
                    mMouseState.RightButton,
                    mMouseState.XButton1,
                    mMouseState.XButton2);
            }
#endif


#endif

            if (mWasJustCleared)
            {
#if FRB_XNA
                mLastWheel = mMouseState.ScrollWheelValue;
#endif
                mWasJustCleared = false;
            }

            #region Update Double Click/Push

            for (int i = 0; i < NumberOfButtons; i++)
            {
                mDoubleClick[i] = false;
                mDoublePush[i] = false;

                MouseButtons asMouseButton = (MouseButtons)i;

                if (ButtonReleased(asMouseButton))
                {
                    if (currentTime - mLastClickTime[i] < MaximumSecondsBetweenClickForDoubleClick)
                    {
                        mDoubleClick[i] = true;
                    }
                    mLastClickTime[i] = currentTime;
                }
                if (ButtonPushed(asMouseButton))
                {
                    if (currentTime - mLastPushTime[i] < MaximumSecondsBetweenClickForDoubleClick)
                    {
                        mDoublePush[i] = true;
                    }
                    mLastPushTime[i] = currentTime;
                }
            }
            #endregion
            
            if (secondDifference != 0)
            {
                // If it's 0, then it means that this is the first frame.  Just skip over 
                // setting velocity if that's the case.
                mXVelocity = (mMouseState.X - mLastFrameMouseState.X) / secondDifference;
                mYVelocity = (mMouseState.Y - mLastFrameMouseState.Y) / secondDifference;
            }
        }

        #endregion

        #region Private

        // Not sure why this is here, but
        // I don't think we use it
        //struct VertexPositionNormal
        //{
        //    public Vector3 Position;
        //    public Vector3 Normal;
        //}


        #endregion

#endif

        #endregion

        #region IEquatable<Mouse> Members

        bool IEquatable<Mouse>.Equals(Mouse other)
        {
            return this == other;
        }

        #endregion
    }
}
