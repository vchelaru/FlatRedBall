using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace FlatRedBall.Input
{
    public partial class Keyboard : IInputReceiverKeyboard, IInputDevice
    {
        #region Enums

        public enum KeyAction
        {
            KeyPushed,
            // No need for KeyDown since that's assumed
            KeyReleased,
            KeyTyped
        }

        #endregion

        #region Fields
        FlatRedBall.Input.KeyboardStateProcessor keyboardStateProcessor = new FlatRedBall.Input.KeyboardStateProcessor();

        public const int NumberOfKeys = 255;

        bool[] mKeysTyped;
        double[] mLastTimeKeyTyped;
        bool[] mLastTypedFromPush;

        char[] mKeyToChar;

        bool[] mKeysIgnoredForThisFrame;

        Dictionary<Keys, KeyReference> cachedKeys = new Dictionary<Keys, KeyReference>();

        #endregion

        #region Properties

        public bool AutomaticallyPushEventsToInputReceiver { get; set; } = true;

        public bool IsShiftDown => KeyDown(Keys.LeftShift) || KeyDown(Keys.LeftShift);
        public bool IsCtrlDown => KeyDown(Keys.LeftControl) || KeyDown(Keys.RightControl);
        public bool IsControlDown => IsCtrlDown;
        public bool IsAltDown => KeyDown(Keys.LeftAlt) || KeyDown(Keys.RightAlt);

        List<Keys> keysTypedInternal = new List<Keys>();
        public IReadOnlyCollection<Microsoft.Xna.Framework.Input.Keys> KeysTyped
        {
            get
            {
                keysTypedInternal.Clear();

                for (int i = 0; i < NumberOfKeys; i++)
                {
                    var key = (Keys)i;
                    if (KeyTyped(key))
                    {
                        keysTypedInternal.Add(key);
                    }
                }
                return keysTypedInternal;
            }
        }


        #endregion

        #region Methods

        #region Constructor
#if FRB_MDX
        internal Keyboard(System.Windows.Forms.Control owner)
#else
        internal Keyboard()
#endif
        {

#if SILVERLIGHT
            Microsoft.Xna.Framework.Input.Keyboard.CreatesNewState = false;
#endif

            mKeysTyped = new bool[NumberOfKeys];
            mLastTimeKeyTyped = new double[NumberOfKeys];
            mLastTypedFromPush = new bool[NumberOfKeys];
            mKeysIgnoredForThisFrame = new bool[NumberOfKeys];

            FillKeyCodes();
            
            for (int i = 0; i < NumberOfKeys; i++)
            {
                mLastTimeKeyTyped[i] = 0;
                mKeysTyped[i] = false;
                mLastTypedFromPush[i] = false;

//                textCodes[i] = (char)0;
            }

#if FRB_MDX
			// Create a new Device with the keyboard guid
			mKeyboardDevice = new Device(SystemGuid.Keyboard);

			// Set data format to keyboard data
            mKeyboardDevice.SetDataFormat(DeviceDataFormat.Keyboard);

			// Set the cooperative level to foreground non-exclusive
			// and deactivate windows key
            mKeyboardDevice.SetCooperativeLevel(owner, CooperativeLevelFlags.NonExclusive | 
				CooperativeLevelFlags.Foreground | CooperativeLevelFlags.NoWindowsKey);

			// Try to access keyboard

            try
            {
                mKeyboardDevice.Acquire();
            }
            catch (InputLostException)
            {
                int m = 3;
            }
            catch (OtherApplicationHasPriorityException)
            {
                int m = 3;
            }

            // i don't know why this doesn't work, but input still works without it.

            mKeyboardDevice.Properties.BufferSize = 5;


#endif

        }

        #endregion

        #region Public Methods

        public bool AnyKeyPushed()
        {
            return keyboardStateProcessor.AnyKeyPushed();
        }

        public void Clear()
        {
            keyboardStateProcessor.Clear();
        }


        public void ControlPositionedObject(PositionedObject positionedObject)
        {
            // Use the default velocity for controlling positioned objects.
            ControlPositionedObject(positionedObject, 100);
        }


        public void ControlPositionedObject(PositionedObject positionedObject, float velocity)
        {
            if (KeyDown(Keys.Up))
                positionedObject.Velocity.Y = velocity;
            else if (KeyDown(Keys.Down))
                positionedObject.Velocity.Y = -velocity;
            else
                positionedObject.Velocity.Y = 0;

            if (KeyDown(Keys.Right))
                positionedObject.Velocity.X = velocity;
            else if (KeyDown(Keys.Left))
                positionedObject.Velocity.X = -velocity;
            else
                positionedObject.Velocity.X = 0;

#if FRB_MDX
            // Remember, FRB MDX is left handed, FRB XNA is right handed.
            if (KeyDown(Keys.Minus))
                positionedObject.ZVelocity = -velocity;
            else if (KeyDown(Keys.Equals))
                positionedObject.ZVelocity = velocity;
            else
                positionedObject.ZVelocity = 0;

#else
            if (KeyDown(Keys.OemMinus))
                positionedObject.ZVelocity = velocity;
            else if (KeyDown(Keys.OemPlus))
                positionedObject.ZVelocity = -velocity;
            else
                positionedObject.ZVelocity = 0;
#endif

        }


        public void ControlPositionedObjectAcceleration(PositionedObject positionedObject, float acceleration)
        {
            if (KeyDown(Keys.Up))
                positionedObject.YAcceleration = acceleration;
            else if (KeyDown(Keys.Down))
                positionedObject.YAcceleration = -acceleration;
            else
                positionedObject.YAcceleration = 0;

            if (KeyDown(Keys.Right))
                positionedObject.XAcceleration = acceleration;
            else if (KeyDown(Keys.Left))
                positionedObject.XAcceleration = -acceleration;
            else
                positionedObject.XAcceleration = 0;

#if FRB_MDX
            // Remember, FRB MDX is left handed, FRB XNA is right handed.
            if (KeyDown(Keys.Minus))
                positionedObject.ZVelocity = -acceleration;
            else if (KeyDown(Keys.Equals))
                positionedObject.ZVelocity = acceleration;
            else
                positionedObject.ZVelocity = 0;

#else


            if (KeyDown(Keys.OemMinus))
                positionedObject.ZAcceleration = acceleration;
            else if (KeyDown(Keys.OemPlus))
                positionedObject.ZAcceleration = -acceleration;
            else
                positionedObject.ZAcceleration = 0;
#endif
        }


        public bool ControlCPushed()
        {
            return IsCtrlDown && KeyPushed(Keys.C);
        }

        public bool ControlVPushed()
        {
            return IsCtrlDown && KeyPushed(Keys.V);
        }

        public bool ControlXPushed()
        {
            return IsCtrlDown && KeyPushed(Keys.X);
        }

        public bool ControlZPushed()
        {
            return IsCtrlDown && KeyPushed(Keys.Z);
        }

        public string GetStringTyped()
        { 
            if (InputManager.CurrentFrameInputSuspended)
                return "";

#if ANDROID
            return processedString;
#else

            string returnString = "";

            bool isCtrlPressed = IsCtrlDown;


            for (int i = 0; i < NumberOfKeys; i++)
            {
                if (mKeysTyped[i])
                {
                    // If the user pressed CTRL + some key combination then ignore that input so that 
                    // the letters aren't written.
                    Keys asKey = (Keys)i;


                    if (isCtrlPressed && (asKey == Keys.V || asKey == Keys.C || asKey == Keys.Z || asKey == Keys.A || asKey == Keys.X))
                    {
                        continue;
                    }
                    returnString += KeyToStringAtCurrentState(i);
                }
            }

            #region Add Text if the user presses CTRL+V
            if (
                isCtrlPressed
                && InputManager.Keyboard.KeyPushed(Keys.V)
                )
            {

#if !MONOGAME
                bool isSTAThreadUsed =
                    System.Threading.Thread.CurrentThread.GetApartmentState() == System.Threading.ApartmentState.STA;

#if DEBUG
                if (!isSTAThreadUsed)
                {
                    throw new InvalidOperationException("Need to set [STAThread] on Main to support copy/paste");
                }
#endif

                if (isSTAThreadUsed && System.Windows.Forms.Clipboard.ContainsText())
                {
                    returnString += System.Windows.Forms.Clipboard.GetText();

                }
#endif

            }
            #endregion

            return returnString;
#endif
        }


        public void IgnoreKeyForOneFrame(Keys key)
        {
            mKeysIgnoredForThisFrame[(int)key] = true;
        }


        public bool IsKeyLetter(Keys key)
        {
#if FRB_MDX
            return (key >= Keys.Q && key <= Keys.P) ||
                (key >= Keys.A && key <= Keys.L) ||
                (key >= Keys.Z && key <= Keys.M);
#else
            return key >= Keys.A && key <= Keys.Z;
#endif
        }
        

        public bool KeyDown(Keys key)
        {
            if (mKeysIgnoredForThisFrame[(int)key])
            {
                return false;
            }

			#if ANDROID
			if(KeyDownAndroid(key))
			{
				return KeyDownAndroid(key);
			}
			#endif



            return !InputManager.CurrentFrameInputSuspended && keyboardStateProcessor.IsKeyDown(key);
        }

        /// <summary>
        /// Returns true if the argument key was not down last frame, but is down this frame.
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns>Whether the key was just pushed.</returns>
        public bool KeyPushed(Keys key)
        {
            if (mKeysIgnoredForThisFrame[(int)key] || InputManager.mIgnorePushesThisFrame)
            {
                return false;
            }

#if ANDROID
			if(KeyPushedAndroid(key))
			{
				return true;
			}
#endif


            return !InputManager.CurrentFrameInputSuspended && keyboardStateProcessor.KeyPushed(key);
        }


        public bool KeyPushedConsideringInputReceiver(Keys key)
        {
            return KeyPushed(key) && (InputManager.InputReceiver == null || InputManager.InputReceiver.IgnoredKeys.Contains(key));
        }


        public bool KeyReleased(Keys key)
        {
            if (mKeysIgnoredForThisFrame[(int)key])
            {
                return false;
            }

			#if ANDROID
			if(KeyReleasedAndroid(key))
			{
				return true;
			}
			#endif

            return !InputManager.CurrentFrameInputSuspended && 
                keyboardStateProcessor.KeyReleased(key);
        }

        
        /// <summary>
        /// Returns whether a key was "typed".  A type happens either when the user initially pushes a key down, or when
        /// it gets typed again from holding the key down.  This works similar to how the keyboard types in text editors
        /// when holding down a key.
        /// </summary>
        /// <param name="key">The key to check for.</param>
        /// <returns>Whether the key was typed.</returns>
        public bool KeyTyped(Keys key)
        {
            if (mKeysIgnoredForThisFrame[(int)key])
            {
                return false;
            }

#if ANDROID
            if (KeyPushedAndroid(key))
            {
                return true;
            }
#endif

            return !InputManager.CurrentFrameInputSuspended && mKeysTyped[(int)key];
        }


        /// <summary>
        /// Retrns a KeyReference for the argument key.
        /// </summary>
        /// <param name="key">The key, such as Keys.A.</param>
        /// <returns>The reference, which can be used to check for input.</returns>
        public KeyReference GetKey(Keys key)
		{
            if(cachedKeys.ContainsKey(key) == false)
            {
			    var newReference = new KeyReference ();

			    newReference.Key = key;
                cachedKeys.Add(key, newReference);
            }
            return cachedKeys[key];
		}

        public I1DInput Get1DInput(Keys negative, Keys positive)
        {
            var toReturn = new DirectionalKeyGroup();
            toReturn.LeftKey = negative;
            toReturn.RightKey = positive;

            return toReturn;
        }

        /// <summary>
        /// Returns an instance of I2DInput which can be used to read 2D input using the four
        /// argument keys.
        /// </summary>
        /// <param name="left">The key to use for the left direction.</param>
        /// <param name="right">The key to use for the right direction.</param>
        /// <param name="up">The key to use for the up direction.</param>
        /// <param name="down">The key to use for the down direction.</param>
        /// <returns>The I2DInput instance which can be used to read input.</returns>
        public I2DInput Get2DInput(Keys left, Keys right, Keys up, Keys down)
        {
            var toReturn = new DirectionalKeyGroup();
            toReturn.LeftKey = left;
            toReturn.RightKey = right;
            toReturn.UpKey = up;
            toReturn.DownKey = down;

            return toReturn;
        }

        /// <summary>
        /// Returns an I2DInput using WASD keys.
        /// </summary>
        /// <returns>An input using WASD as an I2DInput object.</returns>
        public I2DInput GetWasdInput() =>
            Get2DInput(Keys.A, Keys.D, Keys.W, Keys.S);

        #endregion

        #region Internal Methods

        internal void Update()
        {
#if ANDROID
			ProcessAndroidKeys();
#endif

            keyboardStateProcessor.Update();

            for (int i = 0; i < NumberOfKeys; i++)
            {
                mKeysIgnoredForThisFrame[i] = false;
                mKeysTyped[i] = false;

                if(KeyPushed((Keys)(i)))
                {
                    mKeysTyped[i] = true;
                    mLastTimeKeyTyped[i] = TimeManager.CurrentTime;
                    mLastTypedFromPush[i] = true;
                }


            }

            const double timeAfterInitialPushForRepeat = .5;
            const double timeBetweenRepeats = .07;           
            
            for (int i = 0; i < NumberOfKeys; i++)
            {


                if (KeyDown((Keys)(i)))
                {
                    if ((mLastTypedFromPush[i] && TimeManager.CurrentTime - mLastTimeKeyTyped[i] > timeAfterInitialPushForRepeat) ||
                        (mLastTypedFromPush[i] == false && TimeManager.CurrentTime - mLastTimeKeyTyped[i] > timeBetweenRepeats)
                      )
                    {
                        mLastTypedFromPush[i] = false;
                        mLastTimeKeyTyped[i] = TimeManager.CurrentTime;
                        mKeysTyped[i] = true;
                    }
                }
            }
        }

        public void PushKeyDownToInputReceiver(Keys key, bool isShiftDown, bool isAltDown, bool isCtrlDown)
        {
            InputManager.InputReceiver?.HandleKeyDown(key, isShiftDown, isAltDown, isCtrlDown);
        }

        public void PushCharEnteredToInputReceiver(char character)
        {
            InputManager.InputReceiver?.HandleCharEntered(character);
        }

        #endregion

        #region Private Methods

        private void FillKeyCodes()
        {
            mKeyToChar = new char[NumberOfKeys];

            for (int i = 0; i < NumberOfKeys; i++)
            {
                mKeyToChar[i] = (char)0;
            }

            mKeyToChar[(int)Keys.A] = 'a';
            mKeyToChar[(int)Keys.B] = 'b';
            mKeyToChar[(int)Keys.C] = 'c';
            mKeyToChar[(int)Keys.D] = 'd';
            mKeyToChar[(int)Keys.E] = 'e';
            mKeyToChar[(int)Keys.F] = 'f';
            mKeyToChar[(int)Keys.G] = 'g';
            mKeyToChar[(int)Keys.H] = 'h';
            mKeyToChar[(int)Keys.I] = 'i';
            mKeyToChar[(int)Keys.J] = 'j';
            mKeyToChar[(int)Keys.K] = 'k';
            mKeyToChar[(int)Keys.L] = 'l';
            mKeyToChar[(int)Keys.M] = 'm';
            mKeyToChar[(int)Keys.N] = 'n';
            mKeyToChar[(int)Keys.O] = 'o';
            mKeyToChar[(int)Keys.P] = 'p';
            mKeyToChar[(int)Keys.Q] = 'q';
            mKeyToChar[(int)Keys.R] = 'r';
            mKeyToChar[(int)Keys.S] = 's';
            mKeyToChar[(int)Keys.T] = 't';
            mKeyToChar[(int)Keys.U] = 'u';
            mKeyToChar[(int)Keys.V] = 'v';
            mKeyToChar[(int)Keys.W] = 'w';
            mKeyToChar[(int)Keys.X] = 'x';
            mKeyToChar[(int)Keys.Y] = 'y';
            mKeyToChar[(int)Keys.Z] = 'z';

            mKeyToChar[(int)Keys.D1] = '1';
            mKeyToChar[(int)Keys.D2] = '2';
            mKeyToChar[(int)Keys.D3] = '3';
            mKeyToChar[(int)Keys.D4] = '4';
            mKeyToChar[(int)Keys.D5] = '5';
            mKeyToChar[(int)Keys.D6] = '6';
            mKeyToChar[(int)Keys.D7] = '7';
            mKeyToChar[(int)Keys.D8] = '8';
            mKeyToChar[(int)Keys.D9] = '9';
            mKeyToChar[(int)Keys.D0] = '0';

            mKeyToChar[(int)Keys.NumPad1] = '1';
            mKeyToChar[(int)Keys.NumPad2] = '2';
            mKeyToChar[(int)Keys.NumPad3] = '3';
            mKeyToChar[(int)Keys.NumPad4] = '4';
            mKeyToChar[(int)Keys.NumPad5] = '5';
            mKeyToChar[(int)Keys.NumPad6] = '6';
            mKeyToChar[(int)Keys.NumPad7] = '7';
            mKeyToChar[(int)Keys.NumPad8] = '8';
            mKeyToChar[(int)Keys.NumPad9] = '9';
            mKeyToChar[(int)Keys.NumPad0] = '0';

            mKeyToChar[(int)Keys.Decimal] = '.';

            mKeyToChar[(int)Keys.Space] = ' ';
            mKeyToChar[(int)Keys.Enter] = '\n';


            mKeyToChar[(int)Keys.Subtract] = '-';
            mKeyToChar[(int)Keys.Add] = '+';
            mKeyToChar[(int)Keys.Divide] = '/';
            mKeyToChar[(int)Keys.Multiply] = '*';

            mKeyToChar[(int)Keys.OemTilde] = '`';
            mKeyToChar[(int)Keys.OemSemicolon] = ';';
            mKeyToChar[(int)Keys.OemQuotes] = '\'';
            mKeyToChar[(int)Keys.OemQuestion] = '/';
            mKeyToChar[(int)Keys.OemPlus] = '=';
            mKeyToChar[(int)Keys.OemPipe] = '\\';
            mKeyToChar[(int)Keys.OemPeriod] = '.';
            mKeyToChar[(int)Keys.OemOpenBrackets] = '[';
            mKeyToChar[(int)Keys.OemCloseBrackets] = ']';
            mKeyToChar[(int)Keys.OemMinus] = '-';
            mKeyToChar[(int)Keys.OemComma] = ',';


        }

        private string KeyToStringAtCurrentState(int key)
        {
            bool isShiftDown = KeyDown(Keys.LeftShift) || KeyDown(Keys.RightShift);

#if !MONOGAME
            if (System.Windows.Forms.Control.IsKeyLocked(System.Windows.Forms.Keys.CapsLock))
            {
                isShiftDown = !isShiftDown;
            }
#endif

            #region If Shift is down, return a different key
            if (isShiftDown && IsKeyLetter((Keys)key))
            {
                return ((char)(mKeyToChar[key] - 32)).ToString();
            }

            else
            {
                if (KeyDown(Keys.LeftShift) || KeyDown(Keys.RightShift))
                {
                    switch ((Keys)key)
                    {
                        case Keys.D1: return "!";
                        case Keys.D2: return "@";
                        case Keys.D3: return "#";
                        case Keys.D4: return "$";
                        case Keys.D5: return "%";
                        case Keys.D6: return "^";
                        case Keys.D7: return "&";
                        case Keys.D8: return "*";
                        case Keys.D9: return "(";
                        case Keys.D0: return ")";

                        case Keys.OemTilde: return "~";
                        case Keys.OemSemicolon: return ":";
                        case Keys.OemQuotes: return "\"";
                        case Keys.OemQuestion: return "?";
                        case Keys.OemPlus: return "+";
                        case Keys.OemPipe: return "|";
                        case Keys.OemPeriod: return ">";
                        case Keys.OemOpenBrackets: return "{";
                        case Keys.OemCloseBrackets: return "}";
                        case Keys.OemMinus: return "_";
                        case Keys.OemComma: return "<";
                        case Keys.Space: return " ";
                        default: return "";
                    }
                }
                else if (mKeyToChar[key] != (char)0)
                {
                    return mKeyToChar[key].ToString();
                }
                else
                {
                    return "";
                }            
            }

            #endregion

        }

        #endregion

        #endregion

        #region IInputDevice Explicit Implementation

        I2DInput IInputDevice.Default2DInput
        {
            get
            {
                return this.Get2DInput(Keys.A, Keys.D, Keys.W, Keys.S);
            }
        }

        IPressableInput IInputDevice.DefaultUpPressable => GetKey(Keys.W); 
        IPressableInput IInputDevice.DefaultDownPressable => GetKey(Keys.S); 
        IPressableInput IInputDevice.DefaultLeftPressable => GetKey(Keys.A); 
        IPressableInput IInputDevice.DefaultRightPressable => GetKey(Keys.D); 

        I1DInput IInputDevice.DefaultHorizontalInput => Get1DInput(Keys.A, Keys.D);

        I1DInput IInputDevice.DefaultVerticalInput => Get1DInput(Keys.S, Keys.W);

        IPressableInput IInputDevice.DefaultPrimaryActionInput => GetKey(Keys.Space);

        IPressableInput IInputDevice.DefaultSecondaryActionInput => GetKey(Keys.LeftShift);

        IPressableInput IInputDevice.DefaultConfirmInput => GetKey(Keys.Enter);
        IPressableInput IInputDevice.DefaultCancelInput => GetKey(Keys.Escape);

        IPressableInput IInputDevice.DefaultJoinInput => GetKey(Keys.Enter);

        IPressableInput IInputDevice.DefaultPauseInput => GetKey(Keys.Escape);

        IPressableInput IInputDevice.DefaultBackInput => GetKey(Keys.Escape);


        #endregion
    }
}
