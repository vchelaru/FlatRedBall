
#if ANDROID || IOS
#define USE_TOUCH
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using FlatRedBall.Math;
using FlatRedBall.Gui;
using Microsoft.Xna.Framework.Input.Touch;

namespace FlatRedBall.Input
{
    public class TouchScreen
    {
        #region Fields

        int mLastFrameNumberOfTouches;

        int mNumberTouchPointsNeededForPush;

        // This exists so that a double-tap will cause a screen release
        bool mScreenReleaseOnNextNoTouch = false;

#if USE_TOUCH
        Point mCurrentFrameAverageTouchPoints;
        Point mLastFrameAverageTouchPoint;
        Point mPushedPoint;

#endif

        public List<GestureSample> LastFrameGestures
        {
            get;
            private set;
        }


        public TouchCollection TouchCollection
        {
            get;
            private set;
        }

        #endregion

        #region Properties

        public int NumberOfReadGestures
        {
            get;
            private set;
        }

        public bool ReadsGestures
        {
            get;
            set;
        }

        public bool DragReleased
        {
            get;
            set;
        }

        public bool WasHolding
        {
            get;
            set;
        }

        public bool WasFlicking
        {
            get;
            set;
        }

        public bool FlickRelease
        {
            get
            {
                return WasFlicking && !FlickedAny;
            }
        }

        public Point AverageTouchPoint
        {
            get
            {
#if ANDROID || IOS
                return mCurrentFrameAverageTouchPoints;

#else
                return new Point();
#endif
            }
        }

        public Point AverageTouchPointChange
        {
            get
            {
#if ANDROID || IOS
                if (CurrentNumberOfTouches != mLastFrameNumberOfTouches)
                {
                    return new Point();
                }
                else
                {
                    return
                        new Point(
                            mCurrentFrameAverageTouchPoints.X - mLastFrameAverageTouchPoint.X,
                            mCurrentFrameAverageTouchPoints.Y - mLastFrameAverageTouchPoint.Y);
                }
#else
                return new Point();
#endif
            }
        }

        public bool NumberOfTouchesChanged
        {
            get { return this.CurrentNumberOfTouches != this.LastFrameNumberOfTouches; }
        }

        public int CurrentNumberOfTouches
        {
            get;
            internal set;
        }

        public int LastFrameNumberOfTouches
        {
            get { return mLastFrameNumberOfTouches; }
        }

        public int NumberTouchPointsNeededForPush
        {
            internal get
            {
                return mNumberTouchPointsNeededForPush;
            }

            set
            {
                mNumberTouchPointsNeededForPush = value;
                if (mNumberTouchPointsNeededForPush < 1 || mNumberTouchPointsNeededForPush > MaximumTouchCount)
                {
                    throw new InvalidOperationException(string.Format("Invalid number of touch points, {0}, passed to NumberTouchPointsNeededForPush. Must be between 1 and MaximumTouchCount({1})", mNumberTouchPointsNeededForPush, MaximumTouchCount));
                }
            }
        }

        public bool ScreenPushed
        {
            get 
            {
                // For Eric:  This doesn't work!  This logic was used in the Average Velocity and it would never return true.  
                // Update May 24, 2011:
                // I don't know why the code
                // was also testing to see if
                // CurrentNumberOfTouches was less
                // than NumberTouchPointsNeededForPush.
                // I think NumberTouchPointsNeededForPush
                // defaults to 1 and setting it to something
                // else can cause bugs, but if the user touches
                // with both fingers at the same time, that should
                // be a push, but it isn't according to this.
                //return (CurrentNumberOfTouches > 0 && CurrentNumberOfTouches <= NumberTouchPointsNeededForPush) && mLastFrameNumberOfTouches == 0;
                return (CurrentNumberOfTouches > 0) && mLastFrameNumberOfTouches == 0;
            }
        }

        public bool ScreenDown
        {
            get
            {
                return CurrentNumberOfTouches> 0 && CurrentNumberOfTouches <= NumberTouchPointsNeededForPush;// && mLastFrameNumberOfTouches == NumberTouchPointsNeededForPush;// InputManager.Mouse.ButtonDown(Mouse.MouseButtons.LeftButton);
            }
        }

        public bool ScreenReleased
        {
            get
            {
                if (ReadsGestures)
                {
                    // We used to have DoubleTap here, but that seems to cause issues because a user can push, release, then push and that calls a double tap

                    bool returnValue =
                    returnValue = Tap ||
                        //DoubleTap || 
                        DragReleased || FlickRelease;

                    if (!returnValue)
                    {
                        if (mScreenReleaseOnNextNoTouch && CurrentNumberOfTouches == 0)
                        {
                            mScreenReleaseOnNextNoTouch = false;
                            returnValue = true;
                        }
                    }

                    return returnValue;

                }
                else
                {
                    return CurrentNumberOfTouches == 0 && mLastFrameNumberOfTouches == NumberTouchPointsNeededForPush;
                }
            }
        }

        public bool ScreenReleasedNoSlide
        {
            get
            {
                int axisThreshold = GuiManager.Cursor.ClickNoSlideThreshold;
#if ANDROID || IOS
                return ScreenReleased &&
                        System.Math.Abs(X - mPushedPoint.X) < axisThreshold &&
                        System.Math.Abs(Y - mPushedPoint.Y) < axisThreshold;
#else
                return ScreenReleased;
#endif
            }
        }

        public bool PinchStarted
        {
            get
            {
                return (IsPinching && !WasPinching) || (CurrentNumberOfTouches == 2 && mLastFrameNumberOfTouches < 2);
            }
        }

        public bool PinchStopped
        {
            get
            {
                return (!IsPinching && WasPinching) || (CurrentNumberOfTouches < 2 && mLastFrameNumberOfTouches == 2);
            }
        }

        public int X
        {
            get
            {
#if ANDROID || IOS
                return mCurrentFrameAverageTouchPoints.X; 
#else
                return 0;
#endif
            }
        }

        public int Y
        {
            get 
            { 
#if ANDROID || IOS
                return mCurrentFrameAverageTouchPoints.Y;
#else
                return 0;
#endif
            }
        }

        public float XVelocity
        {
            get
            {
                if (ScreenPushed)
                {
                    return 0;
                }
                else
                {
                    return InputManager.Mouse.XVelocity;
                }
            }
        }

        public float YVelocity
        {
            get
            {
                if (ScreenPushed)
                {
                    return 0;
                }
                else
                {
                    return InputManager.Mouse.YVelocity;
                }
            }
        }

        // For XChange and YChange we return 0 if the
        // user just pushed the screen.  Otherwise the
        // values would consider how far the user moved
        // when his finger was not on the screen.
        public float XChange
        {
            get
            {
                if (ScreenPushed)
                {
                    return 0;
                }
                else
                {
                    return InputManager.Mouse.XChange;
                }
            }
        }

        public float YChange
        {
            get
            {
                if (ScreenPushed)
                {
                    return 0;
                }
                else
                {
                    return InputManager.Mouse.YChange;
                }
            }
        }

        public int MaximumTouchCount
        {
            get;
            internal set;
        }

		public bool DoubleTap { get; set; }
        public bool Tap { get; set; }
        public bool Held { get; set; }
		internal Vector2 TouchPosition1LastFrame { get; set; }
		internal Vector2 TouchPosition2LastFrame { get; set; }
		public bool IsPinching { get; internal set; }
        public bool WasPinching { get; internal set; }
		public float PinchRatioChange { get; internal set; }
        public bool FlickedLeft { get; internal set; }
        public bool FlickedRight { get; internal set; }
        public bool FlickedUp { get; internal set; }
        public bool FlickedDown { get; internal set; }
        public bool FlickedAny
        {
            get
            {
                return FlickedDown || FlickedLeft || FlickedRight || FlickedUp;
            }
        }

        public bool ReadsInput
        {
            get;
            set;
        }
        #endregion

        #region Methods

		public void Initialize()
		{
            ReadsInput = true;

            LastFrameGestures = new List<GestureSample>();
#if USE_TOUCH
            ReadsGestures = true;
            TouchPanel.EnabledGestures = 
                GestureType.Tap |
                GestureType.DoubleTap |
                GestureType.Hold |
				GestureType.Pinch |
				GestureType.PinchComplete |
                GestureType.DragComplete |
                GestureType.FreeDrag |
                GestureType.HorizontalDrag |
                GestureType.VerticalDrag |
                GestureType.Flick;

            TouchPanelCapabilities touchPanelCapabilities = TouchPanel.GetCapabilities();
            MaximumTouchCount = touchPanelCapabilities.MaximumTouchCount;
#else
            MaximumTouchCount = 2;
#endif
            NumberTouchPointsNeededForPush = 1;
		}

        public Vector2 WorldAverageTouchPointChangeAt(float z)
        {
            float outX;
            float outY;

            MathFunctions.ScreenToAbsoluteDistance(AverageTouchPointChange.X, AverageTouchPointChange.Y,
                out outX, out outY, z, SpriteManager.Camera);

            return new Vector2(outX, outY);
        }

        public void ControlCamera()
        {
            ControlCamera(SpriteManager.Camera);   
        }

        public void ControlCamera(Camera cameraToControl)
        {
            // Vic says: Eventually we'll want this
            // to use a pixels per unit calculation instead
            // of a set coefficient.  But we'll worry about that
            // later.
            const float coefficient = .1f;

            cameraToControl.X -= XChange * coefficient;
            cameraToControl.Y += YChange * coefficient;


        }

        public float WorldXAt(float absoluteZ)
        {
            return Cursor.WorldXAt(absoluteZ, SpriteManager.Camera, SpriteManager.Camera.Orthogonal, SpriteManager.Camera.OrthogonalWidth, this.X);
        }

        public float WorldYAt(float absoluteZ)
        {
            //return InputManager.Mouse.WorldYAt(absoluteZ);
            return Cursor.WorldYAt(absoluteZ, SpriteManager.Camera, SpriteManager.Camera.Orthogonal, SpriteManager.Camera.OrthogonalWidth, this.Y);
        }

        public float WorldXChangeAt(float absoluteZ)
        {
            if (ScreenPushed)
            {
                return 0;
            }
            else
            {
                return InputManager.Mouse.WorldXChangeAt(absoluteZ);
            }
        }

        public float WorldYChangeAt(float absoluteZ)
        {
            if (ScreenPushed)
            {
                return 0;
            }
            else
            {
                return InputManager.Mouse.WorldYChangeAt(absoluteZ);
            }
        }


        internal void Update()
        {
            /////////////////Early Out//////////////////////
            if (!ReadsInput)
            {
                return;
            }
            ///////////////End Early Out///////////////////

#if USE_TOUCH
            mLastFrameAverageTouchPoint = mCurrentFrameAverageTouchPoints;
#endif
            mLastFrameNumberOfTouches = CurrentNumberOfTouches;
            WasPinching = IsPinching;
            WasFlicking = FlickedAny;

            Tap = false;
			DoubleTap = false;
            Held = false;
            DragReleased = false;
			PinchRatioChange = 0;
            FlickedLeft = FlickedRight = false;
            FlickedUp = FlickedDown = false;
#if USE_TOUCH
            this.TouchCollection = TouchPanel.GetState();

			GetPositionFromTouches(this.TouchCollection);

            NumberOfReadGestures = 0;
            LastFrameGestures.Clear();

			while ( ReadsGestures && TouchPanel.IsGestureAvailable )
			{
				GestureSample gesture = TouchPanel.ReadGesture();

                LastFrameGestures.Add(gesture);

                NumberOfReadGestures++;
				switch ( gesture.GestureType )
				{
                    case GestureType.Tap:
                        Tap = true;
                        mCurrentFrameAverageTouchPoints = new Point((int)gesture.Position.X, (int)gesture.Position.Y);
						OffsetAveragePointsByClientPosition();
                        break;
					case GestureType.DoubleTap:
						DoubleTap = true;
						break;
                    case GestureType.Hold:
                        Held = true;
                        break;

					case GestureType.Pinch:
						if ( IsPinching == false ) 
						{
							TouchPosition1LastFrame = gesture.Position;
							TouchPosition2LastFrame = gesture.Position2;
						}
                        IsPinching = true;
						// generate a value betweem -1 and 1 based on the direction and amount of difference since last frame
                        
						float distanceLastFrame = Vector2.Distance(TouchPosition1LastFrame, TouchPosition2LastFrame);
						float distanceThisFrame = Vector2.Distance(gesture.Position, gesture.Position2);
                        
						TouchPosition1LastFrame = gesture.Position;
						TouchPosition2LastFrame = gesture.Position2;

						PinchRatioChange = distanceThisFrame - distanceLastFrame;

						break;
                    case GestureType.DragComplete:
                        DragReleased = true;
                        break;
					case GestureType.PinchComplete:
						IsPinching = false;
						break;
                    case GestureType.Flick:
                        {
                            if (gesture.Delta.X < 0)
                                FlickedLeft = true;
                            else if (gesture.Delta.X > 0)
                                FlickedRight = true;
                            else
                                FlickedLeft = FlickedRight = false;

                            if(gesture.Delta.Y < 0)
                                FlickedUp = true;
                            else if(gesture.Delta.Y > 0)
                                FlickedDown = true;
                            else
                                FlickedUp = FlickedDown = false;
                        }break;
				}
			}

            if (CurrentNumberOfTouches <= 1 && mLastFrameNumberOfTouches == 2)
                IsPinching = false;

             
            if (DoubleTap)
            {
                mScreenReleaseOnNextNoTouch = true;
            }

#else
            if (InputManager.Mouse.ButtonDown(Mouse.MouseButtons.LeftButton))
            {
                if (InputManager.Keyboard.KeyDown(Microsoft.Xna.Framework.Input.Keys.Space))
                {
                    CurrentNumberOfTouches = 2;
                }
                else
                {
                    CurrentNumberOfTouches = 1;
                }

            }
            else
            {
                CurrentNumberOfTouches = 0;
            }
#endif
        }


#if USE_TOUCH
		void GetPositionFromTouches (TouchCollection touchCollection)
		{
			//Update the number of current touch points.
			CurrentNumberOfTouches = touchCollection.Count;

			if (CurrentNumberOfTouches == 0)
			{
				mCurrentFrameAverageTouchPoints = mLastFrameAverageTouchPoint;

			}
			else
			{





				Vector2 middlePosition = Vector2.Zero;


				mCurrentFrameAverageTouchPoints.X = (int)touchCollection[0].Position.X;
				mCurrentFrameAverageTouchPoints.Y = (int)touchCollection[0].Position.Y;

				OffsetAveragePointsByClientPosition ();

				if (ScreenPushed)
				{
					// We don't want velocity to be set in reaction to a new push, so let's set the last position to the current so no velocity is registered:
					mLastFrameAverageTouchPoint = mCurrentFrameAverageTouchPoints;

					mPushedPoint = new Point (X, Y);
				}
			}
		}

		void OffsetAveragePointsByClientPosition ()
		{

		#if ANDROID
			// Not sure why but on Android (I've tested on Genymotion)
			// the window's position is at X = 88, so positions are coming
			// out as 88 pixels less than what they should be.  In other words
			// when I am clicking the top left of the view the X value is coming 
			// out as -88.  If I add this value then this seems to fix it, but not
			// sure if this is expected behavior:

			mCurrentFrameAverageTouchPoints.X += FlatRedBallServices.Game.Window.ClientBounds.X;
			mCurrentFrameAverageTouchPoints.Y += FlatRedBallServices.Game.Window.ClientBounds.Y;
		#endif




		}


#endif
        #endregion

    }
}
