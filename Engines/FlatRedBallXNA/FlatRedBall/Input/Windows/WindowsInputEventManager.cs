
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Runtime.InteropServices;

namespace FlatRedBall.Input.Windows
{
    #region Classes

    public class CharacterEventArgs : EventArgs
    {
        private readonly char character;
        private readonly int lParam;

        public CharacterEventArgs(char character, int lParam)
        {
            this.character = character;
            this.lParam = lParam;
        }

        public char Character
        {
            get { return character; }
        }

        public int Param
        {
            get { return lParam; }
        }

        public int RepeatCount
        {
            get { return lParam & 0xffff; }
        }

        public bool ExtendedKey
        {
            get { return (lParam & (1 << 24)) > 0; }
        }

        public bool AltPressed
        {
            get { return (lParam & (1 << 29)) > 0; }
        }

        public bool PreviousState
        {
            get { return (lParam & (1 << 30)) > 0; }
        }

        public bool TransitionState
        {
            get { return (lParam & (1 << 31)) > 0; }
        }
    }

    public class KeyEventArgs : EventArgs
    {
        private Keys keyCode;

        public KeyEventArgs(Keys keyCode)
        {
            this.keyCode = keyCode;
        }

        public Keys KeyCode
        {
            get { return keyCode; }
        }
    }

    #endregion

    #region Delegate definitions

    public delegate void CharEnteredHandler(object sender, CharacterEventArgs e);
    public delegate void KeyEventHandler(object sender, KeyEventArgs e);

    #endregion

    public class WindowsInputEventManager
    {
        #region Events
        /// <summary>
        /// Event raised when a character has been entered.
        /// </summary>
        public event Action<char> CharEntered;

        /// <summary>
        /// Event raised when a key has been pressed down. May fire multiple times due to keyboard repeat.
        /// </summary>
        public event Action<Keys> KeyDown;

        /// <summary>
        /// Event raised when a key has been released.
        /// </summary>
        public event Action<Keys> KeyUp;

        #endregion

        delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        bool isInitialized;
        IntPtr prevWndProc;
        WndProc hookProcDelegate;
        IntPtr hIMC;

        #region Win32 Constants
        const int GWL_WNDPROC = -4;

        const int WM_KEYDOWN = 0x100;
        const int WM_KEYUP = 0x101;
        const int WM_CHAR = 0x102;
        const int WM_IME_SETCONTEXT = 0x281;
        const int WM_INPUTLANGCHANGE = 0x51;
        const int WM_GETDLGCODE = 0x87;
        const int WM_IME_COMPOSITION = 0x10F;

        const int DLGC_WANTALLKEYS = 4;

        const int WM_MOUSEMOVE = 0x200;

        const int WM_LBUTTONDOWN = 0x201;
        const int WM_LBUTTONUP = 0x202;
        const int WM_LBUTTONDBLCLK = 0x203;

        const int WM_RBUTTONDOWN = 0x204;
        const int WM_RBUTTONUP = 0x205;
        const int WM_RBUTTONDBLCLK = 0x206;

        const int WM_MBUTTONDOWN = 0x207;
        const int WM_MBUTTONUP = 0x208;
        const int WM_MBUTTONDBLCLK = 0x209;

        const int WM_MOUSEWHEEL = 0x20A;

        const int WM_XBUTTONDOWN = 0x20B;
        const int WM_XBUTTONUP = 0x20C;
        const int WM_XBUTTONDBLCLK = 0x20D;

        const int WM_MOUSEHOVER = 0x2A1;
        #endregion

        #region DLL Imports
        [DllImport("Imm32.dll")]
        static extern IntPtr ImmGetContext(IntPtr hWnd);

        [DllImport("Imm32.dll")]
        static extern IntPtr ImmAssociateContext(IntPtr hWnd, IntPtr hIMC);

        [DllImport("user32.dll")]
        static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        #endregion
        
        public bool IsShiftDown
        {
            get
            {
                KeyboardState state = Microsoft.Xna.Framework.Input.Keyboard.GetState();
                return state.IsKeyDown(Keys.LeftShift) || state.IsKeyDown(Keys.RightShift);
            }
        }

        public bool IsCtrlDown
        {
            get
            {
                KeyboardState state = Microsoft.Xna.Framework.Input.Keyboard.GetState();
                return state.IsKeyDown(Keys.LeftControl) || state.IsKeyDown(Keys.RightControl);
            }
        }

        public bool IsAltDown
        {
            get
            {
                KeyboardState state = Microsoft.Xna.Framework.Input.Keyboard.GetState();
                return state.IsKeyDown(Keys.LeftAlt) || state.IsKeyDown(Keys.RightAlt);
            }
        }

        /// <summary>
        /// Initialize the TextInput with the given GameWindow.
        /// </summary>
        /// <param name="window">The XNA window to which text input should be linked.</param>
        public void Initialize(GameWindow window)
        {
            if (isInitialized == false)
            {
                hookProcDelegate = new WndProc(HookProc);
                prevWndProc = (IntPtr)SetWindowLong(window.Handle, GWL_WNDPROC, (int)Marshal.GetFunctionPointerForDelegate(hookProcDelegate));

                hIMC = ImmGetContext(window.Handle);

                isInitialized = true;
            }
        }

        IntPtr HookProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            IntPtr returnCode = CallWindowProc(prevWndProc, hWnd, msg, wParam, lParam);

            switch (msg)
            {
                case WM_GETDLGCODE:
                    returnCode = (IntPtr)(returnCode.ToInt32() | DLGC_WANTALLKEYS);
                    break;

                case WM_KEYDOWN:
                    if (KeyDown != null)
                        KeyDown((Keys)wParam);
                    break;

                case WM_KEYUP:
                    if (KeyUp != null)
                        KeyUp((Keys)wParam);
                    break;

                case WM_CHAR:
                    if (CharEntered != null)
                        CharEntered((char)wParam);
                    break;

                case WM_IME_SETCONTEXT:
                    if (wParam.ToInt32() == 1)
                        ImmAssociateContext(hWnd, hIMC);
                    break;

                case WM_INPUTLANGCHANGE:
                    ImmAssociateContext(hWnd, hIMC);
                    returnCode = (IntPtr)1;
                    break;
                    
            }

            return returnCode;
        }
        
    }
}