using System;
using System.Collections.Generic;
using System.Text;
#if FRB_MDX

using Microsoft.DirectX.Direct3D;
using Direct3D = Microsoft.DirectX.Direct3D;
#else

#endif

using FlatRedBall.Graphics;
using FlatRedBall.ManagedSpriteGroups;
namespace FlatRedBall.Gui
{
    public class ChatWindow : Window
    {
        #region Fields

        ListBox mListBox;
        TextBox mTextBox;

        // This field is not currently used, but may be eventually.
        //int mMaximumNumberOfRows = 100;

        string mLastTextAdded;

        #endregion

        #region Properties

        public string LastTextAdded
        {
            get { return mLastTextAdded; }
        }

        public ListBox ListBox
        {
            get { return mListBox; }
        }

        #endregion

        #region Events

        public event GuiMessage TextAdded;

        #endregion

        #region Event Methods

        void ResizeEvent(Window callingWindow)
        {
            float border = .5f;

            mTextBox.ScaleX = ScaleX - border;
            mTextBox.X = ScaleX;
            mTextBox.Y = 2 * ScaleY - border - mTextBox.ScaleY;

            mListBox.ScaleX = ScaleX - border;
            mListBox.ScaleY = ScaleY - border - mTextBox.ScaleY;
            mListBox.Y = border + mListBox.ScaleY;
            mListBox.X = ScaleX;

        }

        void TextBoxEnter(Window callingWindow)
        {
            AddText(mTextBox.Text);
            mTextBox.Text = "";

            Input.InputManager.ReceivingInput = mTextBox;
        }

        #endregion

        #region Methods

        #region Constructors
        public ChatWindow(Cursor cursor)
            : base(cursor)
		{
            mListBox = new ListBox(mCursor);
            AddWindow(mListBox);

            mTextBox = new TextBox(mCursor);
            AddWindow(mTextBox);
            mTextBox.EnterPressed += TextBoxEnter;

            Resizing += ResizeEvent;



            ScaleX = 5;
            ScaleY = 5;
        }


        #endregion

        public void AddText(string text)
        {
            mLastTextAdded = text;

            // the text needs to be broken up into lines
            text = TextManager.InsertNewLines(text, GuiManager.TextSpacing, mListBox.TextScaleX * 2, TextManager.DefaultFont);
            string[] lines = text.Split('\n');

            foreach (string s in lines)
            {
                mListBox.AddItem(s);
            }

            mListBox.HighlightItem(mListBox.Items[mListBox.mItems.Count - 1], false);
            mListBox.HighlightItem((CollapseItem)null, false);

            if (TextAdded != null)
            {
                TextAdded(this);
            }
        }

        #endregion
    }
}
