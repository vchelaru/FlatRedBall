using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Gui.PropertyGrids
{
    #region XML Docs
    /// <summary>
    /// A StructReferencePropertyGrid used by the FlatRedBall Engine when editing longs in a ListDisplayWindow.
    /// </summary>
    #endregion
    public class LongPropertyGrid : StructReferencePropertyGrid<long>
    {
        #region Fields

        UpDown mUpDown;

        bool mCloseOnEnter = true;

        #endregion

        #region Properties

        public bool CloseOnEnter
        {
            get { return mCloseOnEnter; }
            set { mCloseOnEnter = value; }
        }

        public override long SelectedObject
        {
            get
            {
                return base.SelectedObject;
            }
            set
            {
                base.SelectedObject = value;
                if (mUpDown != null &&
                    (GuiManager.Cursor.WindowPushed != mUpDown.UpDownButton))
                {
                    mUpDown.CurrentValue = value;
                }
            }
        }

        #endregion

        #region Event Methods

        private void ChangeLong(Window callingWindow)
        {
            if (mUpDown != null)
            {
                long outputValue = (long)mUpDown.CurrentValue;
                SelectedObject = outputValue;
                UpdateObject(null);
            }
        }

        private void ChangeLongAndClose(Window callingWindow)
        {
            ChangeLong(callingWindow);

            CloseWindow();
        }

        #endregion

        #region Methods

        public LongPropertyGrid(Cursor cursor, ListDisplayWindow windowOfObject, int indexOfObject)
            : base(cursor, windowOfObject, indexOfObject)
        {
            ExcludeAllMembers();
            MinimumScaleY = 2;
            ScaleY = 2;
            mUpDown = new UpDown(mCursor);
            mUpDown.ScaleX = 5;
            mUpDown.Precision = 0;

            this.AddWindow(mUpDown);

            mUpDown.ValueChanged += ChangeLong;

            mUpDown.textBox.EnterPressed += ChangeLongAndClose;
        }

        #endregion
    }
}
