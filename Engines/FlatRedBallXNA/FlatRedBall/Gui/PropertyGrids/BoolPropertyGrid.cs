using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Gui.PropertyGrids
{
    #region XML Docs
    /// <summary>
    /// A StructReferencePropertyGrid used by the FlatRedBall Engine when editing bools in a ListDisplayWindow.
    /// </summary>
    #endregion
    public class BoolPropertyGrid : StructReferencePropertyGrid<bool>
    {
        #region Fields

        ComboBox mTrueFalse;

        bool mCloseOnEnter = true;

        #endregion

        #region Properties

        public bool CloseOnEnter
        {
            get { return mCloseOnEnter; }
            set { mCloseOnEnter = value; }
        }

        #endregion

        #region Event Methods

        private void ChangeBool(Window callingWindow)
        {
            if (mTrueFalse != null)
            {
                mSelectedObject = (bool)(callingWindow as ComboBox).SelectedObject;
            }
            UpdateObject(callingWindow);

            this.CloseWindow();
        }

        #endregion

        #region Methods

        public BoolPropertyGrid(Cursor cursor, ListDisplayWindow windowOfObject, int indexOfObject)
            : base(cursor, windowOfObject, indexOfObject)
        {
            ExcludeAllMembers();
            MinimumScaleY = 2;
            ScaleY = 2;
            mTrueFalse = new ComboBox(mCursor);
            mTrueFalse.ScaleX = 5;

            mTrueFalse.AddItem("True", true);
            mTrueFalse.AddItem("False", false);

            this.AddWindow(mTrueFalse);

            mTrueFalse.SelectedObject = windowOfObject.Items[indexOfObject];
            mTrueFalse.Text = windowOfObject.Items[indexOfObject].ReferenceObject.ToString();

            mTrueFalse.ItemClick += ChangeBool;
            
        }

        #endregion
    }
}