using FlatRedBall.Gui;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Security;
using System.Text;

namespace FlatRedBall.Forms.Controls
{
    public class PasswordBox : TextBoxBase
    {
        #region Fields/Properties

#if !UWP
        SecureString securePassword = new SecureString();
        public SecureString SecurePassword
        {
            get { return securePassword; }
        }
        public string Password
        {
            get
            {
                return SecureStringToString(SecurePassword);

            }
            set
            {
                SecurePassword.Clear();
                if (value != null)
                {
                    foreach (var character in value)
                    {
                        SecurePassword.AppendChar(character);
                    }
                }

                UpdateDisplayedCharacters();

                PasswordChanged?.Invoke(this, null);
            }
        }

        String SecureStringToString(SecureString value)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = System.Runtime.InteropServices.Marshal.SecureStringToGlobalAllocUnicode(value);
                return System.Runtime.InteropServices.Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }
#else
        string password;
        public string Password
        {
            get
            {
                return password;
            }
            set
            {
                if(password != value)
                {
                    password = value;

                    UpdateDisplayedCharacters();

                    PasswordChanged?.Invoke(this, null);
                }
            }
        }
#endif


        // Update Gum's default to include this first:
        //public char PasswordChar { get; set; } = '●';
        public char PasswordChar { get; set; } = '*';

        public event EventHandler PasswordChanged;

        protected override string DisplayedText
        {
            get
            {
#if UWP
                return new string(PasswordChar, Password?.Length ?? 0);
#else
                return new string(PasswordChar, SecurePassword.Length);
#endif
            }
        }

        #endregion

        #region Initialize Methods

        public PasswordBox() : base() { }

        public PasswordBox(GraphicalUiElement visual) : base(visual) { }

        protected override void ReactToVisualChanged()
        {
            base.ReactToVisualChanged();



            UpdateDisplayedCharacters();
        }
        #endregion

        #region Event Handler Methods

        public override void HandleCharEntered(char character)
        {
            if (HasFocus)
            {
                // If text is null force it to be an empty string so we can add characters

                if (character == '\b'
                    // I think CTRL Backspace?
                    || character == (char)127
                    // esc
                    || character == (char)27)
                {
                    // do nothing, handled with a backspace above
                    //    HandleBackspace();
                }
                else if (character == '\r')
                {
                    // no enter supported on passwords, do we send an event?
                }
                else
                {
#if UWP
                    if(password == null)
                    {
                        password = "";
                    }
                    password = this.password.Insert(CaretIndex, character.ToString());
#else
                    this.SecurePassword.InsertAt(CaretIndex, character);

#endif
                    caretIndex++;

                    UpdateCaretPositionToCaretIndex();
                    OffsetTextToKeepCaretInView();
                    UpdateDisplayedCharacters();
                    PasswordChanged?.Invoke(this, null);
                }

            }
        }

        public override void HandleBackspace(bool isCtrlDown = false)
        {
            if (HasFocus && caretIndex > 0)
            {
                if (isCtrlDown)
                {
                    for (int i = caretIndex - 1; i > -1; i--)
                    {
#if UWP
                        password = password.Remove(i);
#else
                        SecurePassword.RemoveAt(i);
#endif
                    }

                    caretIndex = 0;
                }
                else
                {
                    var whereToRemoveFrom = caretIndex - 1;
                    // Move the care to the left one before removing from the text. Otherwise, if the
                    // caret is at the end of the word, modifying the word will shift the caret to the left, 
                    // and that could cause it to shift over two times.
                    caretIndex--;
#if UWP
                    password = password.Remove(whereToRemoveFrom);
#else
                    SecurePassword.RemoveAt(whereToRemoveFrom);
#endif
                }
                UpdateDisplayedCharacters();
                PasswordChanged?.Invoke(this, null);
            }
        }

        protected override void HandleDelete()
        {
#if UWP
            if (caretIndex < (password?.Length ?? 0))
            {
                password = password.Remove(caretIndex);

                UpdateDisplayedCharacters();
                PasswordChanged?.Invoke(this, null);
            }
#else
            if (caretIndex < (SecurePassword?.Length ?? 0))
            {
                SecurePassword.RemoveAt(caretIndex);

                UpdateDisplayedCharacters();
                PasswordChanged?.Invoke(this, null);
            }
#endif
        }

        public void Clear()
        {
#if UWP
            password = null;
#else
            SecurePassword.Clear();
#endif
            UpdateDisplayedCharacters();
            PasswordChanged?.Invoke(this, null);
        }


        private void UpdateDisplayedCharacters()
        {
#if UWP
            var newText = new string(PasswordChar, password?.Length ?? 0);
#else
            var newText = new string(PasswordChar, SecurePassword.Length);
#endif
            if (this.coreTextObject.RawText != newText)
            {
                textComponent.SetProperty("Text", newText);

                CaretIndex = System.Math.Min(CaretIndex, Password?.Length ?? 0);

            }
        }

        #endregion
    }
}
