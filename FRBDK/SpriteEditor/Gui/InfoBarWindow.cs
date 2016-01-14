using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall;

namespace SpriteEditor.Gui
{
    public class InfoBarWindow : InfoBar
    {
        #region Fields

        TextDisplay mCursorTextDisplay;
        double mTimeLastSave = 0;

        #endregion

        #region Properties

        #endregion

        #region Methods

        #region Constructor

        public InfoBarWindow(Cursor cursor)
            : base(cursor)
        {
            mCursorTextDisplay = new TextDisplay(mCursor);
            AddWindow(mCursorTextDisplay);
            mCursorTextDisplay.X = 1;
            mTimeLastSave = TimeManager.CurrentTime;
        }

        #endregion

        #region Public Methods

        public void Activity()
        {
            int secondsFromLastSave = (int)(TimeManager.CurrentTime - mTimeLastSave);
            if (secondsFromLastSave % SAVE_UPDATE_INC == 0)
                mCursorTextDisplay.Text = SAVE_TEXT + secondsFromLastSave.ToString();
        }

        const string SAVE_TEXT = "Save elapse: ";
        const int SAVE_UPDATE_INC = 5;

        public void ResetSaveTime()
        {
            mTimeLastSave = TimeManager.CurrentTime;
        }

        #endregion

        #endregion
    }
}
