using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Gui
{
    #region XML Docs
    /// <summary>
    /// UI element which stretches across the bottom of the screen.  This element
    /// automatically resizes horiontally to fill the entire bottom of the screen.
    /// </summary>
    #endregion
    public class InfoBar : Window
    {
        #region Fields

        public const float InfoBarHeight = MenuStrip.MenuStripHeight;

        TextDisplay mTextDisplay;

        #endregion        
        
        #region Properties

        public string Text
        {
            get { return mTextDisplay.Text; }
            set { mTextDisplay.Text = value; }
        }

        #endregion

        #region Methods


        public InfoBar(Cursor cursor)
            : base(cursor)
        {
            ScaleX = GuiManager.Camera.XEdge;
            ScaleY = MenuStrip.MenuStripHeight / 2.0f;

            X = ScaleX - .001f;
            Y = 2 * GuiManager.Camera.YEdge - ScaleY;
            Y += .001f;

            mTextDisplay = new TextDisplay(mCursor);
            AddWindow(mTextDisplay);
            mTextDisplay.X = 1;

            mTextDisplay.Text = "";

        }

        #endregion
    }
}
