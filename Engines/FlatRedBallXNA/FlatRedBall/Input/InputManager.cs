#define SUPPORTS_XBOX_GAMEPADS

using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall.Gui;
using Microsoft.Xna.Framework.Input;


namespace FlatRedBall.Input
{
    #region XML Docs
    /// <summary>
    /// Containing functionality for keyboard, mouse, and joystick input.
    /// </summary>
    #endregion
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
#if WINDOWS_8
                return 0;
#else
                int count = 0;

                for (int i = 0; i < Xbox360GamePads.Length; i++)
                {
                    if (Xbox360GamePads[i].IsConnected)
                    {
                        count++;
                    }
                }

                return count;
#endif
            }
        }

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

            public int PlayerIndex { get; set; }
            public bool Connected { get; set; }
        }

        public delegate void ControllerConnectionHandler(object sender, ControllerConnectionEventArgs e);

        public static event ControllerConnectionHandler ControllerConnectionEvent;

        #endregion

        #region Methods
        
        #region Public Methods

        public static void CheckControllerConnectionChange()
        {
            int padNumber;

#if !WINDOWS_PHONE && !MONODROID
            padNumber = 4;
#else
            padNumber = 1;
#endif

#if !WINDOWS_8
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
#endif
        }





        #endregion

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
#if !WINDOWS_PHONE && !MONODROID
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
