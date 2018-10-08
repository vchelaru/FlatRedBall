#define SUPPORTS_XBOX_GAMEPADS
#define SUPPORTS_TOUCH_SCREEN

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Gui;

using Microsoft.Xna.Framework.Input;

namespace FlatRedBall.Input
{
    public static partial class InputManager
    {
        // The ClearInput method should not only
        // clear input for this frame, but it should
        // prevent pushes from happening next frame.  To
        // do this properly, a variable needs to be set on
        // whether pushes should be ignored.  However, this
        // variable will prevent pushes NEXT frame, not this
        // frame.  So there will need to be two variables.  One
        // which determines whether to ignore pushes this frame,
        // and one which will be used to set that variable - one that
        // determines whether to ignore pushes next frame.
        internal static bool mIgnorePushesNextFrame = false;
        internal static bool mIgnorePushesThisFrame = false;

        static bool mCurrentFrameInputSuspended;

        static Mouse mMouse;

		#region XML Docs
        /// <summary>
        /// Reference to an IInputReceiver which will have its ReceiveInput method called every frame.
        /// </summary>
        /// <remarks>
        /// If this reference is not null, the reference's ReceiveInput method is called in the InputManager.GetInputState method.
        /// <seealso cref="FlatRedBall.Gui.IInputReceiver"/>
        /// </remarks>
        #endregion
        static FlatRedBall.Gui.IInputReceiver mReceivingInput;
        // When a user clicks on an element which is an IInputReceiver,
        // it is set as the object which receieves input.  Whenever the
        // user clicks the mouse, the InputManager's receivingInput gets set.
        // To prevent the immediate resetting of the receivingInput, this bool
        // is set to true, then resetted next frame.  If this is true, ignore clicks
        // for setting the receivingInput to null;
        static bool mReceivingInputJustSet;// = false;


        static Keyboard mKeyboard;


        public static Mouse Mouse
        {
            get { return mMouse; }
            // Vic on December 6, 2009 said:
            // Do we need a setter for the mouse?
            // I don't think we do, and this may have
            // just been really old code that never got
            // wiped out.
            
            //set { mMouse = value; }
        }


        public static bool CurrentFrameInputSuspended
        {
            get { return mCurrentFrameInputSuspended; }
            set { mCurrentFrameInputSuspended = value; }
        }

        public static Keyboard Keyboard
        {
            get { return mKeyboard; }
            set { mKeyboard = value; }
        }
        public static IInputReceiverKeyboard InputReceiverKeyboard
        {
            get; set;
        }

        [Obsolete("Use the InputReceiver property instead")]
        public static IInputReceiver ReceivingInput
        {
            get { return InputReceiver; }
            set
            {
                InputReceiver = value;
            }
        }

        public static IInputReceiver InputReceiver
        {
            get { return mReceivingInput; }
            set
            {
                if (value != mReceivingInput)
                {
                    // Set this to null to prevent 
                    IInputReceiver oldReceiver = mReceivingInput;
                    mReceivingInput = value;
                    if (oldReceiver != null)
                    {
                        oldReceiver.LoseFocus();
                    }
                }
                mReceivingInputJustSet = true;

                if (mReceivingInput != null)
                {
                    mReceivingInput.OnGainFocus();
                }

            }
        }

        public static bool ReceivingInputJustSet
        {
            get { return mReceivingInputJustSet; }
            set { mReceivingInputJustSet = value; }
        }

        public static bool BackPressed { get; internal set; }

        static InputManager()
        {


        }

        public static void BackHandled()
        {
            BackPressed = false;
        }

        public static void ClearAllInput()
        {
            Keyboard.Clear();

            Mouse.Clear();

            ClearXbox360GamePadInput();

            mIgnorePushesNextFrame = true;
        }


        public static bool IsKeyConsumedByInputReceiver(Keys key)
        {
            return mReceivingInput != null && mReceivingInput.IgnoredKeys.Contains(key) == false;
        }

        // made public for unit tests
        public static void Initialize(IntPtr windowHandle)
        {
            mKeyboard = new Keyboard();
            InputReceiverKeyboard = mKeyboard;

            mMouse = new Mouse(windowHandle);

#if SUPPORTS_TOUCH_SCREEN
            InitializeTouchScreen();
#endif

#if SUPPORTS_XBOX_GAMEPADS
            InitializeXbox360GamePads();
#endif


        }

        public static void Update()
        {
            mCurrentFrameInputSuspended = false;

            mIgnorePushesThisFrame = mIgnorePushesNextFrame;
            mIgnorePushesNextFrame = false;

            if (mMouse.Active)
            {
                mMouse.Update(TimeManager.SecondDifference, TimeManager.CurrentTime);
            }

            mKeyboard.Update();

            UpdateInputReceiver();

#if SUPPORTS_TOUCH_SCREEN
            mTouchScreen.Update();
#endif


#if SUPPORTS_XBOX_GAMEPADS
            PerformXbox360GamePadUpdate();
#endif
        }

        private static void UpdateInputReceiver()
        {
            // Need to call the ReceiveInput method after testing out typed keys
            if (InputReceiver != null)
            {
                InputReceiver.OnFocusUpdate();
                InputReceiver.ReceiveInput();

                if (Keyboard.AutomaticallyPushEventsToInputReceiver)
                {
                    var shift = InputReceiverKeyboard.IsShiftDown;
                    var ctrl = InputReceiverKeyboard.IsCtrlDown;
                    var alt = InputReceiverKeyboard.IsAltDown;

                    foreach (var key in InputReceiverKeyboard.KeysTyped)
                    {
                        InputManager.InputReceiver.HandleKeyDown(key, shift, alt, ctrl);
                    }

                    var stringTyped = InputReceiverKeyboard.GetStringTyped();

                    if(stringTyped != null)
                    {
                        for (int i = 0; i < stringTyped.Length; i++)
                        {
                            InputReceiver.HandleCharEntered(stringTyped[i]);
                        }
                    }
                }
            }
        }
    }
}
