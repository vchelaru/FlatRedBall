using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;

using FlatRedBall.Graphics;
using FlatRedBall.Gui;

namespace EditorObjects.Gui
{
    public class ComboBoxWindow : Window
    {
        #region Fields

        TextField mTextField;
        ComboBox mComboBox;

        Button mCancelButton;
        Button mOkButton;

        #endregion

        #region Properties

        public string Message
        {
            get { return mTextField.DisplayText; }
            set { mTextField.DisplayText = value; }
        }

        public object SelectedObject
        {
            get { return mComboBox.SelectedObject; }
        }

        public string Text
        {
            get { return mComboBox.Text; }
        }

        #endregion

        #region Events

        public event GuiMessage OkClick = null;

        #endregion

        #region Delegate Methods

        void OnOkButtonClick(Window callingWindow)
        {
            if (OkClick != null)
                OkClick(this);

            GuiManager.RemoveWindow(this);
        }



        #endregion

        #region Methods

        public ComboBoxWindow()
            : base(GuiManager.Cursor)
        {
            GuiManager.AddWindow(this);
            ScaleX = 12;
            ScaleY = 9;
            this.Closing += GuiManager.RemoveWindow;
            
            mTextField = new TextField();
            mTextField.SetDimensions(-2, -8, 1, 23, 0);
            mTextField.Z = 100;
            mTextField.WindowParent = this;

            mComboBox = new ComboBox(mCursor);
            AddWindow(mComboBox);
            mComboBox.ScaleX = ScaleX - 2;
            mComboBox.ScaleY = 1.4f;
            mComboBox.SetPositionTL(ScaleX, 2 * ScaleY - 5);

            mCancelButton = new Button(mCursor);
            AddWindow(mCancelButton);
            mCancelButton.ScaleY = 1.5f;
            mCancelButton.Text = "Cancel";
            mCancelButton.SetPositionTL(2 * ScaleX - 5.2f, 2 * ScaleY - 2);
            mCancelButton.Click += GuiManager.RemoveWindow;

            mOkButton = new Button(mCursor);
            AddWindow(mOkButton);
            mOkButton.ScaleY = 1.5f;
            mOkButton.Text = "Ok";
            mOkButton.SetPositionTL(5.2f, 2 * ScaleY - 2);
        }

        public void AddItem(string itemText, object referenceObject)
        {
            mComboBox.AddItem(itemText, referenceObject);

            
        }

        #endregion
    }
}
