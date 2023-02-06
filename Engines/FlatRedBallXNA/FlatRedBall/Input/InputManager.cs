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
    /// <summary>
    /// Containing functionality for keyboard, mouse, and joystick input.
    /// </summary>
    public static partial class InputManager
    {
        #region Fields

        static Dictionary<int, bool> mControllerConnectedStatus;



#if SUPPORTS_XBOX_GAMEPADS
        static Xbox360GamePad[] mXbox360GamePads;
        static GenericGamePad[] genericGamePads;
#endif
        static bool mUpdateXbox360GamePads = true;

        static TouchScreen mTouchScreen;
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

        public static int NumberOfConnectedGenericGamepads
        {
            get
            {
                int count = 0;

                for (int i = 0; i < GenericGamePads.Length; i++)
                {
                    if (GenericGamePads[i].IsConnected)
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

        /// <summary>
        /// Whether to update generic gamepads. This is only needed if using
        /// the GenericGamepads array instead of Xbox360GamePads. Although it 
        /// is a small amount, setting this value to false can save some every-frame
        /// allocations.
        /// </summary>
        public static bool UpdateGenericGamePads = true;

#if SUPPORTS_XBOX_GAMEPADS

        /// <summary>
        /// Returns an array of Xbox360GamePads. 
        /// </summary>
        /// <remarks>
        /// This name was created when the Xbox360 was the only controller type supported by XNA. Since then, many devices
        /// implement X Input, and newer hardware such as Xbox One also appear in this array when connected.
        /// </remarks>
        public static Xbox360GamePad[] Xbox360GamePads => mXbox360GamePads;
        public static GenericGamePad[] GenericGamePads => genericGamePads;

        static Xbox360GamePad[] mConnectedXbox360GamePads;
        public static Xbox360GamePad[] ConnectedXbox360GamePads => mConnectedXbox360GamePads;

#endif

        public static TouchScreen TouchScreen => mTouchScreen;


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
            get => mKeyboard; 
            set => mKeyboard = value; 
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
            /// Note that future versions of FlatRedBall may support more than 4 connected controllers, so the range may increase.
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

        static InputManager()
        {


        }

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
                    // This used to be called before updating the status, but if the code
                    // responding to this event depends on the # of connected gamepads, the
                    // values will be out of date. Therefore, move this after the values are assigned:
                    //if (ControllerConnectionEvent != null)
                    //    ControllerConnectionEvent(null,
                    //            new ControllerConnectionEventArgs(i, mXbox360GamePads[i].IsConnected));

                    mControllerConnectedStatus[i] = mXbox360GamePads[i].IsConnected;

                    mConnectedXbox360GamePads = Xbox360GamePads.Where(item => item.IsConnected).ToArray();

                    if (ControllerConnectionEvent != null)
                        ControllerConnectionEvent(null,
                                new ControllerConnectionEventArgs(i, mXbox360GamePads[i].IsConnected));

                }
            }

            if(mConnectedXbox360GamePads == null)
            {
                mConnectedXbox360GamePads = Xbox360GamePads.Where(item => item.IsConnected).ToArray();
            }
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

            InitializeGenericGamepads();
#endif


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
            PerformGamePadUpdate();
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

                    // This allocates. We could potentially make this return 
                    // an IList or List. That's a breaking change for a tiny amount
                    // of allocation....what to do....
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

        private static void InitializeGenericGamepads()
        {
            genericGamePads = new GenericGamePad[]
            {
                new GenericGamePad(0),
                new GenericGamePad(1),
                new GenericGamePad(2),
                new GenericGamePad(3),
                new GenericGamePad(4),
                new GenericGamePad(5),
                new GenericGamePad(6),
                new GenericGamePad(7),
            };
        }

        static partial void PlatformSpecificXbox360GamePadUpdate();

        private static void PerformGamePadUpdate()
        {

            // Dec 18, 2021 - why do we check this before making updates. 
            // If we do it before, we'll miss the first frame of the game
            // if gamepads are connected:
            //CheckControllerConnectionChange();

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

            if(UpdateGenericGamePads)
            {
                for(int i = 0; i < genericGamePads.Length; i++)
                {
                    genericGamePads[i].Update();
                }
            }

            CheckControllerConnectionChange();
        }

        #endregion
    }
}
