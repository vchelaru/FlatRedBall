using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Gui.PropertyGrids
{
    #region XML Docs
    /// <summary>
    /// A StructReferencePropertyGrid used by the FlatRedBall Engine when editing ints in a ListDisplayWindow.
    /// </summary>
    #endregion

    public class IntPropertyGrid : StructReferencePropertyGrid<int>
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

        public override int SelectedObject
        {
            get
            {
                return base.SelectedObject;
            }
            set
            {

                    base.SelectedObject = value;
                    if (mUpDown != null && 
                        (GuiManager.Cursor.WindowPushed != mUpDown.UpDownButton)  )
                    {
                        mUpDown.CurrentValue = value;
                    }
            }
        }

        #endregion

        #region Event Methods

        private void ChangeInt(Window callingWindow)
        {
            if (mUpDown != null)
            {
                int outputValue = (int)mUpDown.CurrentValue;
                SelectedObject = outputValue;
                UpdateObject(null);

            }

        }

        private void ChangeIntAndClose(Window callingWindow)
        {
            ChangeInt(callingWindow);

            CloseWindow();
        }

        #endregion

        #region Methods

        public IntPropertyGrid(Cursor cursor, ListDisplayWindow windowOfObject, int indexOfObject)
            : base(cursor, windowOfObject, indexOfObject)
        {
            ExcludeAllMembers();
            MinimumScaleY = 2;
            ScaleY = 2;
            mUpDown = new UpDown(mCursor);
            mUpDown.ScaleX = 5;
            mUpDown.Precision = 0;

            this.AddWindow(mUpDown);

            mUpDown.ValueChanged += ChangeInt;
            
            mUpDown.textBox.EnterPressed += ChangeIntAndClose;


        }

        #endregion
    }
}
