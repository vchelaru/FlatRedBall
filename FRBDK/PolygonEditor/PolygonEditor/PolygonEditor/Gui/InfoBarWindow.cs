using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;

namespace PolygonEditor.Gui
{
    public class InfoBarWindow : InfoBar
    {
        #region Fields

        TextDisplay mCursorTextDisplay;

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
        }

        #endregion

        #region Public Methods

        public void Activity()
        {
            mCursorTextDisplay.Text = string.Format("Cursor: ({0}, {1})",
                GuiManager.Cursor.WorldXAt(0), GuiManager.Cursor.WorldYAt(0));
        }

        #endregion

        #endregion
    }
}
