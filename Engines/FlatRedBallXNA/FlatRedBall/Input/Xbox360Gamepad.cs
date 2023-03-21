#define IMPLEMENT_INTERNALS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace FlatRedBall.Input
{
    #region Enums
    public enum DeadzoneType
    {
        Radial= 0,
        //BoundingBox = 1, // Not currently supported
        Cross
        
    }
    public enum ButtonLayout
    {
        Unknown,
        Xbox,
        NintendoPro,
        GameCube
    }


    public enum GamepadLayout
    {
        Unknown,
        Keyboard,
        NES,
        SuperNintendo,
        Nintendo64,
        GameCube,
        SwitchPro,
        Genesis,
        Xbox360,
        PlayStationDualShock,

    }

    #endregion

    public class Xbox360GamePad : IInputDevice
    {
        #region Static Dictionaries for identifying the gamepad

        public static Dictionary<GamepadLayout, HashSet<string>> GamepadNameTypeMap = new Dictionary<GamepadLayout, HashSet<string>>();
        public static Dictionary<GamepadLayout, HashSet<string>> GamepadIdTypeMap = new Dictionary<GamepadLayout, HashSet<string>>();

        #endregion


        #region Enums

        /// <summary>
        /// Enumeration representing the buttons on the Xbox360 controller.  The values for each
        /// entry matches the value of the Xbox 360 button index in Managed DirectX.  This improves
        /// portability between FlatRedBall Managed DirectX and FlatRedBall XNA.
        /// </summary>
        public enum Button
        {
            A,                      // 0
            B,                      // 1
            X,                      // 2
            Y,                      // 3
            LeftShoulder,           // 4
            RightShoulder,          // 5
            Back,                   // 6
            Start,                  // 7
            LeftStick,              // 8
            RightStick,             // 9
            DPadUp,                 // 10
            DPadDown,               // 11
            DPadLeft,               // 12
            DPadRight,              // 13
            LeftTrigger,            // 14
            RightTrigger,           // 15
            LeftStickAsDPadUp,      // 16
            LeftStickAsDPadDown,    // 17
            LeftStickAsDPadLeft,    // 18
            LeftStickAsDPadRight    // 19
        }

        // This serves as a sentinel value.
        public const int NumberOfButtons = 20;

        public enum DPadDirection
        {
            Up,
            Down,
            Left,
            Right
        }

        public enum DirectionalControlDevice
        {
            LeftStick,
            RightStick,
            DPad
        }


        #endregion

        #region Fields

        DelegateBased1DInput dPadHorizontal;
        DelegateBased1DInput dPadVertical;

        DelegateBased2DInput dPad;

        const float AnalogOnThreshold = .5f;

        GamePadState mGamePadState;
        GamePadState mLastGamePadState;

        AnalogStick mLeftStick;
        AnalogStick mRightStick;

        PlayerIndex mPlayerIndex;

        KeyboardButtonMap mButtonMap;

        double[] mLastButtonPush = new double[NumberOfButtons];
        double[] mLastRepeatRate = new double[NumberOfButtons];

        GamePadCapabilities mCapabilities;

        bool[] mButtonsIgnoredForThisFrame = new bool[NumberOfButtons];

        Dictionary<Button, Xbox360ButtonReference> cachedButtons = new Dictionary<Button, Xbox360ButtonReference>();


        #endregion

        #region Properties

        float deadzone = .1f;
        /// <summary>
        /// The deadzone value. The application of this value depends on the DeadzoneType. Setting this value applies the deadzone value to all
        /// AnalogSticks. 
        /// </summary>
        public float Deadzone
        {
            get => // prefer to use analog stick values if they exist:
                LeftStick?.Deadzone ??
                    RightStick?.Deadzone ??
                    deadzone;
            set
            {
                deadzone = value;
                if (LeftStick != null) LeftStick.Deadzone = value;
                if (RightStick != null) RightStick.Deadzone = value;
            }
        }

        // This is stored here in case there are no analog sticks.
        DeadzoneType deadzoneType = DeadzoneType.Radial;// matches the behavior prior to May 22, 2022 when this property was introduced

        /// <summary>
        /// The value determining how deadzones are calculated. Setting this value applies the value to all AnalogSticks.
        /// </summary>
        public DeadzoneType DeadzoneType 
        { 
            get
            {
                // prefer to use analog stick values if they exist:
                return LeftStick?.DeadzoneType ??
                    RightStick?.DeadzoneType ??
                    deadzoneType;
            }
            set
            {
                deadzoneType = value;
                if (LeftStick != null) LeftStick.DeadzoneType = value;
                if (RightStick != null) RightStick.DeadzoneType = value;
            }
        } 
            

        public I1DInput DPadHorizontal
        {
            get
            {
                if(dPadHorizontal == null)
                {
                    dPadHorizontal = new DelegateBased1DInput(
                        () =>
                        {
                            if(this.ButtonDown(Button.DPadLeft))
                            {
                                return -1;
                            }
                            else if(this.ButtonDown(Button.DPadRight))
                            {
                                return 1;
                            }
                            else
                            {
                                return 0;
                            }
                        },
                            () =>
                            {

                                if (this.ButtonPushed(Button.DPadLeft))
                                {
                                    return -1 / TimeManager.SecondDifference;
                                }
                                else if (this.ButtonPushed(Button.DPadRight))
                                {
                                    return 1 / TimeManager.SecondDifference;
                                }
                                else
                                {
                                    return 0;
                                }


                            }
                        );
                
                }

                return dPadHorizontal;
            }
        }

        public I1DInput DPadVertical
        {
            get
            {
                if (dPadVertical == null)
                {
                    dPadVertical = new DelegateBased1DInput(
                        () =>
                        {
                            if (this.ButtonDown(Button.DPadDown))
                            {
                                return -1;
                            }
                            else if (this.ButtonDown(Button.DPadUp))
                            {
                                return 1;
                            }
                            else
                            {
                                return 0;
                            }
                        },
                        () =>
                        {

                            if (this.ButtonPushed(Button.DPadDown))
                            {
                                return -1 / TimeManager.SecondDifference;
                            }
                            else if (this.ButtonPushed(Button.DPadUp))
                            {
                                return 1 / TimeManager.SecondDifference;
                            }
                            else
                            {
                                return 0;
                            }


                        }
                        );

                }

                return dPadVertical;
            }
        }

        public I2DInput DPad
        {
            get
            {
                if(this.dPad == null)
                {
                    Func<float> getX = () =>
                        {
                            if (this.ButtonDown(Button.DPadLeft))
                            {
                                return -1;
                            }
                            else if (this.ButtonDown(Button.DPadRight))
                            {
                                return 1;
                            }
                            else
                            {
                                return 0;
                            }
                        };
                    Func<float> getXVelocity = () =>
                            {
                                if (this.ButtonPushed(Button.DPadLeft))
                                {
                                    return -1 / TimeManager.SecondDifference;
                                }
                                else if (this.ButtonPushed(Button.DPadRight))
                                {
                                    return 1 / TimeManager.SecondDifference;
                                }
                                else
                                {
                                    return 0;
                                }
                            };


                    Func<float> getY = () =>
                    {
                        if (this.ButtonDown(Button.DPadDown))
                        {
                            return -1;
                        }
                        else if (this.ButtonDown(Button.DPadUp))
                        {
                            return 1;
                        }
                        else
                        {
                            return 0;
                        }
                    };
                    Func<float> getYVelocity = () =>
                    {
                        if (this.ButtonPushed(Button.DPadDown))
                        {
                            return -1 / TimeManager.SecondDifference;
                        }
                        else if (this.ButtonPushed(Button.DPadUp))
                        {
                            return 1 / TimeManager.SecondDifference;
                        }
                        else
                        {
                            return 0;
                        }
                    };

                    this.dPad = new DelegateBased2DInput(
                        getX,
                        getY,
                        getXVelocity,
                        getYVelocity);

                }
                return this.dPad;
            }
        }

        /// <summary>
        /// Returns whether the current Xbox360GamePad hardware is connected, or if 
        /// the FakeIsConnected property is set to true.
        /// </summary>
        /// <seealso cref="FakeIsConnected"/>
        public bool IsConnected
        {
            get
            {
                if (this.FakeIsConnected == true)
                {
                    return true;
                }
                else
                {
                    return mGamePadState.IsConnected;
                }
            }
        }

        /// <summary>
        /// This value can force an Xbox360GamePad's 
        /// IsConnected to be true even if the controller
        /// is not connected.  This can be used to simulate
        /// multiple connected controllers.
        /// </summary>
        /// <seealso cref="IsConnected"/>
        public bool FakeIsConnected
        {
            set;
            get;
        }

        /// <summary>
        /// Returns a reference to the left analog stick. This value will always be non-null even if the gamepad doesn't have a physical analog stick.
        /// </summary>
        public AnalogStick LeftStick => mLeftStick;


        /// <summary>
        /// Returns a reference to the right analog stick. This value will always be non-null even if the gamepad doesn't have a physical analog stick.
        /// </summary>
        public AnalogStick RightStick => mRightStick;

        bool AreShoulderAndTriggersFlipped => 
            // January 3, 2023
            // Not sure why but
            // all of a sudden these
            // are no longer acting flipped
            // on gamecube. I'm going to keep
            // this here and use this property
            // in case something flips back or in
            // case there's some setting I'm not aware
            // of.
            //ButtonLayout == ButtonLayout.GameCube
            false;

        public KeyboardButtonMap ButtonMap
        {
            set { mButtonMap = value; }
            get { return mButtonMap; }
        }

        /// <summary>
        /// The left trigger values as reported directly by the gamepad, not flipped for Gamecube
        /// </summary>
        AnalogButton mLeftTrigger;
        AnalogButton mFlippedLeftTrigger;

        /// <summary>
        /// Returns the left trigger's current value.  When not pressed this property returns
        /// 0.0f.  When fully pressed this property returns 1.0f. 
        /// </summary>
        /// <remarks>
        /// This accounts for the Gamecube controller returning inverted shoulder/trigger and inverts internally. Therefore
        /// this value is always in the trigger position regardless of gamepad.
        /// </remarks>
        public AnalogButton LeftTrigger => AreShoulderAndTriggersFlipped
            ? mFlippedLeftTrigger
            : mLeftTrigger ;

        /// <summary>
        /// The right trigger values as reported directly by the gamepad, not flipped for Gamecube
        /// </summary>
        AnalogButton mRightTrigger;
        AnalogButton mFlippedRightTrigger;
        /// <summary>
        /// Returns the right trigger's current value.  When not pressed this property returns
        /// 0.0f.  When fully pressed this property returns 1.0f;
        /// </summary>
        /// <remarks>
        /// This accounts for the Gamecube controller returning inverted shoulder/trigger and inverts internally. Therefore
        /// this value is always in the trigger position regardless of gamepad.
        /// </remarks>
        public AnalogButton RightTrigger => AreShoulderAndTriggersFlipped
            ? mFlippedRightTrigger : mRightTrigger ;

        /// <summary>
        /// Returns whether this game pad was disconnected last frame but is connected this frame.
        /// </summary>
        public bool WasConnectedThisFrame
        {
            get
            {
                return !mLastGamePadState.IsConnected && mGamePadState.IsConnected;
            }
        }


        /// <summary>
        /// Returns whether this game pad was connected last frame but is disconnected this frame.
        /// </summary>
        public bool WasDisconnectedThisFrame
        {
            get
            {
                return mLastGamePadState.IsConnected && !mGamePadState.IsConnected;
            }
        }

        /// <summary>
        /// Returns the game pad type as reported by the underlying capabilities.
        /// </summary>
        public GamePadType GamePadType => mCapabilities.GamePadType;

        public ButtonLayout ButtonLayout { get; set; }

        public GamepadLayout GamepadLayout { get; set; }

        public GamePadCapabilities Capabilities => mCapabilities;
        #endregion

        #region Methods

        #region Constructor

        static Xbox360GamePad()
        {
            // For info on how IDs work, see this:
            // https://community.monogame.net/t/support-for-modern-atari-vcs-controllers/15177
            GamepadNameTypeMap[GamepadLayout.NES] = new HashSet<string>
            {
                
            };
            GamepadIdTypeMap[GamepadLayout.NES] = new HashSet<string>
            {
                // "Retro" controller
            };

            GamepadNameTypeMap[GamepadLayout.SuperNintendo] = new HashSet<string>
            {
                "Retro Controller"
            };
            GamepadIdTypeMap[GamepadLayout.SuperNintendo] = new HashSet<string>
            {
            };

            GamepadNameTypeMap[GamepadLayout.Genesis] = new HashSet<string>
            {

            };
            GamepadIdTypeMap[GamepadLayout.Genesis] = new HashSet<string>
            {

            };

            GamepadNameTypeMap[GamepadLayout.Nintendo64] = new HashSet<string>
            {

            };
            GamepadIdTypeMap[GamepadLayout.Nintendo64] = new HashSet<string>
            {

            };

            GamepadNameTypeMap[GamepadLayout.GameCube] = new HashSet<string>
            {
                "Controller (HORIPAD S)"
            };
            GamepadIdTypeMap[GamepadLayout.GameCube] = new HashSet<string>
            {
                "00000003-0e6f-0000-8501-000077006800" // PDF Wired Fightpad Pro 
            };


            GamepadNameTypeMap[GamepadLayout.SwitchPro] = new HashSet<string>
            {
                "Nintendo Switch Pro Controller"
            };
            GamepadIdTypeMap[GamepadLayout.SwitchPro] = new HashSet<string>
            {
                "Nintendo Switch Pro Controller"
            };


            GamepadNameTypeMap[GamepadLayout.Xbox360]  = new HashSet<string>
            {
                "Controller (Xbox One For Windows)"
            };
            GamepadIdTypeMap[GamepadLayout.Xbox360] = new HashSet<string>
            {
            };

            GamepadNameTypeMap[GamepadLayout.PlayStationDualShock] = new HashSet<string>
            {

            };
            GamepadIdTypeMap[GamepadLayout.PlayStationDualShock] = new HashSet<string>
            {

            };
        }

        internal Xbox360GamePad(PlayerIndex playerIndex)
        {
            for (int i = 0; i < mLastButtonPush.Length; i++)
            {
                mLastButtonPush[i] = -1;
            }


            mPlayerIndex = playerIndex;
            mLeftStick = new AnalogStick();
            mRightStick = new AnalogStick();

            mLeftTrigger = new AnalogButton();
            mLeftTrigger.Name = "Left Trigger";
            mRightTrigger = new AnalogButton();
            mRightTrigger.Name = "Right Trigger";
        }

        #endregion

        #region Public Methods

        #region Button States

        /// <summary>
        /// Returns whether any button was pushed on this Xbox360GamePad.  
        /// This considers face buttons, trigger buttons, shoulder buttons, and d pad.
        /// 
        /// Justin 3/19/22: added optional arguments to explicitly ignore directional
        /// and analog inputs as buttons as these are often not intended to register
        /// as buttons. The defaults ensure back compat.
        /// </summary>
        /// <param name="ignoreDirectionals">Whether to ignore directions, such as the D-Pad</param>
        /// <param name="ignoreAnalogs">Whether to consider analogs, such as triggers and sticks, as buttons</param>
        /// <returns>Whether any button was pushed.</returns>
        public bool AnyButtonPushed(bool ignoreDirectionals = false, bool ignoreAnalogs = false)
        {
            for (int i = 0; i < NumberOfButtons; i++)
            {
                var button = (Button)i;

                if (ignoreDirectionals &&
                    (button == Button.DPadLeft ||
                        button == Button.DPadUp ||
                        button == Button.DPadRight ||
                        button == Button.DPadDown ||
                        button == Button.LeftStickAsDPadLeft ||
                        button == Button.LeftStickAsDPadUp ||
                        button == Button.LeftStickAsDPadRight ||
                        button == Button.LeftStickAsDPadDown))
                {
                        continue;
                }

                if (ignoreAnalogs &&
                    (button == Button.LeftTrigger ||
                    button == Button.RightTrigger ||
                    button == Button.LeftStickAsDPadLeft ||
                        button == Button.LeftStickAsDPadUp ||
                        button == Button.LeftStickAsDPadRight ||
                        button == Button.LeftStickAsDPadDown))
                {
                    continue;
                }

                if (ButtonPushed(button))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns whether the argument button is being held down. For analog buttons, such as LeftTrigger 
        /// and RightTrigger, the AnalogOnThreshold value is used to determine if the button is down.
        /// </summary>
        /// <param name="button">The button to check.</param>
        /// <returns>Returns true if the button is down, otherwise false.</returns>
        public bool ButtonDown(Button button)
        {
            if (mButtonsIgnoredForThisFrame[(int)button] || InputManager.CurrentFrameInputSuspended)
                return false;

            bool returnValue = false;

            #region If there is a ButtonMap

            if (this.mButtonMap != null)
            {
                switch (button)
                {
                    case Button.A:
                        returnValue |= mButtonMap.A != Keys.None && InputManager.Keyboard.KeyDown(mButtonMap.A);
                        break;
                    case Button.B:
                        returnValue |= mButtonMap.B != Keys.None && InputManager.Keyboard.KeyDown(mButtonMap.B);
                        break;
                    case Button.X:
                        returnValue |= mButtonMap.X != Keys.None && InputManager.Keyboard.KeyDown(mButtonMap.X);
                        break;
                    case Button.Y:
                        returnValue |= mButtonMap.Y != Keys.None && InputManager.Keyboard.KeyDown(mButtonMap.Y);
                        break;
                    case Button.LeftShoulder:
                        returnValue |= mButtonMap.LeftShoulder != Keys.None && InputManager.Keyboard.KeyDown(mButtonMap.LeftShoulder);
                        break;
                    case Button.RightShoulder:
                        returnValue |= mButtonMap.RightShoulder != Keys.None && InputManager.Keyboard.KeyDown(mButtonMap.RightShoulder);
                        break;
                    case Button.Back:
                        returnValue |= mButtonMap.Back != Keys.None && InputManager.Keyboard.KeyDown(mButtonMap.Back);
                        break;
                    case Button.Start:
                        returnValue |= mButtonMap.Start != Keys.None && InputManager.Keyboard.KeyDown(mButtonMap.Start);
                        break;
                    case Button.LeftStick:
                        returnValue |= mButtonMap.LeftStick != Keys.None && InputManager.Keyboard.KeyDown(mButtonMap.LeftStick);
                        break;
                    case Button.RightStick:
                        returnValue |= mButtonMap.RightStick != Keys.None && InputManager.Keyboard.KeyDown(mButtonMap.RightStick);
                        break;
                    case Button.DPadUp:
                        returnValue |= mButtonMap.DPadUp != Keys.None && InputManager.Keyboard.KeyDown(mButtonMap.DPadUp);
                        break;
                    case Button.DPadDown:
                        returnValue |= mButtonMap.DPadDown != Keys.None && InputManager.Keyboard.KeyDown(mButtonMap.DPadDown);
                        break;
                    case Button.DPadLeft:
                        returnValue |= mButtonMap.DPadLeft != Keys.None && InputManager.Keyboard.KeyDown(mButtonMap.DPadLeft);
                        break;
                    case Button.DPadRight:
                        returnValue |= mButtonMap.DPadRight != Keys.None && InputManager.Keyboard.KeyDown(mButtonMap.DPadRight);
                        break;
                    case Button.LeftTrigger:
                        returnValue |= mButtonMap.LeftTrigger != Keys.None && InputManager.Keyboard.KeyDown(mButtonMap.LeftTrigger);
                        break;
                    case Button.RightTrigger:
                        returnValue |= mButtonMap.RightTrigger != Keys.None && InputManager.Keyboard.KeyDown(mButtonMap.RightTrigger);
                        break;
                    //default:
                    //    return false;
                }
            }

            #endregion

            #region Handle the buttons if there isn't a ButtonMap (this can happen even if there is a ButtonMap)


            bool areShouldersAndTriggersFlipped = AreShoulderAndTriggersFlipped;


            switch (button)
            {
                case Button.A:
                    returnValue |= mGamePadState.Buttons.A == ButtonState.Pressed;
                    break;
                case Button.B:
                    returnValue |= mGamePadState.Buttons.B == ButtonState.Pressed;
                    break;
                case Button.X:
                    returnValue |= mGamePadState.Buttons.X == ButtonState.Pressed;
                    break;
                case Button.Y:
                    returnValue |= mGamePadState.Buttons.Y == ButtonState.Pressed;
                    break;
                case Button.LeftShoulder:
                    if(areShouldersAndTriggersFlipped)
                    {
                        returnValue |= mLeftTrigger.Position >= AnalogOnThreshold;
                    }
                    else
                    {
                        returnValue |= mGamePadState.Buttons.LeftShoulder == ButtonState.Pressed;
                    }
                    break;
                case Button.RightShoulder:
                    if(areShouldersAndTriggersFlipped)
                    {
                        returnValue |= mRightTrigger.Position >= AnalogOnThreshold;
                    }
                    else
                    {
                        returnValue |= mGamePadState.Buttons.RightShoulder == ButtonState.Pressed;
                    }
                    break;
                case Button.Back:
                    returnValue |= mGamePadState.Buttons.Back == ButtonState.Pressed;
                    break;
                case Button.Start:
                    returnValue |= mGamePadState.Buttons.Start == ButtonState.Pressed;
                    break;
                case Button.LeftStick:
                    returnValue |= mGamePadState.Buttons.LeftStick == ButtonState.Pressed;
                    break;
                case Button.RightStick:
                    returnValue |= mGamePadState.Buttons.RightStick == ButtonState.Pressed;
                    break;
                case Button.DPadUp:
                    returnValue |= mGamePadState.DPad.Up == ButtonState.Pressed;
                    break;
                case Button.DPadDown:
                    returnValue |= mGamePadState.DPad.Down == ButtonState.Pressed;
                    break;
                case Button.DPadLeft:
                    returnValue |= mGamePadState.DPad.Left == ButtonState.Pressed;
                    break;
                case Button.DPadRight:
                    returnValue |= mGamePadState.DPad.Right == ButtonState.Pressed;
                    break;
                case Button.LeftTrigger:
                    if(areShouldersAndTriggersFlipped)
                    {
                        returnValue |= mGamePadState.Buttons.LeftShoulder == ButtonState.Pressed;
                    }
                    else
                    {
                        returnValue |= mLeftTrigger.Position >= AnalogOnThreshold;
                    }
                    break;
                case Button.RightTrigger:
                    if (areShouldersAndTriggersFlipped)
                    {
                        returnValue |= mGamePadState.Buttons.RightShoulder == ButtonState.Pressed;
                    }
                    else
                    {
                        returnValue |= mRightTrigger.Position >= AnalogOnThreshold;
                    }
                    break;
            }

            #endregion

            return returnValue;

        }

        /// <summary>
        /// Returns whether the argument button type is pushed. For analog buttons, such as LeftTrigger 
        /// and RightTrigger, the AnalogOnThreshold value is used to determine if the button is pressed.
        /// </summary>
        /// <param name="button">The button to check.</param>
        /// <returns>true if the button is pressed, otherwise false</returns>
        public bool ButtonPushed(Button button)
        {
            if (InputManager.mIgnorePushesThisFrame || mButtonsIgnoredForThisFrame[(int)button] || InputManager.CurrentFrameInputSuspended)
                return false;

            bool returnValue = false;


            if (this.mButtonMap != null)
            {
                switch (button)
                {
                    case Button.A:
                        returnValue |= InputManager.Keyboard.KeyPushed(mButtonMap.A);
                        break;
                    case Button.B:
                        returnValue |= InputManager.Keyboard.KeyPushed(mButtonMap.B);
                        break;
                    case Button.X:
                        returnValue |= InputManager.Keyboard.KeyPushed(mButtonMap.X);
                        break;
                    case Button.Y:
                        returnValue |= InputManager.Keyboard.KeyPushed(mButtonMap.Y);
                        break;
                    case Button.LeftShoulder:
                        returnValue |= InputManager.Keyboard.KeyPushed(mButtonMap.LeftShoulder);
                        break;
                    case Button.RightShoulder:
                        returnValue |= InputManager.Keyboard.KeyPushed(mButtonMap.RightShoulder);
                        break;
                    case Button.Back:
                        returnValue |= InputManager.Keyboard.KeyPushed(mButtonMap.Back);
                        break;
                    case Button.Start:
                        returnValue |= InputManager.Keyboard.KeyPushed(mButtonMap.Start);
                        break;
                    case Button.LeftStick:
                        returnValue |= InputManager.Keyboard.KeyPushed(mButtonMap.LeftStick);
                        break;
                    case Button.RightStick:
                        returnValue |= InputManager.Keyboard.KeyPushed(mButtonMap.RightStick);
                        break;
                    case Button.DPadUp:
                        returnValue |= InputManager.Keyboard.KeyPushed(mButtonMap.DPadUp);
                        break;
                    case Button.DPadDown:
                        returnValue |= InputManager.Keyboard.KeyPushed(mButtonMap.DPadDown);
                        break;
                    case Button.DPadLeft:
                        returnValue |= InputManager.Keyboard.KeyPushed(mButtonMap.DPadLeft);
                        break;
                    case Button.DPadRight:
                        returnValue |= InputManager.Keyboard.KeyPushed(mButtonMap.DPadRight);
                        break;
                    case Button.LeftTrigger:
                        returnValue |= InputManager.Keyboard.KeyPushed(mButtonMap.LeftTrigger);
                        break;
                    case Button.RightTrigger:
                        returnValue |= InputManager.Keyboard.KeyPushed(mButtonMap.RightTrigger);
                        break;
                }
            }

            bool areShouldersAndTriggersFlipped = AreShoulderAndTriggersFlipped;

            switch (button)
            {
                case Button.A:
                    returnValue |= mGamePadState.Buttons.A == ButtonState.Pressed && mLastGamePadState.Buttons.A == ButtonState.Released;
                    break;
                case Button.B:
                    returnValue |= mGamePadState.Buttons.B == ButtonState.Pressed && mLastGamePadState.Buttons.B == ButtonState.Released;
                    break;
                case Button.X:
                    returnValue |= mGamePadState.Buttons.X == ButtonState.Pressed && mLastGamePadState.Buttons.X == ButtonState.Released;
                    break;
                case Button.Y:
                    returnValue |= mGamePadState.Buttons.Y == ButtonState.Pressed && mLastGamePadState.Buttons.Y == ButtonState.Released;
                    break;
                case Button.LeftShoulder:
                    if(areShouldersAndTriggersFlipped)
                    {
                        returnValue |= mLeftTrigger.Position >= AnalogOnThreshold && mLeftTrigger.LastPosition < AnalogOnThreshold;
                    }
                    else
                    {
                        returnValue |= mGamePadState.Buttons.LeftShoulder == ButtonState.Pressed && mLastGamePadState.Buttons.LeftShoulder == ButtonState.Released;
                    }
                    break;
                case Button.RightShoulder:
                    if (areShouldersAndTriggersFlipped)
                    {
                        returnValue |= mRightTrigger.Position >= AnalogOnThreshold && mRightTrigger.LastPosition < AnalogOnThreshold;
                    }
                    else
                    {
                        returnValue |= mGamePadState.Buttons.RightShoulder == ButtonState.Pressed && mLastGamePadState.Buttons.RightShoulder == ButtonState.Released;
                    }
                    break;
                case Button.Back:
                    returnValue |= mGamePadState.Buttons.Back == ButtonState.Pressed && mLastGamePadState.Buttons.Back == ButtonState.Released;
                    break;
                case Button.Start:
                    returnValue |= mGamePadState.Buttons.Start == ButtonState.Pressed && mLastGamePadState.Buttons.Start == ButtonState.Released;
                    break;
                case Button.LeftStick:
                    returnValue |= mGamePadState.Buttons.LeftStick == ButtonState.Pressed && mLastGamePadState.Buttons.LeftStick == ButtonState.Released;
                    break;
                case Button.RightStick:
                    returnValue |= mGamePadState.Buttons.RightStick == ButtonState.Pressed && mLastGamePadState.Buttons.RightStick == ButtonState.Released;
                    break;
                case Button.DPadUp:
                    returnValue |= mGamePadState.DPad.Up == ButtonState.Pressed && mLastGamePadState.DPad.Up == ButtonState.Released;
                    break;
                case Button.DPadDown:
                    returnValue |= mGamePadState.DPad.Down == ButtonState.Pressed && mLastGamePadState.DPad.Down == ButtonState.Released;
                    break;
                case Button.DPadLeft:
                    returnValue |= mGamePadState.DPad.Left == ButtonState.Pressed && mLastGamePadState.DPad.Left == ButtonState.Released;
                    break;
                case Button.DPadRight:
                    returnValue |= mGamePadState.DPad.Right == ButtonState.Pressed && mLastGamePadState.DPad.Right == ButtonState.Released;
                    break;
                case Button.LeftTrigger:
                    if (areShouldersAndTriggersFlipped)
                    {
                        returnValue |= mGamePadState.Buttons.LeftShoulder == ButtonState.Pressed && mLastGamePadState.Buttons.LeftShoulder == ButtonState.Released;
                    }
                    else
                    {
                        returnValue |= mLeftTrigger.Position >= AnalogOnThreshold && mLeftTrigger.LastPosition < AnalogOnThreshold;
                    }
                    break;
                case Button.RightTrigger:
                    if (areShouldersAndTriggersFlipped)
                    {
                        returnValue |= mGamePadState.Buttons.RightShoulder == ButtonState.Pressed && mLastGamePadState.Buttons.RightShoulder == ButtonState.Released;
                    }
                    else
                    {
                        returnValue |= mRightTrigger.Position >= AnalogOnThreshold && mRightTrigger.LastPosition < AnalogOnThreshold;
                    }
                    break;
                case Button.LeftStickAsDPadUp:
                    returnValue |= LeftStick.AsDPadPushed(DPadDirection.Up);
                    break;
                case Button.LeftStickAsDPadDown:
                    returnValue |= LeftStick.AsDPadPushed(DPadDirection.Down);
                    break;
                case Button.LeftStickAsDPadLeft:
                    returnValue |= LeftStick.AsDPadPushed(DPadDirection.Left);
                    break;
                case Button.LeftStickAsDPadRight:
                    returnValue |= LeftStick.AsDPadPushed(DPadDirection.Right);
                    break;
            }

            return returnValue;
        }


        public bool ButtonReleased(Button button)
        {
            if (mButtonsIgnoredForThisFrame[(int)button] || InputManager.CurrentFrameInputSuspended)
                return false;

            bool returnValue = false;

            if (this.mButtonMap != null)
            {
                switch (button)
                {
                    case Button.A:
                        returnValue |= InputManager.Keyboard.KeyReleased(mButtonMap.A);
                        break;
                    case Button.B:
                        returnValue |= InputManager.Keyboard.KeyReleased(mButtonMap.B);
                        break;
                    case Button.X:
                        returnValue |= InputManager.Keyboard.KeyReleased(mButtonMap.X);
                        break;
                    case Button.Y:
                        returnValue |= InputManager.Keyboard.KeyReleased(mButtonMap.Y);
                        break;
                    case Button.LeftShoulder:
                        returnValue |= InputManager.Keyboard.KeyReleased(mButtonMap.LeftShoulder);
                        break;
                    case Button.RightShoulder:
                        returnValue |= InputManager.Keyboard.KeyReleased(mButtonMap.RightShoulder);
                        break;
                    case Button.Back:
                        returnValue |= InputManager.Keyboard.KeyReleased(mButtonMap.Back);
                        break;
                    case Button.Start:
                        returnValue |= InputManager.Keyboard.KeyReleased(mButtonMap.Start);
                        break;
                    case Button.LeftStick:
                        returnValue |= InputManager.Keyboard.KeyReleased(mButtonMap.LeftStick);
                        break;
                    case Button.RightStick:
                        returnValue |= InputManager.Keyboard.KeyReleased(mButtonMap.RightStick);
                        break;
                    case Button.DPadUp:
                        returnValue |= InputManager.Keyboard.KeyReleased(mButtonMap.DPadUp);
                        break;
                    case Button.DPadDown:
                        returnValue |= InputManager.Keyboard.KeyReleased(mButtonMap.DPadDown);
                        break;
                    case Button.DPadLeft:
                        returnValue |= InputManager.Keyboard.KeyReleased(mButtonMap.DPadLeft);
                        break;
                    case Button.DPadRight:
                        returnValue |= InputManager.Keyboard.KeyReleased(mButtonMap.DPadRight);
                        break;
                    case Button.LeftTrigger:
                        returnValue |= InputManager.Keyboard.KeyReleased(mButtonMap.LeftTrigger);
                        break;
                    case Button.RightTrigger:
                        returnValue |= InputManager.Keyboard.KeyReleased(mButtonMap.RightTrigger);
                        break;
                }
            }

            bool areShouldersAndTriggersFlipped = AreShoulderAndTriggersFlipped;

            switch (button)
            {
                case Button.A:
                    returnValue |= mGamePadState.Buttons.A == ButtonState.Released && mLastGamePadState.Buttons.A == ButtonState.Pressed;
                    break;
                case Button.B:
                    returnValue |= mGamePadState.Buttons.B == ButtonState.Released && mLastGamePadState.Buttons.B == ButtonState.Pressed;
                    break;
                case Button.X:
                    returnValue |= mGamePadState.Buttons.X == ButtonState.Released && mLastGamePadState.Buttons.X == ButtonState.Pressed;
                    break;
                case Button.Y:
                    returnValue |= mGamePadState.Buttons.Y == ButtonState.Released && mLastGamePadState.Buttons.Y == ButtonState.Pressed;
                    break;
                case Button.LeftShoulder:
                    if(areShouldersAndTriggersFlipped)
                    {
                        returnValue |= mLeftTrigger.Position < AnalogOnThreshold && mLeftTrigger.LastPosition >= AnalogOnThreshold;
                    }
                    else
                    {
                        returnValue |= mGamePadState.Buttons.LeftShoulder == ButtonState.Released && mLastGamePadState.Buttons.LeftShoulder == ButtonState.Pressed;
                    }
                    break;
                case Button.RightShoulder:
                    if (areShouldersAndTriggersFlipped)
                    {
                        returnValue |= mRightTrigger.Position < AnalogOnThreshold && mRightTrigger.LastPosition >= AnalogOnThreshold;
                    }
                    else
                    {
                        returnValue |= mGamePadState.Buttons.RightShoulder == ButtonState.Released && mLastGamePadState.Buttons.RightShoulder == ButtonState.Pressed;
                    }
                    break;
                case Button.Back:
                    returnValue |= mGamePadState.Buttons.Back == ButtonState.Released && mLastGamePadState.Buttons.Back == ButtonState.Pressed;
                    break;
                case Button.Start:
                    returnValue |= mGamePadState.Buttons.Start == ButtonState.Released && mLastGamePadState.Buttons.Start == ButtonState.Pressed;
                    break;
                case Button.LeftStick:
                    returnValue |= mGamePadState.Buttons.LeftStick == ButtonState.Released && mLastGamePadState.Buttons.LeftStick == ButtonState.Pressed;
                    break;
                case Button.RightStick:
                    returnValue |= mGamePadState.Buttons.RightStick == ButtonState.Released && mLastGamePadState.Buttons.RightStick == ButtonState.Pressed;
                    break;
                case Button.DPadUp:
                    returnValue |= mGamePadState.DPad.Up == ButtonState.Released && mLastGamePadState.DPad.Up == ButtonState.Pressed;
                    break;
                case Button.DPadDown:
                    returnValue |= mGamePadState.DPad.Down == ButtonState.Released && mLastGamePadState.DPad.Down == ButtonState.Pressed;
                    break;
                case Button.DPadLeft:
                    returnValue |= mGamePadState.DPad.Left == ButtonState.Released && mLastGamePadState.DPad.Left == ButtonState.Pressed;
                    break;
                case Button.DPadRight:
                    returnValue |= mGamePadState.DPad.Right == ButtonState.Released && mLastGamePadState.DPad.Right == ButtonState.Pressed;
                    break;
                case Button.LeftTrigger:
                    if (areShouldersAndTriggersFlipped)
                    {
                        returnValue |= mGamePadState.Buttons.LeftShoulder == ButtonState.Released && mLastGamePadState.Buttons.LeftShoulder == ButtonState.Pressed;
                    }
                    else
                    {
                        returnValue |= mLeftTrigger.Position < AnalogOnThreshold && mLeftTrigger.LastPosition >= AnalogOnThreshold;
                    }
                    break;
                case Button.RightTrigger:
                    if (areShouldersAndTriggersFlipped)
                    {
                        returnValue |= mGamePadState.Buttons.RightShoulder == ButtonState.Released && mLastGamePadState.Buttons.RightShoulder == ButtonState.Pressed;
                    }
                    else
                    {
                        returnValue |= mRightTrigger.Position < AnalogOnThreshold && mRightTrigger.LastPosition >= AnalogOnThreshold;
                    }
                    break;
            }

            return returnValue;

        }
        
        /// <summary>
        /// Returns whether the argument was pushed this frame, or whether it is continually being held down and a "repeat" press
        /// has occurred.
        /// </summary>
        /// <param name="button">The button to test, which includes DPad directions.</param>
        /// <param name="timeAfterPush">The number of seconds after initial push to wait before raising repeat rates. This value is typically larger than timeBetweenRepeating.</param>
        /// <param name="timeBetweenRepeating">The number of seconds between repeats once the timeAfterPush. This value is typically smaller than timeAfterPush.</param>
        /// <returns>Whether the button was pushed or repeated this frame.</returns>
        public bool ButtonRepeatRate(Button button, double timeAfterPush = .35, double timeBetweenRepeating = .12)
        {
            if (mButtonsIgnoredForThisFrame[(int)button])
                return false;

            if (ButtonPushed(button))
                return true;

            // If this method is called multiple times per frame this line
            // of code guarantees that the user will get true every time until
            // the next TimeManager.Update (next frame).
            bool repeatedThisFrame = mLastRepeatRate[(int)button] == TimeManager.CurrentTime;

            if (repeatedThisFrame ||
                (
                ButtonDown(button) &&
                TimeManager.CurrentTime - mLastButtonPush[(int)button] > timeAfterPush &&
                TimeManager.CurrentTime - mLastRepeatRate[(int)button] > timeBetweenRepeating)
                )
            {
                mLastRepeatRate[(int)button] = TimeManager.CurrentTime;
                return true;
            }

            return false;

        }

        /// <summary>
        /// Returns an Xbox360ButtonReference for the argument Button.
        /// </summary>
        /// <param name="button">The button, such as Button.A</param>
        /// <returns>The reference, which can then be used to check for input.</returns>
        public Xbox360ButtonReference GetButton(Button button)
        {
            if(cachedButtons.ContainsKey(button) == false)
            {
                var newReference = new Xbox360ButtonReference();
                newReference.Button = button;
                newReference.GamePad = this;
                cachedButtons.Add(button, newReference);
            }
            return cachedButtons[button];
        }
        #endregion

        /// <summary>
        /// Clears the input on this controller for this frame. This includes
        /// analog stick values, button values, and trigger values.
        /// </summary>
        public void Clear()
        {
#if MONOGAME
            mGamePadState = GamePadState.Default;
            mLastGamePadState = GamePadState.Default;
#else
            // XNA doesn't have .Default
            mGamePadState = new GamePadState();
            mLastGamePadState = new GamePadState() ;
#endif
            for (int i = 0; i < NumberOfButtons; i++)
            {
                if (ButtonPushed((Button)i))
                {
                    IgnoreButtonForOneFrame((Button)i);
                }
            }

            mLeftStick.Clear();
            mRightStick.Clear();

            mLeftTrigger.Clear();
            mRightTrigger.Clear();

            mFlippedLeftTrigger?.Clear();
            mFlippedRightTrigger?.Clear();
        }


        #region Control Positioned Object

        public void ControlPositionedObject(PositionedObject positionedObject)
        {
            // Make this more suitable for 2D games by increasing the default value
            //ControlPositionedObject(positionedObject, 10);
            ControlPositionedObject(positionedObject, 64);
        }

        public void ControlPositionedObject(PositionedObject positionedObject, float velocity)
        {
            positionedObject.XVelocity = this.LeftStick.Position.X * velocity;
            positionedObject.YVelocity = this.LeftStick.Position.Y * velocity;

            if(ButtonDown(Button.DPadLeft)) positionedObject.XVelocity = -velocity;
            if(ButtonDown(Button.DPadRight)) positionedObject.XVelocity = velocity;
            if(ButtonDown(Button.DPadUp)) positionedObject.YVelocity = velocity;
            if(ButtonDown(Button.DPadDown)) positionedObject.YVelocity = -velocity;

            if (ButtonDown(Button.LeftShoulder))
                positionedObject.ZVelocity = velocity;
            else if (ButtonDown(Button.RightShoulder))
                positionedObject.ZVelocity = -velocity;
            else
                positionedObject.ZVelocity = 0;
        }


        public void ControlPositionedObjectDPad(PositionedObject positionedObject, float velocity)
        {
            if (ButtonDown(Button.DPadLeft))
                positionedObject.XVelocity = -velocity;
            else if (ButtonDown(Button.DPadRight))
                positionedObject.XVelocity = velocity;
            else
                positionedObject.XVelocity = 0;

            if (ButtonDown(Button.DPadUp))
                positionedObject.YVelocity = velocity;
            else if (ButtonDown(Button.DPadDown))
                positionedObject.YVelocity = -velocity;
            else
                positionedObject.YVelocity = 0;
        }


        public void ControlPositionedObjectAcceleration(PositionedObject positionedObject, float acceleration)
        {
            positionedObject.Acceleration.X = this.LeftStick.Position.X * acceleration;
            positionedObject.Acceleration.Y = this.LeftStick.Position.Y * acceleration;
        }


        public void ControlPositionedObjectFpsStyle(PositionedObject positionedObject, Vector3 up)
        {
            positionedObject.Velocity = new Vector3();

            positionedObject.Velocity += positionedObject.RotationMatrix.Forward * LeftStick.Position.Y * 7;
            positionedObject.Velocity += positionedObject.RotationMatrix.Right * LeftStick.Position.X * 7;

            positionedObject.RotationMatrix *= Matrix.CreateFromAxisAngle(positionedObject.RotationMatrix.Right, TimeManager.SecondDifference * RightStick.Position.Y);
            positionedObject.RotationMatrix *= Matrix.CreateFromAxisAngle(up, -TimeManager.SecondDifference * RightStick.Position.X);
        }

        #endregion

        /// <summary>
        /// Creates a ButtonMap for this controller using the default bindings.  This is 
        /// a quick way to simulate an Xbox360 controller using the keyboard.
        /// </summary>
        /// <remarks>
        /// This creates the following bindings:
        /// * Left analog stick = arrow keys
        /// * A button = A key
        /// * B button = S key
        /// * X button = Q key
        /// * Y button = W key
        /// * Left trigger = E key
        /// * Right trigger = R key
        /// * Left shoulder = D key
        /// * Right Shoulder = F key
        /// * Back button = Backspace key
        /// * Start button = Enter key
        /// 
        /// This will not simulate that the controller is connected, so you will have to set 
        /// FakeIsConnected to true if your game checks the connected state.
        /// </remarks>
        public void CreateDefaultButtonMap()
        {
            this.ButtonMap = new KeyboardButtonMap();

            ButtonMap.LeftAnalogLeft = Keys.Left;
            ButtonMap.LeftAnalogRight = Keys.Right;
            ButtonMap.LeftAnalogUp = Keys.Up;
            ButtonMap.LeftAnalogDown = Keys.Down;

            ButtonMap.A = Keys.A;
            ButtonMap.B = Keys.S;
            ButtonMap.X = Keys.Q;
            ButtonMap.Y = Keys.W;

            ButtonMap.LeftTrigger = Keys.E;
            ButtonMap.RightTrigger = Keys.R;
            ButtonMap.LeftShoulder = Keys.D;
            ButtonMap.RightShoulder = Keys.F;


            ButtonMap.Back = Keys.Back;
            ButtonMap.Start = Keys.Enter;
        }

        /// <summary>
        /// Makes this Xbox360Gamepad ignore the argument button for the rest of the current frame.
        /// </summary>
        /// <param name="buttonToIgnore">The button that should be ignored for the rest of the current frame.</param>
        public void IgnoreButtonForOneFrame(Button buttonToIgnore)
        {
            mButtonsIgnoredForThisFrame[(int)buttonToIgnore] = true;

        }


        /// <summary>
        /// Updates the Xbox360Gamepad according to the argument gamepadState.  This is publicly available for games
        /// which need to simulate Xbox360Gamepads.
        /// </summary>
        /// <remarks>
        /// This function is normally called automatically by the FlatRedBall Engine
        /// in its regular update loop.  You only need to call this function if you want
        /// to override the behavior of the gamepad.  Be sure to call this function after
        /// FlatRedBallServices.Update, but before any custom game logic (such as ScreenManager.Activity).
        /// </remarks>
        /// <param name="gamepadState">The state containing the data for this frame.</param>
        public void Update(GamePadState gamepadState)
        {
            UpdateInputManagerBack();

            mLastGamePadState = mGamePadState;

            mGamePadState = gamepadState;

            if(WasConnectedThisFrame)
            {
                UpdateToGamepadType();
            }

            UpdateAnalogStickAndTriggerValues();

            UpdateLastButtonPushedValues();
        }

        private void UpdateToGamepadType()
        {
            var oldLayout = ButtonLayout;

#if MONOGAME_381
            var name = mCapabilities.DisplayName;
            var id = mCapabilities.Identifier;

#else
            var name = "Xbox";
            var id = "";
#endif

            // default to xbox since that's the most common type of controller (I think?)

            var found = false;
            GamepadLayout = GamepadLayout.Xbox360;

            foreach (var kvp in GamepadIdTypeMap)
            {
                if(kvp.Value.Contains(id))
                {
                    GamepadLayout = kvp.Key;
                    found = true;
                    break;
                }
            }

            if(!found)
            {
                foreach(var kvp in GamepadNameTypeMap) 
                {
                    if(kvp.Value.Contains(name))
                    {
                        GamepadLayout = kvp.Key;
                        found = true;
                        break;
                    }
                }
            }

            if(!found && name != null)
            {
                if(name.Contains("Xbox") == true)
                {
                    GamepadLayout = GamepadLayout.Xbox360;
                }
                else if(name.Contains("Nintendo") == true)
                {
                    GamepadLayout = GamepadLayout.SwitchPro;
                }
                else if(name.Contains("PS3") || name.Contains("PS4") || name.Contains("PS5"))
                {
                    GamepadLayout = GamepadLayout.PlayStationDualShock;
                }
            }

            switch(GamepadLayout)
            {
                case GamepadLayout.NES:
                    ButtonLayout = ButtonLayout.Xbox; //?
                    break;
                case GamepadLayout.SuperNintendo:
                    ButtonLayout = ButtonLayout.NintendoPro;
                    break;
                case GamepadLayout.Nintendo64:
                    ButtonLayout = ButtonLayout.Xbox; // ?
                    break;
                case GamepadLayout.GameCube:
                    ButtonLayout = ButtonLayout.GameCube;
                    break;
                case GamepadLayout.SwitchPro:
                    ButtonLayout = ButtonLayout.NintendoPro;
                    break;
                case GamepadLayout.Genesis:
                    ButtonLayout = ButtonLayout.Xbox;
                    break;
                case GamepadLayout.Xbox360:
                    ButtonLayout = ButtonLayout.Xbox;
                    break;
                case GamepadLayout.PlayStationDualShock:
                    ButtonLayout = ButtonLayout.Xbox;
                    break;
            }

            if (oldLayout != ButtonLayout)
            {
                if (AreShoulderAndTriggersFlipped)
                {
                    mFlippedLeftTrigger = new AnalogButton();
                    mFlippedRightTrigger = new AnalogButton();
                    mFlippedLeftTrigger.Name = "Left Trigger (flipped)";
                    mFlippedRightTrigger.Name = "Right Trigger (flipped)";
                }
                else
                {
                    mFlippedLeftTrigger = null;
                    mFlippedRightTrigger = null;
                }
            }
        }

        private void UpdateLastButtonPushedValues()
        {
            // Set the last pushed and clear the ignored input

            for (int i = 0; i < NumberOfButtons; i++)
            {
                mButtonsIgnoredForThisFrame[i] = false;

                if (ButtonPushed((Button)i))
                {
                    mLastButtonPush[i] = TimeManager.CurrentTime;
                }
            }
        }

        private void UpdateAnalogStickAndTriggerValues()
        {
            if (mButtonMap == null)
            {
                var leftStick = mGamePadState.ThumbSticks.Left;
                var rightStick = mGamePadState.ThumbSticks.Right;

                mLeftStick.Update(leftStick);
                mRightStick.Update(rightStick);

                if(AreShoulderAndTriggersFlipped)
                {
                    mFlippedLeftTrigger.Update((int)mGamePadState.Buttons.LeftShoulder);
                    mFlippedRightTrigger.Update((int)mGamePadState.Buttons.RightShoulder);

                }

                // Even if using Gamecube, record these values as they are used above in button maps
                mLeftTrigger.Update(mGamePadState.Triggers.Left);
                mRightTrigger.Update(mGamePadState.Triggers.Right);

            }
            else
            {
                Vector2 newPosition = new Vector2();

                #region Set the left analog stick position
                if (mButtonMap.LeftAnalogLeft != Keys.None && InputManager.Keyboard.KeyDown(mButtonMap.LeftAnalogLeft))
                {
                    newPosition.X = -1;
                }

                else if (mButtonMap.LeftAnalogRight != Keys.None && InputManager.Keyboard.KeyDown(mButtonMap.LeftAnalogRight))
                {
                    newPosition.X = 1;
                }

                if (mButtonMap.LeftAnalogUp != Keys.None && InputManager.Keyboard.KeyDown(mButtonMap.LeftAnalogUp))
                {
                    newPosition.Y = 1;
                }

                else if (mButtonMap.LeftAnalogDown != Keys.None && InputManager.Keyboard.KeyDown(mButtonMap.LeftAnalogDown))
                {
                    newPosition.Y = -1;
                }

                //cap for diagonal presses
                if (System.Math.Abs(newPosition.X) > 0.7071068f
                    && System.Math.Abs(newPosition.Y) > 0.7071068f)
                {
                    newPosition.X = System.Math.Sign(newPosition.X) * 0.7071068f;
                    newPosition.Y = System.Math.Sign(newPosition.Y) * 0.7071068f;
                }

                mLeftStick.Update(newPosition);

                #endregion

                #region Set the right analog stick position

                newPosition = new Vector2();

                if (mButtonMap.RightAnalogLeft != Keys.None && InputManager.Keyboard.KeyDown(mButtonMap.RightAnalogLeft))
                {
                    newPosition.X = -1;
                }

                else if (mButtonMap.RightAnalogRight != Keys.None && InputManager.Keyboard.KeyDown(mButtonMap.RightAnalogRight))
                {
                    newPosition.X = 1;
                }

                if (mButtonMap.RightAnalogUp != Keys.None && InputManager.Keyboard.KeyDown(mButtonMap.RightAnalogUp))
                {
                    newPosition.Y = 1;
                }

                else if (mButtonMap.RightAnalogDown != Keys.None && InputManager.Keyboard.KeyDown(mButtonMap.RightAnalogDown))
                {
                    newPosition.Y = -1;
                }

                //cap for diagonal presses
                if (System.Math.Abs(newPosition.X) > 0.7071068f
                    && System.Math.Abs(newPosition.Y) > 0.7071068f)
                {
                    newPosition.X = System.Math.Sign(newPosition.X) * 0.7071068f;
                    newPosition.Y = System.Math.Sign(newPosition.Y) * 0.7071068f;
                }

                mRightStick.Update(newPosition);

                #endregion

                #region Set the trigger positions

                float newAnalogPosition = 0;

                if (mButtonMap.LeftTrigger != Keys.None && InputManager.Keyboard.KeyDown(mButtonMap.LeftTrigger))
                {
                    newAnalogPosition = 1;
                }
                else
                {
                    newAnalogPosition = 0;
                }

                mLeftTrigger.Update(newAnalogPosition);

                if (mButtonMap.RightTrigger != Keys.None && InputManager.Keyboard.KeyDown(mButtonMap.RightTrigger))
                {
                    newAnalogPosition = 1;
                }
                else
                {
                    newAnalogPosition = 0;
                }

                mRightTrigger.Update(newAnalogPosition);

                #endregion

                // Button remapping is used when the methods for push, release, and down are called.
                // Nothing to do here.
            }
        }

        private void UpdateInputManagerBack()
        {
#if WINDOWS_PHONE || MONOGAME
            if (mGamePadState.IsButtonDown(Buttons.Back) && !mLastGamePadState.IsButtonDown(Buttons.Back))
            {
                InputManager.BackPressed = true;
            }
#endif

        }

        /// <summary>
        /// Sets the vibration of the game pad.
        /// </summary>
        /// <param name="leftMotor">The low-frequency motor.  Set between 0.0f and 1.0f</param>
        /// <param name="rightMotor">The high-frequency  motor.  Set between 0.0f and 1.0f</param>
        /// <returns>True if the vibration motors were successfully set; false if the controller
        /// was unable to process the request.
        /// </returns>
        public bool SetVibration(float leftMotor, float rightMotor)
        {


#if IMPLEMENT_INTERNALS
            return Microsoft.Xna.Framework.Input.GamePad.SetVibration(
                mPlayerIndex, leftMotor, rightMotor);
#else
            return false;
#endif
        }

        public override string ToString()
        {

#if MONOGAME_381
            var toReturn = $"{mPlayerIndex} {Capabilities.Identifier} Connected:{IsConnected} LeftStick:{mLeftStick}";
#else
            var toReturn = $"{mPlayerIndex} Connected:{IsConnected} LeftStick:{mLeftStick}";
#endif

            for (int i = 0; i < NumberOfButtons; i++)
            {
                var button = (Button)i;
                if(ButtonDown(button))
                {
                    toReturn += " " + button;
                }
            }

            return toReturn;
        }


#endregion

#region Internal Methods

        internal void Update()
        {
            GamePadState gamepadState;

#if MONOGAME_381
            // Using PlayerIndex gives us only Xbox controllers. Using int indexes gives us all:
            //gamepadState = Microsoft.Xna.Framework.Input.GamePad.GetState(mPlayerIndex, GamePadDeadZone.None);
            gamepadState = Microsoft.Xna.Framework.Input.GamePad.GetState((int)mPlayerIndex, GamePadDeadZone.None);

            if(mCapabilities.DisplayName == null || WasConnectedThisFrame)
            {
                // This can crash internally:
                // System.NullReferenceException: Object reference not set to an instance of an object.
                // at Microsoft.Xna.Framework.Input.GamePad.PlatformGetCapabilities(Int32 index)
                // at Microsoft.Xna.Framework.Input.GamePad.GetCapabilities(Int32 index)
                // We can survive without capabilities so let's tolerate this crash.
                // February 19, 2023
                // Potentially we want to check if this has crashed multiple times? Is this a one-time thing at the beginning
                // or will it repeat? Not sure...
                try
                {
                    mCapabilities = Microsoft.Xna.Framework.Input.GamePad.GetCapabilities((int)mPlayerIndex);
                }
                catch (NullReferenceException) { }
            }
#else
            gamepadState = Microsoft.Xna.Framework.Input.GamePad.GetState(mPlayerIndex, GamePadDeadZone.None);

            mCapabilities = Microsoft.Xna.Framework.Input.GamePad.GetCapabilities(mPlayerIndex);
#endif

            Update(gamepadState);


        }


#endregion

#endregion

#region IInputDevice Explicit Implementation

        I2DInput IInputDevice.Default2DInput
        {
            get
            {
                return this.LeftStick.Or(this.DPad);
            }
        }

        IRepeatPressableInput defaultUpPressable;
        IRepeatPressableInput IInputDevice.DefaultUpPressable
        { 
            get 
            { 
                if(defaultUpPressable == null)
                {
                    defaultUpPressable = GetButton(Button.DPadUp).Or(LeftStick.UpAsButton); 
                }
                return defaultUpPressable;
            } 
        }

        IRepeatPressableInput defaultDownPressable;
        IRepeatPressableInput IInputDevice.DefaultDownPressable
        { 
            get 
            {
                if(defaultDownPressable == null)
                {
                    defaultDownPressable = GetButton(Button.DPadDown).Or(LeftStick.DownAsButton); ;
                }
                return defaultDownPressable;
            } 
        }

        IRepeatPressableInput defaultLeftPressable;
        IRepeatPressableInput IInputDevice.DefaultLeftPressable
        { 
            get 
            { 
                if(defaultLeftPressable == null)
                {
                    defaultLeftPressable = GetButton(Button.DPadLeft).Or(LeftStick.LeftAsButton); 
                }
                return defaultLeftPressable;
            } 
        }

        IRepeatPressableInput defaultRightPressable;
        IRepeatPressableInput IInputDevice.DefaultRightPressable
        { 
            get 
            { 
                if(defaultRightPressable == null)
                {
                    defaultRightPressable = GetButton(Button.DPadRight).Or(LeftStick.RightAsButton); 
                }
                return defaultRightPressable;
            } 
        }

        I1DInput defaultHorizontalInput;
        I1DInput IInputDevice.DefaultHorizontalInput
        {
            get
            {
                if(defaultHorizontalInput == null)
                {
                    defaultHorizontalInput = this.LeftStick.Horizontal.Or
                        (this.DPad.CreateHorizontal());
                }
                return defaultHorizontalInput;
            }
        }

        I1DInput defaultVerticalInput;
        I1DInput IInputDevice.DefaultVerticalInput
        {
            get
            {
                if(defaultVerticalInput == null)
                {
                    defaultVerticalInput = this.LeftStick.Vertical.Or
                        (this.DPad.CreateVertical());
                }
                return defaultVerticalInput;
            }
        }

        public IPressableInput DefaultPrimaryActionInput =>
            ButtonLayout == ButtonLayout.NintendoPro 
            ? GetButton(Button.B)
            : GetButton(Button.A);

        public IPressableInput DefaultSecondaryActionInput => 
            ButtonLayout == ButtonLayout.NintendoPro ? GetButton(Button.Y)
            : ButtonLayout == ButtonLayout.GameCube ? GetButton(Button.B)
            : GetButton(Button.X);

        IPressableInput IInputDevice.DefaultConfirmInput => GetButton(Button.A);

        IPressableInput IInputDevice.DefaultCancelInput => GetButton(Button.B);

        IPressableInput IInputDevice.DefaultJoinInput => GetButton(Button.Start);

        IPressableInput IInputDevice.DefaultPauseInput => GetButton(Button.Start);

        IPressableInput IInputDevice.DefaultBackInput => GetButton(Button.Back);


#endregion

    }
}
