#define SUPPORTS_XBOX_GAMEPADS

using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall.Gui;
using Microsoft.Xna.Framework.Input;


namespace FlatRedBall.Input
{
    /// <summary>
    /// Containing functionality for keyboard, mouse, and joystick input.
    /// </summary>
    public static partial class InputManager
    {
        #region Fields

        static Dictionary<int, bool> mControllerConnectedStatus;



#if SUPPORTS_XBOX_GAMEPADS
        static Xbox360GamePad[] mXbox360GamePads;
#endif
        static bool mUpdateXbox360GamePads = true;

        static TouchScreen mTouchScreen;

        #endregion

        #region Properties

        public static int NumberOfConnectedGamePads
        {
            get
            {
                int count = 0;

                for (int i = 0; i < Xbox360GamePads.Length; i++)
                {
                    if (Xbox360GamePads[i].IsConnected)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>
        /// Whether to perform every-frame update logic on all gamepads.
        /// </summary>
        public static bool UpdateXbox360GamePads
        {
            get { return mUpdateXbox360GamePads; }
            set { mUpdateXbox360GamePads = value; }
        }

#if SUPPORTS_XBOX_GAMEPADS
        public static Xbox360GamePad[] Xbox360GamePads
        {
            get { return mXbox360GamePads; }
        }
#endif

        public static TouchScreen TouchScreen
        {
            get
            {
                return mTouchScreen;
            }
        }

        #endregion

        #region Events

        public class ControllerConnectionEventArgs : EventArgs
        {
            public ControllerConnectionEventArgs() : base() {}
            public ControllerConnectionEventArgs(int playerIndex, bool connected)
                : this()
            {
                PlayerIndex = playerIndex;
                Connected = connected;
            }

            /// <summary>
            /// The index of the gamepad which connected or disconnected. This is 0-based, so values are 0 to 3 inclusive.
            /// </summary>
            public int PlayerIndex { get; set; }
            /// <summary>
            /// Whether the gamepad was connected. If false, the gamepad was disconnected.
            /// </summary>
            public bool Connected { get; set; }
        }

        public delegate void ControllerConnectionHandler(object sender, ControllerConnectionEventArgs e);

        /// <summary>
        /// Event raised whenever an Xbox360Controller is connected or disonnected. 
        /// </summary>
        public static event ControllerConnectionHandler ControllerConnectionEvent;

        #endregion

        #region Methods
        
        #region Public Methods

        public static void CheckControllerConnectionChange()
        {
            int padNumber;

#if !MONODROID
            padNumber = 4;
#else
            padNumber = 1;
#endif

            for (int i = 0; i < padNumber; i++)
            {
                if (mXbox360GamePads[i].IsConnected != mControllerConnectedStatus[i])
                {
                    if (ControllerConnectionEvent != null)
                        ControllerConnectionEvent(null,
                                new ControllerConnectionEventArgs(i, mXbox360GamePads[i].IsConnected));
                    mControllerConnectedStatus[i] = mXbox360GamePads[i].IsConnected;
                }
            }
        }

        #endregion


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
            // Nov 8, 2020 - now we disable input when the window has no focus. Not sure if we want to make that controlled by a variable
            if (InputReceiver != null && FlatRedBallServices.Game.IsActive)
            {
                InputReceiver.OnFocusUpdate();
                // OnFocusUpdate could set the InputReceiver to null, so handle that:
                InputReceiver?.ReceiveInput();

                if (Keyboard.AutomaticallyPushEventsToInputReceiver)
                {
                    var shift = InputReceiverKeyboard.IsShiftDown;
                    var ctrl = InputReceiverKeyboard.IsCtrlDown;
                    var alt = InputReceiverKeyboard.IsAltDown;

                    foreach (var key in InputReceiverKeyboard.KeysTyped)
                    {
                        InputManager.InputReceiver?.HandleKeyDown(key, shift, alt, ctrl);
                    }

                    var stringTyped = InputReceiverKeyboard.GetStringTyped();

                    if (stringTyped != null)
                    {
                        for (int i = 0; i < stringTyped.Length; i++)
                        {
                            // receiver could get nulled out by itself when something like enter is pressed
                            InputReceiver?.HandleCharEntered(stringTyped[i]);
                        }
                    }
                }
            }
        }

        private static void InitializeTouchScreen()
        {
            mTouchScreen = new TouchScreen();
            mTouchScreen.Initialize();
        }

        private static void ClearXbox360GamePadInput()
        {
#if SUPPORTS_XBOX_GAMEPADS
            for (int i = 0; i < mXbox360GamePads.Length; i++)
            {
                mXbox360GamePads[i].Clear();
            }
#endif
        }


        private static void InitializeXbox360GamePads()
        {
#if SUPPORTS_XBOX_GAMEPADS
            mXbox360GamePads = new Xbox360GamePad[]
            {
                new Xbox360GamePad(Microsoft.Xna.Framework.PlayerIndex.One),
                new Xbox360GamePad(Microsoft.Xna.Framework.PlayerIndex.Two),
                new Xbox360GamePad(Microsoft.Xna.Framework.PlayerIndex.Three),
                new Xbox360GamePad(Microsoft.Xna.Framework.PlayerIndex.Four)
            };
#endif

            mControllerConnectedStatus = new Dictionary<int, bool>();
            mControllerConnectedStatus.Add(0, false);
            mControllerConnectedStatus.Add(1, false);
            mControllerConnectedStatus.Add(2, false);
            mControllerConnectedStatus.Add(3, false);
        }

        static partial void PlatformSpecificXbox360GamePadUpdate();

        private static void PerformXbox360GamePadUpdate()
        {
            CheckControllerConnectionChange();

            if (mUpdateXbox360GamePads)
            {
                BackPressed = false;
    #if SUPPORTS_XBOX_GAMEPADS

                mXbox360GamePads[0].Update();
    #if !MONODROID
                mXbox360GamePads[1].Update();
                mXbox360GamePads[2].Update();
                mXbox360GamePads[3].Update();
    #endif
    #endif
                PlatformSpecificXbox360GamePadUpdate();
            }

        }

        #endregion
    }
}
