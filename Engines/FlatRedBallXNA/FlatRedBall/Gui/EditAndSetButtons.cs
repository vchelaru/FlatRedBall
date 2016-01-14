using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Gui
{
    #region XML Docs
    /// <summary>
    /// UI element used to give the user an option whether to edit an existing 
    /// instance of an object or to reassign it.
    /// </summary>
    /// <remarks>
    /// This object is usually used in PropertyGrids.
    /// </remarks>
    #endregion
    public class EditAndSetButtons : Window
    {
        #region Fields

        Button mEditButton;
        Button mSetButton;

        #endregion

        #region Events

        public event GuiMessage EditButtonClick;
        public event GuiMessage SetButtonClick;

        #endregion

        #region Event and Delegate Methods

        void OnEditButtonClick(Window callingWindow)
        {
            if (EditButtonClick != null)
            {
                EditButtonClick(this);
            }
        }

        void OnSetButtonClick(Window callingWindow)
        {
            if (SetButtonClick != null)
            {
                SetButtonClick(this);
            }
        }

        #endregion

        #region Methods

        public EditAndSetButtons(Cursor cursor) : base(cursor)
        {
            ScaleY = 1.6f;
            ScaleX = 5.7f;

            mEditButton = new Button(mCursor);
            this.AddWindow(mEditButton);
            mEditButton.ScaleX = 2.5f;
            mEditButton.X = 3f;
            mEditButton.Text = "Edit";
            mEditButton.Click += OnEditButtonClick;



            mSetButton =  new Button(mCursor);
            this.AddWindow(mSetButton);
            mSetButton.ScaleX = 2.5f;
            mSetButton.X = 2*ScaleX - 3f;
            mSetButton.Text = "Set"; 
            mSetButton.Click += OnSetButtonClick;
        }

        #endregion
    }
}
