using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Gui.PropertyGrids
{
    #region XML Docs
    /// <summary>
    /// A StructReferencePropertyGrid used by the FlatRedBall Engine when editing doubles in a ListDisplayWindow.
    /// </summary>
    #endregion
    public class DoublePropertyGrid : StructReferencePropertyGrid<double>
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

        public override double SelectedObject
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
                    mUpDown.CurrentValue = (float)value;
                }
            }
        }

        #endregion

        #region Event Methods

        private void ChangeDouble(Window callingWindow)
        {
            if (mUpDown != null)
            {
                double outputValue = (double)mUpDown.CurrentValue;
                SelectedObject = outputValue;
                UpdateObject(null);
            }
        }

        private void ChangeDoubleAndClose(Window callingWindow)
        {
            ChangeDouble(callingWindow);

            CloseWindow();
        }

        #endregion

        #region Methods

        public DoublePropertyGrid(Cursor cursor, ListDisplayWindow windowOfObject, int indexOfObject)
            : base(cursor, windowOfObject, indexOfObject)
        {
            ExcludeAllMembers();
            MinimumScaleY = 2;
            ScaleY = 2;
            mUpDown = new UpDown(mCursor);
            mUpDown.ScaleX = 5;
            mUpDown.Precision = 15;

            this.AddWindow(mUpDown);

            mUpDown.ValueChanged += ChangeDouble;

            mUpDown.textBox.EnterPressed += ChangeDoubleAndClose;
        }

        #endregion
    }
}
