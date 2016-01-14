using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall.Graphics;

namespace EditorObjects.Gui
{
    public class OkListWindow : Window
    {
        #region Fields

        TextDisplay mMessageTextDisplay;
        Button mOkButton;
        ListBox mListBox;

        #endregion

        #region Events

        public event GuiMessage OkButtonClick;

        #endregion

        #region Event Methods

        private void OkButtonClicked(Window callingWindow)
        {
            callingWindow.Parent.Visible = false;

            if (OkButtonClick != null)
            {
                OkButtonClick(this);
            }
        }

        private void ShowSelectedItemInNewWindow(Window callingWindow)
        {
            string messageToShow = ((ListBox)callingWindow).GetFirstHighlightedItem().Text;

            GuiManager.ShowMessageBox(messageToShow, "");
        }

        private void WindowResize(Window callingWindow)
        {
            mMessageTextDisplay.Text = mMessageTextDisplay.Text.Replace("\n", "");
            mMessageTextDisplay.Wrap(2 * ScaleX - 2f);
            mMessageTextDisplay.X = 1;

            mListBox.ScaleX = ScaleX - 1;
            mListBox.ScaleY = ScaleY - 6;
            mListBox.X = ScaleX;
            mListBox.Y = ScaleY + 2;

            mOkButton.X = ScaleX * 2 - 1 - mOkButton.ScaleX;
            mOkButton.Y = ScaleY * 2 - 1 - mOkButton.ScaleY;
        }

        #endregion

        #region Methods

        #region Constructor

        public OkListWindow(string message, string title)
            : base(GuiManager.Cursor)
        {
            mName = title;
            ScaleX = 13;
            ScaleY = 15;
            HasMoveBar = true;
            HasCloseButton = true;
            MinimumScaleX = 9;
            MinimumScaleY = 10;

            mOkButton = new Button(mCursor);
            AddWindow(mOkButton);
            mOkButton.ScaleX = 1.7f;
			mOkButton.ScaleY = 1.2f;
            mOkButton.Text = "Ok";
			mOkButton.Click += new GuiMessage(OkButtonClicked);

            mMessageTextDisplay = new TextDisplay(mCursor);
            AddWindow(mMessageTextDisplay);
            mMessageTextDisplay.Text = message;
            mMessageTextDisplay.X = 1;
            mMessageTextDisplay.Y = 1.4f;

            mListBox = new ListBox(mCursor);
            AddWindow(mListBox);
            mListBox.StrongSelect += ShowSelectedItemInNewWindow;

            this.Resizable = true;
            Resizing += WindowResize;

            WindowResize(this);

            GuiManager.AddDominantWindow(this);
        }

        #endregion

        #region Public Methods

        public CollapseItem AddItem(string itemText)
        {
            return mListBox.AddItem(itemText);
        }

        public CollapseItem AddItem(string itemText, object referenceObject)
        {
            return mListBox.AddItem(itemText, referenceObject);
        }

        public object GetFirstHighlightedObject()
        {
            return mListBox.GetFirstHighlightedObject();
        }

        #endregion

        #endregion

    }
}
