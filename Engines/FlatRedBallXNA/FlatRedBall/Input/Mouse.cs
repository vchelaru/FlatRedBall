using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Input
{
    public delegate void ModifyMouseState(ref MouseState mouseState);

    /// <summary>
    /// Represents the mouse hardware.
    /// </summary>
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

        /// <summary>
        /// Returns whether any mouse button was pushed.
        /// </summary>
        /// <returns></returns>
        public bool AnyButtonPushed()
        {
            bool valueToReturn = false;

            for (int i = 0; i < NumberOfButtons; i++)
            {
                valueToReturn |= ButtonPushed((MouseButtons)i);
            }

            return valueToReturn;
        }


        /// <summary>
        /// The maximum duration between clicks for it to be considered a double
        /// click instead of two single clicks.
        /// </summary>
        public float MaximumSecondsBetweenClickForDoubleClick { get; set; } = .25f;

        public MouseState MouseState => mMouseState;

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

        /// <summary>
        /// A 1D Input for the scroll wheel, where scroll wheel values will be normalized to be between -1 and 1 per wheel "tick".
        /// </summary>
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

        /// <summary>
        /// The main mouse for the game.
        /// </summary>
        public static Mouse Main => InputManager.Mouse;

        /// <summary>
        /// Returns the client rectangle-relative X pixel coordinate of the cursor.
        /// </summary>
        public int X
        {
            get 
            {

                return mMouseState.X; 
            }
        }

        /// <summary>
        /// Returns the client rectangle-Y pixel coordinate of the cursor.
        /// </summary>
        public int Y
        {
            get 
            { 

                return mMouseState.Y;
            }
        }

        /// <summary>
        /// The number of pixels that the mouse has moved on the
        /// X axis since the last frame.
        /// </summary>
        public int XChange
        {
            get 
            { 
                return mMouseState.X - mLastFrameMouseState.X + mLastFrameRepositionX; 
            }
        }

        /// <summary>
        /// The number of pixels that the mouse has moved on the
        /// Y axis since the last frame.
        /// </summary>
        public int YChange
        {
            get 
            { 
                return mMouseState.Y - mLastFrameMouseState.Y + mLastFrameRepositionY; 
            }
        }

        /// <summary>
        /// The rate of change of the X property in 
        /// pixels per second.
        /// </summary>
        public float XVelocity
        {
            get { return mXVelocity; }
        }

        /// <summary>
        /// The rate of change of the Y property in 
        /// pixels per second.
        /// </summary>
        public float YVelocity
        {
            get { return mYVelocity; }
        }

        I2DInput IInputDevice.Default2DInput => Zero2DInput.Instance;
        IRepeatPressableInput IInputDevice.DefaultUpPressable => FalsePressableInput.Instance;
        IRepeatPressableInput IInputDevice.DefaultDownPressable => FalsePressableInput.Instance;
        IRepeatPressableInput IInputDevice.DefaultLeftPressable => FalsePressableInput.Instance;
        IRepeatPressableInput IInputDevice.DefaultRightPressable => FalsePressableInput.Instance;
        I1DInput IInputDevice.DefaultHorizontalInput => Zero1DInput.Instance;
        I1DInput IInputDevice.DefaultVerticalInput => Zero1DInput.Instance;
        IPressableInput IInputDevice.DefaultPrimaryActionInput => FalsePressableInput.Instance;
        IPressableInput IInputDevice.DefaultSecondaryActionInput => FalsePressableInput.Instance;
        IPressableInput IInputDevice.DefaultConfirmInput => FalsePressableInput.Instance;
        IPressableInput IInputDevice.DefaultCancelInput => FalsePressableInput.Instance;
        IPressableInput IInputDevice.DefaultJoinInput => FalsePressableInput.Instance;
        IPressableInput IInputDevice.DefaultPauseInput => FalsePressableInput.Instance;
        IPressableInput IInputDevice.DefaultBackInput => FalsePressableInput.Instance;

        #endregion

        #region Events

        public static event ModifyMouseState ModifyMouseState;

        #endregion

        #region Methods

        #region Constructor/Initialize
        internal Mouse(IntPtr windowHandle)
        {
#if !MONOGAME
            Microsoft.Xna.Framework.Input.Mouse.WindowHandle = windowHandle;
#endif
            mLastClickTime = new double[NumberOfButtons];
            mLastPushTime = new double[NumberOfButtons];
            mDoubleClick = new bool[NumberOfButtons];
            mDoublePush = new bool[NumberOfButtons];
        }

        internal void Initialize()
        {

        }

        #endregion


        #region Public Methods

        /// <summary>
        /// Returns a reference to the argument button as an IPressableInput.
        /// </summary>
        /// <param name="button">The button to return.</param>
        /// <returns>A reference to the button as an IPressableInput.</returns>
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
      
        /// <summary>
        /// Whether the provided button was just pushed. This is only true for
        /// the first frame after a button is Down having been Up in the previous frame.
        /// </summary>
        /// <param name="button">The button to check</param>
        /// <returns>Boolean indicating whether the provided button was just pressed in this frame.</returns>
        public bool ButtonPushed(MouseButtons button)
        {
			//Removed checking for focus to keep consistent with the other mouse events.  
			//Checking should now be done manually

//            bool isOwnerFocused = true;




            if (mActive == false || InputManager.mIgnorePushesThisFrame) // || !isOwnerFocused)
                return false;

            switch (button)
            {
                case MouseButtons.LeftButton:
                    return !InputManager.CurrentFrameInputSuspended && mMouseState.LeftButton == ButtonState.Pressed && mLastFrameMouseState.LeftButton == ButtonState.Released;
                case MouseButtons.RightButton:
                    return !InputManager.CurrentFrameInputSuspended && mMouseState.RightButton == ButtonState.Pressed && mLastFrameMouseState.RightButton == ButtonState.Released;
                case MouseButtons.MiddleButton:
                    return !InputManager.CurrentFrameInputSuspended && mMouseState.MiddleButton == ButtonState.Pressed && mLastFrameMouseState.MiddleButton == ButtonState.Released;
                case MouseButtons.XButton1:
                    return !InputManager.CurrentFrameInputSuspended && mMouseState.XButton1 == ButtonState.Pressed && mLastFrameMouseState.XButton1 == ButtonState.Released;
                case MouseButtons.XButton2:
                    return !InputManager.CurrentFrameInputSuspended && mMouseState.XButton2 == ButtonState.Pressed && mLastFrameMouseState.XButton2 == ButtonState.Released;
                default:
                    return false;
            }

        }

        /// <summary>
        /// Whether the provided button was just released. This is only true for the first frame after
        /// a button enters Up having been Down in the previous frame.
        /// </summary>
        /// <param name="button">The button to check</param>
        /// <returns>Boolean indicating whether the provided button was just released in this frame.</returns>
        public bool ButtonReleased(MouseButtons button)
        {
            bool isMouseStateNull = false;

            if (mActive == false || isMouseStateNull)
                return false;

            switch (button)
            {
                case MouseButtons.LeftButton:
                    return !InputManager.CurrentFrameInputSuspended &&
                        mMouseState.LeftButton == ButtonState.Released && mLastFrameMouseState.LeftButton == ButtonState.Pressed;
                case MouseButtons.RightButton:
                    return !InputManager.CurrentFrameInputSuspended &&
                        mMouseState.RightButton == ButtonState.Released && mLastFrameMouseState.RightButton == ButtonState.Pressed;
                case MouseButtons.MiddleButton:
                    return !InputManager.CurrentFrameInputSuspended && mMouseState.MiddleButton == ButtonState.Released && mLastFrameMouseState.MiddleButton == ButtonState.Pressed;
                case MouseButtons.XButton1:
                    return !InputManager.CurrentFrameInputSuspended && mMouseState.XButton1 == ButtonState.Released && mLastFrameMouseState.XButton1 == ButtonState.Pressed;
                case MouseButtons.XButton2:
                    return !InputManager.CurrentFrameInputSuspended && mMouseState.XButton2 == ButtonState.Released && mLastFrameMouseState.XButton2 == ButtonState.Pressed;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Whether the provided button is Down
        /// </summary>
        /// <param name="button">The button to check.</param>
        /// <returns>Boolean indicating whether the provided button is down.</returns>
        public bool ButtonDown(MouseButtons button)
        {
            if (mActive == false)
                return false;

            switch (button)
            {
                case MouseButtons.LeftButton:
                    return !InputManager.CurrentFrameInputSuspended && mMouseState.LeftButton == ButtonState.Pressed;
                case MouseButtons.RightButton:
                    return !InputManager.CurrentFrameInputSuspended && mMouseState.RightButton == ButtonState.Pressed;
                case MouseButtons.MiddleButton:
                    return !InputManager.CurrentFrameInputSuspended && mMouseState.MiddleButton == ButtonState.Pressed;
                case MouseButtons.XButton1:
                    return !InputManager.CurrentFrameInputSuspended && mMouseState.XButton1 == ButtonState.Pressed;
                case MouseButtons.XButton2:
                    return !InputManager.CurrentFrameInputSuspended && mMouseState.XButton2 == ButtonState.Pressed;
                default:
                    return false;
            }
        }

#if !XBOX360


        /// <summary>
        /// Whether the provided button was just pressed and released twice. Will only be true for the
        /// frame immediately following the double click event.
        /// </summary>
        /// <param name="button">The button to check</param>
        /// <returns>Boolean indicating whether the provieded button was just double clicked.</returns>
        public bool ButtonDoubleClicked(MouseButtons button)
        {
            return mActive && !InputManager.CurrentFrameInputSuspended && mDoubleClick[(int)button];
        }

        /// <summary>
        /// Whether the provided button was pressed, released, and pressed again (but not yet released).
        /// Will only be true for the frame immediately following the double press event.
        /// </summary>
        /// <param name="button">The button to check</param>
        /// <returns>Boolean indicating whether the provided button was just double pushed.</returns>
        public bool ButtonDoublePushed(MouseButtons button)
        {
            return mActive && !InputManager.CurrentFrameInputSuspended && mDoublePush[(int)button];
        }
#endif
        #endregion

        /// <summary>
        /// Clears all input and marks the mouse as having been just cleared so that pushes do not fire next frame.
        /// </summary>
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

            Vector3 relativePositionForView = orbitCenter - positionedObject.Position;

            // Vic says:  Why do we invert?  Well, because the CreateLookAt matrix method creates
            // a matrix by which you multiply everything in the world to simulate the camera looking
            // at an object.  But we don't want to rotate the world to look like the camera is looking 
            // at a point - instead we want to rotate the camera so that it actually is looking at the point.
            // FlatRedBall will take care of the actual inverting when it goes to draw the world.
            positionedObject.RotationMatrix = Matrix.Invert(Matrix.CreateLookAt(new Vector3(), relativePositionForView, upVector));

            if (this.ScrollWheelChange != 0)
            {
                float scrollCoefficient = (positionedObject.Position - orbitCenter).Length() * .125f;

                positionedObject.Position += scrollCoefficient *
                    this.ScrollWheelChange * positionedObject.RotationMatrix.Forward;
            }

        }

        public Ray GetMouseRay(Camera camera)
        {
            return MathFunctions.GetRay(X, Y, 1, camera);
        }

        /// <summary>
        /// Returns whether the Mouse is over the argument Circle.
        /// </summary>
        /// <param name="circle">The Circle to check.</param>
        /// <returns>Whether the mouse is over the argument Circle.</returns>
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

        /// <summary>
        /// Returns whether the mouse position is within the bounds of the game window. This may be true even
        /// if the game window does not have focus since this is only performing a bounds check.
        /// </summary>
        /// <returns>Whether the mouse is within the bounds of the game window.</returns>
        public bool IsInGameWindow()
        {

            // Not sure why we do greater than 0 instead of greater than or equal to
            // 0.  On W8 the cursor initially starts at 0,0 and that is in the window, 
            // so we want to consider 0 inside.
            return X >= 0 && X < FlatRedBallServices.ClientWidth &&
                Y >= 0 && Y < FlatRedBallServices.ClientHeight;
        }

        public void SetScreenPosition(int newX, int newY)
        {

            // The velocity should not change when positions are set.
            MouseState currentState = Microsoft.Xna.Framework.Input.Mouse.GetState();
            mThisFrameRepositionX += newX - currentState.X;
            mThisFrameRepositionY += newY - currentState.Y;

            Microsoft.Xna.Framework.Input.Mouse.SetPosition(newX, newY);
        }

        [Obsolete("This method is not supported, and will be removed in future versions of FRB.")]
        public void ShowNativeWindowsCursor()
        {

        }


        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append("Internal MouseState:").Append(mMouseState.ToString());
            stringBuilder.Append("\nWorldX at 100 units: ").Append(mXAt100Units);
            stringBuilder.Append("\nWorldY at 100 units: ").Append(mYAt100Units);
            stringBuilder.Append("\nScrollWheel: ").Append(ScrollWheelChange);
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
                    FlatRedBall.Math.MathFunctions.WindowToAbsolute(
                        X - camera.DestinationRectangle.Left,
                        Y - camera.DestinationRectangle.Top,
                        ref xAt100Units, ref yAt100Units,
                        FlatRedBall.Math.MathFunctions.ForwardVector3.Z * 100, camera,
                         Camera.CoordinateRelativity.RelativeToCamera);

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
                    FlatRedBall.Math.MathFunctions.WindowToAbsolute(
                        X - camera.DestinationRectangle.Left,
                        Y - camera.DestinationRectangle.Top,
                        ref xAt100Units, ref yAt100Units,
                        FlatRedBall.Math.MathFunctions.ForwardVector3.Z * 100, camera,
                         Camera.CoordinateRelativity.RelativeToCamera);
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

            return -resultY;
        }

        #endregion

        //#endif

        #endregion


        #region Internal Methods


        internal void Update(float secondDifference, double currentTime)
        {

            mLastFrameMouseState = mMouseState;

            mLastFrameRepositionX = mThisFrameRepositionX;
            mLastFrameRepositionY = mThisFrameRepositionY;

            mThisFrameRepositionX = 0;
            mThisFrameRepositionY = 0;

            XnaSpecificUpdateLogic(secondDifference, currentTime);
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

        private void XnaSpecificUpdateLogic(float secondDifference, double currentTime)
        {
            mLastWheel = mMouseState.ScrollWheelValue;

            mMouseState = Microsoft.Xna.Framework.Input.Mouse.GetState();
            if(MouseState.LeftButton == ButtonState.Pressed)
            {
                int m = 3;
            }
            if (ModifyMouseState != null)
            {
                ModifyMouseState(ref mMouseState);
            }



            if (mWasJustCleared)
            {
                mLastWheel = mMouseState.ScrollWheelValue;
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


        #endregion

        #region IEquatable<Mouse> Members

        bool IEquatable<Mouse>.Equals(Mouse other)
        {
            return this == other;
        }

        #endregion
    }
}
