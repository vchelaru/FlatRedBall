using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Graphics;

namespace FlatRedBall.Gui
{
    #region XML Docs
    /// <summary>
    /// Window which can be used to read a user name and password.  The password text box
    /// displays ****** instead of the actual passwords.
    /// </summary>
    #endregion
    public class UserNamePasswordWindow 
#if !SILVERLIGHT
        : PropertyGrid<UserNamePasswordWindow.UsernamePassword>
#endif
    {
        #region Classes

        public class UsernamePassword
        {
            public string UserName;
            public string Password;
        }

        #endregion

        #region Fields

        #endregion

        #region Properties

        public string Password
        {
            get { return SelectedObject.Password; }
            set { SelectedObject.Password = value; }
        }

        public string UserName
        {
            get { return SelectedObject.UserName; }
            set { SelectedObject.UserName = value; }
        }

        #endregion

        #region Events

        public event GuiMessage OkClick;

        #endregion

        #region Event Methods

        void OnOkClick(Window callingWindow)
        {
            this.CloseWindow();

            if (OkClick != null)
            {
                OkClick(this);
            }
        }

        void OnCancelClick(Window callingWindow)
        {
            this.CloseWindow();
        }

        #endregion

        #region Methods

        public UserNamePasswordWindow(Cursor cursor) : base(cursor)
        {
            SelectedObject = new UsernamePassword();

            #region Create the OK button

            Button okButton = new Button(mCursor);

            okButton.Text = "Ok";
            okButton.Click += OnOkClick;
            okButton.ScaleX = 5;

            this.AddWindow(okButton);

            #endregion

            #region Create the cancel button

            Button cancelButton = new Button(mCursor);

            cancelButton.Text = "Cancel";
            cancelButton.Click += OnCancelClick;
            cancelButton.ScaleX = 5;

            this.AddWindow(cancelButton);

            #endregion

            GetUIElementForMember("UserName").ScaleX = 20;

            #region Adjust the Password textbox

            TextBox asTextBox = GetUIElementForMember("Password") as TextBox;
            asTextBox.ScaleX = 20;
            asTextBox.HideCharacters = true;

            #endregion

        }

        #endregion

    }
}
